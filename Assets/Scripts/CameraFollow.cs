using UnityEngine;

/// <summary>
/// 플레이어를 부드럽게 따라간다. z 는 그대로 유지한다.
/// 부착 대상: Main Camera
/// </summary>
[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [Tooltip("비워두면 Player 태그를 가진 오브젝트를 자동으로 찾는다")]
    [SerializeField] private Transform target;

    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField] private Vector2 offset = Vector2.zero;

    private Vector3 _velocity;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target != null) transform.position = DesiredPosition();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = Vector3.SmoothDamp(
            transform.position, DesiredPosition(), ref _velocity, smoothTime);
    }

    private Vector3 DesiredPosition()
    {
        return new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z);
    }
}
