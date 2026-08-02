using UnityEngine;

/// <summary>
/// 카메라를 따라다니는 타일 스프라이트 1장으로 무한 스크롤 격자 배경을 만든다.
///
/// 타일 오브젝트를 생성·파괴하지 않는다. 격자는 한 칸 단위로 완전히 동일하므로,
/// 오브젝트 위치를 타일 크기에 스냅시키면 패턴이 월드에 고정된 것처럼 보인다.
/// 카메라가 반 칸 움직이면 배경은 제자리 → 상대적으로 반 칸 밀린 것처럼 보이고,
/// 한 칸을 넘어가면 정확히 한 칸 점프하는데 패턴이 같아서 눈에 띄지 않는다.
///
/// 드로우콜 1, 메시는 size가 바뀔 때만 재생성(사실상 시대 전환 시 2번),
/// 매 프레임 하는 일은 위치 대입 하나뿐이라 저사양 브라우저에서도 부담이 없다.
/// 격자 텍스처는 런타임에 절차 생성하므로 빌드 용량이 늘지 않는다.
///
/// 카메라 배경색은 EraManager가 그대로 관리하고, 이 컴포넌트는 그 위에 반투명 라인만 얹는다.
/// EraManager를 수정하지 않기 위해 CurrentEra를 매 프레임 비교만 한다(enum 비교 1회).
/// 부착 대상: BackgroundGrid (SpriteRenderer가 있는 빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundGrid : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [Tooltip("비워두면 원시 설정으로 고정된다")]
    [SerializeField] private EraManager eraManager;

    [Header("원시 시대")]
    [SerializeField] private float primitiveTileSize = 2.5f;
    [Tooltip("배경(#2A3A2E)보다 약간 밝은 톤. 알파로 세기를 조절한다")]
    [SerializeField] private Color primitiveLineColor = new Color(0.55f, 0.75f, 0.60f, 0.10f);

    [Header("중세 시대")]
    [Tooltip("원시보다 촘촘하게 — 정돈된 시대라는 인상을 준다")]
    [SerializeField] private float medievalTileSize = 1.8f;
    [SerializeField] private Color medievalLineColor = new Color(0.62f, 0.55f, 0.80f, 0.10f);

    [Header("격자 텍스처 (런타임 생성)")]
    [SerializeField] private int texturePixels = 128;
    [Tooltip("타일 한 칸에서 라인이 차지하는 픽셀 수")]
    [SerializeField] private int lineThickness = 2;

    [Header("배치")]
    [Tooltip("게임플레이 스프라이트(z=0)보다 뒤")]
    [SerializeField] private float zDepth = 1f;
    [SerializeField] private int sortingOrder = -1000;
    [Tooltip("화면 밖으로 얼마나 더 덮을지. 1.2 = 20% 여유")]
    [SerializeField] private float coverMargin = 1.2f;

    private SpriteRenderer _renderer;
    private Sprite _primitiveSprite;
    private Sprite _medievalSprite;

    private float _tileSize;
    private bool _eraApplied;
    private EraManager.Era _appliedEra;
    private float _appliedOrthoSize = -1f;
    private float _appliedAspect = -1f;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        _renderer = GetComponent<SpriteRenderer>();
        _renderer.drawMode = SpriteDrawMode.Tiled;
        _renderer.tileMode = SpriteTileMode.Continuous;
        _renderer.sortingOrder = sortingOrder;

        Texture2D tex = BuildGridTexture();
        _primitiveSprite = MakeTileSprite(tex, primitiveTileSize);
        _medievalSprite = MakeTileSprite(tex, medievalTileSize);
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        EraManager.Era era = eraManager != null ? eraManager.CurrentEra : EraManager.Era.Primitive;
        if (!_eraApplied || era != _appliedEra) ApplyEra(era);

        // 창 크기가 바뀌었을 때만 다시 계산한다 (메시 재생성을 매 프레임 유발하지 않기 위해)
        if (!Mathf.Approximately(targetCamera.orthographicSize, _appliedOrthoSize) ||
            !Mathf.Approximately(targetCamera.aspect, _appliedAspect))
        {
            Resize();
        }

        // 타일 격자에 스냅. 패턴이 한 칸마다 동일하므로 점프가 보이지 않는다.
        Vector3 cam = targetCamera.transform.position;
        transform.position = new Vector3(
            Mathf.Floor(cam.x / _tileSize) * _tileSize,
            Mathf.Floor(cam.y / _tileSize) * _tileSize,
            zDepth);
    }

    private void ApplyEra(EraManager.Era era)
    {
        bool medieval = era == EraManager.Era.Medieval;

        _tileSize = medieval ? medievalTileSize : primitiveTileSize;
        _renderer.sprite = medieval ? _medievalSprite : _primitiveSprite;
        _renderer.color = medieval ? medievalLineColor : primitiveLineColor;

        _appliedEra = era;
        _eraApplied = true;

        Resize();
    }

    /// <summary>덮을 크기를 타일의 정수 배로 맞춘다. 반칸이 남으면 타일 위상이 흔들린다.</summary>
    private void Resize()
    {
        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;

        int cols = Mathf.CeilToInt(viewWidth * coverMargin / _tileSize) + 1;
        int rows = Mathf.CeilToInt(viewHeight * coverMargin / _tileSize) + 1;

        _renderer.size = new Vector2(cols * _tileSize, rows * _tileSize);

        _appliedOrthoSize = targetCamera.orthographicSize;
        _appliedAspect = targetCamera.aspect;
    }

    /// <summary>왼쪽·아래 모서리에만 선을 그린 타일. 반복하면 격자가 된다.</summary>
    private Texture2D BuildGridTexture()
    {
        int n = Mathf.Max(8, texturePixels);
        int t = Mathf.Clamp(lineThickness, 1, n / 4);

        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            // 스크롤이 부드럽도록 Bilinear. Point는 이동 중 선이 픽셀 단위로 튄다.
            filterMode = FilterMode.Bilinear,
            name = "BackgroundGridTile"
        };

        var pixels = new Color32[n * n];
        var clear = new Color32(255, 255, 255, 0);
        var line = new Color32(255, 255, 255, 255);

        for (int y = 0; y < n; y++)
        {
            bool rowIsLine = y < t;
            int row = y * n;
            for (int x = 0; x < n; x++)
                pixels[row + x] = (rowIsLine || x < t) ? line : clear;
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    /// <summary>스프라이트 한 장이 정확히 타일 한 칸(worldSize)이 되도록 pixelsPerUnit을 맞춘다.</summary>
    private static Sprite MakeTileSprite(Texture2D tex, float worldSize)
    {
        float ppu = tex.width / Mathf.Max(0.01f, worldSize);
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
    }
}
