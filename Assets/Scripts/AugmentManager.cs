using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레벨업 이벤트를 구독해 시간을 멈추고 증강 3장을 보여준 뒤, 선택하면 플레이어에 적용하고 재개한다.
/// 보스 등장 배너보다 우선순위가 높다: 배너 표시 중 레벨업이 뜨면 배너를 즉시 끈다.
/// 부착 대상: HUD_Canvas (AugmentPanel/Card0~2 를 자식으로 둔다)
/// </summary>
[DisallowMultipleComponent]
public class AugmentManager : MonoBehaviour
{
    [SerializeField] private AugmentData[] allAugments;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private AugmentCardUI[] cards;
    [SerializeField] private PlayerMove playerMove;
    [SerializeField] private AutoAimShooter playerShooter;
    [SerializeField] private Health playerHealth;
    [SerializeField] private BossBannerUI bossBanner;

    [Tooltip("PhaseShift(위상 이동) 증강이 부여하는 피격 무적 시간(초)")]
    [SerializeField] private float phaseShiftInvincibility = 0.5f;

    public bool IsShowing { get; private set; }

    /// <summary>카드 선택 후 재개될 때 발행. 대기 중이던 보스 배너를 여기서 띄운다.</summary>
    public event System.Action OnPanelClosed;

    private void Start()
    {
        if (playerMove == null || playerShooter == null || playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (playerMove == null) playerMove = player.GetComponent<PlayerMove>();
                if (playerShooter == null) playerShooter = player.GetComponent<AutoAimShooter>();
                if (playerHealth == null) playerHealth = player.GetComponent<Health>();
            }
        }

        if (panelRoot != null) panelRoot.SetActive(false);

        if (GameManager.Instance != null) GameManager.Instance.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        if (allAugments == null || allAugments.Length == 0 || cards == null || cards.Length == 0) return;

        // 카드가 배너보다 우선순위가 높다: 배너가 떠 있으면 바로 끊는다.
        if (bossBanner != null) bossBanner.CancelImmediate();

        IsShowing = true;
        Time.timeScale = 0f;
        if (panelRoot != null) panelRoot.SetActive(true);

        List<AugmentData> picks = PickRandom(cards.Length);
        for (int i = 0; i < cards.Length; i++)
        {
            if (i < picks.Count) cards[i].Setup(picks[i], Choose);
        }
    }

    private List<AugmentData> PickRandom(int count)
    {
        List<AugmentData> pool = new List<AugmentData>(allAugments);
        List<AugmentData> picked = new List<AugmentData>();

        int take = Mathf.Min(count, pool.Count);
        for (int i = 0; i < take; i++)
        {
            int index = Random.Range(0, pool.Count);
            picked.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return picked;
    }

    private void Choose(AugmentData data)
    {
        Apply(data);

        if (panelRoot != null) panelRoot.SetActive(false);
        Time.timeScale = 1f;
        IsShowing = false;

        OnPanelClosed?.Invoke();
    }

    private void Apply(AugmentData data)
    {
        switch (data.Type)
        {
            case AugmentType.MoveSpeed:
                if (playerMove != null) playerMove.MoveSpeed *= 1f + data.Value;
                break;
            case AugmentType.FireRate:
                if (playerShooter != null) playerShooter.FireInterval *= 1f - data.Value;
                break;
            case AugmentType.Damage:
                if (playerShooter != null) playerShooter.DamageMultiplier *= 1f + data.Value;
                break;
            case AugmentType.ExpRadius:
                if (GameManager.Instance != null) GameManager.Instance.MultiplyExpAbsorbRadius(1f + data.Value);
                break;
            case AugmentType.MaxHp:
                if (playerHealth != null) playerHealth.IncreaseMaxHp(playerHealth.MaxHp * data.Value);
                break;
            case AugmentType.Pierce:
                if (playerShooter != null) playerShooter.PierceCount += Mathf.Max(1, Mathf.RoundToInt(data.Value));
                break;
            case AugmentType.MultiShot:
                if (playerShooter != null) playerShooter.ExtraShots += Mathf.Max(1, Mathf.RoundToInt(data.Value));
                break;
            case AugmentType.PhaseShift:
                if (playerMove != null) playerMove.MoveSpeed *= 1f + data.Value;
                if (playerHealth != null) playerHealth.EnableHitInvincibility(phaseShiftInvincibility);
                break;
        }
    }
}
