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

    [Tooltip("장판 알파. 아트가 선 위주라 진해도 안의 적을 가리지 않는다")]
    [SerializeField] private float fieldAlpha = 0.95f;

    [Tooltip("장판 회전 속도(도/초). 멈춘 반투명 원은 화려한 바닥 위에서 그냥 사라진다")]
    [SerializeField] private float spinSpeed = 18f;

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

    private Transform _field;
    private float _timer;
    private float _appliedRadius = -1f;
    private float _spinDeg;

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
    /// 장판 아트 한 장. 예전에는 채움·링·소용돌이 세 겹을 코드로 겹쳐 만들었는데,
    /// 2026-08-10에 디자인 담당의 장판 아트로 교체하면서 한 장으로 줄였다.
    /// 프리팹을 건드리지 않으려고 런타임에 자식으로 만드는 것은 그대로다.
    /// </summary>
    private void BuildVisual()
    {
        if (_field != null) return;

        var go = new GameObject("FieldArt");
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxSprites.TimeStopField != null ? FxSprites.TimeStopField : FxTextures.Ring;
        sr.color = new Color(1f, 1f, 1f, fieldAlpha); // 아트 원본 색 그대로. 곱하면 그림이 그 색조로 덮인다
        sr.sortingOrder = -2;   // 플레이어(0)와 적보다 아래 — 지대가 적을 가리면 안 된다

        _field = go.transform;
        ShowVisual(false);
    }

    private void ShowVisual(bool on)
    {
        if (_field == null) return;
        _field.gameObject.SetActive(on);
    }

    private void Update()
    {
        if (_field == null) return;

        // 죽으면 지대를 끈다.
        if (!CanAct)
        {
            ShowVisual(false);
            return;
        }

        ShowVisual(true);

        float r = Radius;

        // 반경이 바뀌면(지대 증대 증강) 그림도 같이 커진다.
        if (!Mathf.Approximately(r, _appliedRadius))
        {
            ApplyFieldSize(r);
            _appliedRadius = r;
        }

        _spinDeg += spinSpeed * Time.deltaTime;
        _field.localRotation = Quaternion.Euler(0f, 0f, _spinDeg);

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = fireInterval;

        Tick(r);
    }

    /// <summary>
    /// **보이는 링이 곧 판정 경계가 되도록** 장판 그림을 맞춘다.
    ///
    /// 원본 아트는 원근으로 눌린 타원이다(가로 53.5% / 세로 37.5% — 픽셀 실측).
    /// 그대로 쓰면 세로 사거리가 실제의 절반처럼 보여서, "화면에서는 빗나갔는데 맞았다"가 난다.
    /// 이 프로젝트가 참격·홀·감속 지대에서 계속 지켜 온 규칙이 "보이는 것이 곧 판정"이라
    /// 세로를 늘려 정원으로 만든다 — 원근을 포기하고 판정 일치를 택한 것이다.
    ///
    /// 그림 중심이 프레임 중심보다 조금 아래인 것(-2.2%)은 **스프라이트 피벗**으로 잡아 뒀다.
    /// 위치로 밀어 보정하면 장판이 회전할 때 그 오프셋만큼 원이 흔들린다(피벗이 곧 회전축이므로).
    /// </summary>
    private void ApplyFieldSize(float r)
    {
        Sprite sp = _field.GetComponent<SpriteRenderer>().sprite;
        if (sp == null) return;

        float d = r * 2f;
        float drawnW = sp.bounds.size.x * FxSprites.TimeStopSpanX;
        float drawnH = sp.bounds.size.y * FxSprites.TimeStopSpanY;
        if (drawnW <= 0.0001f || drawnH <= 0.0001f) return;

        float sx = d / drawnW;
        float sy = d / drawnH;
        _field.localScale = new Vector3(sx, sy, 1f);
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
