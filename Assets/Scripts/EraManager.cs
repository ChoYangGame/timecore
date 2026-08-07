using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 원시 → 중세 → 현대 → 미래 시대 전환을 관리한다. WaveManager.OnBossDefeated를 구독해
/// 아레나 중앙에 모래시계를 스폰하고, 플레이어가 먹으면(Hourglass.OnCollected)
/// 화면 정리 → 페이드아웃 → 배경/적 프리팹/웨이브 교체 → 페이드인 → 배너 순으로 진행한다.
/// 플레이어 레벨/증강/HP는 그대로 유지한다(한 판 안에서 이어지는 구조).
///
/// 시대는 eraConfigs 배열 순서대로 진행한다. 배열 인덱스 = Era enum 값이라는 규약이며,
/// 시대를 늘리려면 enum에 값을 추가하고 배열을 같은 순서로 채우면 된다.
/// 부착 대상: EraManager (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class EraManager : MonoBehaviour
{
    public enum Era { Primitive, Medieval, Modern, Future }

    [Serializable]
    public class EraConfig
    {
        public string eraLabel = "원시 시대";

        [Tooltip("디렉터 난입 배너·결과 화면에 쓰는 짧은 이름")]
        public string eraShortName = "원시";

        public Color backgroundColor = new Color(0.165f, 0.227f, 0.180f, 1f);

        public Enemy enemyPrefab;

        [Tooltip("이 시대 적의 스프라이트 색. 프리팹 색을 덮어쓴다 — 프리팹을 늘리지 않고 시대를 구분하려는 것")]
        public Color enemyColor = Color.white;

        [Tooltip("이 시대 적의 최대 체력 배율. 웨이브 가산과 곱해진다")]
        public float enemyHpMultiplier = 1f;

        public Color bossColor = Color.white;
        public string bossName = "고대의 포식자";

        [Tooltip("이 시대 보스의 최대 체력 배율")]
        public float bossHpMultiplier = 1f;
    }

    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BossBannerUI bossBanner;
    [SerializeField] private AugmentManager augmentManager;
    [SerializeField] private Transform player;
    [SerializeField] private Image fadeImage;
    [Tooltip("보스 처치 후 아레나 중앙에 스폰되는 모래시계. 플레이어가 먹으면 다음 시대로 전환된다.")]
    [SerializeField] private Hourglass hourglassPrefab;

    [Header("시대별 설정 — 배열 순서가 곧 진행 순서다 (인덱스 = Era enum 값)")]
    [Tooltip("enemyPrefab만 인스펙터에서 채우면 된다. 나머지 기본값은 코드에 있다.")]
    [SerializeField]
    private EraConfig[] eraConfigs =
    {
        new EraConfig
        {
            eraLabel = "원시 시대",
            eraShortName = "원시",
            backgroundColor = new Color(0.165f, 0.227f, 0.180f, 1f),
            // 바닥이 중간 톤 모래갈색이라 같은 갈색은 묻힌다. 채도를 올린 적갈색으로 띄운다.
            enemyColor = new Color(0.769f, 0.271f, 0.165f, 1f),
            enemyHpMultiplier = 1f,
            bossColor = new Color(0.941f, 0.463f, 0.290f, 1f),
            bossName = "고대의 포식자",
            bossHpMultiplier = 1f,
        },
        new EraConfig
        {
            eraLabel = "중세 시대",
            eraShortName = "중세",
            backgroundColor = new Color(0.180f, 0.165f, 0.220f, 1f),
            // 바닥이 따뜻한 회색 석재라 회색은 묻힌다. 차가운 강철청으로 색온도를 반대로 둔다.
            enemyColor = new Color(0.180f, 0.290f, 0.420f, 1f),
            enemyHpMultiplier = 1.2f,
            bossColor = new Color(0.357f, 0.541f, 0.749f, 1f),
            bossName = "강철의 심문관",
            bossHpMultiplier = 1.2f,
        },
        new EraConfig
        {
            eraLabel = "현대 시대",
            eraShortName = "현대",
            backgroundColor = new Color(0.145f, 0.165f, 0.200f, 1f),
            // 바닥이 어두운 청회색 아스팔트라 밝은 올리브가 명도로 갈린다.
            enemyColor = new Color(0.624f, 0.749f, 0.180f, 1f),
            enemyHpMultiplier = 1.45f,
            bossColor = new Color(0.831f, 0.910f, 0.361f, 1f),
            bossName = "강화 병기",
            bossHpMultiplier = 1.45f,
        },
        new EraConfig
        {
            eraLabel = "미래 시대",
            eraShortName = "미래",
            backgroundColor = new Color(0.110f, 0.180f, 0.200f, 1f),
            // 바닥이 밝은 회색 금속이고 시안 발광 장식이 깔려 있다. 플레이어 자체도 시안이라
            // 시안 계열을 쓰면 셋이 겹친다 — 시안의 보색인 마젠타로 색상 자체를 갈라놓는다.
            enemyColor = new Color(0.612f, 0.114f, 0.451f, 1f),
            enemyHpMultiplier = 1.75f,
            bossColor = new Color(0.847f, 0.322f, 0.898f, 1f),
            bossName = "시간의 지배자",
            bossHpMultiplier = 1.75f,
        },
    };

    [Header("연출 타이밍")]
    [SerializeField] private float fadeDuration = 0.8f;

#if UNITY_EDITOR
    [Tooltip("테스트용: 이 키를 누르면 즉시 다음 시대로 전환한다 (대기 없이). 빌드에서는 컴파일되지 않는다.")]
    [SerializeField] private Key debugEraSwitchKey = Key.N;
#endif

    public Era CurrentEra { get; private set; } = Era.Primitive;

    /// <summary>전환 연출(페이드~배너)이 진행 중인지. AnomalyDirector가 배너 충돌을 피할 때 본다.</summary>
    public bool IsTransitioning => _isTransitioning;

    /// <summary>마지막 시대 보스 처치 시 발행. 지금은 이벤트만 나간다(게임 클리어 화면은 GameOverController가 받는다).</summary>
    public event Action OnGameClear;

    private bool _isTransitioning;

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (targetCamera == null) targetCamera = Camera.main;

        EraConfig first = GetConfig(CurrentEra);

        if (waveManager != null)
        {
            waveManager.OnBossDefeated += HandleBossDefeated;
            if (first != null) PushConfigToWaveManager(first);
        }

        if (targetCamera != null && first != null) targetCamera.backgroundColor = first.backgroundColor;

        if (fadeImage != null) fadeImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (waveManager != null) waveManager.OnBossDefeated -= HandleBossDefeated;
    }

