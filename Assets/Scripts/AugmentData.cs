using UnityEngine;

/// <summary>
/// 증강 1종의 데이터. Assets 하위에 에셋으로 만들어 AugmentManager의 풀에 등록한다.
/// </summary>
public enum AugmentType
{
    MoveSpeed,
    FireRate,
    Damage,
    ExpRadius,
    MaxHp,
    Pierce,
    MultiShot,
    PhaseShift,

    // ── 아래는 직업 전용. 반드시 끝에 추가한다 —
    //    중간에 끼우면 이미 만들어진 .asset의 type 인덱스가 전부 밀린다.
    /// <summary>칼잡이 — 참격 반경 증가</summary>
    BladeReach,
    /// <summary>칼잡이 — 호의 폭 증가 (동시에 맞는 적이 늘어난다)</summary>
    BladeArc,
    /// <summary>매지션 — 궤도 코어 수 증가</summary>
    OrbCount,
    /// <summary>매지션 — 공전 반경 증가</summary>
    OrbRadius,

    // ── 2차 확장. 여기도 끝에만 붙인다.
    /// <summary>총잡이 — 탄속 증가</summary>
    BulletSpeed,
    /// <summary>총잡이 — 총알 크기 증가 (콜라이더가 같이 커져 판정도 넓어진다)</summary>
    BulletSize,
    /// <summary>총잡이 — 사거리 증가</summary>
    Range,
    /// <summary>칼잡이 — 반대쪽에도 동시 참격</summary>
    BladeBackSwing,
    /// <summary>칼잡이 — 참격에 밀어내기 부여</summary>
    BladeKnockback,
    /// <summary>칼잡이 — 참격으로 처치 시 회복</summary>
    BladeLifesteal,
    /// <summary>매지션 — 공전 속도 증가</summary>
    OrbSpeed,
    /// <summary>매지션 — 코어 타격 반경 증가</summary>
    OrbHitRadius,
    /// <summary>매지션 — 코어 타격 시 주위에도 절반 피해</summary>
    OrbBlast,
}

/// <summary>증강이 어느 직업에게 뜨는지. Any면 모든 직업 공용이다.</summary>
public enum AugmentClassFilter
{
    Any,
    Gunner,
    Blade,
    Mage,
}

[CreateAssetMenu(fileName = "Augment_", menuName = "TimeCore/Augment Data")]
public class AugmentData : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [TextArea][SerializeField] private string description;
    [SerializeField] private AugmentType type;

    [Tooltip("이 증강이 뜨는 직업. Any면 공용이다.\n" +
             "관통·다중 사출처럼 총알이 있어야 성립하는 증강은 총잡이로 묶어야 한다 —" +
             "칼잡이가 '총알 관통'을 뽑으면 아무 일도 일어나지 않는다")]
    [SerializeField] private AugmentClassFilter classFilter = AugmentClassFilter.Any;

    public AugmentClassFilter ClassFilter => classFilter;

    [Tooltip("한 번 먹으면 다시 뜨지 않는 증강. 후방 참격·연쇄 붕괴처럼 켜고 끄는 방식이라\n" +
             "두 번째로 뽑히면 아무 일도 일어나지 않는 것들에 켠다.\n" +
             "배율이 누적되는 증강은 꺼 둔다 — 반복해서 먹는 게 정상 성장이다")]
    [SerializeField] private bool unique;

    /// <summary>한 번 획득하면 풀에서 빠지는 증강인지.</summary>
    public bool Unique => unique;

    /// <summary>이 증강이 주어진 직업에게 떠도 되는지.</summary>
    public bool AllowedFor(PlayerClass cls)
    {
        switch (classFilter)
        {
            case AugmentClassFilter.Gunner: return cls == PlayerClass.Gunner;
            case AugmentClassFilter.Blade: return cls == PlayerClass.Blade;
            case AugmentClassFilter.Mage: return cls == PlayerClass.Mage;
            default: return true;
        }
    }

    [Tooltip("타입별로 다르게 해석되는 값. 대부분 비율(0.15=15%), Pierce/MultiShot은 정수 개수, PhaseShift 무적시간은 별도 상수(Health.EnableHitInvincibility)로 처리")]
    [SerializeField] private float value;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public AugmentType Type => type;
    public float Value => value;
}
