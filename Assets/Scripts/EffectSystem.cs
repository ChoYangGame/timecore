using UnityEngine;

/// <summary>
/// 경량 이펙트. 내장 Square 스프라이트 조각을 방사형으로 튀기고 축소·페이드시킨다.
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
    [Tooltip("조각에 쓸 스프라이트. 다른 오브젝트와 같은 내장 Square를 쓴다")]
    [SerializeField] private Sprite pieceSprite;

    [Tooltip("미리 만들어 둘 조각 수. 넘치면 가장 오래된 조각부터 재사용한다")]
    [SerializeField] private int poolSize = 96;

    [Tooltip("적·플레이어(0)보다 위에 그린다")]
    [SerializeField] private int sortingOrder = 50;

    [Tooltip("조각이 퍼지다 멎는 정도. 클수록 빨리 멎는다")]
    [SerializeField] private float drag = 3.5f;

    private static EffectSystem _instance;

    private Transform[] _tf;
    private SpriteRenderer[] _sr;
    private Vector2[] _velocity;
    private Color[] _color;
    private float[] _life;
    private float[] _maxLife;
    private float[] _size;

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
        int n = Mathf.Max(8, poolSize);

        _tf = new Transform[n];
        _sr = new SpriteRenderer[n];
        _velocity = new Vector2[n];
        _color = new Color[n];
        _life = new float[n];
        _maxLife = new float[n];
        _size = new float[n];

        if (pieceSprite != null)
        {
            float w = pieceSprite.bounds.size.x;
            if (w > 0.0001f) _spriteWidth = w;
        }

        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("FxPiece");
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pieceSprite;
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
        int count = 6, float speed = 5f, float size = 0.32f, float lifetime = 0.4f)
    {
        if (_instance != null) _instance.Emit(position, color, count, speed, size, lifetime);
    }

    private void Emit(Vector2 position, Color color, int count, float speed, float size, float lifetime)
    {
        if (_tf == null || _tf.Length == 0) return;

        count = Mathf.Clamp(count, 1, 24);
        float baseAngle = Random.value * 360f;

        for (int i = 0; i < count; i++)
        {
            int slot = _next;
            _next = (_next + 1) % _tf.Length;

            // 이미 살아있던 조각을 덮어쓰면 살아있는 수는 그대로다.
            if (_life[slot] <= 0f) _alive++;

            // 균등 방사에 약간의 흔들림. 완전 랜덤이면 한쪽에 뭉쳐 터진 것처럼 안 보인다.
            float angle = (baseAngle + (360f / count) * i + Random.Range(-12f, 12f)) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            _velocity[slot] = dir * (speed * Random.Range(0.7f, 1.25f));
            _color[slot] = color;
            _maxLife[slot] = lifetime * Random.Range(0.8f, 1.2f);
            _life[slot] = _maxLife[slot];
            _size[slot] = size * Random.Range(0.75f, 1.3f);

            _tf[slot].position = position;
            _tf[slot].localScale = Vector3.one * (_size[slot] / _spriteWidth);

            Color c = color;
            _sr[slot].color = c;
            _sr[slot].enabled = true;
        }
    }

    private void Update()
    {
        if (_alive <= 0) return;

        float dt = Time.deltaTime;
        float damp = Mathf.Max(0f, 1f - drag * dt);

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
            _velocity[i] *= damp;

            float s = _size[i] * t;
            _tf[i].localScale = new Vector3(s / _spriteWidth, s / _spriteWidth, 1f);

            Color c = _color[i];
            c.a = t;
            _sr[i].color = c;
        }
    }
}
