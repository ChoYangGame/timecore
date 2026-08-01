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
    PhaseShift
}

[CreateAssetMenu(fileName = "Augment_", menuName = "TimeCore/Augment Data")]
public class AugmentData : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [TextArea][SerializeField] private string description;
    [SerializeField] private AugmentType type;

    [Tooltip("타입별로 다르게 해석되는 값. 대부분 비율(0.15=15%), Pierce/MultiShot은 정수 개수, PhaseShift 무적시간은 별도 상수(Health.EnableHitInvincibility)로 처리")]
    [SerializeField] private float value;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public AugmentType Type => type;
    public float Value => value;
}
