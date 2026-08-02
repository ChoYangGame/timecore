using UnityEngine;
using UnityEngine.UI;

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
    }

    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartRun);
    }

    public void StartRun()
    {
        if (!IsWaitingToStart) return;

        IsWaitingToStart = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        // 생존 시간은 여기서부터 센다. 타이틀 대기 시간이 기록에 섞이면 안 된다.
        if (GameManager.Instance != null) GameManager.Instance.ResetRunTimer();

        Time.timeScale = 1f;
    }
}
