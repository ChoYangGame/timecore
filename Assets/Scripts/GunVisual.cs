using UnityEngine;

/// <summary>
/// 거너폼이 들고 다니는 총. 플레이어를 따라다니고, 총알이 나갈 때마다 발사 애니메이션을 한 번 재생한다.
///
/// **반동은 코드가 아니라 그림에 들어 있다.** 원본 8장의 y는 전부 같은데 x만
/// 1~3번 → 4~5번(250px 왼쪽) → 6~8번(복귀)으로 움직인다. 공통 크롭으로 반입했기 때문에
/// 프레임을 순서대로 넘기기만 하면 총이 뒤로 밀렸다 돌아온다. 총 스프라이트의 로컬 -x가
/// 총열 뒤쪽이라, 어느 각도로 돌려도 반동은 항상 총열 축을 따라간다.
///
/// 조준 각도를 따라 도는 이유: 자동 조준이라 적이 어느 방향에든 있는데, 총이 반대쪽을 보면서
/// 반동만 치면 고장 난 것처럼 읽힌다. 왼쪽을 겨눌 때는 회전만으로는 총이 뒤집히므로
/// flipY로 세운다(회전 + flipY = 좌우 반전).
///
/// 적이 없어 쏘지 않는 동안에는 **플레이어가 방향키로 보고 있는 쪽**을 겨눈다 —
/// 그래야 방향키 입력이 화면에 드러난다(플레이어 그림 자체는 정면 대칭이라 반전이 안 보인다).
///
/// 부착 대상: Player의 자식 "GunVisual" (SpriteRenderer만)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class GunVisual : MonoBehaviour
{
    [Tooltip("발사 프레임. 순서대로 한 번 재생하고 0번으로 돌아간다")]
    [SerializeField] private Sprite[] fireFrames;

    [Tooltip("발사 애니메이션 재생 속도(초당 프레임). 발사 간격(0.45초)보다 짧게 끝나야 한다")]
    [SerializeField] private float framesPerSecond = 26f;

    [Tooltip("플레이어 중심에서 총을 든 손까지의 거리")]
    [SerializeField] private float holdRadius = 0.5f;

    [Tooltip("총이 조준 방향으로 돌아가는 속도(초당 도). 즉시 꺾이면 딱딱하다")]
    [SerializeField] private float turnSpeed = 900f;

    [Tooltip("마지막 발사 후 이 시간이 지나면 총구를 플레이어가 보는 쪽으로 되돌린다")]
    [SerializeField] private float aimHoldDuration = 1.2f;

    private SpriteRenderer _sr;
    private AutoAimShooter _shooter;
    private PlayerVisual _visual;
    private Health _health;

    private float _angle;          // 지금 향한 각도(도)
    private float _targetAngle;    // 향해야 할 각도(도)
    private float _sinceFired = 999f;
    private float _frameTimer;
    private int _frame;
    private bool _playing;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        Transform p = transform.parent;
        if (p != null)
        {
            _shooter = p.GetComponent<AutoAimShooter>();
            _visual = p.GetComponent<PlayerVisual>();
            _health = p.GetComponent<Health>();

            SpriteRenderer parentSr = p.GetComponent<SpriteRenderer>();
            if (parentSr != null) _sr.sortingOrder = parentSr.sortingOrder + 1;
        }

        if (_shooter != null) _shooter.OnFired += HandleFired;
        if (fireFrames != null && fireFrames.Length > 0) _sr.sprite = fireFrames[0];
    }

    private void OnDestroy()
    {
        if (_shooter != null) _shooter.OnFired -= HandleFired;
    }

    private void HandleFired(Vector2 dir)
    {
        _sinceFired = 0f;
        _targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 이미 재생 중이어도 0번부터 다시 시작한다 — 연사 증강으로 발사 간격이
        // 애니메이션 길이보다 짧아지면 중간에서 이어붙는 것보다 매 발 처음부터가 낫다.
        _frame = 0;
        _frameTimer = 0f;
        _playing = true;
    }

    private void Update()
    {
        // 거너가 아니면(칼잡이·매지션) 총 자체가 없어야 한다. 무기 셋 다 Player에 붙어 있고
        // 고른 하나만 enabled라, 그 값을 그대로 본다.
        bool show = _shooter != null && _shooter.enabled
                    && (_health == null || !_health.IsDead);
        if (_sr.enabled != show) _sr.enabled = show;
        if (!show) return;

        _sinceFired += Time.deltaTime;

        // 한동안 쏘지 않았으면 플레이어가 보는 쪽으로 총구를 되돌린다.
        if (_sinceFired > aimHoldDuration && _visual != null)
            _targetAngle = _visual.FacingLeft ? 180f : 0f;

        _angle = Mathf.MoveTowardsAngle(_angle, _targetAngle, turnSpeed * Time.deltaTime);

        float rad = _angle * Mathf.Deg2Rad;
        transform.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * holdRadius;
        transform.localRotation = Quaternion.Euler(0f, 0f, _angle);

        // 왼쪽을 겨누면 회전만으로는 총이 거꾸로 선다. 위아래를 뒤집어 세운다.
        _sr.flipY = Mathf.Abs(Mathf.DeltaAngle(_angle, 0f)) > 90f;

        if (!_playing || fireFrames == null || fireFrames.Length == 0) return;

        _frameTimer += Time.deltaTime;
        float step = 1f / Mathf.Max(framesPerSecond, 0.01f);
        if (_frameTimer < step) return;

        int advance = (int)(_frameTimer / step);
        _frameTimer -= advance * step;
        _frame += advance;

        // 루프가 아니라 1회 재생이다. 끝나면 정지 포즈(0번)로 돌아가 다음 발사를 기다린다.
        if (_frame >= fireFrames.Length)
        {
            _frame = 0;
            _playing = false;
        }

        _sr.sprite = fireFrames[_frame];
    }
}
