using UnityEngine;

/// <summary>
/// 이펙트용 스프라이트를 코드로 만든다. 파일이 아니라 런타임 생성이라 **빌드 용량 증가가 0**이다.
///
/// 이 프로젝트의 이펙트는 전부 내장 Square 한 장을 늘려 쓰고 있었다. 그래서 무엇을 해도
/// 사각형으로 보였다(사용자 지적: "이팩트들이 그냥 너무 네모네모"). 에셋스토어 VFX 팩은
/// 대개 ParticleSystem 기반인데, 이 프로젝트는 저사양 브라우저 때문에 그것을 일부러 버렸다 —
/// 결국 팩에서 텍스처만 꺼내 쓰게 되므로, 텍스처를 직접 만드는 편이 용량·라이선스 모두 이득이다.
///
/// **전부 64x64에 PPU 64로 만든다.** 그래야 모든 스프라이트의 월드 bounds가 정확히 1x1이 되고,
/// 크기 계산이 `worldSize / 1`로 통일된다(스프라이트마다 bounds가 다르면 스케일 식이 어긋난다).
/// 6장 합쳐 약 96KB RAM. 밉맵 없음, Bilinear, Clamp.
///
/// 부착 대상: 없음 (정적 클래스)
/// </summary>
public static class FxTextures
{
    private const int Size = 64;

    private static Sprite _dot, _ring, _glowBar, _softSquare, _edgeGradient, _solid;

    // 도메인 리로드를 끈 에디터에서 이전 판의 텍스처를 물고 있는 것을 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _dot = _ring = _glowBar = _softSquare = _edgeGradient = _solid = null;
    }

    /// <summary>가장자리가 부드러운 원. 조각·불티·파편 기본형.</summary>
    public static Sprite Dot => _dot != null ? _dot : (_dot = Build("FxDot", (u, v) =>
    {
        float r = Radius(u, v);
        // 가운데는 꽉 차고 바깥으로 갈수록 빠르게 사라진다. 1.6을 곱해 코어를 만든다.
        float a = Mathf.Clamp01((1f - r) * 1.6f);
        return a * a;
    }));

    /// <summary>속이 빈 고리. 충격파·순간이동에 쓴다.</summary>
    public static Sprite Ring => _ring != null ? _ring : (_ring = Build("FxRing", (u, v) =>
    {
        float r = Radius(u, v);
        float d = Mathf.Abs(r - 0.72f) / 0.28f;   // 고리 위에서 0, 안팎으로 멀어질수록 1
        float a = Mathf.Clamp01(1f - d);
        return a * a;
    }));

    /// <summary>
    /// 가로로는 균일하고 세로로만 부드럽게 빠지는 띠. 레이저 글로우·예고선처럼
    /// "길게 늘여 쓰는" 층에 쓴다. 사각형을 늘리면 위아래 경계가 칼같이 서서 빛으로 안 보인다.
    /// </summary>
    public static Sprite GlowBar => _glowBar != null ? _glowBar : (_glowBar = Build("FxGlowBar", (u, v) =>
    {
        float t = Mathf.Abs(v * 2f - 1f);         // 중앙 0, 위아래 끝 1
        float a = Mathf.Clamp01(1f - t);
        return a * a;
    }));

    /// <summary>모서리만 살짝 부드러운 사각형. 지면 파편처럼 각이 살아 있어야 하는 것에 쓴다.</summary>
    public static Sprite SoftSquare => _softSquare != null ? _softSquare : (_softSquare = Build("FxSoftSquare", (u, v) =>
    {
        float e = Mathf.Max(Mathf.Abs(u * 2f - 1f), Mathf.Abs(v * 2f - 1f));
        return Mathf.Clamp01((1f - e) / 0.14f);
    }));

    /// <summary>아래(v=0)가 불투명하고 위로 갈수록 사라지는 그라데이션. 화면 가장자리 비네트용.</summary>
    public static Sprite EdgeGradient => _edgeGradient != null ? _edgeGradient : (_edgeGradient = Build("FxEdge", (u, v) =>
    {
        // 지수를 1보다 크게 두면 가장자리에 붙고 안쪽으로 빠르게 옅어진다.
        return Mathf.Pow(1f - v, 1.8f);
    }));

    /// <summary>완전 불투명. 화면 전체 플래시처럼 균일하게 덮어야 하는 곳에.</summary>
    public static Sprite Solid => _solid != null ? _solid : (_solid = Build("FxSolid", (u, v) => 1f));

    /// <summary>중심에서의 거리. 0이 중심, 1이 변에 닿는 원의 가장자리.</summary>
    private static float Radius(float u, float v)
    {
        float dx = u * 2f - 1f;
        float dy = v * 2f - 1f;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// alpha 함수 하나로 텍스처를 굽는다. 색은 전부 흰색이고 알파만 다르다 —
    /// 실제 색은 SpriteRenderer.color가 입히므로 시대별로 텍스처를 따로 만들 필요가 없다.
    /// </summary>
    private static Sprite Build(string name, System.Func<float, float, float> alpha)
    {
        Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };

        Color[] pixels = new Color[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            // 픽셀 중심 기준(+0.5)으로 샘플링해야 좌우가 대칭이 된다.
            float v = (y + 0.5f) / Size;

            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Size;
                pixels[y * Size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(u, v)));
            }
        }

        tex.SetPixels(pixels);
        tex.Apply(false, false);

        // PPU = Size 라서 월드 bounds가 정확히 1x1이 된다.
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
        sprite.name = name;
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
