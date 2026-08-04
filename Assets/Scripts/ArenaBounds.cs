using UnityEngine;

/// <summary>
/// 아레나(맵)의 논리적 경계를 관리하는 단일 소스.
/// 배경 스프라이트의 실제 월드 크기(renderer.bounds)에서 inset만큼 안쪽으로 줄인 Rect(걸을 수 있는 영역)를 들고 있는다.
/// PPU나 배경 오브젝트의 localScale을 어떻게 바꿔도 시각적 벽과 논리적 벽이 항상 일치한다.
///
/// VisualRect는 inset을 빼지 않은 스프라이트 원본 크기 그대로다. CameraFollow는 이걸로 클램프한다 —
/// Rect(걸을 수 있는 영역)로 클램프하면 플레이어가 벽에 딱 붙었을 때도 카메라가 딱 그 안쪽에서 멈춰서
/// inset만큼의 테두리 장식이 화면 밖으로 밀려나 "벽에 막혔다"는 게 안 보이는 문제가 있었다.
/// 부착 대상: ArenaBounds (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class ArenaBounds : MonoBehaviour
{
    public static ArenaBounds Instance { get; private set; }

    public Rect Rect => _rect;
    public Rect VisualRect => _visualRect;

    private Rect _rect;
    private Rect _visualRect;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>배경 스프라이트가 바뀌거나 크기가 바뀔 때(시대 전환, arenaScale 조정) 호출된다.</summary>
    public void Recalculate(SpriteRenderer background, Vector2 inset)
    {
        if (background == null) return;

        Bounds b = background.bounds;
        _visualRect = Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
        _rect = Rect.MinMaxRect(
            b.min.x + inset.x, b.min.y + inset.y,
            b.max.x - inset.x, b.max.y - inset.y);
    }

    public Vector2 Clamp(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, _rect.xMin, _rect.xMax),
            Mathf.Clamp(pos.y, _rect.yMin, _rect.yMax));
    }

    public bool Contains(Vector2 pos) => _rect.Contains(pos);

    /// <summary>아레나 둘레 위의 균등 랜덤한 점. 변의 길이에 비례한 가중치로 변을 고른다.</summary>
    public Vector2 RandomPointOnEdge()
    {
        float w = _rect.width;
        float h = _rect.height;
        float perimeter = 2f * (w + h);
        if (perimeter <= 0f) return _rect.center;

        float t = Random.value * perimeter;

        if (t < w) return new Vector2(_rect.xMin + t, _rect.yMin);
        t -= w;
        if (t < h) return new Vector2(_rect.xMax, _rect.yMin + t);
        t -= h;
        if (t < w) return new Vector2(_rect.xMax - t, _rect.yMax);
        t -= w;
        return new Vector2(_rect.xMin, _rect.yMax - t);
    }
}
