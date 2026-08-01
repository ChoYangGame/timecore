using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 생존 시간 / 킬 수 / 레벨 / 경험치 바 텍스트를 GameManager 값으로 매 프레임 갱신한다.
/// 부착 대상: HUD_Canvas
/// </summary>
[DisallowMultipleComponent]
public class HudController : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image expFillImage;

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        if (timerText != null) timerText.text = FormatTime(gm.SurvivalTime);
        if (killText != null) killText.text = $"Kills {gm.KillCount}";
        if (levelText != null) levelText.text = $"Lv.{gm.Level}";
        if (expFillImage != null) expFillImage.fillAmount = gm.ExpRatio;
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        int min = total / 60;
        int sec = total % 60;
        return $"{min:00}:{sec:00}";
    }
}
