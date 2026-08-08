using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 시대 보스. 프리팹은 하나뿐이고, 시대 인덱스로 고른 패턴 세트 3개를 순서대로 돌린다.
/// (원시 고대의 포식자 / 중세 강철의 심문관 / 현대 강화 병기 / 미래 시간의 지배자)
///
/// 패턴은 전부 이미 있는 기믹 프리팹(HazardBeam / HomingHazard / RiftVent / RiftZone / BossProjectile)의
/// 조합이다. 보스 전용 프리팹이나 스프라이트를 하나도 늘리지 않았다 — WebGL 빌드 용량 증가 0이 목적이다.
/// 12개 패턴을 12개 코루틴으로 쓰면 D-2에 관리가 안 되므로, 종류(Kind) 8개짜리 스위치 하나에
/// 파라미터만 시대별로 다르게 넣는 구조로 짰다.
///
/// 기믹 프리팹 참조는 씬의 EraHazardSpawner / RiftZoneSpawner에서 런타임에 빌려온다.
/// 보스 프리팹에 같은 참조를 또 꽂으려면 프리팹을 외부 편집해야 하는데(에디터 모달 위험) 이득이 없다.
///
/// 부착 대상: Boss 프리팹 (Health, BoxCollider2D isTrigger = true, Rigidbody2D Kinematic, 태그 Enemy)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class Boss : MonoBehaviour
{
    public enum PatternKind
    {
        /// <summary>연속 돌진. 예고 후 직선으로 밀고 들어온다</summary>
        ChargeDash,
        /// <summary>제자리 방사 탄막. 링을 여러 겹 쏘면 각도가 돌아간다</summary>
        RadialBurst,
        /// <summary>플레이어를 조준한 연사</summary>
        AimedVolley,
        /// <summary>예고 직선(HazardBeam) 배치</summary>
        BeamStrike,
        /// <summary>따라오다 굳어 터지는 장판(HomingHazard)</summary>
        HomingField,
        /// <summary>방사탄을 뿜는 분출구(RiftVent) 설치</summary>
        VentDeploy,
        /// <summary>감속 지대(RiftZone) 설치</summary>
        SlowField,
        /// <summary>순간이동 후 즉시 방사. 반복</summary>
        BlinkStrike,
    }

    public enum BeamShape
    {
        /// <summary>보스를 중심으로 사방에 뻗는다. 막대가 양방향이라 실제 갈래는 2배다</summary>
        RadialFromSelf,
        /// <summary>한 축을 골라 나란히</summary>
        Parallel,
        /// <summary>직교 2줄</summary>
        Cross,
        /// <summary>가로·세로 반씩</summary>
        Grid,
    }

    [Serializable]
    public class Pattern
    {
        public string label = "패턴";
        public PatternKind kind = PatternKind.ChargeDash;

        [Tooltip("시전 시간(초). 보스가 멈추고 커졌다 작아진다 — 뭐가 올지 읽을 시간을 주는 구간")]
        public float telegraph = 0.8f;

        [Tooltip("패턴이 끝난 뒤 다음 패턴까지의 휴식(초)")]
        public float recover = 1.4f;

        [Tooltip("패턴이 끝난 뒤 보스 자리에서 추가로 뿜는 방사탄 수. 0이면 없음")]
        public int followUpRadial = 0;

        [Header("ChargeDash")]
        public int dashCount = 1;
        public float dashDuration = 0.8f;
        public float dashSpeed = 10f;
        [Tooltip("돌진과 돌진 사이 재조준 시간")]
        public float dashGap = 0.45f;

        [Header("RadialBurst / BlinkStrike")]
        public int burstCount = 8;
        [Tooltip("연속으로 쏘는 링 수")]
        public int burstRings = 1;
        public float ringDelay = 0.25f;
        [Tooltip("링마다 이만큼 각도를 돌린다. 같은 틈에 계속 서 있지 못하게 한다")]
        public float ringSpin = 15f;

        [Header("AimedVolley")]
        public int volleyShots = 5;
        public float volleyInterval = 0.3f;
        [Tooltip("한 발마다 부채꼴로 몇 갈래인지")]
        public int shotsPerVolley = 1;
        public float spreadDeg = 0f;

        [Header("BeamStrike")]
        public BeamShape beamShape = BeamShape.Parallel;
        [Tooltip("빔 생김새. 시대 기믹과 같은 그림을 써야 보스 패턴도 그 시대 것으로 읽힌다")]
        public HazardBeam.BeamStyle beamStyle = HazardBeam.BeamStyle.Fissure;
        public int beamCount = 3;
        [Tooltip("같은 패턴 안에서 몇 세트를 연달아 까는지")]
        public int beamSets = 1;
        public float beamSetDelay = 0.85f;
        public float beamWarn = 1.2f;
        public float beamFire = 0.45f;
        public float beamWidth = 1.8f;
        public float beamDamage = 16f;
        [Tooltip("줄과 줄 사이 최소 간격(월드 유닛)")]
        public float beamMinGap = 3f;

        [Header("HomingField")]
        public int homingCount = 2;
        public float homingSize = 3.6f;
        public float homingTrack = 1.8f;
        public float homingLock = 0.7f;
        [Tooltip("따라오는 속도. 플레이어 기본 이동속도는 6이다 — 이보다 느려야 떼어낼 수 있다")]
        public float homingSpeed = 3.6f;
        public float homingDamage = 16f;

        [Header("VentDeploy")]
        public int ventCount = 2;
        public float ventLifetime = 6f;
        public int ventBurst = 6;
        public float ventSpin = 16f;
        public float ventInterval = 1.5f;

        [Header("SlowField")]
        public int zoneCount = 3;
        public float zoneSize = 5.5f;

        [Header("BlinkStrike")]
        public int blinkCount = 3;
        [Tooltip("순간이동 후 플레이어와 유지할 거리")]
        public float blinkDistance = 4.5f;
        public float blinkGap = 0.5f;
    }

    [Serializable]
    public class PatternSet
    {
        [Tooltip("어느 보스의 세트인지 알아보기 위한 표시용 이름. 로직에는 쓰이지 않는다")]
        public string bossLabel = "고대의 포식자";
        public Pattern[] patterns;
    }

    [SerializeField] private float moveSpeed = 1.3f;
    [SerializeField] private float contactDamage = 20f;
    [Tooltip("접촉 데미지 재적용까지의 간격(초)")]
    [SerializeField] private float contactCooldown = 0.7f;
    [Tooltip("시전 중 이동속도 배율. 0이면 완전히 멈춘다")]
    [SerializeField] private float castingMoveScale = 0.25f;
    [Tooltip("등장 후 첫 패턴까지의 여유. 배너와 겹치지 않게 한다")]
    [SerializeField] private float openingDelay = 1.6f;
    [Tooltip("돌진 잔상을 남기는 간격(초). 매 프레임 남기면 조각 풀이 바닥난다")]
    [SerializeField] private float trailInterval = 0.045f;

    [Tooltip("모든 패턴이 공유하는 탄. 방사·조준·분출구가 전부 이걸 쓴다")]
    [SerializeField] private BossProjectile projectilePrefab;

    [Header("사망 시 드롭")]
    [SerializeField] private ExpOrb expOrbPrefab;
    [SerializeField] private int expOrbDropCount = 10;
    [SerializeField] private float dropScatterRadius = 1f;

    [Header("시대별 패턴 세트 — 배열 인덱스 = EraManager.Era enum 값")]
    [Tooltip("보스마다 3개씩. 인스펙터에서 비워두면 코드 기본값이 그대로 쓰인다")]
    [SerializeField]
    private PatternSet[] patternSets =
    {
        // ── 원시: 고대의 포식자 — 붙어서 짓밟는 보스. 거리를 벌리는 게 답이다.
        new PatternSet
        {
            bossLabel = "고대의 포식자",
            patterns = new[]
            {
                new Pattern
                {
                    label = "광폭 돌진", kind = PatternKind.ChargeDash,
                    telegraph = 0.9f, recover = 1.5f,
                    dashCount = 3, dashDuration = 0.55f, dashSpeed = 12f, dashGap = 0.45f,
                },
                new Pattern
                {
                    // 보스 발밑에서 사방으로 갈라지는 균열. 막대가 양방향이라 4줄 = 8갈래다.
                    label = "대지 분쇄", kind = PatternKind.BeamStrike,
                    telegraph = 0.7f, recover = 1.6f, followUpRadial = 8,
                    beamShape = BeamShape.RadialFromSelf, beamStyle = HazardBeam.BeamStyle.Fissure,
                    beamCount = 4, beamWarn = 1.1f, beamFire = 0.4f,
                    beamWidth = 1.9f, beamDamage = 16f,
                },
                new Pattern
                {
                    label = "포식자의 추격", kind = PatternKind.HomingField,
                    telegraph = 0.6f, recover = 2f,
                    homingCount = 2, homingSize = 3.6f, homingTrack = 1.9f,
                    homingSpeed = 3.8f, homingDamage = 16f,
                },
            },
        },

        // ── 중세: 강철의 심문관 — 거리를 두고 찍어 누르는 보스. 옆으로 계속 흘려야 한다.
        new PatternSet
        {
            bossLabel = "강철의 심문관",
            patterns = new[]
            {
                new Pattern
                {
                    label = "화살 세례", kind = PatternKind.BeamStrike,
                    telegraph = 0.6f, recover = 1.3f,
                    beamShape = BeamShape.Parallel, beamStyle = HazardBeam.BeamStyle.Volley,
                    beamCount = 4, beamWarn = 1.25f, beamFire = 0.4f,
                    beamWidth = 1.5f, beamDamage = 15f,
                },
                new Pattern
                {
                    label = "심문의 창", kind = PatternKind.AimedVolley,
                    telegraph = 0.8f, recover = 1.4f,
                    volleyShots = 5, volleyInterval = 0.32f, shotsPerVolley = 3, spreadDeg = 16f,
                },
                new Pattern
                {
                    // 큰 낙인 하나가 천천히 따라붙고, 굳는 사이 보스가 탄을 퍼뜨린다.
                    label = "처형 낙인", kind = PatternKind.HomingField,
                    telegraph = 0.7f, recover = 1.7f, followUpRadial = 10,
                    homingCount = 1, homingSize = 5f, homingTrack = 2.2f,
                    homingLock = 0.8f, homingSpeed = 3.4f, homingDamage = 18f,
                },
            },
        },

        // ── 현대: 강화 병기 — 화력으로 구역을 지운다. 안전지대가 계속 옮겨 다닌다.
        new PatternSet
        {
            bossLabel = "강화 병기",
            patterns = new[]
            {
                new Pattern
                {
                    label = "십자 폭격", kind = PatternKind.BeamStrike,
                    telegraph = 0.6f, recover = 1.3f,
                    beamShape = BeamShape.Cross, beamStyle = HazardBeam.BeamStyle.Bombardment,
                    beamCount = 2, beamSets = 2, beamSetDelay = 0.85f,
                    beamWarn = 1f, beamFire = 0.4f, beamWidth = 1.8f, beamDamage = 17f,
                },
                new Pattern
                {
                    label = "기관포 소사", kind = PatternKind.AimedVolley,
                    telegraph = 0.7f, recover = 1.5f,
                    volleyShots = 10, volleyInterval = 0.17f, shotsPerVolley = 1, spreadDeg = 7f,
                },
                new Pattern
                {
                    label = "포탑 전개", kind = PatternKind.VentDeploy,
                    telegraph = 0.8f, recover = 2.2f,
                    ventCount = 2, ventLifetime = 6f, ventBurst = 6, ventSpin = 16f,
                },
            },
        },

        // ── 미래: 시간의 지배자 — 이동 자체를 봉인한다. 느려진 채로 탄을 읽어야 한다.
        new PatternSet
        {
            bossLabel = "시간의 지배자",
            patterns = new[]
            {
                new Pattern
                {
                    label = "시간 정지 영역", kind = PatternKind.SlowField,
                    telegraph = 0.8f, recover = 1.7f, followUpRadial = 12,
                    // 걷는 영역이 약 21.6 x 10.9다. 5.0짜리 3장을 x축으로 갈라 놓으면
                    // 가로는 대부분 덮이지만 세로로는 반드시 빈칸이 남는다 — 도망갈 곳은 있어야 한다.
                    zoneCount = 3, zoneSize = 5f,
                },
                new Pattern
                {
                    label = "위상 도약", kind = PatternKind.BlinkStrike,
                    telegraph = 0.5f, recover = 1.5f,
                    blinkCount = 3, blinkDistance = 4.5f, blinkGap = 0.5f,
                    burstCount = 8, ringSpin = 22f,
                },
                new Pattern
                {
                    label = "시간 균열 격자", kind = PatternKind.BeamStrike,
                    telegraph = 0.6f, recover = 1.6f,
                    beamShape = BeamShape.Grid, beamStyle = HazardBeam.BeamStyle.Laser,
                    beamCount = 4, beamSets = 2, beamSetDelay = 1f,
                    beamWarn = 0.95f, beamFire = 0.4f, beamWidth = 1.5f, beamDamage = 19f,
                },
            },
        },
    };

    /// <summary>지금 돌고 있는 패턴 이름. 실측·디버그용.</summary>
    public string CurrentPatternLabel { get; private set; } = "-";

    private Health _health;
    private Transform _player;
    private float _nextContactTime;

    private bool _isDashing;
    private Vector2 _dashDirection;
    private float _dashSpeed;
    private bool _isCasting;
    private Vector3 _baseScale;
    private float _nextTrailTime;

    // 씬에서 빌려오는 기믹 프리팹. 없으면 해당 패턴만 조용히 건너뛴다.
    private HazardBeam _beamPrefab;
    private HomingHazard _homingPrefab;
    private RiftVent _ventPrefab;
    private RiftZone _zonePrefab;

    private int _eraIndex;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _health.OnDeath += HandleDeath;
        _baseScale = transform.localScale;
    }

    private void OnDestroy()
    {
        if (_health != null) _health.OnDeath -= HandleDeath;
    }

    /// <summary>WaveManager가 Instantiate 직후(= Start 전에) 부른다. 어느 보스의 패턴을 쓸지 정한다.</summary>
    public void ConfigureEra(int eraIndex)
    {
        _eraIndex = eraIndex;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _player = player.transform;

        BorrowGimmickPrefabs();
        StartCoroutine(PatternLoop());
    }

    /// <summary>
    /// 기믹 프리팹은 씬의 스포너가 이미 들고 있다. 보스 프리팹에 중복으로 꽂지 않는 이유는
    /// 프리팹 외부 편집을 피하기 위해서다(에디터 모달로 세션이 막힌 전례가 있다).
    /// </summary>
    private void BorrowGimmickPrefabs()
    {
        EraHazardSpawner hazards = FindFirstObjectByType<EraHazardSpawner>();
        if (hazards != null)
        {
            _beamPrefab = hazards.BeamPrefab;
            _homingPrefab = hazards.HomingPrefab;
            _ventPrefab = hazards.VentPrefab;
            if (projectilePrefab == null) projectilePrefab = hazards.VentProjectilePrefab;
        }

        RiftZoneSpawner zones = FindFirstObjectByType<RiftZoneSpawner>();
        if (zones != null) _zonePrefab = zones.ZonePrefab;
    }

    private void Update()
    {
        if (_health.IsDead || _player == null) return;

        Vector2 moveDir;
        float speed;

        if (_isDashing)
        {
            moveDir = _dashDirection;
            speed = _dashSpeed;
        }
        else
        {
            Vector3 toPlayer = _player.position - transform.position;
            float sqr = toPlayer.sqrMagnitude;
            if (sqr < 0.0001f) return;
            moveDir = toPlayer / Mathf.Sqrt(sqr);
            speed = _isCasting ? moveSpeed * castingMoveScale : moveSpeed;
        }

        Vector3 next = transform.position + (Vector3)(moveDir * (speed * Time.deltaTime));

        // 돌진 3연타가 붙으면서 보스가 화면 밖으로 나가버리는 경우가 생긴다. 아레나 안에 붙잡아 둔다.
        if (ArenaBounds.Instance != null) next = ArenaBounds.Instance.Clamp(next);

        transform.position = next;

        if (_isDashing) EmitDashTrail();
    }

    /// <summary>
    /// 돌진 잔상. 매 프레임 찍으면 저사양에서 조각 풀이 순식간에 바닥나므로 간격을 두고 남긴다.
    /// 제자리에서 알파만 빠지는 Linger라 잔상이 흐르지 않고 지나온 자리에 그대로 남는다.
    /// </summary>
    private void EmitDashTrail()
    {
        if (Time.time < _nextTrailTime) return;
        _nextTrailTime = Time.time + trailInterval;

        EffectSystem.Linger(transform.position, Color.Lerp(_health.BaseColor, Color.black, 0.25f),
            _baseScale.x * 0.85f, 0.28f);
    }

    // ────────────────────────────── 패턴 루프 ──────────────────────────────

    private Pattern[] CurrentPatterns()
    {
        if (patternSets == null || patternSets.Length == 0) return null;
        PatternSet set = patternSets[Mathf.Clamp(_eraIndex, 0, patternSets.Length - 1)];
        return set != null && set.patterns != null && set.patterns.Length > 0 ? set.patterns : null;
    }

    private IEnumerator PatternLoop()
    {
        yield return new WaitForSeconds(openingDelay);

        Pattern[] patterns = CurrentPatterns();
        if (patterns == null)
        {
            // 세트가 비어 있어도 보스가 가만히 서 있으면 안 된다. 기본 방사만이라도 돌린다.
            while (!_health.IsDead)
            {
                FireRadial(8, 0f);
                yield return new WaitForSeconds(3f);
            }
            yield break;
        }

        int index = 0;
        while (!_health.IsDead)
        {
            Pattern p = patterns[index % patterns.Length];
            index++;

            CurrentPatternLabel = p.label;

            yield return Telegraph(p.telegraph);
            if (_health.IsDead) yield break;

            yield return RunPattern(p);
            if (_health.IsDead) yield break;

            if (p.followUpRadial > 0) FireRadial(p.followUpRadial, UnityEngine.Random.Range(0f, 30f));

            yield return new WaitForSeconds(p.recover);
        }
    }

    /// <summary>
    /// 시전 예고. 보스가 거의 멈추고 크기가 부풀었다 돌아온다.
    /// 색을 건드리지 않는 이유: Health의 피격 플래시가 같은 SpriteRenderer 색을 쓰고 있어 서로 덮어쓴다.
    /// </summary>
    private IEnumerator Telegraph(float duration)
    {
        if (duration <= 0f) yield break;

        _isCasting = true;

        // 느리게 번지는 링. 흩어지는 Burst와 달리 "지금 뭔가 모으고 있다"로 읽힌다.
        EffectSystem.Ring(transform.position, Color.Lerp(_health.BaseColor, Color.white, 0.4f),
            10, 3.2f, 0.26f, Mathf.Min(0.5f, duration));

        float t = 0f;
        while (t < duration && !_health.IsDead)
        {
            t += Time.deltaTime;
            float pulse = 1f + 0.12f * Mathf.Sin(t / Mathf.Max(0.0001f, duration) * Mathf.PI);
            transform.localScale = _baseScale * pulse;
            yield return null;
        }

        transform.localScale = _baseScale;
        _isCasting = false;
    }

    private IEnumerator RunPattern(Pattern p)
    {
        switch (p.kind)
        {
            case PatternKind.ChargeDash: return PatternChargeDash(p);
            case PatternKind.RadialBurst: return PatternRadialBurst(p);
            case PatternKind.AimedVolley: return PatternAimedVolley(p);
            case PatternKind.BeamStrike: return PatternBeamStrike(p);
            case PatternKind.HomingField: return PatternHomingField(p);
            case PatternKind.VentDeploy: return PatternVentDeploy(p);
            case PatternKind.SlowField: return PatternSlowField(p);
            case PatternKind.BlinkStrike: return PatternBlinkStrike(p);
            default: return PatternRadialBurst(p);
        }
    }

    // ────────────────────────────── 개별 패턴 ──────────────────────────────

    private IEnumerator PatternChargeDash(Pattern p)
    {
        int count = Mathf.Max(1, p.dashCount);

        for (int i = 0; i < count; i++)
        {
            if (_health.IsDead || _player == null) yield break;

            Vector3 toPlayer = _player.position - transform.position;
            _dashDirection = toPlayer.sqrMagnitude > 0.0001f ? ((Vector2)toPlayer).normalized : Vector2.right;
            _dashSpeed = p.dashSpeed;
            _isDashing = true;

            CameraShake.Shake(0.25f, 0.12f);
            // 박차고 나가는 방향의 반대로 흙을 찬다. 어느 쪽으로 돌진하는지가 조각으로도 읽힌다.
            EffectSystem.Spray(transform.position, -_dashDirection, _health.BaseColor,
                7, 70f, 7f, 0.28f, 0.32f);

            yield return new WaitForSeconds(p.dashDuration);
            _isDashing = false;

            if (i < count - 1) yield return new WaitForSeconds(p.dashGap);
        }
    }

    private IEnumerator PatternRadialBurst(Pattern p)
    {
        int rings = Mathf.Max(1, p.burstRings);

        for (int i = 0; i < rings; i++)
        {
            if (_health.IsDead) yield break;

            FireRadial(p.burstCount, p.ringSpin * i);
            if (i < rings - 1) yield return new WaitForSeconds(p.ringDelay);
        }
    }

    private IEnumerator PatternAimedVolley(Pattern p)
    {
        int shots = Mathf.Max(1, p.volleyShots);
        int branches = Mathf.Max(1, p.shotsPerVolley);

        for (int i = 0; i < shots; i++)
        {
            if (_health.IsDead || _player == null) yield break;

            Vector2 toPlayer = _player.position - transform.position;
            float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            // 갈래가 1개면 정확히 조준, 여러 개면 부채꼴 중앙이 조준선이 된다.
            float step = branches > 1 ? p.spreadDeg : 0f;
            float start = baseAngle - step * (branches - 1) * 0.5f;

            for (int b = 0; b < branches; b++)
            {
                // 단발 연사는 조준선 자체를 흔들어 "정확히 겹쳐 오는" 지루함을 없앤다.
                float jitter = branches > 1 ? 0f : UnityEngine.Random.Range(-p.spreadDeg, p.spreadDeg) * 0.5f;
                LaunchAt(start + step * b + jitter);
            }

            if (i < shots - 1) yield return new WaitForSeconds(p.volleyInterval);
        }
    }

    private IEnumerator PatternBeamStrike(Pattern p)
    {
        if (_beamPrefab == null || ArenaBounds.Instance == null) yield break;

        int sets = Mathf.Max(1, p.beamSets);

        for (int s = 0; s < sets; s++)
        {
            if (_health.IsDead) yield break;

            SpawnBeamSet(p);
            if (s < sets - 1) yield return new WaitForSeconds(p.beamSetDelay);
        }

        // 마지막 세트의 예고가 끝날 때까지는 다음 패턴으로 넘어가지 않는다.
        // 안 기다리면 예고 막대가 살아 있는 채로 다음 예고가 겹쳐 화면을 읽을 수 없다.
        yield return new WaitForSeconds(p.beamWarn + p.beamFire);
    }

    private void SpawnBeamSet(Pattern p)
    {
        Rect arena = ArenaBounds.Instance.Rect;
        float length = Mathf.Sqrt(arena.width * arena.width + arena.height * arena.height);
        Color color = _health.BaseColor;

        int count = Mathf.Max(1, p.beamCount);
        bool verticalFirst = UnityEngine.Random.value < 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle;
            Vector2 center;
            ResolveBeamPlacement(p, arena, i, count, verticalFirst, out angle, out center);

            HazardBeam beam = Instantiate(_beamPrefab, center, Quaternion.identity);
            beam.Configure(new HazardBeam.Spec
            {
                center = center,
                angleDeg = angle,
                length = length,
                width = p.beamWidth,
                color = color,
                warnDuration = p.beamWarn,
                fireDuration = p.beamFire,
                playerDamage = p.beamDamage,
                // 보스전에는 잡몹이 없다. 훑을 이유가 없으니 0으로 둔다(FindObjectsByType 비용 회피).
                enemyDamage = 0f,
                style = p.beamStyle,
            });
        }
    }

    /// <summary>
    /// 배치별 각도와 중심. EraHazardSpawner와 같은 규칙이되, 보스에는
    /// 자기 발밑에서 사방으로 뻗는 RadialFromSelf가 하나 더 있다.
    /// </summary>
    private void ResolveBeamPlacement(Pattern p, Rect arena, int index, int count, bool verticalFirst,
        out float angle, out Vector2 center)
    {
        switch (p.beamShape)
        {
            case BeamShape.RadialFromSelf:
            {
                // 막대는 중심에서 양방향으로 뻗는다 — 180도를 count로 나누면 갈래는 2*count가 된다.
                angle = (180f / Mathf.Max(1, count)) * index + UnityEngine.Random.Range(-6f, 6f);
                center = transform.position;
                return;
            }

            case BeamShape.Cross:
            {
                bool vertical = (index % 2 == 0) == verticalFirst;
                angle = vertical ? 90f : 0f;
                center = SlotCenter(arena, vertical, index / 2, Mathf.Max(1, count / 2), p.beamMinGap);
                return;
            }

            case BeamShape.Grid:
            {
                int half = Mathf.Max(1, count / 2);
                bool vertical = index < half ? verticalFirst : !verticalFirst;
                int slot = index < half ? index : index - half;
                angle = vertical ? 90f : 0f;
                center = SlotCenter(arena, vertical, slot, half, p.beamMinGap);
                return;
            }

            default:
            {
                bool vertical = verticalFirst;
                angle = vertical ? 90f : 0f;
                center = SlotCenter(arena, vertical, index, count, p.beamMinGap);
                return;
            }
        }
    }

    /// <summary>축을 count개 구간으로 나눈 뒤 index번째 구간 안의 랜덤한 위치.</summary>
    private static Vector2 SlotCenter(Rect arena, bool vertical, int index, int count, float minGap)
    {
        float min = vertical ? arena.xMin : arena.yMin;
        float max = vertical ? arena.xMax : arena.yMax;

        float span = (max - min) / Mathf.Max(1, count);
        float slotMin = min + span * index;
        float slotMax = slotMin + span;

        float pad = Mathf.Min(minGap * 0.5f, span * 0.25f);
        float value = span > pad * 2f
            ? UnityEngine.Random.Range(slotMin + pad, slotMax - pad)
            : (slotMin + slotMax) * 0.5f;

        return vertical
            ? new Vector2(value, arena.center.y)
            : new Vector2(arena.center.x, value);
    }

    private IEnumerator PatternHomingField(Pattern p)
    {
        if (_homingPrefab == null || _player == null) yield break;

        Vector2 playerPos = _player.position;
        int count = Mathf.Max(1, p.homingCount);

        for (int i = 0; i < count; i++)
        {
            // 발밑에서 시작하면 추적 구간이 무의미하다. 조금 떨어진 곳에서 걸어오게 한다.
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * UnityEngine.Random.Range(4.5f, 7f);
            Vector2 start = ArenaBounds.Instance != null
                ? ArenaBounds.Instance.Clamp(playerPos + offset)
                : playerPos + offset;

            HomingHazard h = Instantiate(_homingPrefab, start, Quaternion.identity);
            h.Configure(new HomingHazard.Spec
            {
                startPosition = start,
                size = p.homingSize,
                trackDuration = p.homingTrack,
                lockDuration = p.homingLock,
                fireDuration = 0.35f,
                trackSpeed = p.homingSpeed,
                playerDamage = p.homingDamage,
                color = _health.BaseColor,
            });
        }

        // 굳어서 터질 때까지 기다린다. followUpRadial이 그 순간에 겹쳐 들어가는 것이 이 패턴의 노림수다.
        yield return new WaitForSeconds(p.homingTrack + p.homingLock);
    }

    private IEnumerator PatternVentDeploy(Pattern p)
    {
        if (_ventPrefab == null || ArenaBounds.Instance == null) yield break;

        Rect arena = ArenaBounds.Instance.Rect;
        Vector2 playerPos = _player != null ? (Vector2)_player.position : arena.center;
        int count = Mathf.Max(1, p.ventCount);

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = arena.center;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 candidate = new Vector2(
                    UnityEngine.Random.Range(arena.xMin + 1f, arena.xMax - 1f),
                    UnityEngine.Random.Range(arena.yMin + 1f, arena.yMax - 1f));

                pos = candidate;
                if ((candidate - playerPos).sqrMagnitude >= 25f) break;   // 5유닛 이상
            }

            RiftVent v = Instantiate(_ventPrefab, pos, Quaternion.identity);
            v.Configure(new RiftVent.Spec
            {
                position = pos,
                size = 1.6f,
                warnDuration = 1.1f,
                lifetime = p.ventLifetime,
                burstInterval = p.ventInterval,
                burstCount = p.ventBurst,
                spinPerBurst = p.ventSpin,
                color = _health.BaseColor,
            }, projectilePrefab);
        }

        // 분출구는 깔아 두고 보스는 바로 다음 행동으로 넘어간다. 예고 시간만 기다린다.
        yield return new WaitForSeconds(1.1f);
    }

    private IEnumerator PatternSlowField(Pattern p)
    {
        if (_zonePrefab == null || ArenaBounds.Instance == null) yield break;

        Rect arena = ArenaBounds.Instance.Rect;
        float margin = p.zoneSize * 0.5f + 0.5f;
        Rect area = Rect.MinMaxRect(arena.xMin + margin, arena.yMin + margin,
                                    arena.xMax - margin, arena.yMax - margin);

        int count = Mathf.Max(1, p.zoneCount);

        for (int i = 0; i < count; i++)
        {
            // 아레나를 count 구간으로 갈라 하나씩 놓는다. 무작위로 두면 세 장이 한쪽에 겹쳐
            // 나머지 절반이 완전 안전지대가 된다.
            float spanX = Mathf.Max(0f, area.width) / count;
            float x = area.xMin + spanX * (i + 0.5f);
            float y = area.height > 0f ? UnityEngine.Random.Range(area.yMin, area.yMax) : area.center.y;

            Vector2 pos = new Vector2(Mathf.Clamp(x, area.xMin, area.xMax), y);

            RiftZone zone = Instantiate(_zonePrefab, pos, Quaternion.identity);
            zone.Configure(_health.BaseColor, p.zoneSize);

            if (i < count - 1) yield return new WaitForSeconds(0.18f);
        }
    }

    private IEnumerator PatternBlinkStrike(Pattern p)
    {
        int count = Mathf.Max(1, p.blinkCount);

        for (int i = 0; i < count; i++)
        {
            if (_health.IsDead || _player == null) yield break;

            // 사라지는 자리 / 나타나는 자리 양쪽에 링을 남긴다. 어디로 갔는지 눈이 따라갈 수 있어야 한다.
            Color blinkColor = Color.Lerp(_health.BaseColor, Color.white, 0.5f);
            EffectSystem.Ring(transform.position, blinkColor, 10, 7f, 0.26f, 0.3f);
            EffectSystem.Linger(transform.position, _health.BaseColor, _baseScale.x * 0.9f, 0.25f);

            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * p.blinkDistance;
            Vector2 target = (Vector2)_player.position + offset;
            if (ArenaBounds.Instance != null) target = ArenaBounds.Instance.Clamp(target);

            transform.position = target;
            EffectSystem.Ring(target, blinkColor, 12, 9f, 0.28f, 0.32f);
            CameraShake.Shake(0.2f, 0.1f);

            FireRadial(p.burstCount, p.ringSpin * i);

            if (i < count - 1) yield return new WaitForSeconds(p.blinkGap);
        }
    }

    // ────────────────────────────── 탄 발사 ──────────────────────────────

    /// <summary>보스 자리에서 사방으로 등간격 발사. offsetDeg만큼 통째로 돌린다.</summary>
    private void FireRadial(int count, float offsetDeg)
    {
        if (projectilePrefab == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            LaunchAt((360f / count) * i + offsetDeg);
        }
    }

    private void LaunchAt(float angleDeg)
    {
        if (projectilePrefab == null) return;

        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        BossProjectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.Launch(dir);
    }

    // ────────────────────────────── 접촉 / 사망 ──────────────────────────────

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_health.IsDead) return;
        if (Time.time < _nextContactTime) return;
        if (!other.CompareTag("Player")) return;

        Health target = other.GetComponent<Health>();
        if (target == null || target.IsDead) return;

        target.TakeDamage(contactDamage);
        _nextContactTime = Time.time + contactCooldown;
    }

    private void HandleDeath(Health _)
    {
        StopAllCoroutines();
        _isDashing = false;
        _isCasting = false;

        // 필드에 남은 기믹을 전부 걷어낸다. 죽은 보스의 예고 레이저에 맞아 죽는 것은
        // 어떤 설명으로도 방어가 안 된다. 보스전에는 시대 기믹도 같이 돌고 있으므로
        // (WaveManager.BossActive) 보스 것만 골라낼 수 없고, 골라낼 이유도 없다 —
        // 보스를 잡은 순간 화면이 정리되는 편이 맞다.
        HazardBeam.ClearAll();
        HomingHazard.ClearAll();
        RiftVent.ClearAll();
        RiftZone.ClearAll();
        foreach (BossProjectile p in FindObjectsByType<BossProjectile>(FindObjectsSortMode.None))
            Destroy(p.gameObject);

        // 격파 연출. 링 3겹을 속도만 다르게 겹치면 코루틴 없이도 다단 폭발로 보인다 —
        // 오브젝트가 이 프레임에 파괴되므로 시간에 걸친 연출을 여기서 돌릴 수 없다.
        Vector2 at = transform.position;
        Color bright = Color.Lerp(_health.BaseColor, Color.white, 0.6f);

        EffectSystem.Ring(at, Color.white, 12, 14f, 0.3f, 0.35f);
        EffectSystem.Ring(at, bright, 12, 8f, 0.4f, 0.55f);
        EffectSystem.Burst(at, _health.BaseColor, 14, 5f, 0.36f, 0.8f);

        CameraShake.Shake(0.8f, 0.45f);
        ScreenFlash.Full(Color.white, 0.3f, 0.28f);

        // 완전 정지가 아니라 슬로우모션. 마지막 한 방이 들어간 순간을 눈으로 확인할 시간을 준다.
        Hitstop.Do(0.35f, 0.2f);

        if (GameManager.Instance != null) GameManager.Instance.RegisterKill();

        if (expOrbPrefab != null)
        {
            for (int i = 0; i < expOrbDropCount; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * dropScatterRadius;
                Instantiate(expOrbPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            }
        }

        Destroy(gameObject);
    }
}
