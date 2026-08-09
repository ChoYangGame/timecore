using UnityEngine;

/// <summary>
/// 레벨이 오를 때마다 최대 체력을 조금씩 올린다.
///
/// 증강 '균열 복구'(최대 HP +20%)와 역할이 다르다. 저쪽은 골라야 얻는 큰 한 방이고,
/// 이건 고르지 않아도 쌓이는 바닥값이다 — 판이 길어질수록 적이 세지는데
/// 체력이 100에 고정이면 후반에 한 번의 실수가 곧 죽음이 된다.
///
/// 증가폭이 작은 이유: 크게 주면 '균열 복구'를 뽑을 이유가 없어진다.
/// 레벨 10이면 +36으로 기본값의 36%인데, 증강 한 장(+20%)보다 조금 나은 정도다.
///
/// 최대치가 오를 때 같은 양만큼 회복도 된다(Health.IncreaseMaxHp의 동작).
/// 레벨업이 곧 소량 회복이라 밀리던 순간을 한 번 되돌릴 수 있다.
///
/// 부착 대상: Player (Health와 같은 GameObject)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class LevelHealthGrowth : MonoBehaviour
{
    [Tooltip("레벨 1당 오르는 최대 체력. 플레이어 기본값은 100이다")]
    [SerializeField] private float hpPerLevel = 4f;

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void Start()
    {
        // GameManager가 Awake에서 인스턴스를 잡으므로 구독은 Start에서 한다.
        if (GameManager.Instance != null) GameManager.Instance.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    /// <summary>
    /// 다단계 레벨업(경험치가 한 번에 크게 들어온 경우)에도 레벨마다 한 번씩 불린다 —
    /// GameManager가 while 루프 안에서 레벨당 한 번 발행하기 때문이다.
    /// </summary>
    private void HandleLevelUp(int newLevel)
    {
        if (_health == null || _health.IsDead) return;
        _health.IncreaseMaxHp(hpPerLevel);
    }
}
