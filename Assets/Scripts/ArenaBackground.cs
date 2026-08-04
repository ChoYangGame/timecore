using UnityEngine;

/// <summary>
/// 아레나 배경 스프라이트(원시/중세 아트) 1장을 시대에 맞춰 교체한다.
/// BackgroundGrid와 동일한 폴링 컨벤션을 쓴다: EraManager를 건드리지 않기 위해
/// LateUpdate에서 CurrentEra를 매 프레임 비교만 한다(enum 비교 1회, 저사양 부담 없음).
///
/// 시대 전환은 EraManager.TransitionRoutine이 완전 암전 상태에서 ApplyEra()를 호출하는데,
/// CurrentEra가 바뀌는 바로 그 프레임에 이 폴링이 스프라이트를 갈아치우므로
/// 화면이 검은 동안 교체가 끝나 팝인이 보이지 않는다.
///
/// arenaScale로 인스펙터에서 아레나 전체 크기(화면의 1.3~1.5배 권장)를 조정한다.
/// 배경을 키우면 ArenaBounds도 renderer.bounds 기반이라 함께 커진다.
/// 단, inset은 월드 절대값이라 arenaScale을 크게 바꾸면 테두리 여백 비율이 달라지니
/// 그럴 땐 inset 값도 같이 조정할 것.
///
/// 부착 대상: ArenaBackground (SpriteRenderer가 있는 빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class ArenaBackground : MonoBehaviour
{
    [SerializeField] private EraManager eraManager;

    [Header("시대별 스프라이트")]
    [SerializeField] private Sprite primitiveSprite;
    [SerializeField] private Sprite medievalSprite;

    [Header("시대별 inset (테두리 장식만큼 안쪽으로)")]
    [SerializeField] private Vector2 primitiveInset = new Vector2(1.7f, 1.6f);
    [SerializeField] private Vector2 medievalInset = new Vector2(1.7f, 1.6f);

    [Tooltip("아레나 전체 크기 배율. 화면의 1.3~1.5배 권장")]
    [SerializeField] private float arenaScale = 1f;

    [SerializeField] private int sortingOrder = -1000;

    private SpriteRenderer _renderer;
    private EraManager.Era _appliedEra;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingOrder = sortingOrder;
        transform.localScale = Vector3.one * arenaScale;

        // ArenaBounds보다 먼저 Awake가 돌면(스크립트 실행 순서 미보장) 여기서는 스프라이트만 세팅되고
        // Recalculate는 조용히 스킵된다 — Start()에서 다시 한 번 확실히 맞춘다(모든 Awake는 모든 Start보다 먼저 끝난다는 것만은 Unity가 보장).
        Apply(CurrentEra());
    }

    private void Start()
    {
        Apply(CurrentEra());
    }

    private void LateUpdate()
    {
        EraManager.Era era = CurrentEra();
        if (era != _appliedEra) Apply(era);
    }

    private EraManager.Era CurrentEra() => eraManager != null ? eraManager.CurrentEra : EraManager.Era.Primitive;

    private void Apply(EraManager.Era era)
    {
        bool medieval = era == EraManager.Era.Medieval;

        _renderer.sprite = medieval ? medievalSprite : primitiveSprite;

        _appliedEra = era;

        if (ArenaBounds.Instance != null)
            ArenaBounds.Instance.Recalculate(_renderer, medieval ? medievalInset : primitiveInset);
    }
}
