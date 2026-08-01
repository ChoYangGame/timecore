using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Health의 OnDamaged/OnDeath 이벤트를 받아 fillAmount를 갱신한다.
/// 부착 대상: HPBar (Fill 자식 Image를 fillImage에 연결, target에 Player의 Health를 연결)
/// </summary>
[DisallowMultipleComponent]
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Health target;
    [SerializeField] private Image fillImage;

    private void Start()
    {
        // Health.CurrentHp는 Health.Awake()에서 채워진다.
        // Unity가 모든 Awake()를 끝낸 뒤 Start()를 호출하는 걸 보장하므로 여기서 읽어야 안전하다.
        if (target != null) Bind(target);
    }

    private void OnDestroy()
    {
        if (target != null)
        {
            target.OnDamaged -= HandleDamaged;
            target.OnDeath -= HandleDeath;
        }
    }

    private void Bind(Health health)
    {
        target = health;
        target.OnDamaged += HandleDamaged;
        target.OnDeath += HandleDeath;
        if (fillImage != null) fillImage.fillAmount = target.CurrentHp / target.MaxHp;
    }

    private void HandleDamaged(float current, float max)
    {
        if (fillImage != null) fillImage.fillAmount = max > 0f ? current / max : 0f;
    }

    private void HandleDeath(Health _)
    {
        if (fillImage != null) fillImage.fillAmount = 0f;
    }
}
