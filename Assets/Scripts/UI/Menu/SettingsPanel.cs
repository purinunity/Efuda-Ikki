using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    public Button backButton;
    public TitleUIManager titleUIManager;
    private Button subscribedBackButton;
    private Button resetButton;
    private GameObject confirmationRoot;
    private TextMeshProUGUI statusText;

    private void Start()
    {
        EnsureResetUi();
        WireButtonListener();
    }

    private void OnEnable()
    {
        EnsureResetUi();
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

    private void EnsureResetUi()
    {
        if (resetButton != null) return;

        TMP_FontAsset font = GetComponentInChildren<TextMeshProUGUI>(true)?.font
            ?? TMP_Settings.defaultFontAsset;
        RectTransform overlay = RuntimeUiFactory.CreateRect("SaveDataControls", transform);
        RuntimeUiFactory.Stretch(overlay);
        LayoutElement layout = overlay.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;

        resetButton = CreateButton(
            "ResetSaveDataButton",
            overlay,
            "セーブデータを初期化",
            font,
            new Vector2(0.35f, 0.34f),
            new Vector2(0.65f, 0.46f));
        resetButton.onClick.AddListener(() => confirmationRoot.SetActive(true));

        statusText = RuntimeUiFactory.CreateText(
            "ResetStatus",
            overlay,
            font,
            24,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            Color.white);
        RuntimeUiFactory.SetNormalizedRect(statusText.rectTransform, 0.25f, 0.24f, 0.75f, 0.32f);
        statusText.text = string.Empty;

        confirmationRoot = new GameObject("ResetConfirmation", typeof(RectTransform), typeof(Image));
        confirmationRoot.transform.SetParent(overlay, false);
        RuntimeUiFactory.Stretch(confirmationRoot.GetComponent<RectTransform>());
        confirmationRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);

        RectTransform dialog = RuntimeUiFactory.CreateRect("Dialog", confirmationRoot.transform);
        RuntimeUiFactory.SetNormalizedRect(dialog, 0.27f, 0.32f, 0.73f, 0.68f);
        RuntimeUiFactory.GetOrAdd<Image>(dialog.gameObject).color = new Color(0.12f, 0.08f, 0.06f, 1f);

        TextMeshProUGUI message = RuntimeUiFactory.CreateText(
            "Message", dialog, font, 30, FontStyles.Bold,
            TextAlignmentOptions.Center, Color.white);
        RuntimeUiFactory.SetNormalizedRect(message.rectTransform, 0.06f, 0.48f, 0.94f, 0.9f);
        message.text = "すべての進行データを初期化しますか？\nこの操作は元に戻せません。";

        Button confirm = CreateButton(
            "ConfirmReset", dialog, "初期化する", font,
            new Vector2(0.08f, 0.1f), new Vector2(0.47f, 0.4f));
        Button cancel = CreateButton(
            "CancelReset", dialog, "戻る", font,
            new Vector2(0.53f, 0.1f), new Vector2(0.92f, 0.4f));
        confirm.onClick.AddListener(ResetProgress);
        cancel.onClick.AddListener(() => confirmationRoot.SetActive(false));
        confirmationRoot.SetActive(false);
    }

    private void ResetProgress()
    {
        GameProgressStore.ResetProgress();
        GameModeManager.ResetGameModeData();
        confirmationRoot.SetActive(false);
        statusText.text = "セーブデータを初期化しました";
        Debug.Log("Save data was reset from the settings screen.");
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        TMP_FontAsset font,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        Button button = RuntimeUiFactory.CreateButton(name, parent);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        button.GetComponent<Image>().color = new Color(0.65f, 0.18f, 0.12f, 1f);

        TextMeshProUGUI text = RuntimeUiFactory.CreateText(
            "Label", rect, font, 25, FontStyles.Bold,
            TextAlignmentOptions.Center, Color.white);
        RuntimeUiFactory.Stretch(text.rectTransform);
        text.text = label;
        return button;
    }
}
