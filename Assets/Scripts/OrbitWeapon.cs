using UnityEngine;

/// <summary>
/// 매지션. 몸 주위를 도는 코어가 닿는 적에게 지속 피해를 준다.
///
/// 총잡이·칼잡이와 갈리는 지점은 **조준이 없다는 것**이다.
/// 쏘지도 휘두르지도 않고, 코어가 늘 돌고 있어 "어디에 서 있느냐"가 곧 공격이다.
/// 적 무리 사이를 훑고 지나가는 플레이가 최적해가 된다.
///
/// 콜라이더를 쓰지 않는다. 코어마다 자기 타격 주기를 갖고, 그 순간에만
/// 반경 안의 적을 훑는다 — 코어 3개 × 적 40마리라도 주기당 120번 비교다.
/// 적별 쿨다운 표를 두지 않는 이유: 코어가 지나가는 자리에 계속 서 있으면
/// 계속 맞는 것이 맞고, 표를 들면 코어 수만큼 딕셔너리가 늘어난다.
///
/// 부착 대상: Player
/// </summary>
[DisallowMultipleComponent]
public class OrbitWeapon : PlayerWeapon
{
    [Tooltip("코어 하나가 피해를 주는 간격(초). FireRate 증강이 이 값을 줄인다")]
    [SerializeField] private float fireInterval = 0.4f;

    [SerializeField] private float damage = 9f;

    [Tooltip("시작 코어 수. 증강으로 늘어난다")]
    [SerializeField] private int baseOrbCount = 2;

    [Tooltip("코어 수 상한. 저사양 보호 — 코어마다 SpriteRenderer가 하나씩 붙는다")]
    [SerializeField] private int maxOrbCount = 6;

    [Tooltip("공전 반경(월드 유닛)")]
    [SerializeField] private float orbitRadius = 1.7f;

    [Tooltip("공전 속도(도/초)")]
    [SerializeField] private float orbitSpeed = 150f;

    [Tooltip("코어 하나가 적을 때리는 반경. **보이는 코어 크기가 이 값에서 역산된다** —\n" +
             "따로 조절하는 값이 아니라 이것만 바꾸면 그림도 같이 커진다")]
    [SerializeField] private float hitRadius = 0.7f;

    [Tooltip("연쇄 붕괴 증강이 켜는 광역 피해 반경. 0이면 꺼진 상태")]
    [SerializeField] private float blastRadius = 1.6f;

    [SerializeField] private Color orbColor = new Color(0.435f, 0.847f, 0.878f, 1f);

    // kenney_magic_05는 부드러운 글로우라 임계값에 따라 크기가 크게 달라진다(픽셀 실측):
    //   알파 16 → 0.81 / 알파 64 → 0.63 / 알파 128 → 0.42
    // 알파 16은 거의 안 보이는 헤일로까지 포함해서, 그 기준으로 맞추면
    // "눈에는 안 닿았는데 피해가 들어간다"가 그대로 남는다.
    // 눈에 확실히 보이는 경계인 알파 64를 쓴다.
    private const float OrbOpaqueRatio = 0.63f;

    /// <summary>
    /// 보이는 코어가 판정 반경과 정확히 같아지는 스케일.
    ///
    /// 예전에는 orbSize(0.95)와 hitRadius(0.85)가 따로 놀아서
    /// 보이는 반지름 0.385 대 판정 반지름 0.85 — **2.2배** 차이가 났다.
    /// "구체에 안 닿았는데 피해가 들어간다"의 정체가 이것이다.
    /// 이제 판정을 바꾸면 그림이 따라오므로 둘이 어긋날 수가 없다.
    /// </summary>
    private float OrbVisualScale => hitRadius * HitRadiusMultiplier * 2f / OrbOpaqueRatio;

    [Tooltip("코어가 잔상을 남기는 간격(초). 코어만으로는 배경에 묻혀 '뭐가 돌고 있는지' 안 읽힌다.\n" +
             "0으로 두면 잔상을 끈다")]
    [SerializeField] private float trailInterval = 0.06f;

    public override PlayerClass Class => PlayerClass.Mage;

    public override float FireInterval
    {
        get => fireInterval;
        set => fireInterval = value;
    }

    /// <summary>증강으로 늘어나는 코어 수. 실제 개수는 maxOrbCount로 상한이 걸린다.</summary>
    public int ExtraOrbs { get; set; }

    /// <summary>증강으로 늘어나는 공전 반경 배율.</summary>
    public float RadiusMultiplier { get; set; } = 1f;

    /// <summary>증강으로 늘어나는 공전 속도 배율. 같은 자리를 더 자주 훑는다.</summary>
    public float SpeedMultiplier { get; set; } = 1f;

    /// <summary>증강으로 늘어나는 코어 타격 반경 배율.</summary>
    public float HitRadiusMultiplier { get; set; } = 1f;

