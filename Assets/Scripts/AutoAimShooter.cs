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

    [Tooltip("다중 사출 증강이 아무리 누적돼도 한 번에 나가는 총알 수는 이 값을 못 넘는다 (저사양 보호)")]
    [SerializeField] private int maxBulletCount = 5;

    [Tooltip("총알 2발 이상일 때 사이 각도(도)")]
    [SerializeField] private float multiShotSpreadAngle = 10f;

    public float FireInterval
    {
        get => fireInterval;
        set => fireInterval = value;
    }

    /// <summary>증강으로 누적되는 데미지 배율. 총알 프리팹의 기본 데미지에 곱해서 적용한다.</summary>
    public float DamageMultiplier { get; set; } = 1f;

    /// <summary>증강으로 누적되는 관통 횟수. 발사되는 총알마다 그대로 복사된다.</summary>
    public int PierceCount { get; set; }

    /// <summary>증강으로 누적되는 추가 발사 수. 실제 발사 수는 maxBulletCount로 상한이 걸린다.</summary>
    public int ExtraShots { get; set; }

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

        int shotCount = Mathf.Clamp(1 + ExtraShots, 1, maxBulletCount);
        float startAngle = -(shotCount - 1) * multiShotSpreadAngle * 0.5f;

        for (int i = 0; i < shotCount; i++)
        {
            Vector2 shotDir = shotCount == 1 ? dir : Rotate(dir, startAngle + i * multiShotSpreadAngle);

            Vector3 spawnPos = transform.position + (Vector3)(shotDir * muzzleOffset);
            Bullet bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            bullet.Damage = bulletPrefab.Damage * DamageMultiplier;
            bullet.PierceRemaining = PierceCount;
            bullet.Launch(shotDir);
        }
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
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
