using UnityEngine;

/// <summary>
/// 매지션. 몸을 중심으로 지대를 깔고, 그 안에 들어온 적에게 주기마다 피해를 준다.
///
/// 총잡이·칼잡이와 갈리는 지점은 **조준도 사거리도 없다는 것**이다.
/// 쏘지도 휘두르지도 않고 지대는 늘 켜져 있어, "어디에 서 있느냐"가 곧 공격이다.
/// 적 무리 한가운데를 밟고 버티는 플레이가 최적해가 된다.
///
/// 콜라이더를 쓰지 않는다. 주기마다 한 번 적을 훑어 거리 비교만 한다 —
/// 적 40마리라도 주기당 비교 40번이라 저사양 브라우저에서 부담이 없다.
/// 적별 상태표(체류 시간 등)를 두지 않는 것도 같은 이유다.
///
/// **보이는 지대의 테두리가 곧 판정 경계다.** 링 스프라이트를 판정 반경에 맞춰 그리므로
/// 눈에 보이는 원 안에 있으면 반드시 맞고, 밖에 있으면 반드시 안 맞는다.
///
/// 부착 대상: Player
/// </summary>
[DisallowMultipleComponent]
public class FieldWeapon : PlayerWeapon
{
    [Tooltip("지대가 피해를 주는 간격(초). FireRate 증강이 이 값을 줄인다")]
    [SerializeField] private float fireInterval = 0.5f;

    [Tooltip("한 번에 들어가는 피해. 지대 안 적 전부가 동시에 맞는다")]
    [SerializeField] private float damage = 10f;

    [Tooltip("지대 반경(월드 유닛). **보이는 원의 크기가 이 값에서 나온다**")]
    [SerializeField] private float fieldRadius = 2.6f;

    [Tooltip("'주위 파동' 증강이 켜는 바깥 링의 추가 반경. 그 구간은 절반 피해다")]
    [SerializeField] private float outerRingBonus = 1.6f;

    [Tooltip("'시간 정체' 한 겹당 적 이동속도 감소율")]
    [SerializeField] private float slowPerStack = 0.22f;

    [Tooltip("감속이 아무리 겹쳐도 이 배율 밑으로는 안 내려간다. 완전 정지는 게임을 망친다")]
    [SerializeField] private float minSlowMultiplier = 0.4f;

    [Header("연출")]
    [SerializeField] private Color fieldColor = new Color(0.435f, 0.847f, 0.878f, 1f);

    [Tooltip("바닥 채움 알파. 낮게 둔다 — 진하면 지대 안의 적이 안 보인다")]
    [SerializeField] private float fillAlpha = 0.13f;

    [Tooltip("판정 경계를 알리는 테두리 알파. 여기가 1순위 정보라 진하게 둔다")]
    [SerializeField] private float ringAlpha = 0.9f;

    [Tooltip("안쪽 무늬 회전 속도(도/초). 멈춘 반투명 원은 화려한 바닥 위에서 그냥 사라진다")]
    [SerializeField] private float spinSpeed = 22f;

    public override PlayerClass Class => PlayerClass.Mage;

    public override float FireInterval
    {
        get => fireInterval;
        set => fireInterval = value;
    }

    /// <summary>증강으로 늘어나는 지대 반경 배율.</summary>
    public float RadiusMultiplier { get; set; } = 1f;

    /// <summary>'중심 붕괴' — 주기마다 가장 가까운 적 하나에게 더 들어가는 피해.</summary>
    public float FocusDamage { get; set; }

    /// <summary>'주위 파동' — 지대 바깥 링에도 절반 피해가 간다.</summary>
    public bool OuterRing { get; set; }

    /// <summary>'시간 정체' 누적 겹수. 지대 안 적이 느려진다.</summary>
    public int SlowStacks { get; set; }

    /// <summary>지금 켜져 있는 지대. 적이 감속을 물어볼 때 쓴다(RiftZone과 같은 방식).</summary>
    private static FieldWeapon _active;

    /// <summary>
    /// 그 자리의 이동속도 배율. Enemy가 매 프레임 물어본다.
    /// 콜라이더도 이벤트도 없이 감속을 거는 방법이라 적이 지대를 드나들어도 상태가 꼬이지 않는다.
    /// </summary>
    public static float SlowMultiplierAt(Vector3 worldPos)
    {
        FieldWeapon f = _active;
        if (f == null || f.SlowStacks <= 0 || !f.isActiveAndEnabled) return 1f;

        float r = f.Radius;
        if ((worldPos - f.transform.position).sqrMagnitude > r * r) return 1f;

        float m = Mathf.Pow(1f - f.slowPerStack, f.SlowStacks);
        return Mathf.Max(f.minSlowMultiplier, m);
    }

    /// <summary>증강까지 반영한 실제 판정 반경.</summary>
    public float Radius => fieldRadius * RadiusMultiplier;

