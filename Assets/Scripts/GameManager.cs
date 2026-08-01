using System;
using UnityEngine;

/// <summary>
/// 생존 시간 · 킬 수 · 레벨/경험치 상태를 관리하는 싱글톤.
/// 부착 대상: GameManager (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float SurvivalTime { get; private set; }
    public int KillCount { get; private set; }
    public int Level { get; private set; } = 1;
    public float ExpRatio { get; private set; }
    public int CurrentExp { get; private set; }
    public int ExpToNextLevel { get; private set; }

    /// <summary>'시간 왜곡' 증강이 누적시키는 경험치 흡수 반경 배율. ExpOrb가 스폰 시 읽어간다.</summary>
    public float ExpAbsorbRadiusMultiplier { get; private set; } = 1f;

    public event Action<int> OnKillCountChanged;
    public event Action<int, float> OnExpChanged;

    /// <summary>레벨업마다(다단계 레벨업이면 여러 번) 새 레벨 값과 함께 호출된다.</summary>
    public event Action<int> OnLevelUp;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ExpToNextLevel = ComputeExpToNextLevel(Level);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        SurvivalTime += Time.deltaTime;
    }

    public void RegisterKill()
    {
        KillCount++;
        OnKillCountChanged?.Invoke(KillCount);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        CurrentExp += amount;

        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            Level++;
            ExpToNextLevel = ComputeExpToNextLevel(Level);
            OnLevelUp?.Invoke(Level);
        }

        SetExp(Level, ExpToNextLevel > 0 ? (float)CurrentExp / ExpToNextLevel : 0f);
    }

    public void MultiplyExpAbsorbRadius(float factor)
    {
        if (factor <= 0f) return;
        ExpAbsorbRadiusMultiplier *= factor;
    }

    private static int ComputeExpToNextLevel(int level) => 5 + (level - 1) * 3;

    /// <summary>레벨/경험치 비율 변경을 UI에 알리는 내부 갱신용 API.</summary>
    public void SetExp(int level, float ratio)
    {
        Level = level;
        ExpRatio = Mathf.Clamp01(ratio);
        OnExpChanged?.Invoke(Level, ExpRatio);
    }
}
