using UnityEngine;

/// <summary>
/// fireInterval 마다 가장 가까운 적을 향해 자동 발사한다. 적이 없으면 쏘지 않는다.
/// 적 탐색은 매 프레임이 아니라 발사 시점(발사 주기당 1회)에만 수행한다.
/// 부착 대상: Player
/// </summary>
[DisallowMultipleComponent]
public class AutoAimShooter : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private float fireInterval = 0.45f;

    [Tooltip("이 거리 안에 적이 없으면 발사하지 않는다")]
    [SerializeField] private float range = 14f;

    [Tooltip("총알이 플레이어 콜라이더 안에서 생기지 않도록 밀어내는 거리")]
    [SerializeField] private float muzzleOffset = 0.45f;

    public float FireInterval
    {
        get => fireInterval;
        set => fireInterval = value;
    }

    private float _timer;
    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void Update()
    {
        if (bulletPrefab == null) return;
        if (_health != null && _health.IsDead) return;

        _timer += Time.deltaTime;
        if (_timer < fireInterval) return;
        _timer = 0f;

        Transform target = FindNearestEnemy();
        if (target == null) return;

        Vector2 dir = (Vector2)(target.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir = dir.normalized;

        Vector3 spawnPos = transform.position + (Vector3)(dir * muzzleOffset);
        Bullet bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.Launch(dir);
    }

    /// <summary>
    /// 발사 주기마다 한 번만 도는 탐색이라 태그 검색으로 충분하다.
    /// (물리 쿼리를 쓰지 않으므로 저사양에서도 부담이 없다)
    /// </summary>
    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return null;

        Vector3 origin = transform.position;
        float bestSqr = range * range;
        Transform best = null;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null) continue;

            float sqr = (e.transform.position - origin).sqrMagnitude;
            if (sqr > bestSqr) continue;

            bestSqr = sqr;
            best = e.transform;
        }

        return best;
    }
}
