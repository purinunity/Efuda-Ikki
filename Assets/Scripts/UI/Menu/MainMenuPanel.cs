using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;

public class MainMenuPanel : MonoBehaviour
{
    public Button kinouAriButton;
    public Button battleGroundButton;
    public Button settingsButton;
    public TitleUIManager titleUIManager;

    [Header("Mode Button Images")]
    [Tooltip("LocalizeSprite が付いたボタンでは、以下の画像は使われない(LocalizeSprite の画像が優先)")]
    [SerializeField] private Sprite ikkiModeButtonSprite;
    [SerializeField] private Sprite kachinukiModeButtonSprite;
    [SerializeField] private Sprite settingsButtonSprite;
    [SerializeField] private Sprite ikkiModeButtonHoverSprite;
    [SerializeField] private Sprite kachinukiModeButtonHoverSprite;
    [SerializeField] private Sprite lockedBattleGroundButtonSprite;
    [SerializeField] private Sprite settingsButtonHoverSprite;
    [SerializeField] private bool hideButtonTextLabels = true;

    [Header("Scene References")]
    [SerializeField] private TextMeshProUGUI battleGroundStatusLabel;

    [Header("Localization")]
    [Tooltip("「最高記録：{0}連勝」の訳文(UI テーブルのキー)。{0} に最高記録の数字が入る")]
    [SerializeField] private LocalizedString bestStreakText = new LocalizedString();
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

        if (!bestStreakText.IsEmpty)
        {
            // 数字を先に渡してから登録する(言語が変わるたびに OnBestStreakTextChanged が呼ばれる)
            bestStreakText.Arguments = new object[] { GameProgressStore.BestBattleGroundStreak };
            bestStreakText.StringChanged += OnBestStreakTextChanged;
        }
    }

    private void OnDisable()
    {
        UnwireButtonListeners();

        if (!bestStreakText.IsEmpty)
        {
            bestStreakText.StringChanged -= OnBestStreakTextChanged;
        }
    }

    private void UpdateBestStreakText()
    {
        int best = GameProgressStore.BestBattleGroundStreak;

        if (bestStreakText.IsEmpty)
        {
            // 訳文が未設定の場合は日本語で表示する
            battleGroundStatusLabel.text = $"最高記録：{best}連勝";
            return;
        }

        bestStreakText.Arguments = new object[] { best };
        bestStreakText.RefreshString();
    }

    private void OnBestStreakTextChanged(string value)
    {
        if (battleGroundStatusLabel != null)
        {
            battleGroundStatusLabel.text = value;
        }
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

        // LocalizeSprite が付いていれば、画像の切り替えはそちらに任せる
        LocalizeSprite localizeSprite = battleGroundButton.GetComponent<LocalizeSprite>();

        if (image != null)
        {
            if (localizeSprite == null)
            {
                Sprite desiredSprite = unlocked
                    ? kachinukiModeButtonSprite
                    : lockedBattleGroundButtonSprite;
                if (desiredSprite != null)
                {
                    image.sprite = desiredSprite;
                }
            }

            image.color = Color.white;
            image.preserveAspect = true;
        }

        ColorBlock colors = battleGroundButton.colors;
        colors.disabledColor = Color.white;
        battleGroundButton.colors = colors;

        // 解放後だけ「最高記録：〇連勝」を表示する
        if (battleGroundStatusLabel != null)
        {
            battleGroundStatusLabel.gameObject.SetActive(unlocked);
            if (unlocked)
            {
                UpdateBestStreakText();
            }
        }
        else
        {
            Debug.LogError("MainMenuPanel: BattleGroundStatus is not assigned.", this);
        }

        if (localizeSprite != null)
        {
            // 通常・ホバー・ロック中の画像(言語別)の切り替えと、
            // ロック中にマウスを乗せても画像が変わらない設定は LocalizeSprite が行う
            localizeSprite.SetLocked(!unlocked);
        }
        else if (unlocked)
        {
            SpriteState state = battleGroundButton.spriteState;
            state.highlightedSprite = kachinukiModeButtonHoverSprite;
            state.pressedSprite = kachinukiModeButtonHoverSprite;
            state.selectedSprite = kachinukiModeButtonHoverSprite;
            battleGroundButton.spriteState = state;
            battleGroundButton.transition = Selectable.Transition.SpriteSwap;
        }
        else
        {
            // The locked artwork already contains the unlock condition.  Keep
            // the image unchanged when the pointer moves over the disabled button.
            battleGroundButton.transition = Selectable.Transition.None;
        }
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

        // LocalizeSprite が付いているボタンは、画像を上書きしない(言語別の画像を優先する)
        bool localized = button.GetComponent<LocalizeSprite>() != null;

        if (image != null && (localized || sprite != null))
        {
            if (!localized)
            {
                image.sprite = sprite;
            }
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            button.targetGraphic = image;
        }

        if (!localized && hoverSprite != null)
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

}