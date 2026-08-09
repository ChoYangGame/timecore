using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 시간 경과에 따라 난이도를 올리고, 6웨이브(=waveDuration*5초 경과)에 보스를 등장시킨다.
/// GameManager와는 별도 컴포넌트. 부착 대상: WaveManager (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class WaveManager : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Boss bossPrefab;
    [SerializeField] private BossHpUI bossHpUI;
    [SerializeField] private BossBannerUI bossBanner;
    [Tooltip("증강 카드가 배너보다 우선순위가 높다 — 카드 표시 중이면 배너를 대기시킨다")]
    [SerializeField] private AugmentManager augmentManager;

    [SerializeField] private float waveDuration = 25f;
    [SerializeField] private int bossWave = 6;
    [SerializeField] private float spawnIntervalMultiplier = 0.9f;
    [SerializeField] private float minSpawnInterval = 0.4f;
    [Tooltip("웨이브 1당 적 최대체력 가산 비율. 예: 0.15 = +15%")]
    [SerializeField] private float enemyHealthStepBonus = 0.15f;

#if UNITY_EDITOR
    [Tooltip("테스트용: 이 키를 누르면 웨이브 진행과 무관하게 즉시 보스를 소환한다. 빌드에서는 컴파일되지 않는다.")]
    [SerializeField] private Key debugBossSpawnKey = Key.B;
