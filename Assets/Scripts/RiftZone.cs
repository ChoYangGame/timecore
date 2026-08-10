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
/// **판정은 원이다.** 예전에는 사각형이었는데, 그때는 내장 Square 스프라이트로 그려서
/// 보이는 사각형과 판정이 정확히 일치했기 때문이다. 2026-08-10에 디자인 담당의 원형 장판 아트로
/// 교체하면서 판정도 원으로 바꿨다 — 이 프로젝트의 규칙은 "보이는 것이 곧 판정"이고,
/// 원을 그려 놓고 사각으로 맞히는 쪽이 훨씬 나쁜 거짓말이다.
///
/// 부착 대상: RiftZone 프리팹 (SpriteRenderer만. 콜라이더 없음)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RiftZone : MonoBehaviour
{
    [Tooltip("지대의 지름(월드 유닛). 보이는 원의 지름이 곧 이 값이고, 판정도 같다")]
    [SerializeField] private float size = 6f;

    [Tooltip("지대 안에서의 이동속도 배율. 플레이어와 적에게 똑같이 적용된다")]
    [SerializeField] private float slowMultiplier = 0.5f;

    [SerializeField] private float lifetime = 9f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("외형")]
    [Tooltip("장판 알파. 아트 자체가 선 위주라 진하게 둬도 안의 적을 가리지 않는다")]
    [SerializeField] private float maxAlpha = 0.9f;

    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField] private float pulseAmount = 0.12f;

    [Tooltip("장판 회전 속도(도/초). 정지한 그림은 화려한 맵 위에서 그냥 묻힌다 —\n" +
             "움직임이 가장 강한 시선 유도 장치다")]
    [SerializeField] private float spinSpeed = 14f;

    /// <summary>배경(-1000)보다는 위, 적·플레이어·탄(0)보다는 아래.</summary>
    [SerializeField] private int sortingOrder = -500;

    private static readonly List<RiftZone> ActiveZones = new List<RiftZone>();

    public static int ActiveCount => ActiveZones.Count;

    private SpriteRenderer _renderer;
    private float _elapsed;
    private float _radius;

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

            // 제곱 비교. sqrt를 매 프레임 적 40마리 × 존 3개만큼 돌 이유가 없다.
            Vector2 center = zone.transform.position;
            float dx = pos.x - center.x;
            float dy = pos.y - center.y;
            if (dx * dx + dy * dy > zone._radius * zone._radius) continue;

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

    /// <summary>
    /// 스폰 직후 RiftZoneSpawner가 부른다.
    /// tint는 더 이상 쓰지 않는다 — 장판 아트가 고유 색(보라)을 갖고 있고, 거기에 시대 색을 곱하면
    /// 그림이 그 색조로 덮여 버린다(적·보스 컬러 아트에서 이미 겪은 문제와 같다).
    /// 시대마다 색이 달라지는 것보다 "이 무늬 = 감속"이 항상 같은 편이 읽기 쉽다.
    /// </summary>
    public void Configure(Color tint, float worldSize)
    {
        size = worldSize;

        ApplySprite();
        ApplySize();
        ApplyAlpha();
    }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingOrder = sortingOrder;

        ApplySprite();
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

        // 천천히 돌린다. 무늬가 원형이라 회전이 티가 나고, 그게 "시간이 휘었다"를 읽히게 한다.
        transform.localRotation = Quaternion.Euler(0f, 0f, -_elapsed * spinSpeed);

        ApplyAlpha();
    }

    private void ApplySprite()
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null) return;

        Sprite art = FxSprites.SlowField;
        if (art != null) _renderer.sprite = art;
    }

    /// <summary>
    /// **그려진 원의 지름이 판정 지름과 같아지도록** 스케일을 낸다.
    /// 아트 프레임에 여백이 붙어 있어(그림이 프레임의 68%) 스프라이트 크기를 그대로 쓰면
    /// 보이는 원이 판정보다 32% 작아진다.
    /// </summary>
    private void ApplySize()
    {
        _radius = size * 0.5f;

        if (_renderer == null || _renderer.sprite == null) return;

        float drawnWidth = _renderer.sprite.bounds.size.x * FxSprites.SlowFieldSpan;
        if (drawnWidth <= 0.0001f) return;

        transform.localScale = Vector3.one * (size / drawnWidth);
    }

    private void ApplyAlpha()
    {
        if (_renderer == null) return;

        float k;

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

        // 색은 건드리지 않는다(흰색 곱 = 원본 그대로). 알파만 등장·소멸·맥동으로 움직인다.
        _renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(maxAlpha * k));
    }
}
