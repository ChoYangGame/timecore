using UnityEngine;

/// <summary>
/// 이펙트용 외부 스프라이트(Kenney Particle Pack, CC0)를 지연 로드해 캐시한다.
/// FxTextures와 짝이다 — 저쪽은 코드로 굽고 이쪽은 파일에서 읽는다.
///
/// 절차 생성으로 만들 수 있는 것(점·고리·띠·사각형)은 계속 FxTextures가 만든다.
/// 여기 있는 것은 절차 생성으로는 안 나오는 것들뿐이다 — 불규칙한 스파크, 연기의 노이즈,
/// 불꽃 형상, 그을음 얼룩, 소용돌이. 그래서 5장만 들여왔다.
///
/// Resources를 쓰는 이유: 씬·프리팹에 참조 필드를 새로 다는 것이
/// "Unity 켜진 동안 .unity/.prefab 외부 편집 금지" 규칙과 부딪히고,
/// SerializedObject 주입으로 배열을 채우는 것보다 정적 로드가 훨씬 짧다.
/// 5장 × 128px이라 항상 빌드에 포함되는 Resources의 단점도 무시할 수준이다.
///
/// 부착 대상: 없음 (정적 클래스)
/// </summary>
public static class FxSprites
{
    private const string Root = "FX/";

    private static Sprite _spark, _smoke, _flame, _twirl, _scorch, _slash, _orb;

    /// <summary>참격 호(弧). 칼잡이의 휘두르기와 보스 꼬리 휩쓸기에 쓴다 — 막대로는 "벤다"가 안 나온다.</summary>
    public static Sprite Slash => _slash != null ? _slash : (_slash = Load("kenney_slash_03"));

    // Slash 스프라이트가 1유닛 중 실제로 그려지는 비율(픽셀 실측, 알파 64 기준).
    // 눕혀서 쓰므로 Along = 칼날 두께, Across = 호의 폭이다.
    // 이 값이 있어야 "보이는 칼날 = 판정"을 크기에서 역산할 수 있다 —
    // 칼잡이 참격과 보스 꼬리가 같은 규칙을 쓰라고 여기(스프라이트 쪽)에 둔다.
    public const float SlashOpaqueAlong = 0.14f;
    public const float SlashOpaqueAcross = 0.66f;

    /// <summary>마법 오브. 매지션의 궤도 코어에 쓴다.</summary>
    public static Sprite Orb => _orb != null ? _orb : (_orb = Load("kenney_magic_05"));

    /// <summary>불규칙한 스파크. 전기 방전, 적 사망 파편, 레이저 발사구에 쓴다.</summary>
    public static Sprite Spark => _spark != null ? _spark : (_spark = Load("kenney_spark_04"));

    /// <summary>뭉게지는 연기. 균열의 흙먼지, 보스 잔상처럼 "남는 것"에 쓴다.</summary>
    public static Sprite Smoke => _smoke != null ? _smoke : (_smoke = Load("kenney_smoke_04"));

    /// <summary>불꽃. 현대 연쇄 폭격의 폭발 한 방마다 쓴다.</summary>
    public static Sprite Flame => _flame != null ? _flame : (_flame = Load("kenney_flame_05"));

    /// <summary>소용돌이. 시간 감속 지대에 쓴다 — "시간이 휘었다"를 형태로 보여주는 유일한 장치다.</summary>
    public static Sprite Twirl => _twirl != null ? _twirl : (_twirl = Load("kenney_twirl_02"));

    /// <summary>그을음 얼룩. 균열이 갈라진 자리에 남는다.</summary>
    public static Sprite Scorch => _scorch != null ? _scorch : (_scorch = Load("kenney_scorch_01"));

    /// <summary>시간 감속 지대(맵·보스 기믹) 장판. 디자인 담당 아트.</summary>
    public static Sprite SlowField => _slowField != null ? _slowField : (_slowField = Load("slow_field"));

    /// <summary>매지션 공격 지대 장판. 디자인 담당 아트.</summary>
    public static Sprite TimeStopField => _timeStopField != null ? _timeStopField : (_timeStopField = Load("timestop_field"));

    private static Sprite _slowField, _timeStopField;

    // 두 장판이 프레임 안에서 실제로 그려진 비율(알파 24 기준 픽셀 실측).
    // 그림에 여백이 크게 붙어 있어서, 이 값 없이 스프라이트 크기로 스케일을 내면
    // 보이는 원이 판정보다 30~50% 작아진다. "보이는 테두리 = 판정"을 맞추는 데 필요하다.

    /// <summary>slow_field: 프레임 대비 그려진 원의 지름 비율 (가로 68.0% / 세로 67.2% — 사실상 정원).</summary>
    public const float SlowFieldSpan = 0.68f;

    /// <summary>timestop_field: 프레임 대비 그려진 타원의 가로 비율.</summary>
    public const float TimeStopSpanX = 0.535f;

    /// <summary>timestop_field: 프레임 대비 그려진 타원의 세로 비율. 원근으로 눌려 있어 가로보다 작다.</summary>
    public const float TimeStopSpanY = 0.375f;

    /// <summary>timestop_field: 그림 중심이 프레임 중심보다 아래로 치우친 양(프레임 높이 대비).</summary>
    public const float TimeStopOffsetY = -0.022f;

    /// <summary>
    /// 로드 실패해도 null을 그대로 돌려준다. EffectSystem.Emit이 null이면 FxTextures.Dot으로 떨어지므로
    /// 이펙트가 못생겨질 뿐 게임은 계속 돈다 — 마감 직전에 파일 하나 때문에 멈추는 것이 최악이다.
    /// </summary>
    private static Sprite Load(string name)
    {
        Sprite s = Resources.Load<Sprite>(Root + name);
        if (s == null) Debug.LogWarning($"[FxSprites] Resources/{Root}{name} 를 찾지 못했다. 기본 모양으로 떨어진다.");
        return s;
    }
}
