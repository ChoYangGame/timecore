using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 증강 카드 1장. AugmentManager가 매 레벨업마다 데이터를 채우고 클릭 콜백을 다시 연결한다.
/// 부착 대상: AugmentPanel/Card0, Card1, Card2
/// </summary>
[DisallowMultipleComponent]
public class AugmentCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button button;

    public void Setup(AugmentData data, Action<AugmentData> onChosen)
    {
        if (titleText != null) titleText.text = data.DisplayName;
        if (descriptionText != null) descriptionText.text = data.Description;

        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onChosen(data));
    }
}
