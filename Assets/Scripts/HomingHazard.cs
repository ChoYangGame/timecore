using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 추적 장판. 플레이어를 일정 시간 따라다니다가 그 자리에 굳고(고정), 잠깐 뒤 터진다.
///
/// HazardBeam(직선)과 회피 방향이 다른 것이 존재 이유다. 직선은 옆으로 한 번 비키면 끝나지만
/// 이건 따라오는 동안 계속 움직여야 하고, 굳는 순간의 위치를 예측해 반대로 꺾어야 한다.
/// 예고와 발사를 굵기·밝기로 가르는 규약은 HazardBeam과 동일하게 맞췄다.
///
/// 콜라이더 없이 사각 판정. 부착 대상: HomingHazard 프리팹 (SpriteRenderer만)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class HomingHazard : MonoBehaviour
{
    public struct Spec
    {
        public Vector2 startPosition;
        public float size;
        public float trackDuration;
        public float lockDuration;
        public float fireDuration;
        public float trackSpeed;
        public float playerDamage;
        public Color color;
    }

    [Tooltip("추적 구간의 크기 비율. 굳는 순간 1.0으로 튄다")]
    [SerializeField] private float trackSizeRatio = 0.55f;
    [SerializeField] private float trackAlpha = 0.3f;
    [SerializeField] private float lockAlpha = 0.5f;
    [SerializeField] private float fireAlpha = 0.85f;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float lockBlinkSpeed = 14f;

    /// <summary>레이저(-400)와 같은 층.</summary>
    [SerializeField] private int sortingOrder = -400;

    private static readonly List<HomingHazard> ActiveHazards = new List<HomingHazard>();

    public static int ActiveCount => ActiveHazards.Count;

    private SpriteRenderer _renderer;
    private Spec _spec;
    private Transform _player;
    private Health _playerHealth;

    private float _elapsed;
    private bool _configured;
    private bool _damageDone;
    private Vector2 _position;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveHazards.Clear();

    public static void ClearAll()
    {
        HomingHazard[] snapshot = ActiveHazards.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            if (snapshot[i] != null) Destroy(snapshot[i].gameObject);

        ActiveHazards.Clear();
    }

    public void Configure(Spec spec)
    {
        _spec = spec;
        _configured = true;
        _position = spec.startPosition;

        transform.position = _position;
        ApplyVisual();
    }

    /// <summary>지금 판정이 걸려 있는 구간인지.</summary>
    public bool IsFiring => _configured
        && _elapsed >= _spec.trackDuration + _spec.lockDuration
        && _elapsed < _spec.trackDuration + _spec.lockDuration + _spec.fireDuration;

    public bool Contains(Vector2 worldPos)
    {
        float half = _spec.size * 0.5f;
        Vector2 d = worldPos - _position;
        return Mathf.Abs(d.x) <= half && Mathf.Abs(d.y) <= half;
    }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingOrder = sortingOrder;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _player = p.transform;
            _playerHealth = p.GetComponent<Health>();
        }
    }

    private void OnEnable() => ActiveHazards.Add(this);

    private void OnDisable() => ActiveHazards.Remove(this);

    private void Update()
    {
        if (!_configured) return;

        _elapsed += Time.deltaTime;

        float lockAt = _spec.trackDuration;
        float fireAt = lockAt + _spec.lockDuration;
        float endAt = fireAt + _spec.fireDuration;

        if (_elapsed >= endAt + fadeOutDuration)
        {
            Destroy(gameObject);
            return;
        }

        // 추적 구간에만 따라간다. 굳은 뒤에는 그 자리에 머문다 — 이게 회피 지점을 만든다.
        if (_elapsed < lockAt && _player != null)
        {
            Vector2 target = _player.position;
            _position = Vector2.MoveTowards(_position, target, _spec.trackSpeed * Time.deltaTime);
            if (ArenaBounds.Instance != null) _position = ArenaBounds.Instance.Clamp(_position);
            transform.position = _position;
        }

        if (!_damageDone && _elapsed >= fireAt && _elapsed < endAt) ApplyDamage();

        ApplyVisual();
    }

    private void ApplyDamage()
    {
        if (_spec.playerDamage <= 0f || _playerHealth == null || _playerHealth.IsDead) return;
        if (_player == null || !Contains(_player.position)) return;

        _damageDone = true;
        _playerHealth.TakeDamage(_spec.playerDamage);
    }

    private void ApplyVisual()
    {
        if (_renderer == null || _renderer.sprite == null) return;

        float spriteWidth = _renderer.sprite.bounds.size.x;
        if (spriteWidth <= 0.0001f) return;

        float lockAt = _spec.trackDuration;
        float fireAt = lockAt + _spec.lockDuration;
        float endAt = fireAt + _spec.fireDuration;

        float size;
        float alpha;

        if (_elapsed < lockAt)
        {
            size = _spec.size * trackSizeRatio;
            alpha = trackAlpha;
        }
        else if (_elapsed < fireAt)
        {
            // 굳은 뒤 터지기 직전. 빠르게 깜빡여 "이제 온다"를 알린다.
            size = _spec.size;
            alpha = lockAlpha * (0.7f + 0.3f * Mathf.Sin(_elapsed * lockBlinkSpeed));
        }
        else if (_elapsed < endAt)
        {
            size = _spec.size;
            alpha = fireAlpha;
        }
        else
        {
            size = _spec.size;
            alpha = fireAlpha * Mathf.Max(0f, 1f - (_elapsed - endAt) / Mathf.Max(0.0001f, fadeOutDuration));
        }

        transform.localScale = Vector3.one * (size / spriteWidth);

        Color c = _spec.color;
        c.a = Mathf.Clamp01(alpha);
        _renderer.color = c;
    }
}
