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
    [SerializeField] private Sprite ikkiModeButtonHoverSprite;
    [SerializeField] private Sprite kachinukiModeButtonHoverSprite;
    [SerializeField] private Sprite lockedBattleGroundButtonSprite;
    [SerializeField] private Sprite settingsButtonHoverSprite;
    [SerializeField] private bool hideButtonTextLabels = true;
    [SerializeField] private Color lockedBattleGroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);

    [Header("Scene References")]
    [SerializeField] private TextMeshProUGUI battleGroundStatusLabel;
    private Button subscribedIkkiModeButton;
    private Button subscribedBattleGroundButton;
    private Button subscribedSettingsButton;

    private void Awake()
    {
        ApplyModeButtonImages();
    }

    private void Start()
    {
        WireButtonListeners();
        ApplyModeButtonImages();
        RefreshModeAvailability();
    }

    private void OnEnable()
    {
        WireButtonListeners();
    }

    private void OnDisable()
    {
        UnwireButtonListeners();
    }

    private void OnDestroy()
    {
        UnwireButtonListeners();
    }

    private void OnValidate()
    {
        ApplyModeButtonImages();
    }

    private void WireButtonListeners()
    {
        UnwireButtonListeners();

        subscribedIkkiModeButton = kinouAriButton;
        subscribedBattleGroundButton = battleGroundButton;
        subscribedSettingsButton = settingsButton;

        subscribedIkkiModeButton?.onClick.AddListener(HandleIkkiModeClicked);
        subscribedBattleGroundButton?.onClick.AddListener(HandleBattleGroundClicked);
        subscribedSettingsButton?.onClick.AddListener(HandleSettingsClicked);
    }

    private void UnwireButtonListeners()
    {
        subscribedIkkiModeButton?.onClick.RemoveListener(HandleIkkiModeClicked);
        subscribedBattleGroundButton?.onClick.RemoveListener(HandleBattleGroundClicked);
        subscribedSettingsButton?.onClick.RemoveListener(HandleSettingsClicked);

        subscribedIkkiModeButton = null;
        subscribedBattleGroundButton = null;
        subscribedSettingsButton = null;
    }

    private void HandleIkkiModeClicked()
    {
        titleUIManager?.SelectIkkiMode();
    }

    private void HandleBattleGroundClicked()
    {
        titleUIManager?.SelectBattleGroundMode();
    }

    private void HandleSettingsClicked()
    {
        titleUIManager?.ShowSettings();
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
            // The old locked artwork contains its unlock condition across the
            // centre of the mode name.  Keep one clean mode image and show the
            // condition in the scene-authored status area instead.
            image.sprite = kachinukiModeButtonSprite != null
                ? kachinukiModeButtonSprite
                : lockedBattleGroundButtonSprite;
            image.color = unlocked ? Color.white : lockedBattleGroundColor;
            image.preserveAspect = true;
        }

        ColorBlock colors = battleGroundButton.colors;
        colors.disabledColor = Color.white;
        battleGroundButton.colors = colors;

        TextMeshProUGUI statusLabel = EnsureBattleGroundStatusLabel();
        if (statusLabel != null)
        {
            statusLabel.text = unlocked
                ? $"最高記録 {GameProgressStore.BestBattleGroundStreak}人抜き"
                : "ボス撃破で解放";
            statusLabel.color = unlocked
                ? Color.white
                : new Color(1f, 0.35f, 0.35f, 1f);
            ConfigureStatusLabelRect(statusLabel.rectTransform, unlocked);
            statusLabel.gameObject.SetActive(true);
        }

        SpriteState state = battleGroundButton.spriteState;
        Sprite lockedOrHover = unlocked
            ? kachinukiModeButtonHoverSprite
            : image != null ? image.sprite : kachinukiModeButtonSprite;
        state.highlightedSprite = lockedOrHover;
        state.pressedSprite = lockedOrHover;
        state.selectedSprite = lockedOrHover;
        battleGroundButton.spriteState = state;
    }

    private void ApplyModeButtonImages()
    {
        ApplyButtonImage(kinouAriButton, ikkiModeButtonSprite, ikkiModeButtonHoverSprite);
        ApplyButtonImage(battleGroundButton, kachinukiModeButtonSprite, kachinukiModeButtonHoverSprite);
        ApplyButtonImage(settingsButton, settingsButtonSprite, settingsButtonHoverSprite);
    }

    private void ApplyButtonImage(Button button, Sprite sprite, Sprite hoverSprite)
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

        if (hoverSprite != null)
        {
            SpriteState state = button.spriteState;
            state.highlightedSprite = hoverSprite;
            state.pressedSprite = hoverSprite;
            state.selectedSprite = hoverSprite;
            button.spriteState = state;
            button.transition = Selectable.Transition.SpriteSwap;
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

        Debug.LogError("MainMenuPanel: BattleGroundStatus is not assigned.", this);
        return battleGroundStatusLabel;
    }

    private static void ConfigureStatusLabelRect(RectTransform rectTransform, bool unlocked)
    {
        if (rectTransform == null)
        {
            return;
        }

        // Position and size are authored in latest.unity.
    }
}
