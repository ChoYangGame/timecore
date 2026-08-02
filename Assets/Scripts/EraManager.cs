using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 원시 → 중세 시대 전환을 관리한다. WaveManager.OnBossDefeated를 구독해
/// 화면 정리 → 페이드아웃 → 배경/적 프리팹/웨이브 교체 → 페이드인 → 배너 순으로 진행한다.
/// 플레이어 레벨/증강/HP는 그대로 유지한다(한 판 안에서 이어지는 구조).
/// 부착 대상: EraManager (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class EraManager : MonoBehaviour
{
    public enum Era { Primitive, Medieval }

    [Serializable]
    public class EraConfig
    {
        public string eraLabel = "중세 시대";
        public Color backgroundColor = new Color(0.180f, 0.165f, 0.220f, 1f);
        public Enemy enemyPrefab;
        public Color bossColor = Color.white;
        public string bossName = "고대의 포식자";
    }

    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private BossBannerUI bossBanner;
    [SerializeField] private AugmentManager augmentManager;
    [SerializeField] private Transform player;
    [SerializeField] private Image fadeImage;

    [Header("시대별 설정")]
    [SerializeField] private EraConfig primitiveConfig = new EraConfig
    {
        eraLabel = "원시 시대",
        backgroundColor = new Color(0.165f, 0.227f, 0.180f, 1f),
        bossColor = new Color(0.362f, 0.108f, 0.097f, 1f),
        bossName = "고대의 포식자",
    };
    [SerializeField] private EraConfig medievalConfig = new EraConfig
    {
        eraLabel = "중세 시대",
        backgroundColor = new Color(0.180f, 0.165f, 0.220f, 1f),
        bossColor = new Color(0.541f, 0.427f, 0.231f, 1f),
        bossName = "고대의 포식자",
    };

    [Header("연출 타이밍")]
    [SerializeField] private float delayBeforeFade = 1f;
    [SerializeField] private float fadeDuration = 0.8f;

#if UNITY_EDITOR
    [Tooltip("테스트용: 이 키를 누르면 즉시 다음 시대로 전환한다 (대기 없이). 빌드에서는 컴파일되지 않는다.")]
    [SerializeField] private Key debugEraSwitchKey = Key.N;
#endif

    public Era CurrentEra { get; private set; } = Era.Primitive;

    /// <summary>중세 보스 처치 시 발행. 지금은 이벤트만 나간다(게임 클리어 화면은 아직 없음).</summary>
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

        if (waveManager != null)
        {
            waveManager.OnBossDefeated += HandleBossDefeated;
            waveManager.ConfigureBoss(primitiveConfig.bossName, primitiveConfig.bossColor);
        }

        if (targetCamera != null) targetCamera.backgroundColor = primitiveConfig.backgroundColor;

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
        StartCoroutine(TransitionRoutine(skipInitialWait: true));
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

        if (CurrentEra == Era.Medieval)
        {
            // 중세 보스(=예선 최종 보스) 처치. 클리어는 게임오버 여부와 무관하게 알린다 —
            // 사망과 같은 프레임에 겹쳤을 때 어느 쪽을 보여줄지는 GameOverController가 정한다.
            Debug.Log("[EraManager] Game Clear!");
            OnGameClear?.Invoke();
            return;
        }

        // 시대 전환은 판이 끝났으면 하지 않는다.
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        StartCoroutine(TransitionRoutine(skipInitialWait: false));
    }

    private IEnumerator TransitionRoutine(bool skipInitialWait)
    {
        _isTransitioning = true;

        if (!skipInitialWait) yield return new WaitForSecondsRealtime(delayBeforeFade);

        // 증강 카드가 배너/시대 전환보다 우선순위가 높다: 카드가 떠 있으면 닫힐 때까지 대기.
        if (augmentManager != null) yield return new WaitUntil(() => !augmentManager.IsShowing);

        yield return FadeTo(1f);

        Era next = CurrentEra == Era.Primitive ? Era.Medieval : Era.Primitive;
        ApplyEra(next);

        yield return FadeTo(0f);

        EraConfig cfg = GetConfig(next);
        if (bossBanner != null) bossBanner.Show(cfg.eraLabel);

        _isTransitioning = false;
    }

    private void ApplyEra(Era era)
    {
        CurrentEra = era;
        EraConfig cfg = GetConfig(era);

        ClearBattlefield();

        if (targetCamera != null) targetCamera.backgroundColor = cfg.backgroundColor;
        if (enemySpawner != null && cfg.enemyPrefab != null) enemySpawner.SetEnemyPrefab(cfg.enemyPrefab);

        if (waveManager != null)
        {
            waveManager.ResetForNewEra();
            waveManager.ConfigureBoss(cfg.bossName, cfg.bossColor);
        }

        if (player != null) player.position = LeftEdgeSpawnPoint();
    }

    /// <summary>화면에 남은 적/보스 투사체/경험치 오브를 전부 정리한다. 암전 중에만 호출된다.</summary>
    private void ClearBattlefield()
    {
        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) Destroy(e.gameObject);
        foreach (BossProjectile p in FindObjectsByType<BossProjectile>(FindObjectsSortMode.None)) Destroy(p.gameObject);
        foreach (ExpOrb o in FindObjectsByType<ExpOrb>(FindObjectsSortMode.None)) Destroy(o.gameObject);
    }

    /// <summary>기획서 상 시간 균열 지점: 맵(카메라 시야) 왼쪽 안쪽.</summary>
    private Vector3 LeftEdgeSpawnPoint()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return Vector3.zero;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        const float inset = 1.5f;

        Vector3 center = cam.transform.position;
        return new Vector3(center.x - halfWidth + inset, center.y, 0f);
    }

    private EraConfig GetConfig(Era era) => era == Era.Primitive ? primitiveConfig : medievalConfig;

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
