using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 예고 후 발사되는 직선 위험 구역. 시대별 패턴(원시 지면 균열 / 중세 화살 세례 /
/// 현대 십자 폭격 / 미래 레이저 격자)이 전부 이 하나를 각도·개수·굵기만 바꿔 쓴다.
///
/// 예고 구간에는 얇고 흐리게, 발사 구간에는 굵고 진하게 그린다. 피할 시간을 주는 것이
/// 이 기믹의 전부라, 예고와 발사가 굵기로 확실히 갈리게 했다(색만 바꾸면 저사양 화면에서 안 읽힌다).
///
/// 콜라이더를 쓰지 않는다. RiftZone과 같은 원칙으로 중심에서의 세로·가로 거리를 직접 잰다 —
/// 회전이 걸려 있어도 내적 2회면 끝나고, 보이는 막대와 판정이 정확히 일치한다.
/// 같은 대상에게 두 번 맞히지 않으려고 이미 맞은 Health를 들고 있는다(발사 구간 내내 판정하되 1회만).
///
/// 부착 대상: HazardBeam 프리팹 (SpriteRenderer만. 콜라이더 없음)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class HazardBeam : MonoBehaviour
{
    public struct Spec
    {
        public Vector2 center;
        public float angleDeg;
        public float length;
        public float width;
        public Color color;
        public float warnDuration;
        public float fireDuration;
        public float playerDamage;
        public float enemyDamage;
    }

    [Tooltip("예고 구간의 굵기 비율. 발사 순간 이 비율에서 1.0으로 튄다")]
    [SerializeField] private float warnWidthRatio = 0.22f;
    [SerializeField] private float warnAlpha = 0.35f;
    [SerializeField] private float fireAlpha = 0.85f;
    [Tooltip("발사가 끝난 뒤 사라지는 시간")]
    [SerializeField] private float fadeOutDuration = 0.35f;
    [Tooltip("예고 막대가 깜빡이는 속도. 정지한 반투명 막대는 배경으로 읽힌다")]
    [SerializeField] private float warnBlinkSpeed = 9f;

    /// <summary>감속 지대(-500)보다 위, 적·플레이어·탄(0)보다 아래.</summary>
    [SerializeField] private int sortingOrder = -400;

    private static readonly List<HazardBeam> ActiveBeams = new List<HazardBeam>();

    public static int ActiveCount => ActiveBeams.Count;

    private SpriteRenderer _renderer;
    private Spec _spec;
    private Vector2 _dir;
    private Vector2 _perp;
    private float _elapsed;
    private bool _configured;
    private bool _firedOnce;

    private readonly List<Health> _alreadyHit = new List<Health>();
    private Transform _player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveBeams.Clear();

    /// <summary>시대 전환 암전 중 EraManager가 부른다.</summary>
    public static void ClearAll()
    {
        HazardBeam[] snapshot = ActiveBeams.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            if (snapshot[i] != null) Destroy(snapshot[i].gameObject);

        ActiveBeams.Clear();
    }

    public void Configure(Spec spec)
    {
        _spec = spec;
        _configured = true;

        float rad = spec.angleDeg * Mathf.Deg2Rad;
        _dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        _perp = new Vector2(-_dir.y, _dir.x);

        transform.position = spec.center;
        transform.rotation = Quaternion.Euler(0f, 0f, spec.angleDeg);

        ApplyVisual();
    }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingOrder = sortingOrder;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    private void OnEnable() => ActiveBeams.Add(this);

    private void OnDisable() => ActiveBeams.Remove(this);

    private void Update()
    {
        if (!_configured) return;

        _elapsed += Time.deltaTime;

        float endOfFire = _spec.warnDuration + _spec.fireDuration;
        if (_elapsed >= endOfFire + fadeOutDuration)
        {
            Destroy(gameObject);
            return;
        }

        if (_elapsed >= _spec.warnDuration && _elapsed < endOfFire)
        {
            // 발사로 넘어가는 첫 프레임에만 한 번 흔든다. 예고→발사의 "쿵"을 만드는 부분이다.
            if (!_firedOnce)
            {
                _firedOnce = true;
                CameraShake.Shake(0.35f, 0.14f);
            }
            ApplyDamage();
        }

        ApplyVisual();
    }

    /// <summary>발사 구간 동안 매 프레임 판정하되 같은 대상은 1회만 맞는다.</summary>
    private void ApplyDamage()
    {
        if (_spec.playerDamage > 0f && _player != null)
            TryHit(_player.GetComponent<Health>(), _spec.playerDamage);

        // 적 피해가 0이면 적을 훑지 않는다. 기본값은 0 — 레이저가 웨이브를 대신 치워버리면
        // 난이도 곡선이 통째로 무너진다.
        if (_spec.enemyDamage > 0f)
        {
            foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                TryHit(e.GetComponent<Health>(), _spec.enemyDamage);
        }
    }

    private void TryHit(Health target, float damage)
    {
        if (target == null || target.IsDead) return;
        if (!Contains(target.transform.position)) return;
        if (_alreadyHit.Contains(target)) return;

        _alreadyHit.Add(target);
        target.TakeDamage(damage);
    }

    /// <summary>회전한 막대 안인지. 보이는 사각형과 정확히 같은 식이다.</summary>
    public bool Contains(Vector2 worldPos)
    {
        Vector2 d = worldPos - _spec.center;
        if (Mathf.Abs(Vector2.Dot(d, _dir)) > _spec.length * 0.5f) return false;
        if (Mathf.Abs(Vector2.Dot(d, _perp)) > _spec.width * 0.5f) return false;
        return true;
    }

    /// <summary>지금 발사 구간인지. 판정이 실제로 걸리는 구간을 밖에서 확인할 때 쓴다.</summary>
    public bool IsFiring => _configured
        && _elapsed >= _spec.warnDuration
        && _elapsed < _spec.warnDuration + _spec.fireDuration;

    private void ApplyVisual()
    {
        if (_renderer == null || _renderer.sprite == null) return;

        Vector2 spriteSize = _renderer.sprite.bounds.size;
        if (spriteSize.x <= 0.0001f || spriteSize.y <= 0.0001f) return;

        float endOfFire = _spec.warnDuration + _spec.fireDuration;

        float width;
        float alpha;

        if (_elapsed < _spec.warnDuration)
        {
            width = _spec.width * warnWidthRatio;
            // 깜빡임은 0.55~1.0 사이. 완전히 사라지면 예고를 놓친다.
            alpha = warnAlpha * (0.775f + 0.225f * Mathf.Sin(_elapsed * warnBlinkSpeed));
        }
        else if (_elapsed < endOfFire)
        {
            width = _spec.width;
            alpha = fireAlpha;
        }
        else
        {
            width = _spec.width;
            float t = (_elapsed - endOfFire) / Mathf.Max(0.0001f, fadeOutDuration);
            alpha = fireAlpha * Mathf.Max(0f, 1f - t);
        }

        transform.localScale = new Vector3(_spec.length / spriteSize.x, width / spriteSize.y, 1f);

        Color c = _spec.color;
        c.a = Mathf.Clamp01(alpha);
        _renderer.color = c;
    }
}
