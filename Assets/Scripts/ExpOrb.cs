using UnityEngine;

/// <summary>
/// Enemy 사망 위치에 드롭되는 경험치 오브. 흡수 반경 안에 플레이어가 들어오면 가속 이동해 따라붙고,
/// 닿으면 GameManager에 경험치를 전달하고 사라진다.
/// 부착 대상: ExpOrb 프리팹 (SpriteRenderer + CircleCollider2D isTrigger = true)
/// </summary>
[DisallowMultipleComponent]
public class ExpOrb : MonoBehaviour
{
    [SerializeField] private float absorbRadius = 1.5f;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private int expValue = 1;

    private Transform _player;
    private bool _absorbing;
    private float _effectiveAbsorbRadius;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _player = player.transform;

        float multiplier = GameManager.Instance != null ? GameManager.Instance.ExpAbsorbRadiusMultiplier : 1f;
        _effectiveAbsorbRadius = absorbRadius * multiplier;
    }

    private void Update()
    {
        if (_player == null) return;

        Vector3 toPlayer = _player.position - transform.position;
        float sqrDist = toPlayer.sqrMagnitude;

        if (!_absorbing && sqrDist <= _effectiveAbsorbRadius * _effectiveAbsorbRadius) _absorbing = true;
        if (!_absorbing || sqrDist < 0.0001f) return;

        Vector3 dir = toPlayer / Mathf.Sqrt(sqrDist);
        transform.position += dir * (moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance != null) GameManager.Instance.AddExp(expValue);
        Destroy(gameObject);
    }
}
