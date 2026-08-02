using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 판이 끝났을 때(사망 / 클리어) 게임을 멈추고 결과 패널을 띄운다. "재파견" 버튼은 씬을 다시 로드한다.
/// 두 결말이 정지 조건·정리 절차·재시작이 완전히 같아서 하나의 컨트롤러·하나의 패널로 처리하고,
/// 제목과 마지막 행 라벨만 바꿔 끼운다. GameManager.IsGameOver("판이 끝남") 하나로
/// 증강 카드·시대 전환 코루틴과의 timeScale 충돌을 양쪽 결말에서 동일하게 막는다.
/// 부착 대상: HUD_Canvas (GameOverPanel을 panelRoot로 연결)
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
    [SerializeField] private TMP_Text titleText;
    [Tooltip("생존 시간 / 처치 / 레벨 / 웨이브 라벨 4줄")]
    [SerializeField] private TMP_Text labelsText;
    [Tooltip("위 라벨에 대응하는 값 4줄")]
    [SerializeField] private TMP_Text valuesText;
    [Tooltip("도달한 시대 이름. 클리어 시에는 숨긴다 (중세 도달이 자명하므로)")]
    [SerializeField] private TMP_Text eraText;

    [SerializeField] private Button restartButton;

    [Header("결말별 문구")]
    [SerializeField] private string deathTitle = "시간선 붕괴";
    [SerializeField] private string clearTitle = "역사 복구 완료";
    [SerializeField] private string deathLabels = "생존 시간\n처치\n레벨\n도달 웨이브";
    [SerializeField] private string clearLabels = "생존 시간\n처치\n레벨\n최종 웨이브";

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<Health>();
        }

        if (playerHealth != null) playerHealth.OnDeath += HandlePlayerDeath;
        if (eraManager != null) eraManager.OnGameClear += HandleGameClear;
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDeath -= HandlePlayerDeath;
        if (eraManager != null) eraManager.OnGameClear -= HandleGameClear;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
    }

    private bool _resultShown;
    private bool _clearShown;

    private void HandlePlayerDeath(Health _)
    {
        // 결과가 이미 확정됐으면 무시한다. 클리어가 떠 있으면 사망이 덮지 않는다.
        if (_resultShown) return;
        ShowResult(deathTitle, deathLabels, showEra: true, isClear: false);
    }

    private void HandleGameClear()
    {
        if (_clearShown) return;

        // 보스를 잡으면서 죽는 경우 사망과 클리어가 같은 프레임에 겹친다.
        // 어느 쪽이 먼저 도착하든 클리어가 이긴다 — 사망 결과가 먼저 떴어도 덮어쓴다.
        // (timeScale=0이라 물리가 멈추므로 클리어가 사망보다 한 프레임 넘게 늦게 올 수는 없다)
        ShowResult(clearTitle, clearLabels, showEra: false, isClear: true);
    }

    private void ShowResult(string title, string labels, bool showEra, bool isClear)
    {
        _resultShown = true;
        _clearShown = isClear;

        // GameManager가 먼저 알아야 한다 — AugmentManager가 이 플래그를 보고
        // 카드 표시와 timeScale 복구를 건너뛴다 (레벨업이 같은 프레임에 겹치는 경우 방어).
        if (GameManager.Instance != null) GameManager.Instance.MarkGameOver();

        // 스폰을 끄기 전에 시대 전환을 먼저 중단해야 한다.
        // 전환 코루틴이 살아 있으면 ApplyEra()가 스폰을 도로 켜버린다.
        if (eraManager != null) eraManager.AbortTransition();

        if (enemySpawner != null) enemySpawner.SpawningEnabled = false;

        // 결과 패널보다 우선순위가 낮은 UI는 즉시 정리한다.
        if (augmentManager != null) augmentManager.ForceClose();
        if (bossBanner != null) bossBanner.CancelImmediate();

        Populate(title, labels, showEra);

        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Populate(string title, string labels, bool showEra)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (titleText != null) titleText.text = title;
        if (labelsText != null) labelsText.text = labels;

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
            eraText.gameObject.SetActive(showEra);
            if (showEra)
            {
                bool medieval = eraManager != null && eraManager.CurrentEra == EraManager.Era.Medieval;
                eraText.text = medieval ? "중세" : "원시";
            }
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
