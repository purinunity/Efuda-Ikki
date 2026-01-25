using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : MonoBehaviour
{
    public Button kinouAriButton;
    public Button battleGroundButton;
    public Button settingsButton;
    public TitleUIManager titleUIManager;

    private void Start()
    {
        if (kinouAriButton != null)
        {
            kinouAriButton.onClick.AddListener(() => titleUIManager.SelectKinouAriMode());
        }
        if (battleGroundButton != null)
        {
            battleGroundButton.onClick.AddListener(() => titleUIManager.SelectBattleGroundMode());
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => titleUIManager.ShowSettings());
        }
    }
}
