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
    [Tooltip("도달한 시대 이름. 클리어 시에는 숨긴다 (마지막 시대 도달이 자명하므로)")]
    [SerializeField] private TMP_Text eraText;

    [Header("랭킹 (클리어 시에만)")]
    [Tooltip("랭킹 카드 전체(RankingWindow). 사망 시에는 꺼 둔다")]
    [SerializeField] private GameObject rankingRoot;

    [Tooltip("클리어 타임 상위 10개를 한 덩어리로 그린다. 폰트는 LiberationSans여야 한다 — " +
             "Pretendard 서브셋에는 F G J N Q U X Y Z 가 없어 이름이 □로 깨진다")]
    [SerializeField] private TMP_Text rankingText;

    [Tooltip("결과 창(Window). 클리어 때 랭킹 카드와 한 쌍으로 가운데 정렬하려고 왼쪽으로 민다")]
    [SerializeField] private RectTransform resultWindow;

    [Tooltip("클리어 시 결과 창을 밀 X 거리. 창(760)과 카드(430)를 합쳐 가운데를 맞추는 값이다.\n" +
             "밀지 않으면 4:3 화면에서 카드 오른쪽이 잘린다 — 보이는 반폭이 831인데 끝이 840이었다(실측).")]
    [SerializeField] private float clearWindowShiftX = -230f;

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

        // 랭킹 카드는 클리어 때만. 사망 결과에 "클리어 타임 순위"를 붙이면 말이 안 된다.
        if (rankingRoot != null) rankingRoot.SetActive(isClear);

        // 창 하나만 있을 때는 가운데, 카드가 붙으면 둘을 한 덩어리로 보고 왼쪽으로 민다.
        if (resultWindow != null)
        {
            Vector2 p = resultWindow.anchoredPosition;
            p.x = isClear ? clearWindowShiftX : 0f;
            resultWindow.anchoredPosition = p;
        }

        // 패널을 켠 뒤에 부른다 — 비활성 오브젝트에서는 코루틴이 돌지 않는다.
        // 랭킹은 timeScale=0에서도 떠야 하는데, Leaderboard가 자기 오브젝트에서 코루틴을 돌리고
        // UnityWebRequest는 timeScale과 무관해서 문제없다.
        if (isClear) HandleRanking();

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
                EraManager.EraConfig cfg = eraManager != null ? eraManager.CurrentConfig : null;
                eraText.text = cfg != null ? cfg.eraShortName : "원시";
            }
        }
    }

    /// <summary>
    /// 클리어했을 때만 랭킹을 다룬다. 사망 기록까지 올리면 "클리어 타임 랭킹"이 성립하지 않는다.
    /// 이름을 안 적었으면 등록은 건너뛰고 조회만 한다 — 익명으로 순위표를 보는 것은 막을 이유가 없다.
    /// </summary>
    private void HandleRanking()
    {
        if (rankingText == null) return;

        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        rankingText.gameObject.SetActive(true);

        // 이 카드의 문구는 전부 영문이다. 폰트가 LiberationSans라서가 아니라,
        // Pretendard 서브셋 159자에 "불러오는 중 / 기록 없음 / 연결 실패"의 글자가 거의 없어
        // 한글로 쓰면 통째로 □가 된다 (불 러 는 록 없 음 연 결 실 패 전부 미포함).
        rankingText.text = "RANKING\n\nLOADING...";

        long ms = (long)(gm.SurvivalTime * 1000f);
        string playerName = Leaderboard.PlayerName;

        if (string.IsNullOrEmpty(playerName)) Leaderboard.Fetch(RenderRanking);
        else Leaderboard.Submit(playerName, ms, RenderRanking);
    }

    /// <summary>
    /// null이면 통신 실패다. 패널을 지우지 않고 안내만 바꾼다 —
    /// 랭킹이 죽었다고 결과 화면이 비면 클리어한 사람 입장에서 더 나쁘다.
    /// </summary>
    private void RenderRanking(Leaderboard.Entry[] entries)
    {
        if (rankingText == null) return;

        if (entries == null)
        {
            rankingText.text = "RANKING\n\nOFFLINE";
            return;
        }

        if (entries.Length == 0)
        {
            rankingText.text = "RANKING\n\nNO RECORDS YET";
            return;
        }

        string me = Leaderboard.PlayerName;

        // mspace로 강제 고정폭을 준다. LiberationSans는 가변폭이라 이게 없으면 자릿수가 안 맞아
        // 순위표가 계단처럼 어긋난다.
        var sb = new System.Text.StringBuilder("RANKING\n<mspace=0.62em>");
        for (int i = 0; i < entries.Length; i++)
        {
            Leaderboard.Entry e = entries[i];
            bool mine = !string.IsNullOrEmpty(me) && e.name == me;

            // 내 기록은 굵게. 10줄 중 어디에 있는지 바로 찾게 하려는 것.
            if (mine) sb.Append("<b>");
            sb.Append($"{i + 1,2}. {e.name,-8} {Leaderboard.Format(e.ms)}");
            if (mine) sb.Append("</b>");
            if (i < entries.Length - 1) sb.Append('\n');
        }
        sb.Append("</mspace>");

        rankingText.text = sb.ToString();
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