    /// <summary>연쇄 붕괴 증강. 코어가 때린 자리 주변 적에게도 절반 피해가 간다.</summary>
    public bool Blast { get; set; }

    private Transform[] _orbTf;
    private SpriteRenderer[] _orbSr;
    private float[] _hitTimer;

    /// <summary>코어별 직전 타격 각도(도). 이번 각도와 묶어 '쓸고 지나간 부채꼴'로 판정한다.</summary>
    private float[] _lastHitAngle;
    private bool[] _hasLastPos;
    private float _angle;
    private int _activeCount = -1;
    private float _trailTimer;
    private float _appliedScale = -1f;

    protected override void Awake()
    {
        base.Awake();
        BuildOrbs();
    }

    /// <summary>
    /// 다른 직업으로 갈아탈 때(ClassSelectController.ApplyWeapon) 이 컴포넌트가 꺼진다.
    /// 꺼지면 Update가 멈추므로 코어를 여기서 직접 숨기지 않으면 화면에 그대로 남는다.
    /// _activeCount를 -1로 되돌려 두면 다시 켤 때 Update가 알아서 복구한다.
    /// </summary>
    private void OnDisable()
    {
        if (_orbSr == null) return;
        for (int i = 0; i < _orbSr.Length; i++)
        {
            if (_orbSr[i] != null) _orbSr[i].enabled = false;

            // 직전 타격 각도도 버린다. 남겨 두면 다시 켤 때 옛 각도까지 잇는
            // 넓은 부채꼴이 만들어져 지나오지도 않은 자리의 적이 맞는다.
            if (_hasLastPos != null && i < _hasLastPos.Length) _hasLastPos[i] = false;
        }
        _activeCount = -1;
    }

    /// <summary>
    /// 코어를 상한만큼 미리 만들어 두고 필요한 개수만 켠다.
    /// 증강을 먹을 때마다 Instantiate 하면 그 프레임에 렉이 튄다.
    /// </summary>
    private void BuildOrbs()
    {
        // 궤적 배열은 코어 생성 여부와 무관하게 먼저 챙긴다.
        // Play 중 스크립트가 리로드되면 코어(_orbTf)는 직렬화로 살아남는데
        // 새로 추가된 필드는 비어 있어, 아래 조기 반환에 걸리면 그대로 NRE가 난다.
        if (_lastHitAngle == null || _lastHitAngle.Length != maxOrbCount)
        {
            _lastHitAngle = new float[maxOrbCount];
            _hasLastPos = new bool[maxOrbCount];
        }

        if (_orbTf != null) return;

        _orbTf = new Transform[maxOrbCount];
        _orbSr = new SpriteRenderer[maxOrbCount];
        _hitTimer = new float[maxOrbCount];

        for (int i = 0; i < maxOrbCount; i++)
        {
            var go = new GameObject("Orb" + i);
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FxSprites.Orb != null ? FxSprites.Orb : FxTextures.Dot;
            sr.color = orbColor;
            sr.sortingOrder = 1;          // 플레이어(0)보다 위에 그려 코어가 보이게
            sr.enabled = false;

            _orbTf[i] = go.transform;
            _orbSr[i] = sr;

            // 타격 주기를 코어마다 어긋내 피해가 한 프레임에 몰리지 않게 한다.
            _hitTimer[i] = fireInterval * (i / (float)maxOrbCount);
        }
    }

