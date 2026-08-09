using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "파견 시작"을 누르면 뜨는 직업 선택 화면. 셋 중 하나를 고르면 그 무기만 켜고 판을 시작한다.
///
/// 무기 세 개는 전부 Player에 붙어 있고 고른 하나만 enabled가 된다.
/// 런타임에 AddComponent 하지 않는 이유: 무기마다 인스펙터에서 조정한 값(총알 프리팹·데미지·반경)이
/// 있어서 코드로 만들면 그 값을 전부 코드에 박아야 한다.
///
/// 부착 대상: HUD_Canvas (ClassPanel을 panelRoot로 연결)
/// </summary>
[DisallowMultipleComponent]
public class ClassSelectController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TitleController titleController;

    [Header("직업 버튼 3종")]
    [SerializeField] private Button gunnerButton;
    [SerializeField] private Button bladeButton;
    [SerializeField] private Button mageButton;

    public bool IsShowing => panelRoot != null && panelRoot.activeSelf;

    private void Start()
    {
        if (gunnerButton != null) gunnerButton.onClick.AddListener(() => Select(PlayerClass.Gunner));
        if (bladeButton != null) bladeButton.onClick.AddListener(() => Select(PlayerClass.Blade));
        if (mageButton != null) mageButton.onClick.AddListener(() => Select(PlayerClass.Mage));

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>TitleController가 "파견 시작"에서 부른다.</summary>
    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Select(PlayerClass cls)
    {
        if (GameManager.Instance != null) GameManager.Instance.SetSelectedClass(cls);

        ApplyWeapon(cls);

        if (panelRoot != null) panelRoot.SetActive(false);
        if (titleController != null) titleController.StartRun();
    }

    /// <summary>
    /// 고른 직업의 무기만 켜고 나머지는 끈다.
    /// Player에 붙은 PlayerWeapon을 전부 훑으므로 무기를 추가해도 여기를 고칠 필요가 없다.
    /// </summary>
    public static void ApplyWeapon(PlayerClass cls)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        foreach (PlayerWeapon w in player.GetComponents<PlayerWeapon>())
            w.enabled = w.Class == cls;
    }
}
