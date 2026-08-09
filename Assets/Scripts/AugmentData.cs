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
