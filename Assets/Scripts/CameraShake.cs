using UnityEngine;

/// <summary>
/// 카메라 흔들림. 값을 직접 카메라 위치에 쓰지 않고 Offset만 계산해 두고,
/// CameraFollow가 아레나 클램프를 끝낸 뒤 마지막에 더한다 —
/// 실행 순서를 보장하지 않는 LateUpdate 두 개가 서로 위치를 덮어쓰는 것을 피하려는 것이고,
/// 클램프 뒤에 더해야 벽에 붙었을 때도 흔들림이 죽지 않는다.
///
/// 계산은 Update에서 한다(CameraFollow의 LateUpdate보다 확실히 먼저 돈다).
/// 플레이어 피격은 여기서 직접 구독한다 — 카메라 피드백은 이 컴포넌트 하나로 끝나는 게 맞다.
///
/// 부착 대상: Main Camera (CameraFollow와 같은 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class CameraShake : MonoBehaviour
{
    [Tooltip("흔들림 최대 크기(월드 유닛). 아레나가 21.6 x 10.9라 0.5도 꽤 크다")]
    [SerializeField] private float maxOffset = 0.45f;

    [Tooltip("플레이어가 맞았을 때의 세기와 시간")]
    [SerializeField] private float playerHitStrength = 0.55f;
    [SerializeField] private float playerHitDuration = 0.18f;

    private static CameraShake _instance;

    /// <summary>CameraFollow가 마지막에 더하는 값.</summary>
    public static Vector2 CurrentOffset => _instance != null ? _instance._offset : Vector2.zero;

    private Vector2 _offset;
    private float _strength;
    private float _remaining;
    private float _duration;
    private Health _playerHealth;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _playerHealth = p.GetComponent<Health>();
        if (_playerHealth != null) _playerHealth.OnDamaged += HandlePlayerDamaged;
    }

    private void OnDestroy()
    {
        if (_playerHealth != null) _playerHealth.OnDamaged -= HandlePlayerDamaged;
        if (_instance == this) _instance = null;
    }

    private void HandlePlayerDamaged(float current, float max)
    {
        Shake(playerHitStrength, playerHitDuration);
    }

    /// <summary>
    /// strength는 0~1 기준. 이미 흔들리는 중이면 더 센 쪽으로만 덮어쓴다 —
    /// 약한 흔들림이 강한 흔들림을 끊어먹지 않게 하려는 것.
    /// </summary>
    public static void Shake(float strength, float duration)
    {
        if (_instance == null) return;

        if (strength <= _instance._strength && _instance._remaining > 0f) return;

        _instance._strength = Mathf.Clamp01(strength);
        _instance._duration = Mathf.Max(0.01f, duration);
        _instance._remaining = _instance._duration;
    }

    private void Update()
    {
        if (_remaining <= 0f)
        {
            _offset = Vector2.zero;
            _strength = 0f;
            return;
        }

        // 증강 카드로 timeScale=0인 동안에도 흔들림은 잦아들어야 한다.
        _remaining -= Time.unscaledDeltaTime;
        if (_remaining <= 0f)
        {
            _offset = Vector2.zero;
            _strength = 0f;
            return;
        }

        float falloff = _remaining / _duration;
        float amount = maxOffset * _strength * falloff;

        _offset = new Vector2(Random.Range(-amount, amount), Random.Range(-amount, amount));
    }
}
