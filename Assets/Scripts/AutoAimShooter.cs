using UnityEngine;

/// <summary>
/// 총잡이. fireInterval 마다 가장 가까운 적을 향해 자동 발사한다. 적이 없으면 쏘지 않는다.
/// 적 탐색은 매 프레임이 아니라 발사 시점(발사 주기당 1회)에만 수행한다.
///
/// 직렬화된 필드는 건드리지 않고 프로퍼티만 베이스로 올렸다 —
/// 필드를 베이스로 옮기면 씬에 저장된 값이 날아갈 위험이 있다.
///
/// 부착 대상: Player
/// </summary>
[DisallowMultipleComponent]
public class AutoAimShooter : PlayerWeapon
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

    public override PlayerClass Class => PlayerClass.Gunner;

    public override float FireInterval
    {
        get => fireInterval;
        set => fireInterval = value;
    }

    /// <summary>증강으로 누적되는 관통 횟수. 발사되는 총알마다 그대로 복사된다.</summary>
    public int PierceCount { get; set; }

    /// <summary>증강으로 누적되는 추가 발사 수. 실제 발사 수는 maxBulletCount로 상한이 걸린다.</summary>
    public int ExtraShots { get; set; }

    /// <summary>증강으로 누적되는 탄속 배율.</summary>
    public float BulletSpeedMultiplier { get; set; } = 1f;

    /// <summary>
    /// 증강으로 누적되는 총알 크기 배율. 총알 콜라이더가 스케일을 따라가므로
    /// 크기를 키우면 명중 판정도 같이 넓어진다.
    /// </summary>
    public float BulletSizeMultiplier { get; set; } = 1f;

    /// <summary>증강으로 누적되는 사거리 배율.</summary>
    public float RangeMultiplier { get; set; } = 1f;

    private float _timer;

    private void Update()
    {
        if (bulletPrefab == null) return;
        if (!CanAct) return;

        _timer += Time.deltaTime;
        if (_timer < fireInterval) return;
        _timer = 0f;

        Transform target = FindNearestEnemy(range * RangeMultiplier);
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
            bullet.Speed = bulletPrefab.Speed * BulletSpeedMultiplier;
            bullet.PierceRemaining = PierceCount;

            // 프리팹 스케일을 기준으로 곱한다. 생성된 총알의 스케일을 다시 곱하면
            // 증강이 아니라 발사 횟수에 비례해 커진다.
            if (BulletSizeMultiplier != 1f)
                bullet.transform.localScale = bulletPrefab.transform.localScale * BulletSizeMultiplier;

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

}
