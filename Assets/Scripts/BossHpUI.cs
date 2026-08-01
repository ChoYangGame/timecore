using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 보스 전용 HP바 + 이름표. 평소엔 비활성, WaveManager가 보스를 소환/처치할 때 Show/Hide를 호출한다.
/// 부착 대상: BossHUD (HUD_Canvas의 자식, 이름표+바를 담는 컨테이너)
/// </summary>
[DisallowMultipleComponent]
public class BossHpUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text nameText;

    private Health _target;

    public void Show(Health bossHealth, string bossName)
    {
        Unbind();
        _target = bossHealth;
        _target.OnDamaged += HandleDamaged;
        _target.OnDeath += HandleDeath;

        if (nameText != null) nameText.text = bossName;
        if (fillImage != null) fillImage.fillAmount = _target.MaxHp > 0f ? _target.CurrentHp / _target.MaxHp : 0f;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        Unbind();
        gameObject.SetActive(false);
    }

    private void OnDestroy() => Unbind();

    private void Unbind()
    {
        if (_target == null) return;
        _target.OnDamaged -= HandleDamaged;
        _target.OnDeath -= HandleDeath;
        _target = null;
    }

    private void HandleDamaged(float current, float max)
    {
        if (fillImage != null) fillImage.fillAmount = max > 0f ? current / max : 0f;
    }

    private void HandleDeath(Health _) => Hide();
}
