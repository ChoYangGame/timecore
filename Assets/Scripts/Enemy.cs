using UnityEngine;

/// <summary>
/// 플레이어를 향해 transform 으로 직접 이동하고, 닿아 있는 동안 쿨다운마다 접촉 데미지를 준다.
/// 부착 대상: Enemy 프리팹 (Health, BoxCollider2D isTrigger = true 와 같은 GameObject)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float contactDamage = 8f;
    [SerializeField] private ExpOrb expOrbPrefab;

    [Tooltip("접촉 데미지 재적용까지의 간격(초)")]
    [SerializeField] private float contactCooldown = 0.7f;

    private Health _health;
    private Transform _player;
    private float _nextContactTime;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _health.OnDeath += HandleDeath;
    }

    private void OnDestroy()
    {
        if (_health != null) _health.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _player = player.transform;
    }

    private void Update()
    {
        if (_player == null || _health.IsDead) return;

        Vector3 toPlayer = _player.position - transform.position;
        float sqr = toPlayer.sqrMagnitude;
        if (sqr < 0.0001f) return;

        Vector3 dir = toPlayer / Mathf.Sqrt(sqr);
        transform.position += dir * (moveSpeed * Time.deltaTime);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_health.IsDead) return;
        if (Time.time < _nextContactTime) return;
        if (!other.CompareTag("Player")) return;

        Health target = other.GetComponent<Health>();
        if (target == null || target.IsDead) return;

        target.TakeDamage(contactDamage);
        _nextContactTime = Time.time + contactCooldown;
    }

    private void HandleDeath(Health _)
    {
        if (GameManager.Instance != null) GameManager.Instance.RegisterKill();
        if (expOrbPrefab != null) Instantiate(expOrbPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
