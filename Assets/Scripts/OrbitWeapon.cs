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

    [Tooltip("코어 하나가 적을 때리는 반경")]
    [SerializeField] private float hitRadius = 0.55f;

    [SerializeField] private float orbSize = 0.95f;
    [SerializeField] private Color orbColor = new Color(0.435f, 0.847f, 0.878f, 1f);

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

    private Transform[] _orbTf;
    private SpriteRenderer[] _orbSr;
    private float[] _hitTimer;
    private float _angle;
    private int _activeCount = -1;
    private float _trailTimer;

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
            if (_orbSr[i] != null) _orbSr[i].enabled = false;
        _activeCount = -1;
    }

    /// <summary>
    /// 코어를 상한만큼 미리 만들어 두고 필요한 개수만 켠다.
    /// 증강을 먹을 때마다 Instantiate 하면 그 프레임에 렉이 튄다.
    /// </summary>
    private void BuildOrbs()
    {
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

            go.transform.localScale = Vector3.one * orbSize;

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

        // 죽으면 코어를 멈추고 숨긴다.
        if (!CanAct)
        {
            for (int i = 0; i < maxOrbCount; i++) _orbSr[i].enabled = false;
            _activeCount = -1;
            return;
        }

        float dt = Time.deltaTime;
        _angle += orbitSpeed * dt;

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
            float rad = (_angle + step * i) * Mathf.Deg2Rad;
            Vector3 local = new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);
            _orbTf[i].localPosition = local;

            if (trail)
            {
                // 제자리에서 알파만 빠지게 둔다 — 코어가 지나간 자리가 그대로 궤도선이 된다.
                EffectSystem.Linger(_orbTf[i].position,
                    new Color(orbColor.r, orbColor.g, orbColor.b, 0.5f),
                    orbSize * 0.55f, 0.18f, FxSprites.Orb);
            }

            _hitTimer[i] -= dt;
            if (_hitTimer[i] > 0f) continue;

            _hitTimer[i] = fireInterval;
            DamageAround(_orbTf[i].position, dmg);
        }
    }

    private void DamageAround(Vector3 center, float dmg)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float sqr = hitRadius * hitRadius;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null) continue;
            if ((e.transform.position - center).sqrMagnitude > sqr) continue;

            Health h = e.GetComponent<Health>();
            if (h == null || h.IsDead) continue;

            h.TakeDamage(dmg);
            EffectSystem.Burst(center, Color.Lerp(orbColor, Color.white, 0.4f),
                2, 3.5f, 0.16f, 0.18f, FxSprites.Spark);
        }
    }
}
