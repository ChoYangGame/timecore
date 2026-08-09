using UnityEngine;

/// <summary>
/// 칼잡이. 주기마다 가장 가까운 적 방향으로 호(弧)를 그려 베고,
/// 그 호 안에 있는 적 전부에게 한꺼번에 피해를 준다.
///
/// 총잡이와 갈리는 지점은 **사거리와 동시 타격 수**다.
/// 사거리가 2.4로 짧아 적 무리 속으로 들어가야 하지만, 한 번에 여럿을 친다.
/// 총잡이가 거리를 벌리는 직업이면 이쪽은 파고드는 직업이다.
///
/// 판정은 콜라이더 없이 거리·각도 비교로 한다 — 프로젝트 전반이 물리를 피하는 것과 같은 원칙이고,
/// 공격 주기당 1회만 도므로 적이 40마리여도 비교 40번이다.
///
/// 부착 대상: Player
/// </summary>
[DisallowMultipleComponent]
public class BladeWeapon : PlayerWeapon
{
    [SerializeField] private float fireInterval = 0.55f;
    [SerializeField] private float damage = 26f;

    [Tooltip("참격이 닿는 거리. 총잡이(14)보다 훨씬 짧다 — 파고드는 직업이라는 뜻이다")]
    [SerializeField] private float reach = 2.4f;

    [Tooltip("호의 폭(도). 이 각도 안에 있는 적이 전부 맞는다")]
    [SerializeField] private float arcDegrees = 130f;

    [Tooltip("적을 찾는 거리. 이 안에 적이 없으면 휘두르지 않는다")]
    [SerializeField] private float searchRange = 6f;


    [Header("연출")]
    [SerializeField] private float slashLifetime = 0.18f;
    [SerializeField] private Color slashColor = new Color(0.435f, 0.847f, 0.878f, 1f);

    public override PlayerClass Class => PlayerClass.Blade;

    public override float FireInterval
    {
        get => fireInterval;
        set => fireInterval = value;
    }

    /// <summary>증강으로 늘어나는 참격 거리 배율.</summary>
    public float ReachMultiplier { get; set; } = 1f;

    /// <summary>증강으로 늘어나는 호의 폭(도). 동시에 맞는 적 수가 늘어난다.</summary>
    public float BonusArcDegrees { get; set; }

    /// <summary>반대쪽에도 같이 벤다. 파고드는 직업이라 등 뒤가 늘 비어 있는 것을 메운다.</summary>
    public bool BackSwing { get; set; }

    /// <summary>참격에 맞은 적을 밀어내는 거리(월드 유닛). 0이면 밀지 않는다.</summary>
    public float Knockback { get; set; }

    /// <summary>참격으로 적을 죽였을 때 회복하는 HP. 0이면 회복하지 않는다.</summary>
    public float LifestealPerKill { get; set; }

    private float _timer;

    private void Update()
    {
        if (!CanAct) return;

        _timer += Time.deltaTime;
        if (_timer < fireInterval) return;

        Transform target = FindNearestEnemy(searchRange);
        if (target == null) return;   // 적이 없으면 타이머를 소모하지 않는다 — 헛스윙이 없다

        _timer = 0f;

        Vector2 dir = (Vector2)(target.position - transform.position);
        if (dir.sqrMagnitude < 0.0001f) return;
        Swing(dir.normalized);
    }

    private void Swing(Vector2 dir)
    {
        float r = reach * ReachMultiplier;
        float halfArc = (arcDegrees + BonusArcDegrees) * 0.5f;
        float dmg = damage * DamageMultiplier;

        Vector3 origin = transform.position;
        int hits = 0;
        int kills = 0;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject e = enemies[i];
            if (e == null) continue;

            Vector2 to = (Vector2)(e.transform.position - origin);

            // 적의 몸 크기를 사거리에 더한다. 중심점만 보면 보스(스케일 3)는
            // 참격이 몸통을 지나가도 중심이 밖이라 안 맞는다.
            float rad = TargetRadius(e);
            float dist = to.magnitude;
            if (dist > r + rad) continue;

            // 각도도 몸 크기만큼 넓혀 준다. 큰 적은 중심이 호 밖이어도 몸이 호 안에 걸친다.
            float arcPad = dist > 0.0001f
                ? Mathf.Asin(Mathf.Clamp01(rad / dist)) * Mathf.Rad2Deg
                : 180f;

            // 정면 기준 각도가 호의 절반 안이어야 맞는다.
            // 후방 참격 증강을 먹었으면 반대쪽 호도 같은 조건으로 인정한다.
            bool inFront = Vector2.Angle(dir, to) <= halfArc + arcPad;
            bool inBack = BackSwing && Vector2.Angle(-dir, to) <= halfArc + arcPad;
            if (!inFront && !inBack) continue;

            Health h = e.GetComponent<Health>();
            if (h == null || h.IsDead) continue;

            h.TakeDamage(dmg);
            hits++;

            // 죽은 뒤에 미는 것은 의미가 없으니 살아 있을 때만 민다.
            if (!h.IsDead)
            {
                if (Knockback > 0f && to.sqrMagnitude > 0.0001f)
                    e.transform.position += (Vector3)(to.normalized * Knockback);
            }
            else kills++;
        }

        if (kills > 0 && LifestealPerKill > 0f && OwnerHealth != null)
            OwnerHealth.Heal(LifestealPerKill * kills);

        PlaySlashFx(dir, r, halfArc, hits);
        if (BackSwing) PlaySlashFx(-dir, r, halfArc, 0);   // 뒤쪽 연출. 타격 연출은 앞쪽에서 이미 냈다
    }

    /// <summary>
    /// 참격 연출. 호 스프라이트 한 장을 베는 방향으로 눕혀 띄운다.
    /// 맞은 적이 있을 때만 불꽃을 더한다 — 허공을 갈랐는데 타격감이 나면 거짓말이 된다.
    ///
    /// 크기는 눈대중이 아니라 **실제 판정에서 역산한다.** 예전에는 r*1.5 크기를 r*0.55 지점에
    /// 놓아서, 보이는 칼날은 1.0~1.7에 떠 있는데 판정은 2.4까지 닿았다 —
    /// 눈에 안 걸린 적이 죽고 걸친 적이 안 죽는 것처럼 보였다.
    /// 이제 칼날의 바깥 끝이 사거리와, 폭이 호의 현(弦) 길이와 일치한다.
    /// </summary>
    private void PlaySlashFx(Vector2 dir, float r, float halfArc, int hits)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 바깥 호의 현 길이 = 칼날이 덮어야 할 폭. 90도를 넘으면 지름이 최대다.
        float across = 2f * r * Mathf.Sin(Mathf.Min(halfArc, 90f) * Mathf.Deg2Rad);

        // 스프라이트 한 변을 size로 놓으면 실제로 그려지는 건 불투명 비율만큼이다.
        float size = across / FxSprites.SlashOpaqueAcross;
        float thickness = size * FxSprites.SlashOpaqueAlong;

        // 칼날의 바깥 끝을 사거리에 맞춘다.
        Vector2 center = (Vector2)transform.position + dir * (r - thickness * 0.5f);

        EffectSystem.Slash(center, angle, slashColor, size, slashLifetime, FxSprites.Slash);

        if (hits <= 0) return;

        CameraShake.Shake(0.12f + 0.03f * hits, 0.07f);
        EffectSystem.Spray(center, dir, Color.Lerp(slashColor, Color.white, 0.5f),
            Mathf.Min(3 + hits, 8), 55f, 6f, 0.2f, 0.22f, FxSprites.Spark);
    }
}
