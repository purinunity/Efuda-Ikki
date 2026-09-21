using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public Button backButton;
    public TitleUIManager titleUIManager;
    private Button subscribedBackButton;

    private void Start()
    {
        WireButtonListener();
    }

    private void OnEnable()
    {
        WireButtonListener();
    }

    private void OnDisable()
    {
        UnwireButtonListener();
    }

    private void OnDestroy()
    {
        UnwireButtonListener();
    }

    private void WireButtonListener()
    {
        UnwireButtonListener();
        subscribedBackButton = backButton;
        subscribedBackButton?.onClick.AddListener(HandleBackClicked);
    }

    private void UnwireButtonListener()
    {
        subscribedBackButton?.onClick.RemoveListener(HandleBackClicked);
        subscribedBackButton = null;
    }

    private void HandleBackClicked()
    {
        titleUIManager?.BackToMainMenu();
    }
}