    private void Update()
    {
        if (_orbTf == null) return;

        int want = Mathf.Clamp(baseOrbCount + ExtraOrbs, 1, maxOrbCount);
        if (want != _activeCount)
        {
            for (int i = 0; i < maxOrbCount; i++) _orbSr[i].enabled = i < want;
            _activeCount = want;
        }

        // 판정 반경이 바뀌면(코어 과열 증강) 그림도 같이 커진다. 값이 그대로면 건드리지 않는다.
        float scale = OrbVisualScale;
        if (!Mathf.Approximately(scale, _appliedScale))
        {
            for (int i = 0; i < maxOrbCount; i++) _orbTf[i].localScale = Vector3.one * scale;
            _appliedScale = scale;
        }

        // 죽으면 코어를 멈추고 숨긴다.
        if (!CanAct)
        {
            for (int i = 0; i < maxOrbCount; i++) _orbSr[i].enabled = false;
            _activeCount = -1;
            return;
        }

        float dt = Time.deltaTime;
        _angle += orbitSpeed * SpeedMultiplier * dt;

        float r = orbitRadius * RadiusMultiplier;
        float step = 360f / want;
        float dmg = damage * DamageMultiplier;

        // 잔상은 코어 전체가 한 주기에 한 번씩만 뿌린다. 코어마다 매 프레임 뿌리면
        // 조각 풀(160)을 혼자 다 먹어 전투 이펙트가 잘린다.
        bool trail = false;
        if (trailInterval > 0f)
        {
            _trailTimer -= dt;
            if (_trailTimer <= 0f)
            {
                _trailTimer = trailInterval;
                trail = true;
            }
        }

        for (int i = 0; i < want; i++)
        {
            float deg = _angle + step * i;
            float rad = deg * Mathf.Deg2Rad;
            Vector3 local = new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);
            _orbTf[i].localPosition = local;

            if (trail)
            {
                // 제자리에서 알파만 빠지게 둔다 — 코어가 지나간 자리가 그대로 궤도선이 된다.
                EffectSystem.Linger(_orbTf[i].position,
                    new Color(orbColor.r, orbColor.g, orbColor.b, 0.5f),
                    scale * 0.55f, 0.18f, FxSprites.Orb);
            }

            _hitTimer[i] -= dt;
            if (_hitTimer[i] > 0f) continue;

            _hitTimer[i] = fireInterval;

            float fromDeg = _hasLastPos[i] ? _lastHitAngle[i] : deg;
            _lastHitAngle[i] = deg;
            _hasLastPos[i] = true;

            DamageAround(fromDeg, deg, r, dmg, _orbTf[i].position);
        }
    }

    /// <summary>
    /// fromDeg에서 toDeg까지 코어가 쓸고 지나간 **부채꼴 띠**를 판정한다.
    ///
    /// 원래는 타격 시점의 코어 위치 한 점으로만 판정했다. 그러면 공전 한 바퀴에
    /// 고작 6군데(기본값)에서만 때리는데 그 점 사이 간격이 1.78유닛으로
    /// 판정 지름(1.7)보다 넓어 **링 위에 아예 안 맞는 빈틈**이 생겼다.
    /// "코어가 스쳐 갔는데 피해가 없다"의 원인이 이것이었다 — 실측으로 거리 1.0과 2.4가 0 피해였다.
    ///
    /// 두 점을 선분으로 이어 봤지만 현(弦)이 원 안쪽으로 파고들어
    /// 바깥쪽 판정이 각도마다 달라졌다(2.4가 여전히 0). 경로가 원이므로
    /// 반지름 띠 + 각도 구간으로 재는 것이 정확하고 근사 오차도 없다.
    /// 공전 속도나 주기를 어떻게 바꿔도 빈틈이 생기지 않아
    /// '고속 회전' 증강이 판정을 되레 나쁘게 만드는 함정도 같이 막힌다.
    /// </summary>
    private void DamageAround(float fromDeg, float toDeg, float orbitR, float dmg, Vector3 fxAt)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        Vector3 center = transform.position;
        float hitR = hitRadius * HitRadiusMultiplier;

        // 이번 주기에 지나온 각도 폭. 진행 방향이 어느 쪽이든 [lo, hi]로 정규화한다.
        float sweep = Mathf.Clamp(Mathf.DeltaAngle(fromDeg, toDeg), -180f, 180f);
        float lo = Mathf.Min(0f, sweep);
        float hi = Mathf.Max(0f, sweep);

        float blastSqr = Blast ? blastRadius * blastRadius : 0f;
        bool anyDirect = false;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null) continue;

            Vector3 to = e.transform.position - center;
            float dist = to.magnitude;

            // 적의 몸 크기를 판정에 더한다. 중심점만 보면 보스(스케일 3)는
            // 코어가 몸통 위를 지나가도 중심이 띠 밖이라 안 맞는다.
            float reach = hitR + TargetRadius(e);

            bool direct = false;
            if (dist > 0.0001f && Mathf.Abs(dist - orbitR) <= reach)
            {
                // 호의 양 끝에서도 판정 반경만큼은 더 닿아야 한다(끝단 마감).
                // 반지름 띠가 dist >= orbitR - reach 를 보장하므로 pad가 발산하지 않는다.
                float pad = Mathf.Rad2Deg * Mathf.Min(reach / dist, Mathf.PI);
                float delta = Mathf.DeltaAngle(fromDeg, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg);
                direct = delta >= lo - pad && delta <= hi + pad;
            }

            bool splash = Blast && !direct
                && (e.transform.position - fxAt).sqrMagnitude <= blastSqr;
            if (!direct && !splash) continue;

            Health h = e.GetComponent<Health>();
            if (h == null || h.IsDead) continue;

            h.TakeDamage(direct ? dmg : dmg * 0.5f);
            if (direct) anyDirect = true;
        }

        // 조각은 코어당 1회만 뿌린다. 적 수만큼 뿌리면 뭉친 무리에서 풀이 순식간에 마른다.
        if (!anyDirect) return;

        EffectSystem.Burst(fxAt, Color.Lerp(orbColor, Color.white, 0.4f),
            2, 3.5f, 0.16f, 0.18f, FxSprites.Spark);

        if (Blast)
            EffectSystem.Ring(fxAt, new Color(orbColor.r, orbColor.g, orbColor.b, 0.7f),
                0.6f, blastRadius * 2f, 0.22f);
    }
}
