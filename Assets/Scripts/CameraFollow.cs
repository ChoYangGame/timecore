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
    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

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
        float x = target.position.x + offset.x;
        float y = target.position.y + offset.y;

        if (ArenaBounds.Instance != null && _cam != null)
        {
            // 걸을 수 있는 영역(Rect)이 아니라 그림 실제 크기(VisualRect)로 클램프한다.
            // Rect로 클램프하면 플레이어가 벽에 붙어도 카메라가 그 안쪽에서 멈춰서
            // inset만큼의 테두리 장식이 화면 밖으로 밀려나 벽에 막힌 느낌이 안 보인다.
            Rect r = ArenaBounds.Instance.VisualRect;
            float halfHeight = _cam.orthographicSize;
            float halfWidth = halfHeight * _cam.aspect;

            // 아레나가 뷰보다 작거나 같은 축은 카메라를 고정한다. 큰 축만 추적+클램프한다.
            x = r.width <= halfWidth * 2f ? r.center.x : Mathf.Clamp(x, r.xMin + halfWidth, r.xMax - halfWidth);
            y = r.height <= halfHeight * 2f ? r.center.y : Mathf.Clamp(y, r.yMin + halfHeight, r.yMax - halfHeight);
        }

        return new Vector3(x, y, transform.position.z);
    }
}
