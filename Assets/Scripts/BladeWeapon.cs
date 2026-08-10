using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 칼잡이. 주기마다 가장 가까운 적 쪽으로 시간의 홀을 휘두르고,
/// **휘두르는 동안 홀 그림이 실제로 지나간 자리**에 있는 적에게 피해를 준다.
///
/// 예전에는 호(弧) 스프라이트 한 장을 띄우고 거리·각도 비교로 판정했다.
/// 지금은 판정 대상이 그림 그 자체다 — 홀은 가늘고 긴 막대라
/// **선분(자루 끝 ↔ 머리 끝) + 굵기**로 두께 있는 막대 판정을 하고,
/// 한 번 휘두르는 동안 각 적은 한 번만 맞는다.
///
/// **자세의 주인은 이 클래스다.** BladeVisual은 여기서 계산한 프레임 번호·회전·반전을
/// 받아 그리기만 한다. 반대로 두면(그림이 자세를 정하고 판정이 따라가면)
/// 보이는 것과 맞는 것이 어긋나기 시작하는데, 이 프로젝트에서 이미 여러 번 겪은 문제다.
///
/// 판정은 여전히 콜라이더 없이 계산으로 한다 — 프로젝트 전반이 물리를 피하는 것과 같은 원칙이고,
/// 휘두르는 프레임 동안만 도므로 적이 40마리여도 프레임당 비교 40번이다.
///
/// 부착 대상: Player
/// </summary>
[DisallowMultipleComponent]
public class BladeWeapon : PlayerWeapon
{
    [SerializeField] private float fireInterval = 0.55f;
    [SerializeField] private float damage = 26f;

    [Tooltip("적을 찾는 거리. 이 안에 적이 없으면 휘두르지 않는다")]
    [SerializeField] private float searchRange = 6f;

    [Header("홀 판정 — 그려진 홀이 곧 판정이다")]
    [Tooltip("플레이어 중심에서 홀을 쥔 손(그림의 회전축)까지의 거리.\n" +
             "플레이어 그림이 1.63×1.77유닛(반지름 약 0.8)이라 0.45로는 손이 몸 한가운데 있었고,\n" +
             "자루가 0.61 뒤로 더 뻗으니 홀이 몸을 관통해 보였다. 팔을 뻗어 쥔 것처럼 밖으로 뺀다.\n" +
             "부수 효과로 유효 사거리가 1.77 → 2.37이 되는데, 이는 홀 도입 전 참격 시절의 2.4와 거의 같다")]
    [SerializeField] private float holdRadius = 1.05f;

    [Tooltip("회전축에서 홀 머리 끝까지. 원본 그림 실측 533px = 프레임 폭의 0.526")]
    [SerializeField] private float rodHead = 0.68f;

    [Tooltip("회전축에서 자루 끝까지. 원본 그림 실측 478px = 프레임 폭의 0.471")]
    [SerializeField] private float rodTail = 0.61f;

    [Tooltip("막대 판정의 반지름. 시계 머리(지름 약 0.30)를 기준으로 잡았다")]
    [SerializeField] private float rodRadius = 0.14f;

    [Tooltip("휘두르기 재생 속도(초당 프레임). 10장이므로 20이면 0.5초")]
    [SerializeField] private float swingFps = 20f;

    [Tooltip("프레임별 홀의 각도(도). 원본 10장의 주축을 2차 모멘트로 실측한 값이다.\n" +
             "0 → +27(들어올림) → -34(내려침) → 0(복귀). 이 표가 곧 휘두르는 궤적이다")]
    [SerializeField]
    private float[] frameAngles = { 0f, 5.2f, 18.1f, 27.2f, 22.1f, -34.0f, -29.5f, -22.1f, -11.6f, -5.5f };

    [Tooltip("원본에 그려진 총 스윙 폭(도). 호 확장 증강이 이 값을 기준으로 배율을 낸다")]
    [SerializeField] private float baseSweepDegrees = 61f;

    [Header("연출")]
    [Tooltip("타격 불꽃 색. 참격 스프라이트는 더 이상 쓰지 않는다 — 홀 그림이 그 자리를 대신한다")]
    [SerializeField] private Color hitColor = new Color(0.435f, 0.847f, 0.878f, 1f);

