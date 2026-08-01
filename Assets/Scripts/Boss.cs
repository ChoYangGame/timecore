using System.Collections;
using UnityEngine;

/// <summary>
/// 원시시대 보스 "고대의 포식자". 평소엔 플레이어를 느리게 추적하고,
/// 돌진(대기 3초 → 1초간 고속 돌진)과 충격파(제자리에서 투사체 8발 방사)를 번갈아 실행한다.
/// 사망 시 경험치 오브를 여러 개 흩뿌린다.
/// 부착 대상: Boss 프리팹 (Health, BoxCollider2D isTrigger = true, Rigidbody2D Kinematic, 태그 Enemy)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class Boss : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.3f;
    [SerializeField] private float contactDamage = 20f;
    [Tooltip("접촉 데미지 재적용까지의 간격(초)")]
    [SerializeField] private float contactCooldown = 0.7f;

    [Header("패턴 A: 돌진")]
    [SerializeField] private float dashWaitTime = 3f;
    [SerializeField] private float dashDuration = 1f;
    [SerializeField] private float dashSpeed = 9f;

    [Header("패턴 B: 충격파")]
    [SerializeField] private BossProjectile projectilePrefab;
    [SerializeField] private int projectileCount = 8;

    [Header("사망 시 드롭")]
    [SerializeField] private ExpOrb expOrbPrefab;
    [SerializeField] private int expOrbDropCount = 10;
    [SerializeField] private float dropScatterRadius = 1f;

    private Health _health;
    private Transform _player;
    private float _nextContactTime;
    private bool _isDashing;
    private Vector2 _dashDirection;

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

        StartCoroutine(PatternLoop());
    }

    private void Update()
    {
        if (_health.IsDead || _player == null) return;

        Vector2 moveDir;
        float speed;

        if (_isDashing)
        {
            moveDir = _dashDirection;
            speed = dashSpeed;
        }
        else
        {
            Vector3 toPlayer = _player.position - transform.position;
            float sqr = toPlayer.sqrMagnitude;
            if (sqr < 0.0001f) return;
            moveDir = toPlayer / Mathf.Sqrt(sqr);
            speed = moveSpeed;
        }

        transform.position += (Vector3)(moveDir * (speed * Time.deltaTime));
    }

    private IEnumerator PatternLoop()
    {
        while (!_health.IsDead)
        {
            yield return PatternDash();
            if (_health.IsDead) yield break;

            PatternShockwave();
            yield return null;
        }
    }

    private IEnumerator PatternDash()
    {
        yield return new WaitForSeconds(dashWaitTime);
        if (_health.IsDead || _player == null) yield break;

        Vector3 toPlayer = _player.position - transform.position;
        _dashDirection = toPlayer.sqrMagnitude > 0.0001f ? ((Vector2)toPlayer).normalized : Vector2.right;
        _isDashing = true;

        yield return new WaitForSeconds(dashDuration);
        _isDashing = false;
    }

    private void PatternShockwave()
    {
        if (projectilePrefab == null) return;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = (360f / projectileCount) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            BossProjectile p = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            p.Launch(dir);
        }
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
        StopAllCoroutines();

        if (GameManager.Instance != null) GameManager.Instance.RegisterKill();

        if (expOrbPrefab != null)
        {
            for (int i = 0; i < expOrbDropCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * dropScatterRadius;
                Instantiate(expOrbPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            }
        }

        Destroy(gameObject);
    }
}