#if UNITY_EDITOR
    // 디버그 전용. 빌드에서는 Update 콜백 자체가 사라진다.
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[debugEraSwitchKey].wasPressedThisFrame)
        {
            ForceEraTransition();
        }
    }
#endif

    [ContextMenu("즉시 시대 전환")]
    public void ForceEraTransition()
    {
        if (_isTransitioning) return;
        StartCoroutine(TransitionRoutine());
    }

    /// <summary>
    /// 게임오버 등으로 전환을 중단해야 할 때 호출한다. 진행 중인 코루틴을 멈추고 페이드를 걷어낸다.
    /// 중단하지 않으면 페이드가 unscaledDeltaTime으로 도는 탓에 timeScale=0에서도 계속 진행돼
    /// ApplyEra()가 스폰을 다시 켜고 웨이브를 리셋해버린다.
    /// </summary>
    public void AbortTransition()
    {
        if (!_isTransitioning) return;

        StopAllCoroutines();
        _isTransitioning = false;

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(false);
        }
    }

    private void HandleBossDefeated()
    {
        if (_isTransitioning) return;

        if (IsLastEra())
        {
            // 마지막 시대(=예선 최종 보스) 처치. 클리어는 게임오버 여부와 무관하게 알린다 —
            // 사망과 같은 프레임에 겹쳤을 때 어느 쪽을 보여줄지는 GameOverController가 정한다.
            Debug.Log("[EraManager] Game Clear!");
            OnGameClear?.Invoke();
            return;
        }

        // 시대 전환은 판이 끝났으면 하지 않는다.
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        SpawnHourglass();
    }

    private bool IsLastEra()
    {
        if (eraConfigs == null || eraConfigs.Length == 0) return true;
        return (int)CurrentEra >= eraConfigs.Length - 1;
    }

    /// <summary>아레나 중앙에 모래시계를 스폰한다. 플레이어가 먹어야 실제 전환이 시작된다.</summary>
    private void SpawnHourglass()
    {
        if (hourglassPrefab == null)
        {
            // 프리팹이 안 꽂혀 있으면(테스트 씬 등) 예전처럼 바로 전환해 흐름이 막히지 않게 한다.
            StartCoroutine(TransitionRoutine());
            return;
        }

        Vector3 spawnPos = ArenaBounds.Instance != null
            ? (Vector3)ArenaBounds.Instance.Rect.center
            : Vector3.zero;

        Hourglass hourglass = Instantiate(hourglassPrefab, spawnPos, Quaternion.identity);
        hourglass.OnCollected += HandleHourglassCollected;
    }

    private void HandleHourglassCollected()
    {
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        _isTransitioning = true;

        // 증강 카드가 배너/시대 전환보다 우선순위가 높다: 카드가 떠 있으면 닫힐 때까지 대기.
        if (augmentManager != null) yield return new WaitUntil(() => !augmentManager.IsShowing);

        yield return FadeTo(1f);

        ApplyEra(NextEra());

        yield return FadeTo(0f);

        EraConfig cfg = GetConfig(CurrentEra);
        if (bossBanner != null && cfg != null) bossBanner.Show(cfg.eraLabel);

        _isTransitioning = false;
    }

    /// <summary>
    /// 다음 시대. 마지막에서는 처음으로 되돌아간다 — 실제 플레이는 마지막 시대에서 클리어로 빠지므로
    /// 이 순환은 디버그 키(N)로 4시대를 계속 돌려보기 위한 것이다.
    /// </summary>
    private Era NextEra()
    {
        if (eraConfigs == null || eraConfigs.Length == 0) return CurrentEra;

        int next = (int)CurrentEra + 1;
        if (next >= eraConfigs.Length) next = 0;
        return (Era)next;
    }

    private void ApplyEra(Era era)
    {
        CurrentEra = era;
        EraConfig cfg = GetConfig(era);
        if (cfg == null) return;

        ClearBattlefield();

        if (targetCamera != null) targetCamera.backgroundColor = cfg.backgroundColor;
        if (enemySpawner != null && cfg.enemyPrefab != null) enemySpawner.SetEnemyPrefab(cfg.enemyPrefab);

        if (waveManager != null)
        {
            waveManager.ResetForNewEra();
            PushConfigToWaveManager(cfg);
        }

        if (player != null) player.position = LeftEdgeSpawnPoint();
    }

    private void PushConfigToWaveManager(EraConfig cfg)
    {
        waveManager.ConfigureBoss(cfg.bossName, cfg.bossColor, cfg.bossHpMultiplier);
        waveManager.ConfigureEnemyScaling(cfg.enemyHpMultiplier, cfg.enemyColor);
    }

    /// <summary>화면에 남은 적/보스 투사체/경험치 오브/감속 지대를 전부 정리한다. 암전 중에만 호출된다.</summary>
    private void ClearBattlefield()
    {
        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Destroy(e.gameObject);
        foreach (BossProjectile p in FindObjectsByType<BossProjectile>(FindObjectsSortMode.None)) Destroy(p.gameObject);
        foreach (ExpOrb o in FindObjectsByType<ExpOrb>(FindObjectsSortMode.None)) Destroy(o.gameObject);
        RiftZone.ClearAll();
        HazardBeam.ClearAll();
        HomingHazard.ClearAll();
        RiftVent.ClearAll();
        RecoveryCore.ClearAll();

        // 아직 안 열린 스폰 표식까지 지운다. 안 지우면 다음 시대 화면에서 이전 시대 적이 튀어나온다.
        SpawnPortal.ClearAll();
    }

    /// <summary>기획서 상 시간 균열 지점: 아레나 왼쪽 끝.</summary>
    private Vector3 LeftEdgeSpawnPoint()
    {
        if (ArenaBounds.Instance != null)
        {
            const float inset = 1f;
            Rect r = ArenaBounds.Instance.Rect;
            return new Vector3(r.xMin + inset, r.center.y, 0f);
        }

        // ArenaBounds가 없을 때(씬에 배치 안 된 경우)의 폴백: 카메라 시야 왼쪽 안쪽.
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return Vector3.zero;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        const float camInset = 1.5f;

        Vector3 center = cam.transform.position;
        return new Vector3(center.x - halfWidth + camInset, center.y, 0f);
    }

    /// <summary>현재 시대의 설정. GameOverController가 도달 시대 이름을 찍을 때 쓴다.</summary>
    public EraConfig CurrentConfig => GetConfig(CurrentEra);

    public EraConfig GetConfig(Era era)
    {
        if (eraConfigs == null || eraConfigs.Length == 0) return null;
        return eraConfigs[Mathf.Clamp((int)era, 0, eraConfigs.Length - 1)];
    }

    /// <summary>
    /// 현재 시대가 아닌 시대 하나를 무작위로 고른다. AnomalyDirector의 난입 적이 쓴다.
    /// 시대가 2개뿐이던 시절의 "반대 시대"를 4시대로 일반화한 것.
    /// </summary>
    public EraConfig GetRandomOtherEraConfig()
    {
        if (eraConfigs == null || eraConfigs.Length <= 1) return null;

        // 현재를 뺀 개수에서 뽑고, 현재 이상이면 한 칸 밀어 현재를 건너뛴다 (재시도 루프 없이 균등).
        int current = Mathf.Clamp((int)CurrentEra, 0, eraConfigs.Length - 1);
        int pick = UnityEngine.Random.Range(0, eraConfigs.Length - 1);
        if (pick >= current) pick++;

        return eraConfigs[pick];
    }

    // Time.timeScale은 건드리지 않지만, 카드 표시 중 timeScale=0이 걸려 있을 수 있어
    // 페이드 타이머는 unscaledDeltaTime 기반으로 돈다.
    private IEnumerator FadeTo(float targetAlpha)
    {
        if (fadeImage == null) yield break;

        fadeImage.gameObject.SetActive(true);
        Color c = fadeImage.color;
        float startAlpha = c.a;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / fadeDuration));
            fadeImage.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        fadeImage.color = c;

        if (targetAlpha <= 0f) fadeImage.gameObject.SetActive(false);
    }
}
