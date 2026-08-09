using UnityEngine;

/// <summary>플레이어가 고르는 전투 방식. 시작 화면에서 정해지고 판 내내 유지된다.</summary>
public enum PlayerClass
{
    /// <summary>총잡이 — 가장 가까운 적에게 자동 발사. 기존 방식이다.</summary>
    Gunner,
    /// <summary>칼잡이 — 주위를 호(弧)로 벤다. 사거리는 짧고 한 번에 여럿을 친다.</summary>
    Blade,
    /// <summary>매지션 — 몸 주위를 도는 코어가 닿는 적에게 지속 피해를 준다.</summary>
    Mage,
}

/// <summary>
/// 무기 3종의 공통 뼈대.
///
/// 이 베이스가 있는 이유는 하나다 — **증강을 세 벌 만들지 않기 위해서**다.
/// 공격속도·데미지 배율은 어느 직업이든 같은 의미라 여기 두고,
/// 직업 고유 능력치(관통·참격 반경·코어 수)만 각 파생 클래스가 갖는다.
///
/// 부착 대상: Player (세 개가 다 붙어 있고 고른 하나만 enabled)
/// </summary>
public abstract class PlayerWeapon : MonoBehaviour
{
    /// <summary>이 무기가 어느 직업의 것인지. 증강 풀을 거를 때 쓴다.</summary>
    public abstract PlayerClass Class { get; }

    /// <summary>공격 간격(초). 낮을수록 빠르다. FireRate 증강이 곱해 줄인다.</summary>
    public abstract float FireInterval { get; set; }

    /// <summary>증강으로 누적되는 데미지 배율. 기본 데미지에 곱해서 쓴다.</summary>
    public float DamageMultiplier { get; set; } = 1f;

    protected Health OwnerHealth;

    /// <summary>죽은 뒤에도 계속 공격하면 안 된다.</summary>
    protected bool CanAct => OwnerHealth == null || !OwnerHealth.IsDead;

    protected virtual void Awake()
    {
        OwnerHealth = GetComponent<Health>();
    }

    /// <summary>
    /// 적의 몸 반지름(월드 단위). 판정 거리에 더해 쓴다.
    ///
    /// 이게 없으면 판정이 적의 **중심점**만 보게 된다. 일반 적(스케일 1)은 티가 안 나지만
    /// 보스는 스케일이 3이라 몸통 가장자리와 중심이 1.5유닛이나 벌어져,
    /// 참격 이펙트가 보스 몸에 뻔히 겹쳐도 중심이 사거리 밖이면 피해가 0이었다.
    ///
    /// bounds는 월드 공간이라 스케일이 이미 반영돼 있다.
    /// 사각형을 원으로 근사하므로 모서리 쪽은 약간 손해지만, 큰 적이 안 맞는 것보다 낫다.
    /// </summary>
    protected static float TargetRadius(GameObject e)
    {
        Collider2D c = e.GetComponent<Collider2D>();
        if (c != null)
        {
            Vector3 ext = c.bounds.extents;
            return Mathf.Max(ext.x, ext.y);
        }

        SpriteRenderer sr = e.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Vector3 ext = sr.bounds.extents;
            return Mathf.Max(ext.x, ext.y);
        }

        return 0f;
    }

    /// <summary>
    /// 사거리 안에서 가장 가까운 적. 세 무기가 같은 탐색을 쓴다.
    /// 물리 쿼리 대신 태그 검색 + 제곱거리 비교다 — 프로젝트 전반이 콜라이더를 피하는 것과 같은 이유고,
    /// 공격 주기당 1회만 도므로 저사양에서도 부담이 없다.
    /// </summary>
    protected Transform FindNearestEnemy(float range)
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
