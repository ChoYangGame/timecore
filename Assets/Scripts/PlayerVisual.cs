using UnityEngine;

/// <summary>
/// 플레이어 호버 애니메이션 + 방향키에 따른 좌우 반전.
///
/// 적용된 <see cref="SpriteWalkAnimator"/>를 쓰지 않고 따로 만든 이유는 두 가지다.
/// (1) 적은 이동량(dx)으로 방향을 정하는데 플레이어는 **방향키 입력**으로 정해야 한다 —
///     벽에 붙어 밀고 있을 때 이동량은 0이라 방향이 굳어 버린다.
/// (2) 적·보스가 전부 매달려 있는 컴포넌트를 마감 직전에 건드리지 않으려는 것.
///
/// 프레임 그림은 제자리에서 위아래로 뜨는 호버다. 6장의 몸통 높이가 348px로 일정하고
/// y만 오르내려 **공통 크롭**으로 반입했다 — 프레임마다 잘랐으면 이 부유가 사라졌을 것이다.
/// 스프라이트 피벗도 화염을 뺀 몸통 중심(0.630)에 맞춰 두어, 몸이 오르내려도
/// 1×1 콜라이더는 제자리에 남는다.
///
/// 부착 대상: Player (SpriteRenderer와 같은 GameObject)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerVisual : MonoBehaviour
{
    [Tooltip("호버 프레임. 순서대로 돌아간다")]
    [SerializeField] private Sprite[] hoverFrames;

    [Tooltip("초당 프레임 수. 호버는 몹 걷기보다 느긋해야 떠 있는 것으로 읽힌다")]
    [SerializeField] private float framesPerSecond = 8f;

    [Tooltip("원본 그림이 왼쪽을 보고 있으면 켠다. 지금 그림은 정면 대칭이라 결과가 같다")]
    [SerializeField] private bool artFacesLeft;

    /// <summary>지금 왼쪽을 보고 있는지. 총이 쉴 때 어느 쪽을 겨눌지 정할 때 읽는다.</summary>
    public bool FacingLeft { get; private set; }

    private SpriteRenderer _sr;
    private PlayerMove _move;
    private float _timer;
    private int _index;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _move = GetComponent<PlayerMove>();
    }

    /// <summary>
    /// Awake가 아니라 Start인 이유: Health.Awake가 렌더러의 현재 색을 원본색으로 캐시한다.
    /// 여기서 먼저 흰색으로 바꿔 버리면 실행 순서에 따라 덮어써질 수 있다.
    ///
    /// SetAppearance를 거치는 것은 적·보스와 같은 이유다 — 컬러 아트에 시안을 곱하면 색이 죽으므로
    /// 원본색은 흰색이 되고, 시안은 **파편·사망 연출용 강조색**으로 넘어가 그대로 살아남는다.
    /// 피격 플래시도 아트용(전체 흰색이 아닌) 틴트로 바뀐다.
    /// </summary>
    private void Start()
    {
        if (hoverFrames == null || hoverFrames.Length == 0) return;

        Health health = GetComponent<Health>();
        if (health != null) health.SetAppearance(hoverFrames[0], _sr.color);
        else _sr.sprite = hoverFrames[0];
    }

    private void Update()
    {
        if (_move != null)
        {
            float x = _move.LastInput.x;
            // 세로로만 움직일 때는 보던 쪽을 유지한다. 0으로 매번 초기화하면
            // 위아래 이동 중에 방향이 오른쪽으로 튄다.
            if (!Mathf.Approximately(x, 0f))
            {
                FacingLeft = x < 0f;
                _sr.flipX = artFacesLeft ? !FacingLeft : FacingLeft;
            }
        }

        if (hoverFrames == null || hoverFrames.Length == 0) return;

        _timer += Time.deltaTime;
        float step = 1f / Mathf.Max(framesPerSecond, 0.01f);
        if (_timer < step) return;

        int advance = (int)(_timer / step);
        _timer -= advance * step;

        _index = (_index + advance) % hoverFrames.Length;
        _sr.sprite = hoverFrames[_index];
    }
}
