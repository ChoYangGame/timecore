using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 사망을 받아 게임을 멈추고 결과 패널을 띄운다. "재파견" 버튼은 씬을 다시 로드한다.
/// 증강 카드와 timeScale이 충돌하지 않도록 GameManager.IsGameOver 플래그로 조율한다.
/// 부착 대상: GameOverPanel (HUD_Canvas의 자식)
/// </summary>
[DisallowMultipleComponent]
public class GameOverController : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EraManager eraManager;
    [SerializeField] private AugmentManager augmentManager;
    [SerializeField] private BossBannerUI bossBanner;

    [Header("결과 텍스트")]
    [Tooltip("생존 시간 / 처치 / 레벨 / 도달 웨이브 값을 줄바꿈으로 넣는다")]
    [SerializeField] private TMP_Text valuesText;
    [Tooltip("도달한 시대 이름만 표시 (원시 / 중세)")]
    [SerializeField] private TMP_Text eraText;

    [SerializeField] private Button restartButton;

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<Health>();
        }

        if (playerHealth != null) playerHealth.OnDeath += HandlePlayerDeath;
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDeath -= HandlePlayerDeath;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
    }

    private void HandlePlayerDeath(Health _)
    {
        // GameManager가 먼저 알아야 한다 — AugmentManager가 이 플래그를 보고
        // 카드 표시와 timeScale 복구를 건너뛴다 (사망과 레벨업이 같은 프레임에 겹치는 경우 방어).
        if (GameManager.Instance != null) GameManager.Instance.MarkGameOver();

        // 스폰을 끄기 전에 시대 전환을 먼저 중단해야 한다.
        // 전환 코루틴이 살아 있으면 ApplyEra()가 스폰을 도로 켜버린다.
        if (eraManager != null) eraManager.AbortTransition();

        if (enemySpawner != null) enemySpawner.SpawningEnabled = false;

        // 게임오버보다 우선순위가 낮은 UI는 즉시 정리한다.
        if (augmentManager != null) augmentManager.ForceClose();
        if (bossBanner != null) bossBanner.CancelImmediate();

        Populate();

        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Populate()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (valuesText != null)
        {
            int wave = waveManager != null ? waveManager.CurrentWave : 1;
            valuesText.text =
                $"{FormatTime(gm.SurvivalTime)}\n" +
                $"{gm.KillCount}\n" +
                $"Lv.{gm.Level}\n" +
                $"{wave}";
        }

        if (eraText != null)
        {
            bool medieval = eraManager != null && eraManager.CurrentEra == EraManager.Era.Medieval;
            eraText.text = medieval ? "중세" : "원시";
        }
    }

    /// <summary>씬을 다시 로드한다. timeScale은 로드 전에 반드시 되돌려야 한다.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
