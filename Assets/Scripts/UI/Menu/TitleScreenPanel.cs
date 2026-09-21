using UnityEngine;
using UnityEngine.UI;

public class TitleScreenPanel : MonoBehaviour
{
    public Button titleButton;
    public TitleUIManager titleUIManager;
    private Button subscribedTitleButton;

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
        subscribedTitleButton = titleButton;
        subscribedTitleButton?.onClick.AddListener(HandleTitleClicked);
    }

    private void UnwireButtonListener()
    {
        subscribedTitleButton?.onClick.RemoveListener(HandleTitleClicked);
        subscribedTitleButton = null;
    }

    private void HandleTitleClicked()
    {
        titleUIManager?.ShowMainMenu();
    }
}