    private Transform _fill;
    private Transform _ring;
    private Transform _swirl;
    private float _timer;
    private float _appliedRadius = -1f;

    protected override void Awake()
    {
        base.Awake();
        BuildVisual();
    }

    private void OnEnable() => _active = this;

    /// <summary>
    /// 다른 직업으로 갈아타면 이 컴포넌트가 꺼진다. Update가 멈추므로
    /// 지대 그림을 여기서 직접 숨기지 않으면 화면에 그대로 남는다.
    /// </summary>
    private void OnDisable()
    {
        if (_active == this) _active = null;
        ShowVisual(false);
    }

    /// <summary>
    /// 바닥 채움 + 테두리 링 + 회전 무늬 세 겹. RiftZone과 같은 구성이라
    /// 플레이어가 "이건 지속 지대구나"를 이미 배운 문법으로 읽는다.
    /// 프리팹을 건드리지 않으려고 런타임에 자식으로 만든다.
    /// </summary>
    private void BuildVisual()
    {
        if (_fill != null) return;

        _fill = MakeLayer("FieldFill", FxTextures.Dot,
            new Color(fieldColor.r, fieldColor.g, fieldColor.b, fillAlpha), -2);
        _swirl = MakeLayer("FieldSwirl", FxSprites.Twirl != null ? FxSprites.Twirl : FxTextures.Ring,
            new Color(fieldColor.r, fieldColor.g, fieldColor.b, 0.35f), -2);
        _ring = MakeLayer("FieldRing", FxTextures.Ring,
            new Color(fieldColor.r, fieldColor.g, fieldColor.b, ringAlpha), -1);

        ShowVisual(false);
    }

    private Transform MakeLayer(string name, Sprite sprite, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;      // 플레이어(0)와 적보다 아래 — 지대가 적을 가리면 안 된다
        return go.transform;
    }

    private void ShowVisual(bool on)
    {
        if (_fill == null) return;
        _fill.gameObject.SetActive(on);
        _ring.gameObject.SetActive(on);
        _swirl.gameObject.SetActive(on);
    }

    private void Update()
    {
        if (_fill == null) return;

        // 죽으면 지대를 끈다.
        if (!CanAct)
        {
            ShowVisual(false);
            return;
        }

        ShowVisual(true);

        float r = Radius;

        // 반경이 바뀌면(지대 증대 증강) 그림도 같이 커진다. 스프라이트가 1유닛이라 지름을 그대로 스케일로 쓴다.
        if (!Mathf.Approximately(r, _appliedRadius))
        {
            float d = r * 2f;
            _fill.localScale = Vector3.one * d;
            _ring.localScale = Vector3.one * d;
            _swirl.localScale = Vector3.one * (d * 0.72f);
            _appliedRadius = r;
        }

        _swirl.localRotation = Quaternion.Euler(0f, 0f, _swirl.localEulerAngles.z + spinSpeed * Time.deltaTime);

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = fireInterval;

        Tick(r);
    }

    private void Tick(float r)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return;

        Vector3 center = transform.position;
        float dmg = damage * DamageMultiplier;

        float outer = OuterRing ? r + outerRingBonus : 0f;
        float outerSqr = outer * outer;

        Health nearest = null;
        float nearestSqr = float.MaxValue;
        int hits = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null) continue;

            // 적의 몸 크기를 반경에 더한다. 중심점만 보면 보스(스케일 3)는
            // 지대가 몸통을 덮어도 중심이 밖이라 안 맞는다.
            float reach = r + TargetRadius(e);
            float d = (e.transform.position - center).sqrMagnitude;

            bool inside = d <= reach * reach;
            bool ring = !inside && OuterRing && d <= outerSqr;
            if (!inside && !ring) continue;

            Health h = e.GetComponent<Health>();
            if (h == null || h.IsDead) continue;

            h.TakeDamage(inside ? dmg : dmg * 0.5f);
            hits++;

            if (inside && d < nearestSqr) { nearestSqr = d; nearest = h; }
        }

        // '중심 붕괴' — 지대는 광역이라 단일 대상이 약하다. 한 마리에게만 더 얹어 그 구멍을 메운다.
        if (nearest != null && FocusDamage > 0f && !nearest.IsDead)
        {
            nearest.TakeDamage(FocusDamage * DamageMultiplier);
            EffectSystem.Burst(nearest.transform.position,
                Color.Lerp(fieldColor, Color.white, 0.5f), 3, 4f, 0.2f, 0.2f, FxSprites.Spark);
        }

        if (hits <= 0) return;

        // 조각은 틱당 1회만. 적 수만큼 뿌리면 뭉친 무리에서 풀(160)이 순식간에 마른다.
        EffectSystem.Ring(center, new Color(fieldColor.r, fieldColor.g, fieldColor.b, 0.55f),
            r * 1.9f, r * 2.05f, 0.22f);
    }
}