#endif

    [Tooltip("보스 이름표 텍스트. EraManager.ConfigureBoss()가 시대 전환 시 갱신한다.")]
    [SerializeField] private string bossName = "고대의 포식자";

    [Tooltip("보스 스프라이트 색상. EraManager.ConfigureBoss()가 시대 전환 시 갱신한다.")]
    [SerializeField] private Color bossColor = new Color(0.362f, 0.108f, 0.097f, 1f);

    [Tooltip("보스 최대 체력 배율. EraManager가 시대 전환 시 갱신한다 (프리팹 HP는 그대로 두고 시대별로만 곱한다).")]
    [SerializeField] private float bossHpMultiplier = 1f;

    [Tooltip("보스 패턴 세트 인덱스(= EraManager.Era enum 값). EraManager가 시대 전환 시 갱신한다.")]
    [SerializeField] private int bossEraIndex;

    [Tooltip("적 최대 체력 배율. EraManager가 시대 전환 시 갱신한다. 웨이브 가산과 곱해진다.")]
    [SerializeField] private float enemyHpMultiplier = 1f;

    [Tooltip("적 스프라이트 색상. EraManager가 시대 전환 시 갱신한다 (프리팹을 늘리지 않고 시대를 구분한다).")]
    [SerializeField] private Color enemyColor = new Color(0.659f, 0.196f, 0.176f, 1f);

    // 시대별 컬러 아트. EraManager가 전환 때마다 넣어 준다.
    // [SerializeField]를 붙이지 않은 이유: 인스펙터에서 채울 값이 아니라 EraConfig에서 흘러오는 값이고,
    // 직렬화하면 씬에 빈 슬롯 두 개가 더 생겨 EraConfig 쪽과 어느 쪽이 진짜인지 헷갈린다.
    private Sprite bossSprite;
    private Sprite enemySprite;
    private Sprite[] enemyWalkFrames;
    private bool enemyArtFacesLeft;

    // 시대별 적 이동속도 배율. 스폰 직후 1회만 곱한다 (Update에서 매 프레임 곱하면 공짜가 아니다).
    private float enemySpeedMultiplier = 1f;

    public int CurrentWave { get; private set; } = 1;

    /// <summary>
    /// 보스가 살아있는 동안 true. 기믹 스포너들이 "보스전 모드"로 도는 신호다.
    /// _bossSpawned와 따로 두는 이유: _bossSpawned는 시대 전환까지 true로 남아 있어서,
    /// 보스를 잡고 모래시계를 주우러 걸어가는 동안에도 레이저가 계속 깔린다.
    /// </summary>
    public bool BossActive { get; private set; }

    /// <summary>보스 처치 시 발행. EraManager가 구독해 다음 시대 전환(또는 게임 클리어)을 처리한다.</summary>
    public event Action OnBossDefeated;

    [Header("웨이브 전환 연출")]
    [Tooltip("웨이브 배너 색. 보스 배너와 확실히 갈려야 무슨 일이 일어났는지 읽힌다")]
    [SerializeField] private Color waveBannerColor = new Color(0.435f, 0.847f, 0.878f, 1f);
    [SerializeField] private float waveBannerSize = 44f;
    [SerializeField] private float waveBannerHold = 0.9f;

    [Tooltip("보스 배너는 크고 붉고 오래 남는다 + 화면 섬광·흔들림이 붙는다")]
    [SerializeField] private Color bossBannerColor = new Color(0.941f, 0.463f, 0.290f, 1f);
    [SerializeField] private float bossBannerSize = 64f;
    [SerializeField] private float bossBannerHold = 1.8f;

    private float _waveTimer;
    private bool _bossSpawned;
    private string _pendingBannerText;
    private Color _pendingBannerColor;
    private float _pendingBannerSize;
    private float _pendingBannerHold;
    private float _initialSpawnInterval;

    // 이번 웨이브에서 잡은 수 = 전체 처치 - 웨이브 시작 시점의 전체 처치.
    // 별도 이벤트를 걸지 않고 스냅샷 차이로 낸다.
    private int _killsAtWaveStart;

    /// <summary>이번 웨이브에서 잡은 적 수. HUD가 읽는다.</summary>
    public int KillsThisWave
    {
        get
        {
            GameManager gm = GameManager.Instance;
            return gm == null ? 0 : Mathf.Max(0, gm.KillCount - _killsAtWaveStart);
        }
    }

    /// <summary>다음 웨이브까지 남은 시간(초). 보스전 동안에는 0이다.</summary>
    public float TimeToNextWave => _bossSpawned ? 0f : Mathf.Max(0f, waveDuration - _waveTimer);

    /// <summary>보스가 나오는 웨이브. HUD가 "5웨이브에 보스"를 보여줄 때 쓴다.</summary>
    public int BossWave => bossWave;

    /// <summary>웨이브 1회 길이(초). HUD가 진행도 막대를 채울 때 쓴다.</summary>
    public float WaveDuration => waveDuration;

    private void Awake()
    {
        _initialSpawnInterval = enemySpawner != null ? enemySpawner.SpawnInterval : 1.1f;

        if (enemySpawner != null) enemySpawner.OnEnemySpawned += HandleEnemySpawned;
        if (augmentManager != null) augmentManager.OnPanelClosed += HandlePanelClosed;
    }

    private void OnDestroy()
    {
        if (enemySpawner != null) enemySpawner.OnEnemySpawned -= HandleEnemySpawned;
        if (augmentManager != null) augmentManager.OnPanelClosed -= HandlePanelClosed;
    }

    private void Update()
    {
        if (!_bossSpawned)
        {
            _waveTimer += Time.deltaTime;
            if (_waveTimer >= waveDuration)
            {
                _waveTimer = 0f;
                AdvanceWave();
            }
        }

#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current[debugBossSpawnKey].wasPressedThisFrame)
        {
            ForceSpawnBoss();
        }
#endif
    }

    private void AdvanceWave()
    {
        CurrentWave++;

        GameManager gm = GameManager.Instance;
        if (gm != null) _killsAtWaveStart = gm.KillCount;

        if (enemySpawner != null)
        {
            enemySpawner.SpawnInterval = Mathf.Max(minSpawnInterval, enemySpawner.SpawnInterval * spawnIntervalMultiplier);
        }

        if (CurrentWave >= bossWave)
        {
            SpawnBoss();
            return;
        }

        // 웨이브가 올라간 것을 알린다. 이게 없어서 플레이어가 웨이브의 존재 자체를 몰랐다.
        // 보스 배너와 달리 작고 짧게 — 흐름을 끊지 않으면서 "단계가 올라갔다"만 전달한다.
        ShowOrDeferBanner($"WAVE {CurrentWave}", waveBannerColor, waveBannerSize, waveBannerHold);
        CameraShake.Shake(0.18f, 0.08f);
    }

    [ContextMenu("즉시 보스 소환")]
    public void ForceSpawnBoss()
    {
        if (_bossSpawned) return;
        if (CurrentWave < bossWave) CurrentWave = bossWave;
        SpawnBoss();
    }

    private void SpawnBoss()
    {
        if (_bossSpawned || bossPrefab == null) return;
        _bossSpawned = true;
        BossActive = true;

        // 잡몹 스폰만 끈다. 시대 기믹(레이저·장판·분출구·회복 코어)은 BossActive를 보고
        // 간격을 늘린 채 계속 돈다 — 보스전 아레나가 텅 비어 보이지 않게 하려는 것.
        if (enemySpawner != null) enemySpawner.SpawningEnabled = false;

        Boss boss = Instantiate(bossPrefab, RightEdgeSpawnPoint(), Quaternion.identity);
        Health bossHealth = boss.GetComponent<Health>();
        bossHealth.OnDeath += HandleBossDeath;

        // 패턴 세트는 Boss.Start()가 읽는다. Instantiate 직후 = Start 전이라 여기서 정해주면 된다.
        boss.ConfigureEra(bossEraIndex);

        // HP는 UI에 넘기기 전에 확정해야 한다. Show()가 그 시점의 MaxHp를 게이지 기준으로 잡는다.
        if (!Mathf.Approximately(bossHpMultiplier, 1f)) bossHealth.SetMaxHp(bossHealth.MaxHp * bossHpMultiplier);

        // sr.color/sr.sprite 직접 대입이 아니라 SetAppearance를 쓴다 — Health가 Awake에서 캐시한 프리팹 색으로
        // 첫 피격 플래시 직후 되돌아가는 것을 막는다. bossSprite가 null이면 기존 틴트 방식 그대로다.
        bossHealth.SetAppearance(bossSprite, bossColor);

        if (bossHpUI != null) bossHpUI.Show(bossHealth, bossName);

        // 보스는 웨이브 전환과 확실히 갈려야 한다 — 크고, 붉고, 오래 남고,
        // 화면 섬광과 흔들림이 같이 붙는다.
        ShowOrDeferBanner(bossName, bossBannerColor, bossBannerSize, bossBannerHold);
        ScreenFlash.Full(bossBannerColor, 0.22f, 0.3f);
        CameraShake.Shake(0.7f, 0.35f);
    }

    /// <summary>시대 전환 시 EraManager가 다음 보스의 이름/색/체력 배율/패턴 세트를 갱신한다 (프리팹은 재사용).</summary>
    public void ConfigureBoss(string name, Color color, float hpMultiplier, int eraIndex, Sprite sprite = null)
    {
        bossName = name;
        bossColor = color;
        bossSprite = sprite;
        bossHpMultiplier = Mathf.Max(0.01f, hpMultiplier);
        bossEraIndex = Mathf.Max(0, eraIndex);
    }

    /// <summary>
    /// 시대 전환 시 EraManager가 이후 스폰될 적의 능력치·외형·물량을 갱신한다.
    /// ApplyEra에서 ResetForNewEra() 다음에 불리므로 여기서 스폰 간격을 덮어써야 시대 값이 남는다.
    ///
    /// 0 이하를 "값 없음 = 기존 값 유지"로 본다. 인스펙터에서 비워 둔 시대가 있어도
    /// 속도 배율 0으로 적이 얼어붙거나 maxAlive 0으로 스폰이 멎지 않게 하려는 것이다.
    /// (필드를 새로 추가해도 씬이 0을 주지는 않는다 — eraConfigs가 배열 초기화식이라
    ///  역직렬화 때 C# 값이 먼저 깔리고 씬에 있는 필드만 덮인다. 2026-08-09 실측.)
    /// </summary>
    public void ConfigureEnemyScaling(EraManager.EraConfig cfg)
    {
        if (cfg == null) return;

        enemyHpMultiplier = Mathf.Max(0.01f, cfg.enemyHpMultiplier);
        enemySpeedMultiplier = cfg.enemySpeedMultiplier > 0f ? cfg.enemySpeedMultiplier : 1f;
        enemyColor = cfg.enemyColor;
        enemySprite = cfg.enemySprite;
        enemyWalkFrames = cfg.enemyWalkFrames;
        enemyArtFacesLeft = cfg.enemyArtFacesLeft;

        if (enemySpawner == null) return;
        if (cfg.spawnInterval > 0f) enemySpawner.SpawnInterval = cfg.spawnInterval;
        if (cfg.maxAlive > 0) enemySpawner.MaxAlive = cfg.maxAlive;
    }

    /// <summary>시대 전환 시 EraManager가 호출: 웨이브/스폰 상태를 새 시대 기준으로 되돌린다.</summary>
    public void ResetForNewEra()
    {
        CurrentWave = 1;
        _bossSpawned = false;
        BossActive = false;
        _waveTimer = 0f;

        // 시대가 바뀌면 웨이브도 1로 돌아가므로 이번 웨이브 처치 기준점도 다시 잡는다.
        GameManager gm = GameManager.Instance;
        _killsAtWaveStart = gm != null ? gm.KillCount : 0;

        if (enemySpawner != null)
        {
            enemySpawner.SpawnInterval = _initialSpawnInterval;
            enemySpawner.SpawningEnabled = true;
        }
    }

    /// <summary>증강 카드가 떠 있으면 배너를 미루고, 아니면 바로 띄운다.</summary>
    private void ShowOrDeferBanner(string text, Color color, float size, float hold)
    {
        if (bossBanner == null) return;

        if (augmentManager != null && augmentManager.IsShowing)
        {
            _pendingBannerText = text;
            _pendingBannerColor = color;
            _pendingBannerSize = size;
            _pendingBannerHold = hold;
        }
        else
        {
            bossBanner.Show(text, color, size, hold);
        }
    }

    private void HandlePanelClosed()
    {
        if (_pendingBannerText == null || bossBanner == null) return;

        bossBanner.Show(_pendingBannerText, _pendingBannerColor, _pendingBannerSize, _pendingBannerHold);
        _pendingBannerText = null;
    }

    private void HandleBossDeath(Health _)
    {
        BossActive = false;
        if (bossHpUI != null) bossHpUI.Hide();
        MagnetizeAllExpOrbs();
        OnBossDefeated?.Invoke();
    }

    /// <summary>보스를 잡으면 필드에 남은 경험치 오브를 전부 자석처럼 끌어온다. 놓치는 경험치가 없게 하려는 것.</summary>
    private static void MagnetizeAllExpOrbs()
    {
        foreach (ExpOrb orb in FindObjectsByType<ExpOrb>(FindObjectsSortMode.None)) orb.ForceAbsorb();
    }

    private void HandleEnemySpawned(Enemy enemy)
    {
        // 속도는 Health와 무관하므로 먼저 적용한다 — Health가 없어도 시대 속도는 걸려야 한다.
        enemy.ApplySpeedMultiplier(enemySpeedMultiplier);

        Health h = enemy.GetComponent<Health>();
        if (h == null) return;

        // 웨이브 가산은 시대마다 1로 리셋된다. 시대 배율은 그 위에 곱해져 시대 간 난이도 상승을 담당한다.
        float multiplier = (1f + enemyHealthStepBonus * (CurrentWave - 1)) * enemyHpMultiplier;
        h.SetMaxHp(h.MaxHp * multiplier);

        // 걷기 프레임만 넣고 enemySprite를 비워 둔 경우에도 아트로 인정돼야 한다.
        // 아니면 SetAppearance가 "아트 없음"으로 보고 시대 색을 곱해, 애써 그린 공룡이 물든다.
        Sprite still = enemySprite != null ? enemySprite
            : (enemyWalkFrames != null && enemyWalkFrames.Length > 0 ? enemyWalkFrames[0] : null);

        h.SetAppearance(still, enemyColor);

        // SetAppearance가 sprite를 덮어쓰므로 반드시 그 뒤에 프레임을 넘긴다.
        SpriteWalkAnimator walk = enemy.GetComponent<SpriteWalkAnimator>();
        if (walk != null)
        {
            walk.ArtFacesLeft = enemyArtFacesLeft;   // SetFrames보다 먼저 — 첫 방향 판정에 쓰인다
            walk.SetFrames(enemyWalkFrames);
        }
    }

    /// <summary>기획서 상 보스 출현 지점: 아레나 오른쪽 끝.</summary>
    private Vector3 RightEdgeSpawnPoint()
    {
        if (ArenaBounds.Instance != null)
        {
            const float margin = 1f;
            Rect r = ArenaBounds.Instance.Rect;
            return new Vector3(r.xMax - margin, r.center.y, 0f);
        }

        // ArenaBounds가 없을 때(씬에 배치 안 된 경우)의 폴백: 카메라 시야 오른쪽 바깥.
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        const float camMargin = 3f;

        Vector3 center = cam.transform.position;
        return new Vector3(center.x + halfWidth + camMargin, center.y, 0f);
    }
}
