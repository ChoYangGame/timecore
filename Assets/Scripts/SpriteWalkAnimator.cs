using UnityEngine;

/// <summary>
/// 스프라이트 프레임을 순서대로 돌려 걷는 모습을 만들고, 진행 방향에 맞춰 좌우를 뒤집는다.
///
/// Animator/AnimationClip을 쓰지 않는 이유: 시대마다 프레임 묶음이 달라지는데
/// Animator를 쓰면 시대 수만큼 컨트롤러 애셋을 만들고 프리팹을 갈아끼워야 한다.
/// 지금 필요한 건 "배열을 순서대로 넘긴다" 하나뿐이라 코드가 훨씬 짧고,
/// 프레임이 아직 없는 시대는 컴포넌트가 조용히 놀기만 하면 된다.
///
/// 원본 아트는 전부 **오른쪽을 본다**. 왼쪽으로 갈 때는 이미지를 따로 만들지 않고
/// flipX로 뒤집는다 — 프레임이 6장에서 12장으로 늘면 그만큼 용량이 두 배가 되는데,
/// 빌드 용량이 곧 로딩 시간인 WebGL에서는 그냥 낭비다.
///
/// 부착 대상: Enemy 프리팹 (SpriteRenderer와 같은 GameObject)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteWalkAnimator : MonoBehaviour
{
    [Tooltip("초당 프레임 수")]
    [SerializeField] private float framesPerSecond = 10f;

    [Tooltip("이 값보다 조금이라도 더 움직여야 바라보는 방향을 바꾼다.\n" +
             "감속 지대에서 미세하게 떨 때 좌우로 파닥이는 것을 막는다")]
    [SerializeField] private float turnThreshold = 0.002f;

    private SpriteRenderer _sr;
    private Sprite[] _frames;
    private float _timer;
    private int _index;
    private Vector3 _lastPos;
    private bool _facingLeft;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _lastPos = transform.position;
    }

    /// <summary>
    /// 스폰 직후 WaveManager가 그 시대의 프레임을 넘긴다.
    /// 프레임이 없으면(아트가 아직 없는 시대) 아무것도 하지 않고,
    /// Health.SetAppearance가 넣어 둔 정지 스프라이트가 그대로 남는다.
    /// </summary>
    public void SetFrames(Sprite[] frames)
    {
        _frames = frames != null && frames.Length > 0 ? frames : null;
        if (_frames == null) return;

        // 시작 프레임을 흩어 둔다. 전부 같은 프레임부터 시작하면
        // 무리 전체가 한 몸처럼 발을 맞춰 걸어 어색하다.
        _index = Random.Range(0, _frames.Length);
        _timer = Random.value / Mathf.Max(framesPerSecond, 0.01f);
        _sr.sprite = _frames[_index];
    }

    private void Update()
    {
        // 방향 뒤집기는 프레임이 없어도(정지 스프라이트만 있어도) 해 준다.
        Vector3 pos = transform.position;
        float dx = pos.x - _lastPos.x;
        _lastPos = pos;

        if (Mathf.Abs(dx) > turnThreshold)
        {
            bool left = dx < 0f;
            if (left != _facingLeft)
            {
                _facingLeft = left;
                _sr.flipX = left;      // 원본이 오른쪽을 보므로 왼쪽으로 갈 때만 뒤집는다
            }
        }

        if (_frames == null) return;

        _timer += Time.deltaTime;
        float step = 1f / Mathf.Max(framesPerSecond, 0.01f);
        if (_timer < step) return;

        // 한 프레임에 여러 칸이 밀릴 만큼 늦었어도 루프 없이 한 번에 따라잡는다.
        int advance = (int)(_timer / step);
        _timer -= advance * step;

        _index = (_index + advance) % _frames.Length;
        _sr.sprite = _frames[_index];
    }
}
