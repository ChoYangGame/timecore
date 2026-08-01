using UnityEngine;

/// <summary>
/// 보스 충격파 패턴이 방사하는 투사체. Bullet과 별개로, 플레이어에게 데미지를 준다.
/// 직선 이동, 수명 만료 시 자동 파괴.
/// 부착 대상: BossProjectile 프리팹 (BoxCollider2D isTrigger = true)
/// </summary>
[DisallowMultipleComponent]
public class BossProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float damage = 12f;
    [SerializeField] private float lifeTime = 3f;

    private Vector2 _direction = Vector2.right;
    private float _age;

    public void Launch(Vector2 direction)
    {
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _age = 0f;
        transform.right = _direction;
    }

    private void Update()
    {
        _age += Time.deltaTime;
        if (_age >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(_direction * (speed * Time.deltaTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Health target = other.GetComponent<Health>();
        if (target != null) target.TakeDamage(damage);

        Destroy(gameObject);
    }
}
