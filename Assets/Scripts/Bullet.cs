using UnityEngine;

/// <summary>
/// 지정된 방향으로 직진하는 총알. 적에 닿으면 데미지를 주고 사라진다.
/// 부착 대상: Bullet 프리팹 (BoxCollider2D isTrigger = true)
/// </summary>
[DisallowMultipleComponent]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 14f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifeTime = 2f;

    public float Damage
    {
        get => damage;
        set => damage = value;
    }

    /// <summary>남은 관통 횟수. 0이면 첫 타격에 파괴(기존 동작). AutoAimShooter가 발사 시점에 채운다.</summary>
    public int PierceRemaining { get; set; }

    private Vector2 _direction = Vector2.right;
    private float _age;

    /// <summary>발사 직후 AutoAimShooter 가 호출한다.</summary>
    public void Launch(Vector2 direction)
    {
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        _age = 0f;

        // 길쭉한 스프라이트로 교체돼도 방향이 맞도록 회전만 맞춰둔다.
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
        if (!other.CompareTag("Enemy")) return;

        Health target = other.GetComponent<Health>();
        if (target != null) target.TakeDamage(damage);

        if (PierceRemaining > 0)
        {
            PierceRemaining--;
            return;
        }

        Destroy(gameObject);
    }
}
