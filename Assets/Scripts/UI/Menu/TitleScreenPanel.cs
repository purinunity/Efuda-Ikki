using UnityEngine;
using UnityEngine.UI;

public class TitleScreenPanel : MonoBehaviour
{
    public Button titleButton;
    public TitleUIManager titleUIManager;

    private void Start()
    {
        if (titleButton != null)
        {
            titleButton.onClick.AddListener(() => titleUIManager.ShowMainMenu());
        }
    }
}
