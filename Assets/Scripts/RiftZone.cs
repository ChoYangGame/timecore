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
    [Tooltip("바닥에 깔린 것처럼 보이도록 반투명하게 둔다. 적·플레이어는 불투명이라 이것만으로 구분된다")]
    [SerializeField] private float maxAlpha = 0.42f;
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField] private float pulseAmount = 0.15f;

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
        ApplySize();
        ApplyAlpha();
    }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _tint = _renderer.color;
        _renderer.sortingOrder = sortingOrder;
        ApplySize();
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

        float alpha = maxAlpha;

        if (fadeInDuration > 0f && _elapsed < fadeInDuration)
        {
            alpha *= _elapsed / fadeInDuration;
        }
        else if (fadeOutDuration > 0f && _elapsed > lifetime - fadeOutDuration)
        {
            alpha *= Mathf.Max(0f, (lifetime - _elapsed) / fadeOutDuration);
        }
        else
        {
            alpha *= 1f + Mathf.Sin(_elapsed * pulseSpeed) * pulseAmount;
        }

        Color c = _tint;
        c.a = Mathf.Clamp01(alpha);
        _renderer.color = c;
    }
}
