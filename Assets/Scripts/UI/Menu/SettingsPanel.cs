using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    public Button backButton;
    public TitleUIManager titleUIManager;

    [Header("Scene References")]
    [SerializeField] private Button resetButton;
    [SerializeField] private GameObject confirmationRoot;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button confirmResetButton;
    [SerializeField] private Button cancelResetButton;

    private Button subscribedBackButton;

    private void Start()
    {
        ValidateSceneReferences();
        WireButtonListeners();
        HideConfirmation();
    }

    private void OnEnable()
    {
        WireButtonListeners();
    }

    private void OnDisable() => UnwireButtonListeners();

    private void OnDestroy() => UnwireButtonListeners();

    private void WireButtonListeners()
    {
        UnwireButtonListeners();
        subscribedBackButton = backButton;
        subscribedBackButton?.onClick.AddListener(HandleBackClicked);
        resetButton?.onClick.AddListener(ShowConfirmation);
        confirmResetButton?.onClick.AddListener(ResetProgress);
        cancelResetButton?.onClick.AddListener(HideConfirmation);
    }

    private void UnwireButtonListeners()
    {
        subscribedBackButton?.onClick.RemoveListener(HandleBackClicked);
        resetButton?.onClick.RemoveListener(ShowConfirmation);
        confirmResetButton?.onClick.RemoveListener(ResetProgress);
        cancelResetButton?.onClick.RemoveListener(HideConfirmation);
        subscribedBackButton = null;
    }

    private void HandleBackClicked() => titleUIManager?.BackToMainMenu();

    private void ShowConfirmation()
    {
        if (confirmationRoot != null) confirmationRoot.SetActive(true);
    }

    private void HideConfirmation()
    {
        if (confirmationRoot != null) confirmationRoot.SetActive(false);
    }

    private void ResetProgress()
    {
        GameProgressStore.ResetProgress();
        GameModeManager.ResetGameModeData();
        HideConfirmation();
        if (statusText != null) statusText.text = "セーブデータを初期化しました";
        Debug.Log("Save data was reset from the settings screen.");
    }

    private void ValidateSceneReferences()
    {
        if (resetButton == null || confirmationRoot == null || statusText == null ||
            confirmResetButton == null || cancelResetButton == null)
        {
            Debug.LogError("SettingsPanel: scene references are incomplete.", this);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && !string.IsNullOrEmpty(gameObject.scene.path))
        {
            ValidateSceneReferences();
        }
    }
}
