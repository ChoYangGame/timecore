using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 시작 시 타이틀을 띄우고 timeScale=0으로 멈춰 둔다. "파견 시작"을 누르면 게임이 흐르기 시작한다.
/// timeScale=0 동안 GameManager.SurvivalTime과 WaveManager의 타이머는 Time.deltaTime이 0이라
/// 전혀 누적되지 않으므로, 타이틀 대기 시간은 기록에 섞이지 않는다.
/// 부착 대상: HUD_Canvas (TitlePanel을 panelRoot로 연결)
/// </summary>
[DisallowMultipleComponent]
public class TitleController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button startButton;

    [Tooltip("랭킹에 올릴 이름. 비워두면 기록을 등록하지 않는다 (익명 플레이 허용)")]
    [SerializeField] private TMP_InputField nameInput;

    /// <summary>"파견 시작"을 누르기 전인지. 다른 시스템이 시작 전 상태를 물어볼 때 쓴다.</summary>
    public bool IsWaitingToStart { get; private set; }

    // 어떤 Update()보다도 먼저 멈춰야 첫 프레임의 deltaTime조차 새지 않는다.
    private void Awake()
    {
        IsWaitingToStart = true;
        Time.timeScale = 0f;
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    private void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(StartRun);

        if (nameInput != null)
        {
            // 지난 판에서 쓴 이름을 그대로 채워 둔다. 매번 다시 치게 하면 귀찮다.
            nameInput.text = Leaderboard.PlayerName;
            nameInput.characterLimit = 8;
            // 입력하는 동안 바로 걸러 준다. 서버도 같은 검사를 하지만, 눌러 보고 거절당하는 것보다 낫다.
            nameInput.onValueChanged.AddListener(HandleNameChanged);
        }
    }

    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartRun);
        if (nameInput != null) nameInput.onValueChanged.RemoveListener(HandleNameChanged);
    }

    /// <summary>
    /// 한글·소문자·기호를 즉시 떨어낸다. 폰트 서브셋(159자)에 한글 이름을 담을 수 없어
    /// 이름은 A-Z 0-9로 제한하고 LiberationSans로 그린다 (docs/FONT_CHARS.md 참조).
    /// </summary>
    private void HandleNameChanged(string raw)
    {
        string clean = Leaderboard.Sanitize(raw);
        if (clean == raw) return;

        // 대입이 onValueChanged를 다시 부르지만, 두 번째에는 clean == raw라 즉시 빠져나온다.
        nameInput.text = clean;
        nameInput.caretPosition = clean.Length;
    }

    public void StartRun()
    {
        if (!IsWaitingToStart) return;

        IsWaitingToStart = false;

        // 이름은 시작 시점에 확정해 저장한다. 클리어 시점에 InputField를 다시 읽지 않는 이유는
        // 그때 TitlePanel이 이미 비활성이라 참조가 위태롭고, 판 도중에 이름이 바뀌면 안 되기 때문이다.
        if (nameInput != null) Leaderboard.PlayerName = nameInput.text;

        if (panelRoot != null) panelRoot.SetActive(false);

        // 생존 시간은 여기서부터 센다. 타이틀 대기 시간이 기록에 섞이면 안 된다.
        if (GameManager.Instance != null) GameManager.Instance.ResetRunTimer();

        Time.timeScale = 1f;
    }
}
