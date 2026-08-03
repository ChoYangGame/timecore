using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 기획서의 "시간 이상" 시스템을 적응형 디렉터로 구현한 것.
/// 플레이어 상태를 일정 주기로 읽어 세 가지 중 하나로 판단하고, 그 판단을 화면 배너로 알린다.
/// 조용히 난이도만 조절하면 플레이어가 AI의 존재를 알 수 없으므로, 개입은 항상 배너와 함께 나간다.
///
/// 규칙 기반이며 외부 의존성이 없다(순수 C#). 지표는 매 프레임이 아니라 evaluateInterval마다 1회만 계산한다.
/// 부착 대상: AnomalyDirector (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class AnomalyDirector : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EraManager eraManager;
    [SerializeField] private AugmentManager augmentManager;
    [SerializeField] private BossBannerUI bossBanner;
    [SerializeField] private TitleController titleController;
    [SerializeField] private Health playerHealth;

    [Tooltip("난입시킬 시대별 적 프리팹. 현재 시대의 반대쪽이 튀어나온다.")]
    [SerializeField] private Enemy primitiveEnemyPrefab;
    [SerializeField] private Enemy medievalEnemyPrefab;

    [Header("주기")]
    [Tooltip("지표를 계산하는 간격(초). 매 프레임 계산하지 않는다.")]
    [SerializeField] private float evaluateInterval = 5f;
    [Tooltip("게임 시작 후 이 시간 동안은 데이터가 없어 판단하지 않는다")]
    [SerializeField] private float warmupTime = 25f;
    [Tooltip("개입 사이 최소 간격(초). 배너가 시끄러워지는 걸 막는다")]
    [SerializeField] private float minInterventionInterval = 20f;

    [Header("규칙 1 — 위기")]
    [SerializeField] private float crisisHpRatio = 0.35f;
    [SerializeField] private int crisisHitCount = 4;

    [Header("규칙 2 — 과잉 화력")]
    [Tooltip("처치속도/스폰속도. 1.0이면 스폰되는 만큼 정확히 처치 중")]
    [SerializeField] private float overkillPressure = 0.9f;
    [SerializeField] private float overkillHpRatio = 0.65f;
    [SerializeField] private int intruderCount = 4;
    [Tooltip("난입 시 스폰 간격에 곱할 값. 0.71 ≈ 밀도 +40%")]
    [SerializeField] private float intrusionDensityFactor = 0.71f;
    [Tooltip("동시에 존재할 수 있는 난입 적 상한. 난입 적은 EnemySpawner의 maxAlive에 잡히지 않으므로 별도로 막는다")]
    [SerializeField] private int maxConcurrentIntruders = 12;

    [Header("규칙 3 — 전선 정체")]
    [Tooltip("살아있는 적 / maxAlive. 이 값 이하면 화면이 한산하다고 본다")]
    [SerializeField] private float stagnantAliveRatio = 0.25f;
    [SerializeField] private float stagnantKillRate = 0.3f;
    [SerializeField] private float stagnantHpRatio = 0.5f;
    [SerializeField] private float accelerateFactor = 0.85f;

    [Header("안전장치")]
    [Tooltip("디렉터가 만들 수 있는 최대 밀도. 0.5 = 원본의 2배")]
    [SerializeField] private float maxAccumulatedRatio = 0.5f;
    [SerializeField] private float minSpawnInterval = 0.35f;

    [Header("난입 적 외형")]
    [SerializeField] private Color intruderTint = new Color(0.784f, 0.420f, 0.878f, 1f); // #C86BE0
    [Range(0f, 1f)][SerializeField] private float intruderTintStrength = 0.35f;
    [SerializeField] private float intruderScale = 1.15f;

    [Header("판단 로그")]
    [SerializeField] private bool verboseLog = true;
    [SerializeField] private int maxLogEntries = 200;

    /// <summary>SpawnInterval에 디렉터가 실제로 곱해 놓은 누적 비율. 1이면 개입 없음.</summary>
    public float AppliedDensityRatio => _appliedRatio;
    public int DecisionCount => _log.Count;
    public string LastDecision => _log.Count > 0 ? _log[_log.Count - 1] : string.Empty;
    public int EvaluationCount => _evalCount;

    /// <summary>
    /// 평가는 계속 돌았는데 왜 개입이 없었는지를 보여준다.
    /// "개입 5회"만 있으면 디렉터가 멈춘 것처럼 보이지만,
    /// "평가 59회 중 개입 5회"는 계속 감시하며 선별 개입했다는 뜻이 된다.
    /// </summary>
    public string GetActivitySummary()
    {
        return string.Format(
            "평가 {0}회 / 개입 {1}회 | 건너뜀 — 워밍업 {2}, 쿨다운 {3}, 다른 UI·보스전 {4}, 조건 미성립 {5} " +
            "| 마지막 개입 이후 최대 pressure {6:F2}",
            _evalCount, _log.Count, _skipWarmup, _skipCooldown, _skipGuard, _skipNoRule,
            _maxPressureSinceIntervention);
    }

    private readonly List<string> _log = new List<string>();
    private readonly List<Enemy> _intruders = new List<Enemy>();
    private float _elapsed;
    private float _evalTimer;
    private float _lastInterventionTime = -999f;
    private int _lastKillCount;
    private int _hitsInWindow;
    private float _appliedRatio = 1f;
    private bool _eraKnown;
    private EraManager.Era _lastEra;

    private int _evalCount;
    private int _skipWarmup, _skipCooldown, _skipGuard, _skipNoRule;
    private float _maxPressureSinceIntervention;

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<Health>();
        }
        if (playerHealth != null) playerHealth.OnDamaged += HandlePlayerDamaged;

        if (GameManager.Instance != null) _lastKillCount = GameManager.Instance.KillCount;
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDamaged -= HandlePlayerDamaged;
    }

    private void HandlePlayerDamaged(float current, float max) => _hitsInWindow++;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

        // Time.deltaTime이라 타이틀·증강 카드로 멈춘 동안은 흐르지 않는다.
        _elapsed += Time.deltaTime;
        _evalTimer += Time.deltaTime;
        if (_evalTimer < evaluateInterval) return;
        _evalTimer = 0f;

        Evaluate();
    }

    private void Evaluate()
    {
        // 샘플은 개입 여부와 무관하게 항상 갱신한다.
        // 건너뛴 구간의 킬이 다음 계산에 몰려 들어가면 처치속도가 왜곡된다.
        int kills = GameManager.Instance.KillCount;
        float killRate = (kills - _lastKillCount) / evaluateInterval;
        _lastKillCount = kills;

        int hits = _hitsInWindow;
        _hitsInWindow = 0;

        _evalCount++;

        // 시대가 바뀌면 WaveManager.ResetForNewEra()가 SpawnInterval을 초기값으로 되돌린다.
        // 디렉터가 곱해둔 몫은 그 시점에 이미 사라졌으므로 기억도 함께 비운다.
        // 안 그러면 상한에 걸린 것으로 오인해 남은 판 내내 밀도를 못 올린다.
        EraManager.Era currentEra = eraManager != null ? eraManager.CurrentEra : EraManager.Era.Primitive;
        if (!_eraKnown)
        {
            _lastEra = currentEra;
            _eraKnown = true;
        }
        else if (currentEra != _lastEra)
        {
            _lastEra = currentEra;
            _appliedRatio = 1f;
        }

        if (_elapsed < warmupTime) { _skipWarmup++; return; }
        if (!CanIntervene()) { _skipGuard++; return; }
        if (_elapsed - _lastInterventionTime < minInterventionInterval) { _skipCooldown++; return; }

        float spawnInterval = enemySpawner.SpawnInterval;
        float spawnRate = spawnInterval > 0.01f ? 1f / spawnInterval : 0f;
        float pressure = spawnRate > 0.01f ? killRate / spawnRate : 0f;
        float hpRatio = playerHealth != null && playerHealth.MaxHp > 0f
            ? playerHealth.CurrentHp / playerHealth.MaxHp : 1f;
        int alive = enemySpawner.AliveCount;
        float aliveRatio = enemySpawner.MaxAlive > 0 ? (float)alive / enemySpawner.MaxAlive : 0f;

        // --- 규칙 1: 위기 --- 되돌릴 개입이 없으면 조용히 넘어간다(배너·쿨다운 소모 없음)
        if (hpRatio < crisisHpRatio || hits >= crisisHitCount)
        {
            if (RevertDensity())
            {
                Announce("시간 이상 안정화", "균열 복구 중");
                Commit("위기", "누적 밀도 원복 (×1.00 복귀)", killRate, spawnRate, pressure, hpRatio, hits, alive);
            }
            return;
        }

        // --- 규칙 2: 과잉 화력 ---
        if (pressure >= overkillPressure && hpRatio >= overkillHpRatio)
        {
            int spawned = SpawnIntruders(intruderCount);

            // 난입 여력이 없으면(이미 포화) 개입 자체를 하지 않는다.
            // 아무것도 안 나왔는데 "개체 난입" 배너만 뜨면 배너와 화면이 어긋난다.
            if (spawned == 0) return;

            float before = _appliedRatio;
            ApplyDensity(intrusionDensityFactor);

            string eraName = IntruderEraName();
            Announce("시간 이상 감지 — 과잉 화력", $"균열 증폭: {eraName} 개체 난입");
            Commit("과잉 화력",
                $"{eraName} 개체 {spawned} 난입, 밀도 ×{intrusionDensityFactor:F2} (누적 {before:F2}→{_appliedRatio:F2})",
                killRate, spawnRate, pressure, hpRatio, hits, alive);
            return;
        }

        // --- 규칙 3: 전선 정체 (화면이 한산하고 교전이 없는 상태) ---
        if (aliveRatio <= stagnantAliveRatio && killRate < stagnantKillRate && hpRatio >= stagnantHpRatio)
        {
            float before = _appliedRatio;
            ApplyDensity(accelerateFactor);

            Announce("시간 이상 감지 — 전선 정체", "균열 가속: 출현 주기 단축");
            Commit("전선 정체",
                $"출현 주기 ×{accelerateFactor:F2} (누적 {before:F2}→{_appliedRatio:F2})",
                killRate, spawnRate, pressure, hpRatio, hits, alive);
            return;
        }

        // 어느 규칙에도 안 걸렸다. 얼마나 가까웠는지 남겨두면 임계값 조정에 쓸 수 있다.
        _skipNoRule++;
        if (pressure > _maxPressureSinceIntervention) _maxPressureSinceIntervention = pressure;
    }

    /// <summary>다른 UI와 겹치거나 판이 멈춘 상태에서는 개입하지 않는다.</summary>
    private bool CanIntervene()
    {
        if (titleController != null && titleController.IsWaitingToStart) return false;
        if (augmentManager != null && augmentManager.IsShowing) return false;
        if (eraManager != null && eraManager.IsTransitioning) return false;
        if (bossBanner != null && bossBanner.IsShowing) return false;
        // 보스전 중에는 WaveManager가 일반 스폰을 꺼둔다. 그 플래그를 그대로 신호로 쓴다.
        if (enemySpawner == null || !enemySpawner.SpawningEnabled) return false;
        return true;
    }

    /// <summary>
    /// SpawnInterval에 factor를 곱한다. 상한/바닥에 걸려 실제로 덜 곱해질 수 있으므로
    /// "의도한 값"이 아니라 "실제로 곱해진 비율"만 누적한다. 그래야 원복이 정확해진다.
    /// </summary>
    private void ApplyDensity(float factor)
    {
        float desired = _appliedRatio * factor;
        if (desired < maxAccumulatedRatio) factor = maxAccumulatedRatio / _appliedRatio;
        if (factor >= 1f) return;

        float before = enemySpawner.SpawnInterval;
        float after = Mathf.Max(minSpawnInterval, before * factor);
        if (after >= before) return;

        enemySpawner.SpawnInterval = after;
        _appliedRatio *= after / before;
    }

    /// <summary>
    /// 곱해 놓은 누적 비율만큼 정확히 나눠 되돌린다.
    /// WaveManager가 웨이브마다 거는 배율도 곱셈이라 서로 간섭 없이 합성된다.
    /// </summary>
    private bool RevertDensity()
    {
        if (Mathf.Approximately(_appliedRatio, 1f)) return false;

        enemySpawner.SpawnInterval /= _appliedRatio;
        _appliedRatio = 1f;
        return true;
    }

    private int SpawnIntruders(int count)
    {
        Enemy prefab = IntruderPrefab();
        if (prefab == null) return 0;

        // 죽은 난입 적 정리. 개입 시점(최소 20초 간격)에만 도는 루프라 부담이 없다.
        for (int i = _intruders.Count - 1; i >= 0; i--)
            if (_intruders[i] == null) _intruders.RemoveAt(i);

        int room = maxConcurrentIntruders - _intruders.Count;
        if (room <= 0) return 0;
        count = Mathf.Min(count, room);

        for (int i = 0; i < count; i++)
        {
            Enemy e = Instantiate(prefab, RandomPointOutsideView(), Quaternion.identity);

            SpriteRenderer sr = e.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.Lerp(sr.color, intruderTint, intruderTintStrength);
            e.transform.localScale *= intruderScale;

            _intruders.Add(e);
        }
        return count;
    }

    private Enemy IntruderPrefab()
    {
        bool inMedieval = eraManager != null && eraManager.CurrentEra == EraManager.Era.Medieval;
        return inMedieval ? primitiveEnemyPrefab : medievalEnemyPrefab;
    }

    private string IntruderEraName()
    {
        bool inMedieval = eraManager != null && eraManager.CurrentEra == EraManager.Era.Medieval;
        return inMedieval ? "원시" : "중세";
    }

    /// <summary>카메라를 감싸는 외접원 위의 랜덤한 점. EnemySpawner와 같은 방식이지만 독립적으로 계산한다.</summary>
    private Vector3 RandomPointOutsideView()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float radius = Mathf.Sqrt(halfWidth * halfWidth + halfHeight * halfHeight) + 2f;

        float angle = Random.value * Mathf.PI * 2f;
        Vector3 center = cam.transform.position;
        return new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, 0f);
    }

    /// <summary>판단을 화면에 알린다. 첫 줄은 왜(신호), 둘째 줄은 무엇을(개입).</summary>
    private void Announce(string reason, string action)
    {
        if (bossBanner == null) return;
        bossBanner.Show($"<size=85%>{reason}</size>\n<size=55%>{action}</size>");
    }

    private void Commit(string verdict, string action,
        float killRate, float spawnRate, float pressure, float hpRatio, int hits, int alive)
    {
        _lastInterventionTime = _elapsed;
        _maxPressureSinceIntervention = 0f;

        string line = string.Format(
            "[AnomalyDirector] t={0:F1}s | kill/s={1:F2} spawn/s={2:F2} pressure={3:F2} hp={4:F2} hits={5} alive={6}/{7}\n" +
            "                  → 판단: {8}  → 개입: {9}",
            _elapsed, killRate, spawnRate, pressure, hpRatio, hits, alive,
            enemySpawner.MaxAlive, verdict, action);

        if (_log.Count >= maxLogEntries) _log.RemoveAt(0);
        _log.Add(line);

        if (verboseLog) Debug.Log(line);
    }

    /// <summary>판단 전체를 문자열로 반환한다. 제출 문서에 그대로 붙일 수 있는 형태.</summary>
    public string GetLogText()
    {
        StringBuilder sb = new StringBuilder();
        foreach (string s in _log) sb.AppendLine(s);
        return sb.ToString();
    }

    /// <summary>테스트용: 평가 타이머를 기다리지 않고 즉시 1회 판단한다. 게임 로직에서는 쓰지 않는다.</summary>
    public void ForceEvaluate()
    {
        _evalTimer = 0f;
        Evaluate();
    }

    /// <summary>제출 문서에 붙일 수 있게 판단 전체를 한 번에 출력한다.</summary>
    [ContextMenu("판단 로그 전체 출력")]
    public void DumpLog()
    {
        if (_log.Count == 0)
        {
            Debug.Log("[AnomalyDirector] 기록된 판단 없음");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"===== AnomalyDirector 판단 로그 ({_log.Count}건) =====");
        sb.AppendLine(GetActivitySummary());
        foreach (string s in _log) sb.AppendLine(s);
        Debug.Log(sb.ToString());
    }
}
