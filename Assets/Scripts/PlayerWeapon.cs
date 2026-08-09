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
