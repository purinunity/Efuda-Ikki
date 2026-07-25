using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuPanel : MonoBehaviour
{
    public Button kinouAriButton;
    public Button battleGroundButton;
    public Button settingsButton;
    public TitleUIManager titleUIManager;

    [Header("Mode Button Images")]
    [SerializeField] private Sprite ikkiModeButtonSprite;
    [SerializeField] private Sprite kachinukiModeButtonSprite;
    [SerializeField] private Sprite settingsButtonSprite;
    [SerializeField] private bool hideButtonTextLabels = true;
    [SerializeField] private Color lockedBattleGroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);

    private TextMeshProUGUI battleGroundStatusLabel;

    private void Awake()
    {
        ApplyModeButtonImages();
    }

    private void Start()
    {
        if (kinouAriButton != null)
        {
            kinouAriButton.onClick.AddListener(() => titleUIManager.SelectIkkiMode());
        }
        if (battleGroundButton != null)
        {
            battleGroundButton.onClick.AddListener(() => titleUIManager.SelectBattleGroundMode());
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => titleUIManager.ShowSettings());
        }

        ApplyModeButtonImages();
        RefreshModeAvailability();
    }

    private void OnValidate()
    {
        ApplyModeButtonImages();
    }

    public void RefreshModeAvailability()
    {
        if (battleGroundButton == null)
        {
            return;
        }

        bool unlocked = GameProgressStore.IsBattleGroundUnlocked;
        battleGroundButton.interactable = unlocked;

        Image image = battleGroundButton.targetGraphic as Image;
        if (image == null)
        {
            image = battleGroundButton.GetComponent<Image>();
        }

        if (image != null)
        {
            image.color = unlocked ? Color.white : lockedBattleGroundColor;
        }

        ColorBlock colors = battleGroundButton.colors;
        colors.disabledColor = Color.white;
        battleGroundButton.colors = colors;

        TextMeshProUGUI statusLabel = EnsureBattleGroundStatusLabel();
        if (statusLabel != null)
        {
            statusLabel.text = unlocked
                ? $"最高連勝 {GameProgressStore.BestBattleGroundStreak}"
                : "未解放\n最終ボス撃破で解放";
            ConfigureStatusLabelRect(statusLabel.rectTransform, unlocked);
            statusLabel.gameObject.SetActive(true);
        }
    }

    private void ApplyModeButtonImages()
    {
        ApplyButtonImage(kinouAriButton, ikkiModeButtonSprite);
        ApplyButtonImage(battleGroundButton, kachinukiModeButtonSprite);
        ApplyButtonImage(settingsButton, settingsButtonSprite);
    }

    private void ApplyButtonImage(Button button, Sprite sprite)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        if (image != null && sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            button.targetGraphic = image;
        }

        SetTextLabelsActive(button.transform, !hideButtonTextLabels);
    }

    private static void SetTextLabelsActive(Transform root, bool isActive)
    {
        if (root == null)
        {
            return;
        }

        foreach (Text label in root.GetComponentsInChildren<Text>(true))
        {
            label.gameObject.SetActive(isActive);
        }

        foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            label.gameObject.SetActive(isActive);
        }
    }

    private TextMeshProUGUI EnsureBattleGroundStatusLabel()
    {
        if (battleGroundStatusLabel != null)
        {
            return battleGroundStatusLabel;
        }

        Transform existing = battleGroundButton.transform.Find("BattleGroundStatus");
        if (existing != null)
        {
            battleGroundStatusLabel = existing.GetComponent<TextMeshProUGUI>();
            if (battleGroundStatusLabel != null)
            {
                return battleGroundStatusLabel;
            }
        }

        TextMeshProUGUI template = battleGroundButton.GetComponentInChildren<TextMeshProUGUI>(true);
        GameObject labelObject = new GameObject(
            "BattleGroundStatus",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(battleGroundButton.transform, false);

        battleGroundStatusLabel = labelObject.GetComponent<TextMeshProUGUI>();
        if (template != null)
        {
            battleGroundStatusLabel.font = template.font;
        }

        battleGroundStatusLabel.alignment = TextAlignmentOptions.Center;
        battleGroundStatusLabel.enableAutoSizing = true;
        battleGroundStatusLabel.fontSizeMin = 16f;
        battleGroundStatusLabel.fontSizeMax = 32f;
        battleGroundStatusLabel.fontStyle = FontStyles.Bold;
        battleGroundStatusLabel.color = Color.white;
        battleGroundStatusLabel.outlineColor = Color.black;
        battleGroundStatusLabel.outlineWidth = 0.2f;
        battleGroundStatusLabel.raycastTarget = false;
        return battleGroundStatusLabel;
    }

    private static void ConfigureStatusLabelRect(RectTransform rectTransform, bool unlocked)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = unlocked ? new Vector2(0.08f, 0.04f) : new Vector2(0.08f, 0.18f);
        rectTransform.anchorMax = unlocked ? new Vector2(0.92f, 0.3f) : new Vector2(0.92f, 0.82f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
