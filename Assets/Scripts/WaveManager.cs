using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 시간 경과에 따라 난이도를 올리고, 6웨이브(=waveDuration*5초 경과)에 보스를 등장시킨다.
/// GameManager와는 별도 컴포넌트. 부착 대상: WaveManager (빈 GameObject)
/// </summary>
[DisallowMultipleComponent]
public class WaveManager : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Boss bossPrefab;
    [SerializeField] private BossHpUI bossHpUI;
    [SerializeField] private BossBannerUI bossBanner;
    [Tooltip("증강 카드가 배너보다 우선순위가 높다 — 카드 표시 중이면 배너를 대기시킨다")]
    [SerializeField] private AugmentManager augmentManager;

    [SerializeField] private float waveDuration = 25f;
    [SerializeField] private int bossWave = 6;
    [SerializeField] private float spawnIntervalMultiplier = 0.9f;
    [SerializeField] private float minSpawnInterval = 0.4f;
    [Tooltip("웨이브 1당 적 최대체력 가산 비율. 예: 0.15 = +15%")]
    [SerializeField] private float enemyHealthStepBonus = 0.15f;

    [Tooltip("테스트용: 이 키를 누르면 웨이브 진행과 무관하게 즉시 보스를 소환한다")]
    [SerializeField] private Key debugBossSpawnKey = Key.B;

    public int CurrentWave { get; private set; } = 1;

    /// <summary>보스 처치 시 발행. 지금은 다음 시대 전환 없이 이벤트만 나간다.</summary>
    public event Action OnBossDefeated;

    private float _waveTimer;
    private bool _bossSpawned;
    private string _pendingBannerText;

    private void Awake()
    {
        if (enemySpawner != null) enemySpawner.OnEnemySpawned += HandleEnemySpawned;
        if (augmentManager != null) augmentManager.OnPanelClosed += HandlePanelClosed;
    }

    private void OnDestroy()
    {
        if (enemySpawner != null) enemySpawner.OnEnemySpawned -= HandleEnemySpawned;
        if (augmentManager != null) augmentManager.OnPanelClosed -= HandlePanelClosed;
    }

    private void Update()
    {
        if (!_bossSpawned)
        {
            _waveTimer += Time.deltaTime;
            if (_waveTimer >= waveDuration)
            {
                _waveTimer = 0f;
                AdvanceWave();
            }
        }

        if (Keyboard.current != null && Keyboard.current[debugBossSpawnKey].wasPressedThisFrame)
        {
            ForceSpawnBoss();
        }
    }

    private void AdvanceWave()
    {
        CurrentWave++;

        if (enemySpawner != null)
        {
            enemySpawner.SpawnInterval = Mathf.Max(minSpawnInterval, enemySpawner.SpawnInterval * spawnIntervalMultiplier);
        }

        if (CurrentWave >= bossWave) SpawnBoss();
    }

    [ContextMenu("즉시 보스 소환")]
    public void ForceSpawnBoss()
    {
        if (_bossSpawned) return;
        if (CurrentWave < bossWave) CurrentWave = bossWave;
        SpawnBoss();
    }

    private void SpawnBoss()
    {
        if (_bossSpawned || bossPrefab == null) return;
        _bossSpawned = true;

        if (enemySpawner != null) enemySpawner.SpawningEnabled = false;

        Boss boss = Instantiate(bossPrefab, RightEdgeSpawnPoint(), Quaternion.identity);
        Health bossHealth = boss.GetComponent<Health>();
        bossHealth.OnDeath += HandleBossDeath;

        if (bossHpUI != null) bossHpUI.Show(bossHealth, "고대의 포식자");
        ShowOrDeferBanner($"WAVE {bossWave} — BOSS");
    }

    /// <summary>증강 카드가 떠 있으면 배너를 미루고, 아니면 바로 띄운다.</summary>
    private void ShowOrDeferBanner(string text)
    {
        if (bossBanner == null) return;

        if (augmentManager != null && augmentManager.IsShowing)
        {
            _pendingBannerText = text;
        }
        else
        {
            bossBanner.Show(text);
        }
    }

    private void HandlePanelClosed()
    {
        if (_pendingBannerText == null || bossBanner == null) return;

        bossBanner.Show(_pendingBannerText);
        _pendingBannerText = null;
    }

    private void HandleBossDeath(Health _)
    {
        if (bossHpUI != null) bossHpUI.Hide();
        OnBossDefeated?.Invoke();
    }

    private void HandleEnemySpawned(Enemy enemy)
    {
        Health h = enemy.GetComponent<Health>();
        if (h == null) return;

        float multiplier = 1f + enemyHealthStepBonus * (CurrentWave - 1);
        h.SetMaxHp(h.MaxHp * multiplier);
    }

    /// <summary>기획서 상 보스 출현 지점: 맵(카메라 시야) 오른쪽 바깥.</summary>
    private Vector3 RightEdgeSpawnPoint()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        const float margin = 3f;

        Vector3 center = cam.transform.position;
        return new Vector3(center.x + halfWidth + margin, center.y, 0f);
    }
}
