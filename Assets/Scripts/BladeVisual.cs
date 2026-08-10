using UnityEngine;

/// <summary>
/// 칼잡이가 들고 다니는 시간의 홀 그림.
///
/// **자세를 정하지 않는다.** 프레임 번호·회전·반전·쥔 손 위치는 전부 <see cref="BladeWeapon"/>이
/// 판정을 계산하면서 같이 낸 값이고, 이 컴포넌트는 그대로 받아 그리기만 한다.
/// 그림이 자세를 정하고 판정이 따라가는 구조였으면 둘이 어긋날 수 있는데,
/// 이번 요청 자체가 "그림에 히트박스를 만들어 달라"이므로 어긋남을 아예 만들 수 없게 뒤집었다.
///
/// 원본 10장은 자루 중간의 한 점을 축으로 도는 순수 회전이라(무게중심 예측 오차 5px),
/// 공통 크롭 + 그 회전축에 맞춘 피벗으로 반입했다. 그래서 프레임만 넘겨도 홀이 흔들림 없이 돈다.
///
/// 부착 대상: Player의 자식 "BladeVisual" (SpriteRenderer만)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class BladeVisual : MonoBehaviour
{
    [Tooltip("휘두르기 프레임. BladeWeapon.frameAngles와 장수가 맞아야 그림과 판정이 같은 각도를 쓴다")]
    [SerializeField] private Sprite[] swingFrames;

    [Tooltip("휘두르지 않을 때 홀이 플레이어 옆에 놓이는 거리")]
    [SerializeField] private float idleHoldRadius = 0.45f;

    [Tooltip("쉬는 동안 홀이 플레이어가 보는 쪽으로 도는 속도(초당 도)")]
    [SerializeField] private float idleTurnSpeed = 720f;

    private SpriteRenderer _sr;
    private BladeWeapon _blade;
    private PlayerVisual _visual;
    private Health _health;

    /// <summary>후방 참격 증강을 먹었을 때만 생기는 반대쪽 홀. 판정도 BladeWeapon이 같이 낸다.</summary>
    private SpriteRenderer _backSr;

    private float _idleAngle;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        Transform p = transform.parent;
        if (p != null)
        {
            _blade = p.GetComponent<BladeWeapon>();
            _visual = p.GetComponent<PlayerVisual>();
            _health = p.GetComponent<Health>();

            SpriteRenderer parentSr = p.GetComponent<SpriteRenderer>();
            if (parentSr != null) _sr.sortingOrder = parentSr.sortingOrder + 1;
        }

        if (swingFrames != null && swingFrames.Length > 0) _sr.sprite = swingFrames[0];
    }

    private void LateUpdate()
    {
        // 칼잡이가 아니면 홀 자체가 없어야 한다. 무기 셋 다 Player에 붙어 있고
        // 고른 하나만 enabled라 그 값을 그대로 본다.
        bool show = _blade != null && _blade.enabled
                    && (_health == null || !_health.IsDead);
        if (_sr.enabled != show) _sr.enabled = show;
        if (_backSr != null && _backSr.enabled != (show && _blade.BackSwing))
            _backSr.enabled = show && _blade.BackSwing;
        if (!show) return;

        if (_blade.IsSwinging) DrawSwing();
        else DrawIdle();
    }

    /// <summary>
    /// BladeWeapon이 이번 프레임에 판정한 자세를 그대로 옮긴다.
    /// LateUpdate에서 도는 이유: 무기의 Update가 자세를 확정한 **뒤**에 그려야
    /// 한 프레임 늦은 그림이 나오지 않는다.
    /// </summary>
    private void DrawSwing()
    {
        int frame = Mathf.Clamp(_blade.FrameIndex, 0, SafeCount() - 1);
        if (frame < 0) return;

        _sr.sprite = swingFrames[frame];
        _sr.flipY = _blade.Mirrored;
        transform.position = _blade.PivotWorld;
        transform.rotation = Quaternion.Euler(0f, 0f, _blade.RotationDeg);
        transform.localScale = Vector3.one * Mathf.Max(0.01f, _blade.ReachMultiplier);

        _idleAngle = _blade.RotationDeg;

        if (!_blade.BackSwing) return;

        EnsureBack();
        // 뒤쪽 홀은 플레이어를 중심으로 앞쪽을 180도 돌린 것 — 판정 쪽 계산과 같은 규칙이다.
        Vector3 origin = transform.parent.position;
        _backSr.transform.position = origin * 2f - (Vector3)_blade.PivotWorld;
        _backSr.transform.rotation = Quaternion.Euler(0f, 0f, _blade.RotationDeg + 180f);
        _backSr.transform.localScale = transform.localScale;
        _backSr.sprite = _sr.sprite;
        _backSr.flipY = _sr.flipY;
    }

    /// <summary>휘두르지 않을 때. 판정이 없는 시간이라 그림만 플레이어 옆에 얹어 둔다.</summary>
    private void DrawIdle()
    {
        if (SafeCount() > 0) _sr.sprite = swingFrames[0];

        float target = _visual != null && _visual.FacingLeft ? 180f : 0f;
        _idleAngle = Mathf.MoveTowardsAngle(_idleAngle, target, idleTurnSpeed * Time.deltaTime);

        float rad = _idleAngle * Mathf.Deg2Rad;
        transform.position = transform.parent.position
                             + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * idleHoldRadius;
        transform.rotation = Quaternion.Euler(0f, 0f, _idleAngle);
        transform.localScale = Vector3.one * Mathf.Max(0.01f, _blade.ReachMultiplier);
        _sr.flipY = Mathf.Abs(Mathf.DeltaAngle(_idleAngle, 0f)) > 90f;
    }

    private int SafeCount() => swingFrames == null ? 0 : swingFrames.Length;

    private void EnsureBack()
    {
        if (_backSr != null) return;

        var go = new GameObject("BladeVisualBack");
        go.transform.SetParent(transform.parent, false);
        _backSr = go.AddComponent<SpriteRenderer>();
        _backSr.sortingOrder = _sr.sortingOrder;
        _backSr.color = _sr.color;
    }
}
