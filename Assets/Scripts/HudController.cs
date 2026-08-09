using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 상단 HUD를 GameManager / WaveManager / EnemySpawner 값으로 매 프레임 갱신한다.
///
/// 웨이브를 화면에 내보내는 것이 이 클래스의 핵심 목적이다 —
/// 웨이브가 어디에도 표시되지 않아 플레이어가 그 개념이 있는 줄도 몰랐다(사용자 지적).
/// 숫자만 던지지 않고 "몇 웨이브 / 다음까지 얼마 / 이번 웨이브에 몇 마리 잡았고 몇 마리 남았나"를
/// 한 덩어리로 묶어 보여준다.
///
/// 부착 대상: HUD_Canvas
/// </summary>
[DisallowMultipleComponent]
public class HudController : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image expFillImage;

    [Header("웨이브 패널")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private EnemySpawner enemySpawner;

    [Tooltip("'WAVE 3' 같은 큰 표시")]
    [SerializeField] private TMP_Text waveText;

    [Tooltip("다음 웨이브까지 남은 시간. 보스전에는 보스 표시로 바뀐다")]
    [SerializeField] private TMP_Text waveTimerText;

    [Tooltip("이번 웨이브 처치 / 필드에 남은 적")]
    [SerializeField] private TMP_Text waveProgressText;

    [Tooltip("다음 웨이브까지의 진행도 막대")]
    [SerializeField] private Image waveFillImage;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (timerText != null) timerText.text = FormatTime(gm.SurvivalTime);
        if (killText != null) killText.text = $"{gm.KillCount}";
        if (levelText != null) levelText.text = $"Lv.{gm.Level}";
        if (expFillImage != null) expFillImage.fillAmount = gm.ExpRatio;

        if (waveManager == null) return;

        bool boss = waveManager.BossActive;

        if (waveText != null)
            waveText.text = boss ? "BOSS" : $"WAVE {waveManager.CurrentWave}";

        if (waveTimerText != null)
        {
            // 보스전에는 카운트다운이 멈추므로(웨이브 진행이 정지) 숫자를 그대로 두면 거짓말이 된다.
            waveTimerText.text = boss
                ? "—"
                : $"{Mathf.CeilToInt(waveManager.TimeToNextWave)}";
        }

        if (waveFillImage != null)
        {
            // 남은 시간이 아니라 "채워지는" 방향이어야 다음 단계가 다가오는 느낌이 난다.
            float ratio = boss ? 1f : 1f - Mathf.Clamp01(waveManager.TimeToNextWave / Mathf.Max(0.0001f, waveManager.WaveDuration));
            waveFillImage.fillAmount = ratio;
        }

        if (waveProgressText != null)
        {
            int alive = enemySpawner != null ? enemySpawner.AliveCount : 0;
            waveProgressText.text = $"처치 {waveManager.KillsThisWave}   생존 {alive}";
        }
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int min = total / 60;
        int sec = total % 60;
        return $"{min:00}:{sec:00}";
    }
}
