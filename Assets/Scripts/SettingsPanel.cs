using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public Button backButton;
    public TitleUIManager titleUIManager;

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => titleUIManager.BackToMainMenu());
        }
    }
}
