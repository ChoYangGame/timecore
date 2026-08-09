using UnityEngine;

/// <summary>
/// 경량 이펙트. 내장 Square 스프라이트 조각을 움직이고 축소·페이드시킨다.
/// 방출 모양은 네 가지다 — Burst(방사·감속·축소) / Ring(등간격 확장) / Spray(원뿔) / Linger(제자리 페이드).
/// 네 가지 전부 같은 배열·같은 Update를 쓴다. 조각마다 감속과 축소 여부만 다르게 들고 있으면
/// 모양이 갈리기 때문에, 이펙트 종류를 늘려도 프레임 비용 구조는 그대로다.
///
/// ParticleSystem을 쓰지 않는다. 목표가 저사양 브라우저인데 이 게임은 적이 초당 여러 마리 죽어
/// 이펙트가 가장 자주 발생하는 축이다. 파티클 시스템은 인스턴스마다 자체 Update와 메시 갱신을 돌고,
/// 매번 Instantiate하면 GC가 먼저 터진다.
///
/// 그래서 (1) 조각을 미리 만들어 두고 재사용하며 (2) 조각마다 MonoBehaviour를 붙이지 않고
/// 이 컴포넌트가 배열 하나를 통째로 도는 Update 한 개로 전부 움직인다.
/// 살아있는 조각이 없으면 루프가 즉시 빠져나간다.
///
/// 부착 대상: EffectSystem (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class EffectSystem : MonoBehaviour
{
    // 조각 스프라이트는 인스펙터에서 받지 않는다. FxTextures가 코드로 만든 것을 쓴다 —
    // 내장 Square를 쓰던 동안에는 무엇을 해도 이펙트가 네모로 보였다.
    // 모양은 방출 종류마다 다르다: 흩어지는 조각은 Dot, 충격파는 Ring.

    [Tooltip("미리 만들어 둘 조각 수. 넘치면 가장 오래된 조각부터 재사용한다")]
    [SerializeField] private int poolSize = 96;

    [Tooltip("적·플레이어(0)보다 위에 그린다")]
    [SerializeField] private int sortingOrder = 50;

    [Tooltip("Burst 조각이 퍼지다 멎는 정도. 클수록 빨리 멎는다")]
    [SerializeField] private float drag = 3.5f;

    private static EffectSystem _instance;

    private Transform[] _tf;
    private SpriteRenderer[] _sr;
    private Vector2[] _velocity;
    private Color[] _color;
    private float[] _life;
    private float[] _maxLife;
    private float[] _size;
    private float[] _drag;
    /// <summary>수명이 끝날 때까지 줄어드는 비율. 1이면 0까지 줄고, 0이면 크기를 유지한 채 알파만 빠진다.</summary>
    private float[] _shrink;

    private int _next;
    private int _alive;
    private float _spriteWidth = 1f;

    /// <summary>지금 살아있는 조각 수. 프레임 부담 실측에 쓴다.</summary>
    public static int AliveCount => _instance != null ? _instance._alive : 0;

    public static int PoolSize => _instance != null ? _instance._tf.Length : 0;


    private void Awake()
    {
        _instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Build()
    {
        // 96은 이펙트가 Burst 하나뿐이던 시절의 값이다. 링·잔상·앰비언트가 붙으면서
        // 96에서는 오래된 조각을 덮어써 이펙트가 중간에 끊긴다. 씬 값을 고치려면 씬 편집이 필요하므로
        // 코드 쪽에 바닥값을 둔다 — 조각은 GameObject일 뿐이라 빌드 용량과는 무관하다.
        int n = Mathf.Max(160, poolSize);

        _tf = new Transform[n];
        _sr = new SpriteRenderer[n];
        _velocity = new Vector2[n];
        _color = new Color[n];
        _life = new float[n];
        _maxLife = new float[n];
        _size = new float[n];
        _drag = new float[n];
        _shrink = new float[n];

        // FxTextures는 전부 PPU를 크기와 맞춰 구워서 월드 bounds가 1x1이다.
        _spriteWidth = 1f;

        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("FxPiece");
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FxTextures.Dot;
            sr.sortingOrder = sortingOrder;
            sr.enabled = false;

            _tf[i] = go.transform;
            _sr[i] = sr;
        }
    }

    /// <summary>
    /// position에서 조각을 방사형으로 튀긴다. 인스턴스가 없으면(씬에 배치 안 됨) 조용히 넘어간다 —
    /// 이펙트가 없다고 게임 로직이 멈추면 안 된다.
    /// </summary>
    public static void Burst(Vector2 position, Color color,
        int count = 6, float speed = 5f, float size = 0.32f, float lifetime = 0.4f,
        Sprite shape = null)
    {
        if (_instance == null) return;

        count = Mathf.Clamp(count, 1, 24);
        float baseAngle = Random.value * 360f;

        for (int i = 0; i < count; i++)
        {
            // 균등 방사에 약간의 흔들림. 완전 랜덤이면 한쪽에 뭉쳐 터진 것처럼 안 보인다.
            float angle = baseAngle + (360f / count) * i + Random.Range(-12f, 12f);
            _instance.Emit(position, Dir(angle) * (speed * Random.Range(0.7f, 1.25f)), color,
                size * Random.Range(0.75f, 1.3f), lifetime * Random.Range(0.8f, 1.2f),
                _instance.drag, 1f, shape);
        }
    }

    /// <summary>
    /// 크기가 변하는 고리 하나. startSize에서 endSize로 자라거나(충격파) 오므라든다(시전 예고).
    ///
    /// 처음에는 조각 12~14개를 원형으로 흩뿌려 링을 만들었는데, 조각 자체가 고리 모양이라
    /// "도넛으로 만든 도넛"처럼 보였다(실측). 고리는 한 장으로 그리는 게 맞고, 덤으로 조각도 1개만 쓴다.
    /// </summary>
    public static void Ring(Vector2 position, Color color,
        float startSize = 0.8f, float endSize = 3.2f, float lifetime = 0.35f)
    {
        if (_instance == null) return;

        startSize = Mathf.Max(0.01f, startSize);

        // shrink는 "수명이 끝날 때까지 줄어드는 비율"이라 음수면 커진다.
        // size(t) = startSize * (1 + (t-1) * shrink), t는 1(탄생)에서 0(소멸)으로 간다.
        float grow = Mathf.Max(0.01f, endSize) / startSize;
        _instance.Emit(position, Vector2.zero, color, startSize, lifetime, 0f, -(grow - 1f), FxTextures.Ring);
    }

    /// <summary>원뿔 방향 분사. 맞은 방향·발사 방향처럼 "어디서 왔는지"를 보여줘야 할 때 쓴다.</summary>
    public static void Spray(Vector2 position, Vector2 direction, Color color,
        int count = 6, float spreadDeg = 45f, float speed = 6f, float size = 0.28f, float lifetime = 0.35f,
        Sprite shape = null)
    {
        if (_instance == null) return;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        count = Mathf.Clamp(count, 1, 24);
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + Random.Range(-spreadDeg, spreadDeg) * 0.5f;
            _instance.Emit(position, Dir(angle) * (speed * Random.Range(0.6f, 1.3f)), color,
                size * Random.Range(0.75f, 1.25f), lifetime * Random.Range(0.8f, 1.2f),
                _instance.drag, 1f, shape);
        }
    }

    /// <summary>
    /// 방향이 있는 조각 하나. 칼잡이의 참격처럼 "어느 쪽으로 벴는지"가 보여야 할 때 쓴다.
    /// 제자리에서 조금 줄면서 사라진다 — 날아가면 베기가 아니라 발사체로 읽힌다.
    /// </summary>
    public static void Slash(Vector2 position, float rotationDeg, Color color,
        float size, float lifetime, Sprite shape)
    {
        if (_instance == null) return;
        _instance.Emit(position, Vector2.zero, color, size, lifetime, 0f, 0.25f, shape, rotationDeg);
    }

    /// <summary>제자리에서 알파만 빠지는 조각 하나. 잔상·그을음처럼 "지나간 자국"에 쓴다.</summary>
    public static void Linger(Vector2 position, Color color, float size = 0.5f, float lifetime = 0.5f,
        Sprite shape = null)
    {
        if (_instance == null) return;
        _instance.Emit(position, Vector2.zero, color, size, lifetime, 0f, 0f, shape);
    }

    private static Vector2 Dir(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private void Emit(Vector2 position, Vector2 velocity, Color color,
        float size, float lifetime, float dragValue, float shrink, Sprite shape = null,
        float rotationDeg = 0f)
    {
        if (_tf == null || _tf.Length == 0) return;

        int slot = _next;
        _next = (_next + 1) % _tf.Length;

        // 이미 살아있던 조각을 덮어쓰면 살아있는 수는 그대로다.
        if (_life[slot] <= 0f) _alive++;

        // 조각마다 모양이 다를 수 있다. 풀은 공유하되 스프라이트만 갈아끼운다.
        _sr[slot].sprite = shape != null ? shape : FxTextures.Dot;

        _velocity[slot] = velocity;
        _color[slot] = color;
        _maxLife[slot] = Mathf.Max(0.01f, lifetime);
        _life[slot] = _maxLife[slot];
        _size[slot] = size;
        _drag[slot] = dragValue;
        _shrink[slot] = shrink;

        _tf[slot].position = position;
        _tf[slot].localScale = Vector3.one * (size / _spriteWidth);
        // 대부분의 조각은 방향이 없지만 참격처럼 방향이 있는 모양은 눕혀야 한다.
        // 회전을 매번 초기화하는 이유: 풀을 돌려 쓰므로 이전 조각의 각도가 남는다.
        _tf[slot].localRotation = Quaternion.Euler(0f, 0f, rotationDeg);

        _sr[slot].color = color;
        _sr[slot].enabled = true;
    }

    private void Update()
    {
        if (_alive <= 0) return;

        float dt = Time.deltaTime;

        for (int i = 0; i < _tf.Length; i++)
        {
            if (_life[i] <= 0f) continue;

            _life[i] -= dt;
            if (_life[i] <= 0f)
            {
                _sr[i].enabled = false;
                _alive--;
                continue;
            }

            float t = _life[i] / _maxLife[i];   // 1 → 0

            _tf[i].position += (Vector3)(_velocity[i] * dt);

            // 감속은 조각마다 다르다. 링은 0이라 등속으로 번져나가고, Burst는 퍼지다 멎는다.
            if (_drag[i] > 0f) _velocity[i] *= Mathf.Max(0f, 1f - _drag[i] * dt);

            // shrink 1이면 0까지 줄고, 0이면 크기를 유지한 채 알파만 빠진다.
            float s = _size[i] * Mathf.Lerp(1f, t, _shrink[i]);
            _tf[i].localScale = new Vector3(s / _spriteWidth, s / _spriteWidth, 1f);

            Color c = _color[i];
            c.a = t;
            _sr[i].color = c;
        }
    }
}
