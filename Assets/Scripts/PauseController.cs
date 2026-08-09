using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// ESC로 게임을 멈추고 재개/재시작을 고르게 한다.
///
/// 이 프로젝트에서 `Time.timeScale = 0`을 쓰는 곳이 이미 셋이다 —
/// 타이틀(시작 전), 증강 카드(선택 중), 게임오버(판 종료).
/// 그 위에 일시정지를 겹치면 재개할 때 timeScale을 1로 되돌리면서
/// **아직 멈춰 있어야 할 상태까지 풀어 버린다**(카드가 떠 있는데 게임이 도는 식).
/// 그래서 "이미 누군가 멈춰 둔 상황"에서는 아예 일시정지가 걸리지 않게 막는다.
///
/// 부착 대상: HUD_Canvas (PausePanel을 panelRoot로 연결)
/// </summary>
[DisallowMultipleComponent]
public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;

    [Header("겹침 방지용 참조")]
    [Tooltip("시작 전에는 이미 timeScale=0이라 일시정지를 걸면 안 된다")]
    [SerializeField] private TitleController titleController;

    [Tooltip("증강 카드가 떠 있으면 그쪽이 이미 멈춰 둔 상태다")]
    [SerializeField] private AugmentManager augmentManager;

    [SerializeField] private Key pauseKey = Key.Escape;

    public bool IsPaused { get; private set; }

    private void Start()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!Keyboard.current[pauseKey].wasPressedThisFrame) return;

        if (IsPaused) Resume();
        else Pause();
    }

    /// <summary>
    /// 다른 시스템이 이미 게임을 멈춰 둔 상태면 일시정지를 걸지 않는다.
    /// 걸어 버리면 재개할 때 그쪽 정지까지 같이 풀린다.
    /// </summary>
    public bool CanPause()
    {
        if (IsPaused) return false;
        if (titleController != null && titleController.IsWaitingToStart) return false;
        if (augmentManager != null && augmentManager.IsShowing) return false;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return false;
        return true;
    }

    /// <summary>
    /// 가드를 메서드 안에서 건다. 밖에서 확인하는 구조로 두면 호출부가 하나만 빠뜨려도
    /// 타이틀·증강 카드가 떠 있는 동안 timeScale이 이 클래스 손에 넘어간다(실측으로 확인).
    /// </summary>
    public void Pause()
    {
        if (!CanPause()) return;

        IsPaused = true;
        if (panelRoot != null) panelRoot.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (!IsPaused) return;

        IsPaused = false;
        if (panelRoot != null) panelRoot.SetActive(false);

        // 여기서 1로 되돌려도 안전하다 — CanPause()가 다른 시스템이 멈춰 둔 동안에는
        // 애초에 일시정지를 못 걸게 막았으므로, 지금 0인 이유는 이 클래스뿐이다.
        Time.timeScale = 1f;
    }

    /// <summary>씬을 다시 로드한다. timeScale은 로드 전에 반드시 되돌려야 한다.</summary>
    public void Restart()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
