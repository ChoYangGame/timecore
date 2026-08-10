using System;
using UnityEngine;

/// <summary>
/// 보스 처치 후 아레나 중앙에 등장하는 모래시계. 플레이어가 닿으면 다음 시대로 전환된다.
///
/// 가만히 놓여 있으면 배경 장식과 구분이 안 돼 "주우러 가야 할 것"으로 안 읽힌다.
/// 그래서 위아래로 떠다니고, 살짝 기울고, 아래에 그림자가 따라붙는다 —
/// **그림자가 있어야 "떠 있다"가 성립한다.** 위아래로만 움직이면 그냥 흔들리는 것으로 보인다.
///
/// 판정은 뜨지 않는다. 그림만 자식으로 띄우고 콜라이더는 루트에 고정이라,
/// 모래시계가 위로 떠 있는 순간에도 주울 수 있는 자리는 그대로다.
///
/// 부착 대상: Hourglass 프리팹 (SpriteRenderer + CircleCollider2D isTrigger = true)
/// </summary>
[DisallowMultipleComponent]
public class Hourglass : MonoBehaviour
{
    [Header("부유")]
    [Tooltip("위아래로 오르내리는 높이(월드 유닛)")]
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float bobSpeed = 2.4f;

    [Tooltip("좌우로 기우는 각도. 크게 주면 부유가 아니라 흔들림으로 읽힌다")]
    [SerializeField] private float tiltDegrees = 5f;

    [Tooltip("기울기 주기. 상하 주기와 어긋나야 기계적으로 안 보인다")]
    [SerializeField] private float tiltSpeed = 1.55f;

    [Header("연출")]
    [SerializeField] private Color glowColor = new Color(0.72f, 0.42f, 0.95f, 1f);
    [SerializeField] private float glowScale = 1.9f;
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 1f);

    public event Action OnCollected;

    private Transform _visual;
    private Transform _glow;
    private Transform _shadow;
    private SpriteRenderer _glowSr;
    private SpriteRenderer _shadowSr;
    private float _t;
    private float _groundY;
    private float _width = 1f;

    private void Awake()
    {
        var rootSr = GetComponent<SpriteRenderer>();
        if (rootSr == null || rootSr.sprite == null) return;

        Vector3 size = rootSr.sprite.bounds.size;
        _width = size.x;
        _groundY = -size.y * 0.5f;

        // 그림자 → 글로우 → 본체 순으로 겹친다.
        _shadow = MakeChild("HourglassShadow", FxTextures.Dot,
            new Color(shadowColor.r, shadowColor.g, shadowColor.b, 0.32f), rootSr.sortingOrder - 2);
        _shadow.localScale = new Vector3(_width * 0.75f, _width * 0.28f, 1f);
        _shadow.localPosition = new Vector3(0f, _groundY - 0.04f, 0f);
        _shadowSr = _shadow.GetComponent<SpriteRenderer>();

        _glow = MakeChild("HourglassGlow", FxTextures.Dot,
            new Color(glowColor.r, glowColor.g, glowColor.b, 0.22f), rootSr.sortingOrder - 1);
        _glow.localScale = Vector3.one * (_width * glowScale);
        _glowSr = _glow.GetComponent<SpriteRenderer>();

        // 본체를 자식으로 옮긴다. 루트를 움직이면 콜라이더까지 같이 떠서
        // 주울 수 있는 자리가 시시각각 바뀐다.
        _visual = MakeChild("HourglassVisual", rootSr.sprite, Color.white, rootSr.sortingOrder);
        rootSr.enabled = false;
    }

    private Transform MakeChild(string name, Sprite sprite, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go.transform;
    }

    private void Update()
    {
        if (_visual == null) return;

        _t += Time.deltaTime;

        float wave = Mathf.Sin(_t * bobSpeed);        // -1 … 1
        float lift = (wave + 1f) * 0.5f;              //  0 … 1 (바닥에 붙었을 때 0)

        _visual.localPosition = new Vector3(0f, wave * bobHeight, 0f);
        _visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * tiltSpeed) * tiltDegrees);

        // 글로우는 본체를 따라 올라가며 숨 쉬듯 커졌다 작아진다.
        _glow.localPosition = _visual.localPosition;
        _glow.localScale = Vector3.one * (_width * glowScale * (0.94f + lift * 0.12f));
        var gc = _glowSr.color;
        gc.a = 0.16f + lift * 0.14f;
        _glowSr.color = gc;

        // 높이 올라갈수록 그림자는 작고 옅어진다 — 이 대비가 부유감을 만든다.
        float shrink = 1f - lift * 0.28f;
        _shadow.localScale = new Vector3(_width * 0.75f * shrink, _width * 0.28f * shrink, 1f);
        var sc = _shadowSr.color;
        sc.a = 0.34f - lift * 0.16f;
        _shadowSr.color = sc;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Sfx.Play(SfxId.Collect);

        OnCollected?.Invoke();
        Destroy(gameObject);
    }
}
