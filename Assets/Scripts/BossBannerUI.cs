using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 화면 중앙에 문구를 잠깐 띄웠다가 페이드아웃하는 배너. 보스 등장 연출용.
/// 부착 대상: BossBanner (HUD_Canvas의 자식, TMP_Text)
/// </summary>
[DisallowMultipleComponent]
public class BossBannerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeDuration = 0.6f;

    public bool IsShowing => gameObject.activeSelf;

    public void Show(string text)
    {
        if (label == null) return;

        gameObject.SetActive(true);
        label.text = text;
        label.alpha = 1f;

        StopAllCoroutines();
        StartCoroutine(DisplayThenFade());
    }

    /// <summary>더 우선순위가 높은 UI(증강 카드 등)가 끼어들 때 즉시 숨긴다.</summary>
    public void CancelImmediate()
    {
        StopAllCoroutines();
        if (label != null) label.alpha = 1f;
        gameObject.SetActive(false);
    }

    // Time.timeScale이 0이어도(증강 카드가 게임을 멈춘 동안) 배너 자체의 표시/페이드 타이머는
    // 끊기지 않고 흘러야 하므로 unscaledDeltaTime 기반으로 돈다.
    private IEnumerator DisplayThenFade()
    {
        yield return new WaitForSecondsRealtime(displayDuration);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            label.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        gameObject.SetActive(false);
        label.alpha = 1f;
    }
}
