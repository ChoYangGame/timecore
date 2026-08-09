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

    // 난입 적의 프리팹·색·이름은 EraManager.GetRandomOtherEraConfig()가 고른 시대 설정에서 가져온다.
    // 시대가 2개일 때는 "반대 시대"로 충분했지만 4개가 되면서 반대가 정의되지 않아,
    // 프리팹 목록을 여기에 따로 두지 않고 EraManager 하나만 보게 했다 (배선 중복·불일치 제거).

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
    [Tooltip("되돌릴 밀도가 없을 때 깔아주는 회복 코어. 비워두면 예전처럼 조용히 넘어간다")]
    [SerializeField] private RecoveryCoreSpawner recoveryCoreSpawner;

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

    [Header("규칙 4 — 전선 고정 (한 자리에서만 싸움)")]
    [Tooltip("비워두면 규칙 4는 조용히 비활성된다")]
    [SerializeField] private RiftZoneSpawner riftZoneSpawner;
    [Tooltip("평가 간격 동안 플레이어가 이 거리 미만으로 움직였으면 한 자리에 고착됐다고 본다")]
    [SerializeField] private float campMoveDistance = 4f;
    [Tooltip("연속 몇 번 고착 판정이 나와야 개입할지. 5초 간격이므로 2면 약 10초")]
    [SerializeField] private int campStreak = 2;
    [Tooltip("체력이 이보다 낮으면 개입하지 않는다. 몰린 플레이어를 더 몰지 않기 위한 것")]
    [SerializeField] private float campHpRatio = 0.5f;

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

    private Vector2 _lastPlayerPos;
    private bool _playerPosKnown;
    private int _campCount;
    private float _lastMovedDistance;

    private int _evalCount;
    private int _skipWarmup, _skipCooldown, _skipGuard, _skipNoRule;
    private float _maxPressureSinceIntervention;
    private bool _autoDumped;

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

        // 판이 끝나는 모든 경로의 마지막 지점이다. Play 모드 종료와
        // 재파견(GameOverController의 씬 리로드) 양쪽에서 불린다.
        AutoDump();
    }

    // 에디터 Play 종료 시 OnDestroy보다 먼저 불린다. 씬 리로드에는 안 불리므로
    // 이것만으로는 부족하고, 빌드에서 종료 시 OnDestroy가 생략되는 경우를 위한 이중 안전장치다.
    private void OnApplicationQuit() => AutoDump();

    /// <summary>인스펙터 우클릭을 잊어도 요약이 남도록 판당 1회 자동 출력한다.</summary>
    private void AutoDump()
    {
        if (_autoDumped) return;
        _autoDumped = true;
        DumpLog();
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

        // 규칙 4의 "한 자리 고착"은 개입 쿨다운과 무관하게 매 평가마다 센다.
        // 쿨다운 뒤에서 세면 20초에 한 번씩만 표본이 쌓여 연속 판정이 사실상 불가능해진다.
        bool canIntervene = CanIntervene();
        UpdateCampStreak(canIntervene);

        if (_elapsed < warmupTime) { _skipWarmup++; return; }
        if (!canIntervene) { _skipGuard++; return; }
        if (_elapsed - _lastInterventionTime < minInterventionInterval) { _skipCooldown++; return; }

        float spawnInterval = enemySpawner.SpawnInterval;
        float spawnRate = spawnInterval > 0.01f ? 1f / spawnInterval : 0f;
        float pressure = spawnRate > 0.01f ? killRate / spawnRate : 0f;
        float hpRatio = playerHealth != null && playerHealth.MaxHp > 0f
            ? playerHealth.CurrentHp / playerHealth.MaxHp : 1f;
        int alive = enemySpawner.AliveCount;
        float aliveRatio = enemySpawner.MaxAlive > 0 ? (float)alive / enemySpawner.MaxAlive : 0f;

        // --- 규칙 1: 위기 ---
        if (hpRatio < crisisHpRatio || hits >= crisisHitCount)
        {
            if (RevertDensity())
            {
                Announce("시간 이상 안정화", "균열 복구 중");
                Commit("위기", "누적 밀도 원복 (×1.00 복귀)", killRate, spawnRate, pressure, hpRatio, hits, alive);
                return;
            }

            // 되돌릴 밀도가 없으면 예전에는 아무것도 못 했다 — 디렉터가 밀도를 올린 적 없는 판에서는
            // 죽어가는 플레이어에게 해줄 게 없었다는 뜻이다. 그 구멍을 회복 코어로 메운다.
            // 다른 규칙이 전부 압박 수단이라 이것만이 유일한 구제책이다.
            if (recoveryCoreSpawner != null && recoveryCoreSpawner.Spawn())
            {
                Announce("시간 이상 감지 — 생존 위기", "균열 안정: 회복 코어 출현");
                Commit("생존 위기",
                    $"hp {hpRatio:F2} / 피격 {hits}회 → 회복 코어 배치",
                    killRate, spawnRate, pressure, hpRatio, hits, alive);
            }
            return;
        }

        // --- 규칙 2: 과잉 화력 ---
        if (pressure >= overkillPressure && hpRatio >= overkillHpRatio)
        {
            // 시대를 한 번만 뽑아 프리팹·색·배너 문구에 모두 쓴다.
            // 따로 뽑으면 "중세 개체 난입" 배너와 실제 스폰된 적이 어긋난다.
            EraManager.EraConfig intruderEra = eraManager != null ? eraManager.GetRandomOtherEraConfig() : null;
            int spawned = SpawnIntruders(intruderCount, intruderEra);

            // 난입 여력이 없으면(이미 포화) 개입 자체를 하지 않는다.
            // 아무것도 안 나왔는데 "개체 난입" 배너만 뜨면 배너와 화면이 어긋난다.
            if (spawned == 0) return;

            float before = _appliedRatio;
            ApplyDensity(intrusionDensityFactor);

            string eraName = intruderEra.eraShortName;
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

        // --- 규칙 4: 전선 고정 (적은 충분히 있는데 플레이어가 한 구역에서만 싸운다) ---
        // 앞의 세 규칙은 전부 "적을 얼마나 낼지"를 조절한다. 이 규칙만 공간을 바꾼다 —
        // 안전한 자리를 찾아 굳힌 플레이어에게 그 자리를 느린 땅으로 만들어 이동을 강제한다.
        if (_campCount >= campStreak && hpRatio >= campHpRatio && aliveRatio > stagnantAliveRatio)
        {
            // 배치에 실패하면(프리팹 미배선 등) 배너도 쿨다운도 소모하지 않는다.
            // 규칙 2가 난입 여력이 없을 때 조용히 넘어가는 것과 같은 이유다.
            if (riftZoneSpawner == null || !riftZoneSpawner.SpawnOnPlayer()) return;

            _campCount = 0;

            Announce("시간 이상 감지 — 전선 고정", "균열 발생: 시간 감속 지대");
            Commit("전선 고정",
                $"플레이어 이동 {_lastMovedDistance:F1} < {campMoveDistance:F1} ×{campStreak}회 → 감속 지대 배치",
                killRate, spawnRate, pressure, hpRatio, hits, alive);
            return;
        }

        // 어느 규칙에도 안 걸렸다. 얼마나 가까웠는지 남겨두면 임계값 조정에 쓸 수 있다.
        _skipNoRule++;
        if (pressure > _maxPressureSinceIntervention) _maxPressureSinceIntervention = pressure;
    }

    /// <summary>
    /// 직전 평가 이후 플레이어가 얼마나 움직였는지로 "한 자리 고착"을 센다.
    /// 개입할 수 없는 상황(타이틀 대기·보스전·시대 전환·증강 카드)에서는 표본이 의미가 없으므로
    /// 연속 기록을 끊는다 — 카드를 고르느라 멈춰 있던 것을 고착으로 오인하면 안 된다.
    /// </summary>
    private void UpdateCampStreak(bool canIntervene)
    {
        if (playerHealth == null)
        {
            _playerPosKnown = false;
            _campCount = 0;
            return;
        }

        Vector2 pos = playerHealth.transform.position;
        _lastMovedDistance = _playerPosKnown ? Vector2.Distance(pos, _lastPlayerPos) : float.MaxValue;
        _lastPlayerPos = pos;

        bool hadPrevious = _playerPosKnown;
        _playerPosKnown = true;

        if (!canIntervene || !hadPrevious) { _campCount = 0; return; }

        if (_lastMovedDistance < campMoveDistance) _campCount++;
        else _campCount = 0;
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

    private int SpawnIntruders(int count, EraManager.EraConfig intruderEra)
    {
        Enemy prefab = intruderEra != null ? intruderEra.enemyPrefab : null;
        if (prefab == null) return 0;

        // 죽은 난입 적 정리. 개입 시점(최소 20초 간격)에만 도는 루프라 부담이 없다.
        for (int i = _intruders.Count - 1; i >= 0; i--)
            if (_intruders[i] == null) _intruders.RemoveAt(i);

        int room = maxConcurrentIntruders - _intruders.Count;
        if (room <= 0) return 0;
        count = Mathf.Min(count, room);

        // 외형은 난입한 시대의 색, 체력은 현재 시대 기준으로 맞춘다.
        // 난입은 과잉 화력에 대한 압박 수단이라, 초반 시대 적이 튀어나온다고 후반에 무해해지면 규칙이 무의미해진다.
        Color tinted = Color.Lerp(intruderEra.enemyColor, intruderTint, intruderTintStrength);
        EraManager.EraConfig current = eraManager != null ? eraManager.CurrentConfig : null;
        float hpMultiplier = current != null ? current.enemyHpMultiplier : 1f;

        // 속도도 HP와 같은 이유로 '현재' 시대 기준이다 — 원시 적이 미래에 난입했을 때
        // 혼자 느리면 압박 수단으로서 무의미해진다. 0 가드는 인스펙터에서 비워 둔 시대 대비다.
        float speedMultiplier = current != null && current.enemySpeedMultiplier > 0f
            ? current.enemySpeedMultiplier
            : 1f;

        // 난입한 시대의 아트를 그대로 입힌다. 아트가 있으면 tinted는 화면에 안 나오고 파편 색으로만 남는데,
        // "다른 시대에서 왔다"는 신호는 색보다 스프라이트 자체가 더 분명하게 준다.
        Sprite intruderSprite = intruderEra.enemySprite;

        // 걷기 프레임도 같이 넘긴다. 안 넘기면 난입한 놈만 멈춰 선 그림이라
        // "저건 왜 안 움직이지"로 읽힌다.
        Sprite[] intruderFrames = intruderEra.enemyWalkFrames;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = enemySpawner.GetInFieldSpawnPoint();

            // 일반 스폰과 같은 예고 표식을 쓴다. 난입은 한 번에 4마리가 나오므로
            // 예고 없이 필드 안에서 터지면 피할 방법이 없다.
            if (!enemySpawner.SpawnWithPortal(prefab, pos, tinted,
                    e => Dress(e, tinted, hpMultiplier, intruderSprite, intruderFrames, speedMultiplier)))
            {
                // 표식 프리팹이 없으면 예전처럼 가장자리에서 즉시 스폰한다.
                Enemy e = Instantiate(prefab, enemySpawner.GetArenaEdgeSpawnPoint(), Quaternion.identity);
                Dress(e, tinted, hpMultiplier, intruderSprite, intruderFrames, speedMultiplier);
            }
        }
        return count;
    }

    /// <summary>난입 적의 외형·체력·추적 목록 등록. 표식이 열린 뒤에 불릴 수 있어 따로 뺐다.</summary>
    private void Dress(Enemy e, Color tinted, float hpMultiplier, Sprite sprite, Sprite[] frames,
        float speedMultiplier)
    {
        if (e == null) return;

        // 난입 적은 OnEnemySpawned를 타지 않는다(SpawnWithPortal이 콜백만 부른다).
        // 그래서 능력치를 여기서 직접 걸어야 하고, 이중 적용 걱정도 없다.
        e.ApplySpeedMultiplier(speedMultiplier);

        Health h = e.GetComponent<Health>();
        if (h != null)
        {
            // SetAppearance를 써야 첫 피격 플래시 후 프리팹 색으로 되돌아가지 않는다.
            Sprite still = sprite != null ? sprite
                : (frames != null && frames.Length > 0 ? frames[0] : null);
            h.SetAppearance(still, tinted);
            if (!Mathf.Approximately(hpMultiplier, 1f)) h.SetMaxHp(h.MaxHp * hpMultiplier);
        }

        // SetAppearance가 sprite를 덮어쓰므로 반드시 그 뒤에 넘긴다.
        SpriteWalkAnimator walk = e.GetComponent<SpriteWalkAnimator>();
        if (walk != null) walk.SetFrames(frames);

        e.transform.localScale *= intruderScale;

        _intruders.Add(e);
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
        // 개입 0건일 때야말로 "왜 안 했는지"(건너뜀 내역)가 필요하므로 조기 반환하지 않는다.
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"===== AnomalyDirector 판단 로그 (개입 {_log.Count}건 / 경과 {_elapsed:F1}s) =====");
        sb.AppendLine(GetActivitySummary());
        if (_log.Count == 0) sb.AppendLine("(개입 없음 — 위 건너뜀 내역이 그 이유다)");
        foreach (string s in _log) sb.AppendLine(s);
        Debug.Log(sb.ToString());
    }
}
