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
    public bool IsDead { get; private set; }

    /// <summary>사망 시 1회만 호출된다.</summary>
    public event Action<Health> OnDeath;

    /// <summary>피격 시 호출된다. (현재 체력, 최대 체력)</summary>
    public event Action<float, float> OnDamaged;

    private Color _baseColor = Color.white;
    private Coroutine _flashRoutine;

    private void Awake()
    {
        CurrentHp = maxHp;
        if (flashRenderer == null) flashRenderer = GetComponent<SpriteRenderer>();
        if (flashRenderer != null) _baseColor = flashRenderer.color;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHp = Mathf.Max(0f, CurrentHp - amount);
        OnDamaged?.Invoke(CurrentHp, maxHp);

        if (flashRenderer != null && isActiveAndEnabled)
        {
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (CurrentHp > 0f) return;

        IsDead = true;
        OnDeath?.Invoke(this);
        onDeathEvent?.Invoke();
    }

    private IEnumerator FlashRoutine()
    {
        flashRenderer.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        flashRenderer.color = _baseColor;
        _flashRoutine = null;
    }
}
