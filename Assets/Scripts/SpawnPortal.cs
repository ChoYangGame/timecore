using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적이 필드 안에 나타나기 전에 잠깐 뜨는 예고 표식.
///
/// 가장자리 스폰은 적이 걸어 들어오는 시간이 곧 예고였다. 필드 안에서 그냥 튀어나오게 하면
/// 반응할 틈이 없어 억울한 접촉 피해가 난다. 그래서 레이저·분출구에서 세운
/// "예고 → 실행" 규약을 그대로 쓴다 — 표식이 커지며 깜빡이다가 그 자리에 적이 나온다.
///
/// 콜라이더도 판정도 없다. 순수하게 보여주기만 한다.
/// 부착 대상: SpawnPortal 프리팹 (SpriteRenderer만)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpawnPortal : MonoBehaviour
{
    [Tooltip("표식이 시작하는 크기 비율. 예고가 끝날 때 1.0이 된다")]
    [SerializeField] private float startSizeRatio = 0.25f;
    [SerializeField] private float maxAlpha = 0.7f;
    [SerializeField] private float blinkSpeed = 16f;

    /// <summary>감속 지대(-500)·레이저(-400)보다 위, 적·플레이어(0)보다 아래.</summary>
    [SerializeField] private int sortingOrder = -350;

    private static readonly List<SpawnPortal> ActivePortals = new List<SpawnPortal>();

    public static int ActiveCount => ActivePortals.Count;

    private SpriteRenderer _renderer;
    private Color _color = Color.white;
    private float _size = 1.2f;
    private float _warnDuration = 0.7f;
    private float _elapsed;
    private bool _configured;
    private bool _opened;
    private Action _onOpened;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActivePortals.Clear();

    /// <summary>
    /// 시대 전환 암전 중 EraManager가 부른다.
    /// 열리기 전에 지워진 표식은 콜백을 부르지 않는다 — 다음 시대에 이전 시대 적이 튀어나오면 안 된다.
    /// </summary>
    public static void ClearAll()
    {
        SpawnPortal[] snapshot = ActivePortals.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            if (snapshot[i] != null) Destroy(snapshot[i].gameObject);

        ActivePortals.Clear();
    }

    public void Configure(Color color, float size, float warnDuration, Action onOpened)
    {
        _color = color;
        _size = size;
        _warnDuration = Mathf.Max(0.05f, warnDuration);
        _onOpened = onOpened;
        _configured = true;

        ApplyVisual();
    }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingOrder = sortingOrder;
    }

    private void OnEnable() => ActivePortals.Add(this);

    private void OnDisable() => ActivePortals.Remove(this);

    private void Update()
    {
        if (!_configured) return;

        _elapsed += Time.deltaTime;

        if (_elapsed >= _warnDuration)
        {
            Open();
            return;
        }

        ApplyVisual();
    }

    private void Open()
    {
        if (_opened) return;
        _opened = true;

        // 적이 나타나는 순간의 터짐. 표식이 사라지는 게 아니라 무언가로 바뀌었다는 인상을 준다.
        EffectSystem.Burst(transform.position, _color, 6, 4f, 0.26f, 0.3f);

        Action cb = _onOpened;
        _onOpened = null;

        Destroy(gameObject);

        // 콜백에서 적을 만든다. Destroy 뒤에 부르는 이유는 콜백이 예외를 내도
        // 표식이 화면에 남지 않게 하려는 것이다.
        cb?.Invoke();
    }

    private void ApplyVisual()
    {
        if (_renderer == null || _renderer.sprite == null) return;

        float spriteWidth = _renderer.sprite.bounds.size.x;
        if (spriteWidth <= 0.0001f) return;

        float t = Mathf.Clamp01(_elapsed / _warnDuration);

        // 작게 시작해 커지며 열린다. 커지는 방향이라 "여기서 나온다"가 읽힌다.
        float size = _size * Mathf.Lerp(startSizeRatio, 1f, t);
        transform.localScale = Vector3.one * (size / spriteWidth);

        // 끝으로 갈수록 빨리 깜빡인다.
        float blink = 0.7f + 0.3f * Mathf.Sin(_elapsed * blinkSpeed * (0.5f + t));

        Color c = _color;
        c.a = Mathf.Clamp01(maxAlpha * blink);
        _renderer.color = c;
    }
}
