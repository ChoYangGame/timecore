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

    public int CurrentWave { get; private set; } = 1;

    /// <summary>보스 처치 시 발행. EraManager가 구독해 다음 시대 전환(또는 게임 클리어)을 처리한다.</summary>
    public event Action OnBossDefeated;

    private float _waveTimer;
    private bool _bossSpawned;
    private string _pendingBannerText;
    private float _initialSpawnInterval;

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

        if (enemySpawner != null)
        {
            enemySpawner.SpawnInterval = Mathf.Max(minSpawnInterval, enemySpawner.SpawnInterval * spawnIntervalMultiplier);
        }

        if (CurrentWave >= bossWave) SpawnBoss();
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

        if (enemySpawner != null) enemySpawner.SpawningEnabled = false;

        Boss boss = Instantiate(bossPrefab, RightEdgeSpawnPoint(), Quaternion.identity);
        Health bossHealth = boss.GetComponent<Health>();
        bossHealth.OnDeath += HandleBossDeath;

        // 패턴 세트는 Boss.Start()가 읽는다. Instantiate 직후 = Start 전이라 여기서 정해주면 된다.
        boss.ConfigureEra(bossEraIndex);

        // HP는 UI에 넘기기 전에 확정해야 한다. Show()가 그 시점의 MaxHp를 게이지 기준으로 잡는다.
        if (!Mathf.Approximately(bossHpMultiplier, 1f)) bossHealth.SetMaxHp(bossHealth.MaxHp * bossHpMultiplier);

        // sr.color 직접 대입이 아니라 SetBaseColor를 쓴다 — Health가 Awake에서 캐시한 프리팹 색으로
        // 첫 피격 플래시 직후 되돌아가는 것을 막는다.
        bossHealth.SetBaseColor(bossColor);

        if (bossHpUI != null) bossHpUI.Show(bossHealth, bossName);
        ShowOrDeferBanner($"WAVE {bossWave} — BOSS");
    }

    /// <summary>시대 전환 시 EraManager가 다음 보스의 이름/색/체력 배율/패턴 세트를 갱신한다 (프리팹은 재사용).</summary>
    public void ConfigureBoss(string name, Color color, float hpMultiplier, int eraIndex)
    {
        bossName = name;
        bossColor = color;
        bossHpMultiplier = Mathf.Max(0.01f, hpMultiplier);
        bossEraIndex = Mathf.Max(0, eraIndex);
    }

    /// <summary>시대 전환 시 EraManager가 이후 스폰될 적의 체력 배율과 색을 갱신한다.</summary>
    public void ConfigureEnemyScaling(float hpMultiplier, Color color)
    {
        enemyHpMultiplier = Mathf.Max(0.01f, hpMultiplier);
        enemyColor = color;
    }

    /// <summary>시대 전환 시 EraManager가 호출: 웨이브/스폰 상태를 새 시대 기준으로 되돌린다.</summary>
    public void ResetForNewEra()
    {
        CurrentWave = 1;
        _bossSpawned = false;
        _waveTimer = 0f;

        if (enemySpawner != null)
        {
            enemySpawner.SpawnInterval = _initialSpawnInterval;
            enemySpawner.SpawningEnabled = true;
        }
    }

    /// <summary>증강 카드가 떠 있으면 배너를 미루고, 아니면 바로 띄운다.</summary>
    private void ShowOrDeferBanner(string text)
    {
        if (bossBanner == null) return;

        if (augmentManager != null && augmentManager.IsShowing)
        {
            _pendingBannerText = text;
        }
        else
        {
            bossBanner.Show(text);
        }
    }

    private void HandlePanelClosed()
    {
        if (_pendingBannerText == null || bossBanner == null) return;

        bossBanner.Show(_pendingBannerText);
        _pendingBannerText = null;
    }

    private void HandleBossDeath(Health _)
    {
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
        Health h = enemy.GetComponent<Health>();
        if (h == null) return;

        // 웨이브 가산은 시대마다 1로 리셋된다. 시대 배율은 그 위에 곱해져 시대 간 난이도 상승을 담당한다.
        float multiplier = (1f + enemyHealthStepBonus * (CurrentWave - 1)) * enemyHpMultiplier;
        h.SetMaxHp(h.MaxHp * multiplier);

        h.SetBaseColor(enemyColor);
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