    public override PlayerClass Class => PlayerClass.Blade;

    public override float FireInterval
    {
        get => fireInterval;
        set => fireInterval = value;
    }

    /// <summary>
    /// 증강으로 늘어나는 홀 크기 배율. 그림과 판정이 **같이** 커진다 —
    /// 사거리를 늘리려면 홀 자체가 길어지는 수밖에 없다(그림이 판정이므로).
    /// </summary>
    public float ReachMultiplier { get; set; } = 1f;

    /// <summary>증강으로 늘어나는 스윙 폭(도). 홀이 더 크게 휘둘러져 지나가는 자리가 넓어진다.</summary>
    public float BonusArcDegrees { get; set; }

    /// <summary>반대쪽에도 같이 휘두른다. 파고드는 직업이라 등 뒤가 늘 비어 있는 것을 메운다.</summary>
    public bool BackSwing { get; set; }

    /// <summary>홀에 맞은 적을 밀어내는 거리(월드 유닛). 0이면 밀지 않는다.</summary>
    public float Knockback { get; set; }

    /// <summary>홀로 적을 죽였을 때 회복하는 HP. 0이면 회복하지 않는다.</summary>
    public float LifestealPerKill { get; set; }

    // ── BladeVisual이 읽어 가는 현재 자세 ──

    /// <summary>지금 휘두르는 중인지. 이 동안에만 판정이 있다.</summary>
    public bool IsSwinging { get; private set; }

    /// <summary>지금 그려야 할 프레임 번호.</summary>
    public int FrameIndex { get; private set; }

    /// <summary>홀 그림에 줘야 할 Z 회전(도).</summary>
    public float RotationDeg { get; private set; }

    /// <summary>왼쪽을 겨눠 위아래를 뒤집어야 하는지.</summary>
    public bool Mirrored { get; private set; }

    /// <summary>홀의 회전축이 놓일 월드 좌표(= 쥔 손).</summary>
    public Vector2 PivotWorld { get; private set; }

    /// <summary>프레임 수. 그림 장수와 이 표의 길이가 다르면 짧은 쪽을 따른다.</summary>
    public int FrameCount => frameAngles != null ? frameAngles.Length : 0;

    private float _timer;
    private float _swingT;
    private Vector2 _swingDir = Vector2.right;

    // 한 번 휘두르는 동안 같은 적을 여러 프레임에 걸쳐 반복해서 때리지 않게 한다.
    private readonly HashSet<int> _hitThisSwing = new HashSet<int>();

    private void Update()
    {
        if (!CanAct)
        {
            IsSwinging = false;
            return;
        }

        if (IsSwinging) TickSwing();

        _timer += Time.deltaTime;
        if (_timer < fireInterval) return;

        Transform target = FindNearestEnemy(searchRange);
        if (target == null) return;   // 적이 없으면 타이머를 소모하지 않는다 — 헛스윙이 없다

        Vector2 dir = (Vector2)(target.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;

        _timer = 0f;
        StartSwing(dir.normalized);
    }

    private void StartSwing(Vector2 dir)
    {
        _swingDir = dir;
        _swingT = 0f;
        IsSwinging = true;
        _hitThisSwing.Clear();

        Sfx.Play(SfxId.BladeSwing);

        // 겨누는 방향은 휘두르는 내내 고정한다. 여기서 부드럽게 돌리면
        // 그리는 각도와 판정하는 각도가 프레임마다 어긋난다.
        Mirrored = Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, 0f)) > 90f;

