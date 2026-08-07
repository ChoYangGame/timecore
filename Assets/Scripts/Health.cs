using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 체력 / 피격 플래시 / 사망 통지. 플레이어와 적이 공용으로 쓴다.
/// 부착 대상: Player, Enemy 프리팹 (SpriteRenderer 와 같은 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHp = 100f;

    [Tooltip("피격 시 흰색으로 번쩍이는 시간(초)")]
    [SerializeField] private float flashDuration = 0.08f;

    [Tooltip("비워두면 같은 GameObject 의 SpriteRenderer 를 자동으로 찾는다")]
    [SerializeField] private SpriteRenderer flashRenderer;

    [Tooltip("인스펙터에서 사망 연출을 붙이고 싶을 때 사용")]
    [SerializeField] private UnityEvent onDeathEvent;

    public float MaxHp => maxHp;
    public float CurrentHp { get; private set; }

    /// <summary>
    /// 피격 플래시가 되돌릴 원본 색. 사망 이펙트가 이걸 쓴다 —
    /// 맞고 죽는 순간에는 SpriteRenderer.color가 흰색이라 그걸 읽으면 이펙트가 전부 하얗게 나온다.
    /// </summary>
    public Color BaseColor => _baseColor;
    public bool IsDead { get; private set; }
    public bool IsInvincible { get; private set; }

    /// <summary>사망 시 1회만 호출된다.</summary>
    public event Action<Health> OnDeath;

    /// <summary>피격 시 호출된다. (현재 체력, 최대 체력)</summary>
    public event Action<float, float> OnDamaged;

    private Color _baseColor = Color.white;
    private Coroutine _flashRoutine;

    [Tooltip("피격 시 무적 지속시간(초). 0이면 비활성 — '위상 이동' 같은 증강이 켠다.")]
    [SerializeField] private float hitInvincibilityDuration = 0f;
    private Coroutine _invincibilityRoutine;

    private void Awake()
    {
        CurrentHp = maxHp;
        if (flashRenderer == null) flashRenderer = GetComponent<SpriteRenderer>();
        if (flashRenderer != null) _baseColor = flashRenderer.color;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || IsInvincible || amount <= 0f) return;

        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        OnDamaged?.Invoke(CurrentHp, maxHp);

        if (flashRenderer != null && isActiveAndEnabled)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (CurrentHp <= 0f)
        {
            IsDead = true;
            OnDeath?.Invoke(this);
            onDeathEvent?.Invoke();
            return;
        }

        if (hitInvincibilityDuration > 0f && isActiveAndEnabled)
        {
            if (_invincibilityRoutine != null) StopCoroutine(_invincibilityRoutine);
            _invincibilityRoutine = StartCoroutine(InvincibilityRoutine());
        }
    }

    /// <summary>스폰 시점에 최대 체력을 재조정할 때 쓴다 (예: 웨이브 난이도 스케일링).</summary>
    public void SetMaxHp(float newMaxHp, bool healToFull = true)
    {
        maxHp = Mathf.Max(1f, newMaxHp);
        CurrentHp = healToFull ? maxHp : Mathf.Min(CurrentHp, maxHp);
    }

    /// <summary>
    /// 스폰 후 스프라이트 색을 바꿀 때 쓴다 (시대별 적·보스 색 구분).
    /// 피격 플래시가 되돌릴 원본 색까지 같이 갱신한다 — Awake에서 캐시해 둔 프리팹 색으로
    /// 첫 피격 직후 되돌아가는 것을 막는다.
    /// </summary>
    public void SetBaseColor(Color color)
    {
        if (flashRenderer == null) flashRenderer = GetComponent<SpriteRenderer>();
        if (flashRenderer == null) return;

        _baseColor = color;

        // 플래시 중이면 지금 흰색이다. 덮어쓰면 플래시가 끊겨 보이므로 코루틴이 끝나며 _baseColor로 복귀시킨다.
        if (_flashRoutine == null) flashRenderer.color = color;
    }

    /// <summary>
    /// 최대 체력은 그대로 두고 현재 체력만 회복한다. 회복 코어가 쓴다.
    /// 실제로 회복된 양을 돌려준다 — 풀피일 때 코어가 소모되지 않게 하려는 것.
    /// </summary>
    public float Heal(float amount)
    {
        if (IsDead || amount <= 0f) return 0f;

        float before = CurrentHp;
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);

        float healed = CurrentHp - before;
        if (healed > 0f) OnDamaged?.Invoke(CurrentHp, maxHp);
        return healed;
    }

    /// <summary>최대 체력을 늘리고 늘어난 만큼만 즉시 회복한다 (풀피 회복이 아님).</summary>
    public void IncreaseMaxHp(float amount)
    {
        if (amount <= 0f) return;

        maxHp += amount;
        CurrentHp = Mathf.Min(maxHp, CurrentHp + amount);
        OnDamaged?.Invoke(CurrentHp, maxHp);
    }

    /// <summary>'위상 이동' 같은 증강이 호출한다. 이후 피격마다 지정한 시간만큼 무적이 걸린다.</summary>
    public void EnableHitInvincibility(float duration)
    {
        hitInvincibilityDuration = Mathf.Max(hitInvincibilityDuration, duration);
    }

    private IEnumerator FlashRoutine()
    {
        flashRenderer.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        flashRenderer.color = _baseColor;
        _flashRoutine = null;
    }

    private IEnumerator InvincibilityRoutine()
    {
        IsInvincible = true;

        const float blinkInterval = 0.08f;
        float elapsed = 0f;
        while (elapsed < hitInvincibilityDuration)
        {
            if (flashRenderer != null) flashRenderer.enabled = !flashRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        if (flashRenderer != null) flashRenderer.enabled = true;
        IsInvincible = false;
        _invincibilityRoutine = null;
    }
}
