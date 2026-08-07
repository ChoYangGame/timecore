using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 균열 분출구. 고정 지점에서 예고 후 주기적으로 방사형 탄을 뿜는다. 아레나에 "가면 안 되는 구역"을 만든다.
///
/// 탄은 기존 BossProjectile을 그대로 재사용한다(새 프리팹·새 코드 없음).
/// 저사양 브라우저가 목표라 동시 탄 수가 곧 프레임이다 — burstCount·burstInterval·lifetime을
/// 곱한 상한이 눈에 보이도록 한 곳에 모아 뒀고, 기본값은 한 분출구가 최대 5회 × 6발 = 30발이다.
/// 탄 수명이 3초라 실제 동시 존재량은 그보다 훨씬 적다.
///
/// 콜라이더는 BossProjectile 쪽에만 있다. 분출구 자체는 판정이 없다 — 밟아도 안 아프고,
/// 나오는 탄을 피하는 기믹이다.
/// 부착 대상: RiftVent 프리팹 (SpriteRenderer만)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RiftVent : MonoBehaviour
{
    public struct Spec
    {
        public Vector2 position;
        public float size;
        public float warnDuration;
        public float lifetime;
        public float burstInterval;
        public int burstCount;
        public float spinPerBurst;
        public Color color;
    }

    [SerializeField] private BossProjectile projectilePrefab;

    [SerializeField] private float warnAlpha = 0.35f;
    [SerializeField] private float activeAlpha = 0.75f;
    [SerializeField] private float warnSizeRatio = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float warnBlinkSpeed = 10f;
    [SerializeField] private float activePulseSpeed = 6f;

    /// <summary>레이저(-400)와 같은 층.</summary>
    [SerializeField] private int sortingOrder = -400;

    private static readonly List<RiftVent> ActiveVents = new List<RiftVent>();

    public static int ActiveCount => ActiveVents.Count;

    /// <summary>이 분출구가 지금까지 쏜 탄 수. 프레임 부담을 실측할 때 쓴다.</summary>
    public int FiredCount { get; private set; }

    private SpriteRenderer _renderer;
    private Spec _spec;
    private float _elapsed;
    private float _nextBurstAt;
    private float _spin;
    private bool _configured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveVents.Clear();

    public static void ClearAll()
    {
        RiftVent[] snapshot = ActiveVents.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            if (snapshot[i] != null) Destroy(snapshot[i].gameObject);

        ActiveVents.Clear();
    }

    public void Configure(Spec spec, BossProjectile projectile)
    {
        _spec = spec;
        _configured = true;
        if (projectile != null) projectilePrefab = projectile;

        transform.position = spec.position;
        _nextBurstAt = spec.warnDuration;

        ApplyVisual();
    }

    public bool IsActive => _configured
        && _elapsed >= _spec.warnDuration
        && _elapsed < _spec.warnDuration + _spec.lifetime;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingOrder = sortingOrder;
    }

    private void OnEnable() => ActiveVents.Add(this);

    private void OnDisable() => ActiveVents.Remove(this);

    private void Update()
    {
        if (!_configured) return;

        _elapsed += Time.deltaTime;

        float endAt = _spec.warnDuration + _spec.lifetime;
        if (_elapsed >= endAt + fadeOutDuration)
        {
            Destroy(gameObject);
            return;
        }

        if (_elapsed >= _spec.warnDuration && _elapsed < endAt && _elapsed >= _nextBurstAt)
        {
            Burst();
            _nextBurstAt = _elapsed + Mathf.Max(0.1f, _spec.burstInterval);
        }

        ApplyVisual();
    }

    private void Burst()
    {
        if (projectilePrefab == null) return;

        int count = Mathf.Max(1, _spec.burstCount);
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + _spin;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            BossProjectile p = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            p.Launch(dir);
            FiredCount++;
        }

        // 분출은 레이저보다 약하게 흔든다. 1.5초마다 반복되는 것이라 세게 잡으면 화면이 계속 떤다.
        CameraShake.Shake(0.2f, 0.1f);
        EffectSystem.Burst(transform.position, _spec.color, 4, 3f, 0.22f, 0.25f);

        // 매번 조금씩 돌려 같은 틈으로만 계속 서 있을 수 없게 한다.
        _spin += _spec.spinPerBurst;
    }

    private void ApplyVisual()
    {
        if (_renderer == null || _renderer.sprite == null) return;

        float spriteWidth = _renderer.sprite.bounds.size.x;
        if (spriteWidth <= 0.0001f) return;

        float endAt = _spec.warnDuration + _spec.lifetime;

        float size;
        float alpha;

        if (_elapsed < _spec.warnDuration)
        {
            size = _spec.size * warnSizeRatio;
            alpha = warnAlpha * (0.75f + 0.25f * Mathf.Sin(_elapsed * warnBlinkSpeed));
        }
        else if (_elapsed < endAt)
        {
            size = _spec.size;
            alpha = activeAlpha * (0.85f + 0.15f * Mathf.Sin(_elapsed * activePulseSpeed));
        }
        else
        {
            size = _spec.size;
            alpha = activeAlpha * Mathf.Max(0f, 1f - (_elapsed - endAt) / Mathf.Max(0.0001f, fadeOutDuration));
        }

        transform.localScale = Vector3.one * (size / spriteWidth);

        Color c = _spec.color;
        c.a = Mathf.Clamp01(alpha);
        _renderer.color = c;
    }
}
