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

    private PlayerWeapon _weapon;

    /// <summary>이미 획득한 1회성 증강. 두 번째로 뽑히면 효과가 없어 죽은 카드가 되므로 풀에서 뺀다.</summary>
    private readonly HashSet<AugmentData> _takenUnique = new HashSet<AugmentData>();

    /// <summary>
    /// 지금 켜져 있는 무기. 직업 선택이 Start 이후에 일어나므로 미리 잡아두면 안 되고,
    /// 필요할 때 찾아서 캐시한다.
    /// </summary>
    private PlayerWeapon ActiveWeapon
    {
        get
        {
            if (_weapon != null && _weapon.enabled) return _weapon;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return null;

            foreach (PlayerWeapon w in player.GetComponents<PlayerWeapon>())
            {
                if (!w.enabled) continue;
                _weapon = w;
                return _weapon;
            }
            return null;
        }
    }

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

        // 게임오버가 카드보다 우선순위가 높다. 사망과 레벨업이 같은 프레임에 겹쳐도 카드를 띄우지 않는다.
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

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

    /// <summary>
    /// 고른 직업에게 허용된 증강만 남겨 뽑는다.
    /// 거르지 않으면 칼잡이가 "총알 관통"을 뽑고 아무 일도 안 일어난다 —
    /// 효과가 없는 선택지는 버그로 읽힌다.
    /// </summary>
    private List<AugmentData> PickRandom(int count)
    {
        PlayerClass cls = GameManager.Instance != null
            ? GameManager.Instance.SelectedClass
            : PlayerClass.Gunner;

        List<AugmentData> pool = new List<AugmentData>();
        for (int i = 0; i < allAugments.Length; i++)
        {
            AugmentData a = allAugments[i];
            if (a == null || !a.AllowedFor(cls)) continue;
            if (a.Unique && _takenUnique.Contains(a)) continue;   // 이미 켠 스위치는 다시 안 뜬다
            pool.Add(a);
        }

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
        if (data.Unique) _takenUnique.Add(data);

        if (panelRoot != null) panelRoot.SetActive(false);
        IsShowing = false;

        // 게임오버 중이라면 게임을 재개시키면 안 된다 (죽었는데 시간이 다시 흐르는 상황 방지).
        bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        Time.timeScale = gameOver ? 0f : 1f;

        if (!gameOver) OnPanelClosed?.Invoke();
    }

    /// <summary>게임오버 등 더 우선순위가 높은 UI가 끼어들 때 카드를 즉시 닫는다. timeScale은 건드리지 않는다.</summary>
    public void ForceClose()
    {
        if (!IsShowing) return;

        if (panelRoot != null) panelRoot.SetActive(false);
        IsShowing = false;
    }

    private void Apply(AugmentData data)
    {
        switch (data.Type)
        {
            case AugmentType.MoveSpeed:
                if (playerMove != null) playerMove.MoveSpeed *= 1f + data.Value;
                break;
            // 공격속도·데미지는 세 직업 모두에게 같은 의미다. 켜져 있는 무기에 건다.
            case AugmentType.FireRate:
                if (ActiveWeapon != null) ActiveWeapon.FireInterval *= 1f - data.Value;
                break;
            case AugmentType.Damage:
                if (ActiveWeapon != null) ActiveWeapon.DamageMultiplier *= 1f + data.Value;
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

            // ── 칼잡이 전용 ──
            case AugmentType.BladeReach:
                if (ActiveWeapon is BladeWeapon br) br.ReachMultiplier *= 1f + data.Value;
                break;
            case AugmentType.BladeArc:
                if (ActiveWeapon is BladeWeapon ba) ba.BonusArcDegrees += data.Value;
                break;

            // ── 매지션 전용 ──
            case AugmentType.OrbCount:
                if (ActiveWeapon is OrbitWeapon oc) oc.ExtraOrbs += Mathf.Max(1, Mathf.RoundToInt(data.Value));
                break;
            case AugmentType.OrbRadius:
                if (ActiveWeapon is OrbitWeapon orr) orr.RadiusMultiplier *= 1f + data.Value;
                break;

            // ── 총잡이 2차 ──
            case AugmentType.BulletSpeed:
                if (playerShooter != null) playerShooter.BulletSpeedMultiplier *= 1f + data.Value;
                break;
            case AugmentType.BulletSize:
                if (playerShooter != null) playerShooter.BulletSizeMultiplier *= 1f + data.Value;
                break;
            case AugmentType.Range:
                if (playerShooter != null) playerShooter.RangeMultiplier *= 1f + data.Value;
                break;

            // ── 칼잡이 2차 ──
            case AugmentType.BladeBackSwing:
                if (ActiveWeapon is BladeWeapon bb) bb.BackSwing = true;
                break;
            case AugmentType.BladeKnockback:
                if (ActiveWeapon is BladeWeapon bk) bk.Knockback += data.Value;
                break;
            case AugmentType.BladeLifesteal:
                if (ActiveWeapon is BladeWeapon bl) bl.LifestealPerKill += data.Value;
                break;

            // ── 매지션 2차 ──
            case AugmentType.OrbSpeed:
                if (ActiveWeapon is OrbitWeapon os) os.SpeedMultiplier *= 1f + data.Value;
                break;
            case AugmentType.OrbHitRadius:
                if (ActiveWeapon is OrbitWeapon oh) oh.HitRadiusMultiplier *= 1f + data.Value;
                break;
            case AugmentType.OrbBlast:
                if (ActiveWeapon is OrbitWeapon ob) ob.Blast = true;
                break;
        }
    }
}
