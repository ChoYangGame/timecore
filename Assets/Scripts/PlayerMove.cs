using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WASD / 화살표 8방향 이동. transform 직접 이동(물리 미사용).
/// 부착 대상: Player
/// </summary>
[DisallowMultipleComponent]
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    /// <summary>
    /// 이번 프레임에 눌린 방향키(정규화 전). 그림 쪽에서 바라보는 방향을 정할 때 읽는다.
    ///
    /// 실제 이동량(transform 변화)이 아니라 **입력**을 내보내는 이유: 아레나 경계에 붙어
    /// 벽을 밀고 있으면 이동량이 0이라, 이동량으로 방향을 정하면 벽에 닿는 순간
    /// 캐릭터가 엉뚱한 쪽을 본 채로 굳는다.
    /// </summary>
    public Vector2 LastInput { get; private set; }

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void Update()
    {
        if (_health != null && _health.IsDead) { LastInput = Vector2.zero; return; }

        Vector2 input = ReadInput();
        LastInput = input;
        if (input == Vector2.zero) return;

        // 대각선이 빨라지지 않도록 정규화
        input = input.normalized;

        // 시간 감속 지대 안이면 느려진다. 적에게도 같은 배율이 걸린다.
        float speed = moveSpeed * RiftZone.SpeedMultiplierAt(transform.position);
        Vector3 next = transform.position + (Vector3)(input * (speed * Time.deltaTime));

        if (ArenaBounds.Instance != null)
        {
            Vector2 clamped = ArenaBounds.Instance.Clamp(next);
            next.x = clamped.x;
            next.y = clamped.y;
        }
        transform.position = next;
    }

    /// <summary>
    /// Active Input Handling 이 Input System Package (New) 라서 Keyboard.current 를 쓴다.
    /// 브라우저가 아직 포커스를 주지 않았을 때 null 이 될 수 있다.
    /// </summary>
    private static Vector2 ReadInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        float x = 0f;
        float y = 0f;

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;

        return new Vector2(x, y);
    }
}