        TickSwing();
    }

    /// <summary>
    /// 이번 프레임의 홀 자세를 정하고, 그 자리에 걸린 적을 때린다.
    /// 자세 계산과 판정이 **같은 함수 안**에 있어야 둘이 어긋날 수 없다.
    /// </summary>
    private void TickSwing()
    {
        int count = FrameCount;
        if (count <= 0) { IsSwinging = false; return; }

        int frame = Mathf.FloorToInt(_swingT * Mathf.Max(swingFps, 0.01f));
        _swingT += Time.deltaTime;

        if (frame >= count)
        {
            IsSwinging = false;
            FrameIndex = 0;
            return;
        }

        FrameIndex = frame;

        float aim = Mathf.Atan2(_swingDir.y, _swingDir.x) * Mathf.Rad2Deg;
        float phi = frameAngles[frame];

        // 호 확장 증강: 그려진 각도를 배율만큼 더 돌린다. 그림에 없는 각도를 만들어 내는 게 아니라
        // 같은 그림을 더 크게 휘두르는 것이라, 그림과 판정이 계속 붙어 있다.
        float k = 1f + BonusArcDegrees / Mathf.Max(baseSweepDegrees, 1f);

        // flipY가 스프라이트 안의 각도를 -phi로 뒤집으므로, 회전에 실어야 할 추가분도 부호가 반대다.
        float extra = (k - 1f) * phi;
        RotationDeg = Mirrored ? aim - extra : aim + extra;
        float rodWorld = Mirrored ? aim - k * phi : aim + k * phi;

        PivotWorld = (Vector2)transform.position + _swingDir * holdRadius;

        float s = Mathf.Max(0.01f, ReachMultiplier);
        Vector2 axis = new Vector2(Mathf.Cos(rodWorld * Mathf.Deg2Rad), Mathf.Sin(rodWorld * Mathf.Deg2Rad));
        Vector2 head = PivotWorld + axis * (rodHead * s);
        Vector2 tail = PivotWorld - axis * (rodTail * s);

        // 태그 검색은 프레임마다 도는 자리라 한 번만 하고 두 판정이 나눠 쓴다.
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        DamageAlong(enemies, tail, head, rodRadius * s);

        if (!BackSwing) return;

        // 뒤쪽 홀은 앞쪽을 플레이어 기준으로 180도 돌린 것이다. 그림도 같은 규칙으로 그린다.
        Vector2 origin = transform.position;
        DamageAlong(enemies, origin * 2f - tail, origin * 2f - head, rodRadius * s);
    }

    /// <summary>
    /// 선분에서 rodRadius 안에 있는 적을 때린다.
    ///
    /// 적의 몸 크기를 반지름에 더한다 — 중심점만 보면 보스(스케일 3)는
    /// 홀이 몸통을 지나가도 중심이 밖이라 안 맞는다.
    /// </summary>
    private void DamageAlong(GameObject[] enemies, Vector2 a, Vector2 b, float radius)
    {
        float dmg = damage * DamageMultiplier;
        int kills = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null) continue;

            int id = e.GetInstanceID();
            if (_hitThisSwing.Contains(id)) continue;

            Vector2 p = e.transform.position;
            float reach = radius + TargetRadius(e);
            if (SqrDistanceToSegment(p, a, b) > reach * reach) continue;

            Health h = e.GetComponent<Health>();
            if (h == null || h.IsDead) continue;

            _hitThisSwing.Add(id);
            h.TakeDamage(dmg);

            if (!h.IsDead)
            {
                if (Knockback > 0f)
                {
                    Vector2 push = p - (Vector2)transform.position;
                    if (push.sqrMagnitude > 0.0001f)
                        e.transform.position += (Vector3)(push.normalized * Knockback);
                }
            }
            else kills++;

            // 타격 불꽃은 맞은 자리에서 낸다 — 홀이 지나간 곳을 눈으로 확인할 수 있게.
            EffectSystem.Spray(p, (p - a).normalized, Color.Lerp(hitColor, Color.white, 0.5f),
                4, 55f, 6f, 0.2f, 0.22f, FxSprites.Spark);
            CameraShake.Shake(0.1f, 0.06f);
        }

        if (kills > 0 && LifestealPerKill > 0f && OwnerHealth != null)
            OwnerHealth.Heal(LifestealPerKill * kills);
    }

    /// <summary>점에서 선분까지 거리의 제곱. 제곱근을 뽑지 않으려고 제곱끼리 비교한다.</summary>
    private static float SqrDistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 0.000001f) return (p - a).sqrMagnitude;

        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        Vector2 closest = a + ab * t;
        return (p - closest).sqrMagnitude;
    }
}
