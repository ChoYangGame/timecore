using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 회복 코어. 플레이어가 사거리 안에 **머물러 있는 동안** 확보 게이지가 차고, 다 차면 체력을 돌려준다.
///
/// 지금 붙은 다른 기믹(감속 지대·레이저·추적 장판·분출구)은 전부 "움직여라"인데
/// 이것만 "버텨라"다. 코어 위에 레이저가 예고되면 포기할지 버틸지가 갈린다 — 그 충돌이 이 기믹의 값어치다.
///
/// 확보 진행도는 안쪽에 겹쳐 그린 사각형의 크기로 보여준다(문자 없음 = 폰트 재굽기 없음).
/// 사거리를 벗어나면 게이지가 줄어들되 0에서 멈춘다 — 완전 리셋은 한 번 놓쳤을 때 너무 가혹하다.
///
/// 부착 대상: RecoveryCore 프리팹 (SpriteRenderer + 자식 "Fill"의 SpriteRenderer. 콜라이더 없음)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class RecoveryCore : MonoBehaviour
{
    [Tooltip("이 거리 안에 있으면 확보가 진행된다")]
    [SerializeField] private float captureRadius = 2.2f;

    [Tooltip("확보에 걸리는 시간(초). 머물러야 하는 시간이라 길수록 위험하다")]
    [SerializeField] private float captureTime = 3f;

    [Tooltip("사거리를 벗어났을 때 게이지가 줄어드는 속도 배율. 1이면 찬 속도와 같은 속도로 준다")]
    [SerializeField] private float decayRate = 0.6f;

    [Tooltip("확보 성공 시 회복량")]
    [SerializeField] private float healAmount = 35f;

    [Tooltip("이 시간 안에 확보하지 못하면 사라진다")]
    [SerializeField] private float lifetime = 20f;

    [SerializeField] private float fadeOutDuration = 0.6f;

    [Header("외형")]
    [SerializeField] private Color baseColor = new Color(0.353f, 0.878f, 0.514f, 1f);
    [SerializeField] private float idleAlpha = 0.45f;
    [SerializeField] private float activeAlpha = 0.8f;
    [SerializeField] private float pulseSpeed = 3f;

    /// <summary>감속 지대(-500)·레이저(-400)보다 위, 적·플레이어(0)보다 아래.</summary>
    [SerializeField] private int sortingOrder = -300;

    private static readonly List<RecoveryCore> ActiveCores = new List<RecoveryCore>();

    public static int ActiveCount => ActiveCores.Count;

    /// <summary>확보 성공 시 1회. (회복된 양)</summary>
    public event Action<float> OnCaptured;

    private SpriteRenderer _renderer;
    private SpriteRenderer _fill;
    private Transform _player;
    private Health _playerHealth;

    private float _progress;
    private float _elapsed;
    private bool _finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ActiveCores.Clear();

    public static void ClearAll()
    {
        RecoveryCore[] snapshot = ActiveCores.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
            if (snapshot[i] != null) Destroy(snapshot[i].gameObject);

        ActiveCores.Clear();
    }

    /// <summary>0~1 확보 진행도. 검증·디버그용.</summary>
    public float Progress => _progress;

    public void Configure(float radius, float heal)
    {
        captureRadius = radius;
        healAmount = heal;
        ApplySize();
    }

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _renderer.sortingOrder = sortingOrder;

        Transform fillTf = transform.Find("Fill");
        if (fillTf != null)
        {
            _fill = fillTf.GetComponent<SpriteRenderer>();
            if (_fill != null) _fill.sortingOrder = sortingOrder + 1;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _player = p.transform;
            _playerHealth = p.GetComponent<Health>();
        }

        ApplySize();
    }

    private void OnEnable() => ActiveCores.Add(this);

    private void OnDisable() => ActiveCores.Remove(this);

    private void Update()
    {
        _elapsed += Time.deltaTime;

        if (_finished)
        {
            if (_elapsed >= fadeOutDuration) Destroy(gameObject);
            ApplyVisual(false);
            return;
        }

        if (_elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        bool inside = IsPlayerInside();

        if (inside) _progress += Time.deltaTime / Mathf.Max(0.01f, captureTime);
        else _progress -= Time.deltaTime / Mathf.Max(0.01f, captureTime) * decayRate;

        _progress = Mathf.Clamp01(_progress);

        if (_progress >= 1f) Capture();

        ApplyVisual(inside);
    }

    private bool IsPlayerInside()
    {
        if (_player == null || _playerHealth == null || _playerHealth.IsDead) return false;

        Vector2 d = (Vector2)_player.position - (Vector2)transform.position;
        return d.sqrMagnitude <= captureRadius * captureRadius;
    }

    private void Capture()
    {
        _finished = true;
        _elapsed = 0f;

        float healed = _playerHealth != null ? _playerHealth.Heal(healAmount) : 0f;

        Sfx.Play(SfxId.Heal);

        // 확보 성공은 이 게임에서 유일한 "잘했다" 순간이라 조각을 넉넉히 쓴다.
        // 흔들림은 넣지 않는다 — 보상인데 피격과 같은 느낌이 나면 안 된다.
        Color c = baseColor;
        c.a = 1f;
        EffectSystem.Burst(transform.position, c, 12, 6f, 0.34f, 0.55f);

        OnCaptured?.Invoke(healed);
    }

    private void ApplySize()
    {
        if (_renderer == null || _renderer.sprite == null) return;

        float spriteWidth = _renderer.sprite.bounds.size.x;
        if (spriteWidth <= 0.0001f) return;

        transform.localScale = Vector3.one * (captureRadius * 2f / spriteWidth);
    }

    private void ApplyVisual(bool inside)
    {
        if (_renderer != null)
        {
            float alpha = inside ? activeAlpha : idleAlpha;
            if (!_finished) alpha *= 0.9f + 0.1f * Mathf.Sin(_elapsed * pulseSpeed);
            else alpha *= Mathf.Max(0f, 1f - _elapsed / Mathf.Max(0.0001f, fadeOutDuration));

            Color c = baseColor;
            c.a = Mathf.Clamp01(alpha);
            _renderer.color = c;
        }

        // 안쪽 채움이 곧 게이지다. 부모 스케일 위에 얹히므로 로컬 스케일은 0~1 비율 그대로면 된다.
        if (_fill != null)
        {
            _fill.transform.localScale = Vector3.one * _progress;

            Color fc = Color.white;
            fc.a = _finished ? 0f : Mathf.Clamp01(0.55f + 0.35f * _progress);
            _fill.color = fc;
        }
    }
}
