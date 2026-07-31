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

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void Update()
    {
        if (_health != null && _health.IsDead) return;

        Vector2 input = ReadInput();
        if (input == Vector2.zero) return;

        // 대각선이 빨라지지 않도록 정규화
        input = input.normalized;
        transform.position += (Vector3)(input * (moveSpeed * Time.deltaTime));
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
