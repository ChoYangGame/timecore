using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시간 감속 지대. 영역 안에 들어온 대상(플레이어·적)의 이동속도를 slowMultiplier 배로 낮춘다.
/// 플레이어와 적에게 똑같이 걸리는 것이 핵심이다 — 적 무리를 끌고 들어가면 이득이고,
/// 혼자 늦게 밟으면 빠져나오지 못해 손해다. 규칙 하나로 판단이 둘로 갈린다.
///
/// 콜라이더를 쓰지 않는다. 대상이 매 프레임 SpeedMultiplierAt()으로 직접 물어보는 방식이라
/// 물리 연산이 0이다(아레나 경계를 위치 클램프로 처리한 것과 같은 원칙).
/// 존은 동시 2~3개 상한이고 적은 maxAlive 40이라 최악에도 프레임당 수백 번의 좌표 비교뿐이다.
///
/// 영역을 원이 아니라 사각으로 잡았다. 프로젝트의 모든 오브젝트가 내장 Square 스프라이트라
/// 원형 스프라이트를 새로 들이지 않으려는 것이고(WebGL 용량), 보이는 사각형과 판정 영역이
/// 정확히 일치하게 하려는 것이다(ArenaBounds가 시각적 벽과 논리적 벽을 일치시킨 것과 같은 이유).
///
/// 부착 대상: RiftZone 프리팹 (SpriteRenderer만. 콜라이더 없음)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RiftZone : MonoBehaviour
{
    [Tooltip("한 변의 길이(월드 유닛). 아레나 걷는 영역이 약 21.6 x 10.9라 6 근처면 화면의 한 구획을 덮는다")]
    [SerializeField] private float size = 6f;

    [Tooltip("지대 안에서의 이동속도 배율. 플레이어와 적에게 똑같이 적용된다")]
    [SerializeField] private float slowMultiplier = 0.5f;

    [SerializeField] private float lifetime = 9f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("외형")]
    [Tooltip("바닥 채움의 알파. 아주 낮게 둔다 — 시대 색이 맵과 같은 계열이라 조금만 진해도\n" +
             "넓은 색 덩어리로 뭉개진다(실측). 존재감은 테두리와 소용돌이가 낸다")]
    [SerializeField] private float maxAlpha = 0.15f;
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField] private float pulseAmount = 0.15f;

    [Tooltip("판정 경계선의 두께 (한 변 대비 비율)")]
    [SerializeField] private float borderThickness = 0.045f;

    [Tooltip("경계선 알파. 채움보다 훨씬 진해야 '여기부터 느려진다'가 읽힌다")]
    [SerializeField] private float borderAlpha = 0.95f;

    [Tooltip("가운데 소용돌이 회전 속도(도/초). 정지한 그림은 화려한 맵 위에서 그냥 묻힌다 —\n" +
             "움직임이 가장 강한 시선 유도 장치다")]
    [SerializeField] private float swirlSpinSpeed = 26f;

    [SerializeField] private float swirlAlpha = 0.8f;

    // 런타임에 만드는 자식 레이어. 프리팹을 건드리지 않으려는 것이다.
    private SpriteRenderer[] _borders;   // 상·하·좌·우 4장 = 판정 사각형의 테두리
    private SpriteRenderer _swirl;       // 가운데 소용돌이 (회전)
    private Transform _swirlTf;

    /// <summary>배경(-1000)보다는 위, 적·플레이어·탄(0)보다는 아래.</summary>
    [SerializeField] private int sortingOrder = -500;

    private static readonly List<RiftZone> ActiveZones = new List<RiftZone>();

    public static int ActiveCount => ActiveZones.Count;

    private SpriteRenderer _renderer;
    private Color _tint = Color.white;
    private float _elapsed;
    private float _halfSize;

    // 도메인 리로드를 끈 에디터에서 static 목록이 이전 판의 잔해를 물고 있는 것을 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveZones.Clear();

    /// <summary>
    /// pos 지점에 걸린 이동속도 배율. 존이 겹치면 가장 강한(가장 작은) 값 하나만 적용된다 —
    /// 곱하면 두 장 겹친 지점에서 사실상 정지해버려 억울한 죽음이 난다.
    /// </summary>
    public static float SpeedMultiplierAt(Vector2 pos)
    {
        float result = 1f;

        for (int i = 0; i < ActiveZones.Count; i++)
        {
            RiftZone zone = ActiveZones[i];
            if (zone == null) continue;

            Vector2 center = zone.transform.position;
            if (Mathf.Abs(pos.x - center.x) > zone._halfSize) continue;
            if (Mathf.Abs(pos.y - center.y) > zone._halfSize) continue;

            if (zone.slowMultiplier < result) result = zone.slowMultiplier;
        }

        return result;
    }

    /// <summary>시대 전환 암전 중 EraManager가 부른다. 남은 지대가 다음 시대로 넘어가지 않게 한다.</summary>
    public static void ClearAll()
    {
        // OnDisable이 목록을 건드리므로 복사본을 돌린다.
        RiftZone[] snapshot = ActiveZones.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            if (snapshot[i] != null) Destroy(snapshot[i].gameObject);

        ActiveZones.Clear();
    }

    /// <summary>스폰 직후 RiftZoneSpawner가 부른다.</summary>
    public void Configure(Color tint, float worldSize)
    {
        size = worldSize;
        _tint = tint;

        // 바닥 채움은 반드시 사각형이어야 한다. 한때 둥근 소용돌이로 바꿔 놨었는데,
        // 판정은 사각형(Abs(dx) > halfSize)이라 보이는 것과 맞는 범위가 어긋났다.
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null) _renderer.sprite = FxTextures.Solid;

        BuildLayers();
        ApplySize();
        ApplyAlpha();
    }

    /// <summary>
    /// 테두리 4장 + 가운데 소용돌이를 자식으로 만든다.
    /// 자식은 루트의 1x1 로컬 공간에서 배치하므로 존 크기가 바뀌어도 비율이 유지된다.
    /// </summary>
    private void BuildLayers()
    {
        if (_borders != null) return;

        _borders = new SpriteRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Border" + i);
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FxTextures.Solid;
            sr.sortingOrder = sortingOrder + 1;
            _borders[i] = sr;

            bool horizontal = i < 2;
            float sign = (i % 2 == 0) ? 1f : -1f;
            float t = borderThickness;

            // 살짝(1.0 + t) 넘겨 그려 모서리에 틈이 생기지 않게 한다.
            go.transform.localScale = horizontal
                ? new Vector3(1f + t, t, 1f)
                : new Vector3(t, 1f + t, 1f);
            go.transform.localPosition = horizontal
                ? new Vector3(0f, sign * 0.5f, 0f)
                : new Vector3(sign * 0.5f, 0f, 0f);
        }

        var swirlGo = new GameObject("Swirl");
        swirlGo.transform.SetParent(transform, false);
        _swirlTf = swirlGo.transform;
        _swirlTf.localScale = Vector3.one * 0.86f;

        _swirl = swirlGo.AddComponent<SpriteRenderer>();
        _swirl.sprite = FxSprites.Twirl != null ? FxSprites.Twirl : FxTextures.Ring;
        _swirl.sortingOrder = sortingOrder + 2;
    }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _tint = _renderer.color;
        _renderer.sortingOrder = sortingOrder;
        _renderer.sprite = FxTextures.Solid;

        // Configure()를 거치지 않고 스폰되는 경로가 있어도 레이어는 있어야 한다.
        BuildLayers();
        ApplySize();
        ApplyAlpha();
    }

    private void OnEnable() => ActiveZones.Add(this);

    private void OnDisable() => ActiveZones.Remove(this);

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (_elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 소용돌이를 천천히 돌린다. 정지한 반투명 그림은 화려한 바닥 위에서 그냥 사라진다 —
        // 존재를 알리는 것은 색이 아니라 움직임이다.
        if (_swirlTf != null)
            _swirlTf.localRotation = Quaternion.Euler(0f, 0f, -_elapsed * swirlSpinSpeed);

        ApplyAlpha();
    }

    /// <summary>
    /// 스프라이트의 실제 월드 크기로 나눠 스케일을 낸다. Square의 PPU가 바뀌어도
    /// 보이는 사각형이 판정과 어긋나지 않는다.
    /// </summary>
    private void ApplySize()
    {
        _halfSize = size * 0.5f;

        if (_renderer == null || _renderer.sprite == null) return;

        float spriteWidth = _renderer.sprite.bounds.size.x;
        if (spriteWidth <= 0.0001f) return;

        transform.localScale = Vector3.one * (size / spriteWidth);
    }

    private void ApplyAlpha()
    {
        if (_renderer == null) return;

        // 등장·소멸·맥동을 하나의 배율로 계산해 세 레이어에 똑같이 곱한다.
        float k = 1f;

        if (fadeInDuration > 0f && _elapsed < fadeInDuration)
        {
            k = _elapsed / fadeInDuration;
        }
        else if (fadeOutDuration > 0f && _elapsed > lifetime - fadeOutDuration)
        {
            k = Mathf.Max(0f, (lifetime - _elapsed) / fadeOutDuration);
        }
        else
        {
            k = 1f + Mathf.Sin(_elapsed * pulseSpeed) * pulseAmount;
        }

        // 바닥 채움 — 옅게. 안에 있는 적을 가리면 안 된다.
        Color fill = _tint;
        fill.a = Mathf.Clamp01(maxAlpha * k);
        _renderer.color = fill;

        // 테두리 — 흰색을 섞어 띄운다. 판정 경계가 어디인지는 이 선 하나로 전달된다.
        if (_borders != null)
        {
            Color edge = Color.Lerp(_tint, Color.white, 0.55f);
            edge.a = Mathf.Clamp01(borderAlpha * k);
            for (int i = 0; i < _borders.Length; i++)
                if (_borders[i] != null) _borders[i].color = edge;
        }

        // 소용돌이 — 밝게. 0.35만 섞으니 시대 색과 맵이 같은 계열이라 묻혔다(실측).
        // 이게 "시간이 휘었다"를 전달하는 유일한 형태라 확실히 띄운다.
        if (_swirl != null)
        {
            Color s = Color.Lerp(_tint, Color.white, 0.6f);
            s.a = Mathf.Clamp01(swirlAlpha * k);
            _swirl.color = s;
        }
    }
}
