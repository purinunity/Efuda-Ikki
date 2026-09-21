using UnityEngine;
using EfudaIkki.Core;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;

public class StageSelectPanel : MonoBehaviour
{
    [Serializable]
    private sealed class CharacterBinding
    {
        [SerializeField] private string characterId;
        [SerializeField] private Button button;
        [SerializeField] private Sprite normal;
        [SerializeField] private Sprite normalHover;
        [SerializeField] private Sprite locked;
        [SerializeField] private Sprite cleared;
        [SerializeField] private Sprite clearedHover;

        public string CharacterId => characterId;
        public Button Button => button;
        public Sprite Normal => normal;
        public Sprite NormalHover => normalHover;
        public Sprite Locked => locked;
        public Sprite Cleared => cleared;
        public Sprite ClearedHover => clearedHover;

        public CharacterBinding(
            string characterId,
            Button button,
            Sprite normal,
            Sprite normalHover,
            Sprite locked,
            Sprite cleared,
            Sprite clearedHover)
        {
            this.characterId = characterId;
            this.button = button;
            this.normal = normal;
            this.normalHover = normalHover;
            this.locked = locked;
            this.cleared = cleared;
            this.clearedHover = clearedHover;
        }
    }

    private static readonly Vector2[] CharacterFramePositions =
    {
        new Vector2(-540f, -360f),
        new Vector2(-180f, -360f),
        new Vector2(180f, -360f),
        new Vector2(540f, -360f),
        new Vector2(-540f, 0f),
        new Vector2(-180f, 0f),
        new Vector2(180f, 0f),
        new Vector2(540f, 0f),
        new Vector2(0f, 360f)
    };

    public Button[] stageButtons = new Button[9];
    public TextMeshProUGUI stageInfoText;
    public Button backButton;
    public TitleUIManager titleUIManager;

    [Header("Character Select Layout")]
    [SerializeField] private bool fitButtonsToCharacterFrames = true;
    [SerializeField] private Vector2 characterButtonSize = new Vector2(280f, 280f);
    [SerializeField] private bool preserveCharacterSpriteAspect = true;
    [SerializeField] private bool hideLegacyTextLabels = true;
    [SerializeField] private Color unlockedCharacterColor = Color.white;
    [SerializeField] private Sprite[] lockedCharacterSprites = new Sprite[9];
    [SerializeField] private Sprite[] hoverCharacterSprites = new Sprite[9];
    [SerializeField] private Sprite[] clearedCharacterSprites = new Sprite[9];
    [SerializeField] private Sprite[] clearedHoverCharacterSprites = new Sprite[9];
    [Tooltip("Preferred ID-based bindings. Existing scenes continue to use the legacy arrays as a fallback.")]
    [SerializeField] private CharacterBinding[] characterBindings = Array.Empty<CharacterBinding>();
    [SerializeField] private Vector2 backButtonTopLeftOffset = new Vector2(48f, -40f);
    [SerializeField] private Vector2 backButtonSize = new Vector2(200f, 80f);

    private Sprite[] unlockedCharacterSprites;
    private CharacterBinding[] activeCharacterBindings;
    private ShowdownCutInAssetSet sharedAssetSet;
    private readonly Dictionary<Button, UnityAction> characterClickHandlers =
        new Dictionary<Button, UnityAction>();
    private UnityAction backClickHandler;
    private Button subscribedBackButton;
    private bool started;

    private void Awake()
    {
        sharedAssetSet = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        CacheUnlockedCharacterSprites();
        BuildActiveCharacterBindings();
    }

    private void Start()
    {
        ApplyCharacterFrameLayout();
        ApplyBackButtonLayout();
        HideLegacyTextLabels();
        RefreshProgression();

        started = true;
        RegisterButtonListeners();
    }

    private void OnEnable()
    {
        if (started)
        {
            RegisterButtonListeners();
        }
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
    }

    private void OnDestroy()
    {
        UnregisterButtonListeners();
    }

    private void SelectCharacter(string characterId)
    {
        if (!StageCharacterCatalog.TryGet(characterId, out StageCharacterCatalog.Entry character))
        {
            Debug.LogWarning($"Unknown stage character ID: {characterId}");
            return;
        }

        int level = character.Level;
        if (!GameProgressStore.IsIkkiLevelUnlocked(level))
        {
            return;
        }

        if (titleUIManager == null)
        {
            Debug.LogWarning("Stage character cannot be selected because TitleUIManager is not assigned.");
            return;
        }

        titleUIManager.SelectStage(level - 1);
    }

    private void RegisterButtonListeners()
    {
        UnregisterButtonListeners();
        if (activeCharacterBindings != null)
        {
            foreach (CharacterBinding binding in activeCharacterBindings)
            {
                if (binding?.Button == null ||
                    !StageCharacterCatalog.TryGet(binding.CharacterId, out _))
                {
                    continue;
                }

                if (characterClickHandlers.ContainsKey(binding.Button))
                {
                    Debug.LogWarning(
                        $"Stage button is bound to more than one character; ignoring duplicate {binding.CharacterId}.",
                        this);
                    continue;
                }

                string characterId = binding.CharacterId;
                UnityAction action = () => SelectCharacter(characterId);
                characterClickHandlers.Add(binding.Button, action);
                binding.Button.onClick.AddListener(action);
            }
        }

        if (backButton != null)
        {
            backClickHandler = HandleBackClicked;
            subscribedBackButton = backButton;
            backButton.onClick.AddListener(backClickHandler);
        }
    }

    private void UnregisterButtonListeners()
    {
        foreach (KeyValuePair<Button, UnityAction> pair in characterClickHandlers)
        {
            if (pair.Key != null)
            {
                pair.Key.onClick.RemoveListener(pair.Value);
            }
        }

        characterClickHandlers.Clear();
        if (subscribedBackButton != null && backClickHandler != null)
        {
            subscribedBackButton.onClick.RemoveListener(backClickHandler);
        }

        backClickHandler = null;
        subscribedBackButton = null;
    }

    private void HandleBackClicked()
    {
        if (titleUIManager != null)
        {
            titleUIManager.BackFromStageSelect();
        }
    }

    public void RefreshProgression()
    {
        if (activeCharacterBindings == null)
        {
            CacheUnlockedCharacterSprites();
            BuildActiveCharacterBindings();
        }

        int highestUnlockedLevel = GameProgressStore.HighestUnlockedIkkiLevel;
        foreach (CharacterBinding binding in activeCharacterBindings)
        {
            if (binding?.Button == null ||
                !StageCharacterCatalog.TryGet(binding.CharacterId, out StageCharacterCatalog.Entry character))
            {
                continue;
            }

            Button button = binding.Button;
            int level = character.Level;
            bool unlocked = level >= CpuLevelCatalog.MinLevel &&
                            level <= CpuLevelCatalog.MaxLevel &&
                            level <= highestUnlockedLevel;
            bool cleared = unlocked && GameProgressStore.IsIkkiLevelCleared(level);
            button.interactable = unlocked;

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null)
            {
                Sprite lockedSprite = binding.Locked;
                Sprite unlockedSprite = binding.Normal != null ? binding.Normal : image.sprite;
                Sprite clearedSprite = GetClearedSprite(binding, false);

                image.sprite = cleared && clearedSprite != null
                    ? clearedSprite
                    : unlocked || lockedSprite == null
                        ? unlockedSprite
                        : lockedSprite;
                image.color = unlocked || lockedSprite != null
                    ? unlockedCharacterColor
                    : Color.black;
            }

            ConfigureHoverSprite(binding, cleared);

            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
        }
    }

    private void OnValidate()
    {
        CacheUnlockedCharacterSprites();
        BuildActiveCharacterBindings();
        ApplyCharacterFrameLayout();
        ApplyBackButtonLayout();
        HideLegacyTextLabels();
    }

    private void ApplyBackButtonLayout()
    {
        if (backButton == null)
        {
            return;
        }

        RectTransform rectTransform = backButton.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        if (rectTransform.parent != transform)
        {
            rectTransform.SetParent(transform, false);
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = backButtonTopLeftOffset;
        rectTransform.sizeDelta = backButtonSize;
        rectTransform.SetAsLastSibling();
    }

    private void ApplyCharacterFrameLayout()
    {
        if (!fitButtonsToCharacterFrames || stageButtons == null)
        {
            return;
        }

        DisableLayoutGroup();

        int buttonCount = Mathf.Min(stageButtons.Length, CharacterFramePositions.Length);
        for (int i = 0; i < buttonCount; i++)
        {
            Button button = stageButtons[i];
            if (button == null)
            {
                continue;
            }

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = CharacterFramePositions[i];
                rectTransform.sizeDelta = characterButtonSize;
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null)
            {
                image.type = Image.Type.Simple;
                image.preserveAspect = preserveCharacterSpriteAspect;
                image.raycastTarget = true;
                button.targetGraphic = image;
            }

            if (activeCharacterBindings != null && i < activeCharacterBindings.Length)
            {
                ConfigureHoverSprite(activeCharacterBindings[i], false);
            }
            else
            {
                ConfigureHoverSprite(button, i, false);
            }
            SetTextLabelsActive(button.transform, !hideLegacyTextLabels);
        }
    }

    private void ConfigureHoverSprite(Button button, int index, bool cleared)
    {
        if (button == null)
        {
            return;
        }

        Sprite hoverSprite = cleared
            ? GetClearedSprite(index, true)
            : null;
        if (hoverSprite == null)
        {
            hoverSprite = GetSprite(hoverCharacterSprites, index);
        }

        if (hoverSprite == null)
        {
            return;
        }

        SpriteState spriteState = button.spriteState;
        spriteState.highlightedSprite = hoverSprite;
        spriteState.pressedSprite = hoverSprite;
        button.spriteState = spriteState;
        button.transition = Selectable.Transition.SpriteSwap;
    }

    private void ConfigureHoverSprite(CharacterBinding binding, bool cleared)
    {
        if (binding?.Button == null)
        {
            return;
        }

        Sprite hoverSprite = cleared ? GetClearedSprite(binding, true) : null;
        if (hoverSprite == null)
        {
            hoverSprite = binding.NormalHover;
        }

        if (hoverSprite == null)
        {
            return;
        }

        SpriteState spriteState = binding.Button.spriteState;
        spriteState.highlightedSprite = hoverSprite;
        spriteState.pressedSprite = hoverSprite;
        binding.Button.spriteState = spriteState;
        binding.Button.transition = Selectable.Transition.SpriteSwap;
    }

    private static Sprite GetSprite(Sprite[] sprites, int index)
    {
        return sprites != null && index >= 0 && index < sprites.Length
            ? sprites[index]
            : null;
    }

    private Sprite GetClearedSprite(int index, bool mouseOver)
    {
        Sprite[] inspectorSprites = mouseOver
            ? clearedHoverCharacterSprites
            : clearedCharacterSprites;
        Sprite sprite = GetSprite(inspectorSprites, index);
        if (sprite != null)
        {
            return sprite;
        }

        if (sharedAssetSet == null)
        {
            sharedAssetSet = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        }

        Sprite[] sharedSprites = sharedAssetSet == null
            ? null
            : mouseOver
                ? sharedAssetSet.clearedHoverCharacterSprites
                : sharedAssetSet.clearedCharacterSprites;
        return GetSprite(sharedSprites, index);
    }

    private Sprite GetClearedSprite(CharacterBinding binding, bool mouseOver)
    {
        Sprite sprite = mouseOver ? binding.ClearedHover : binding.Cleared;
        if (sprite != null)
        {
            return sprite;
        }

        if (!StageCharacterCatalog.TryGet(binding.CharacterId, out StageCharacterCatalog.Entry character))
        {
            return null;
        }

        return GetClearedSprite(character.Level - 1, mouseOver);
    }

    private void BuildActiveCharacterBindings()
    {
        activeCharacterBindings = new CharacterBinding[StageCharacterCatalog.Entries.Count];
        foreach (StageCharacterCatalog.Entry character in StageCharacterCatalog.Entries)
        {
            int index = character.Level - 1;
            CharacterBinding configured = FindConfiguredBinding(character.Id);
            Button button = configured?.Button ?? GetButton(index);

            activeCharacterBindings[index] = new CharacterBinding(
                character.Id,
                button,
                configured?.Normal ?? GetSprite(unlockedCharacterSprites, index),
                configured?.NormalHover ?? GetSprite(hoverCharacterSprites, index),
                configured?.Locked ?? GetSprite(lockedCharacterSprites, index),
                configured?.Cleared ?? GetSprite(clearedCharacterSprites, index),
                configured?.ClearedHover ?? GetSprite(clearedHoverCharacterSprites, index));
        }
    }

    private CharacterBinding FindConfiguredBinding(string characterId)
    {
        if (characterBindings == null)
        {
            return null;
        }

        foreach (CharacterBinding binding in characterBindings)
        {
            if (binding != null && string.Equals(binding.CharacterId, characterId, StringComparison.Ordinal))
            {
                return binding;
            }
        }

        return null;
    }

    private Button GetButton(int index)
    {
        return stageButtons != null && index >= 0 && index < stageButtons.Length
            ? stageButtons[index]
            : null;
    }

    private void CacheUnlockedCharacterSprites()
    {
        if (stageButtons == null)
        {
            unlockedCharacterSprites = new Sprite[0];
            return;
        }

        unlockedCharacterSprites = new Sprite[stageButtons.Length];
        for (int i = 0; i < stageButtons.Length; i++)
        {
            Button button = stageButtons[i];
            if (button == null)
            {
                continue;
            }

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            unlockedCharacterSprites[i] = image != null ? image.sprite : null;
        }
    }

    private void HideLegacyTextLabels()
    {
        if (!hideLegacyTextLabels)
        {
            return;
        }

        if (stageInfoText != null)
        {
            stageInfoText.gameObject.SetActive(false);
        }

        if (stageButtons == null)
        {
            return;
        }

        foreach (Button button in stageButtons)
        {
            if (button != null)
            {
                SetTextLabelsActive(button.transform, false);
            }
        }
    }

    private void DisableLayoutGroup()
    {
        if (stageButtons == null || stageButtons.Length == 0 || stageButtons[0] == null)
        {
            return;
        }

        Transform layoutRoot = stageButtons[0].transform.parent;
        if (layoutRoot == null)
        {
            return;
        }

        LayoutGroup layoutGroup = layoutRoot.GetComponent<LayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        RectTransform layoutRect = layoutRoot as RectTransform;
        if (layoutRect != null)
        {
            layoutRect.anchorMin = Vector2.zero;
            layoutRect.anchorMax = Vector2.one;
            layoutRect.pivot = new Vector2(0.5f, 0.5f);
            layoutRect.anchoredPosition = Vector2.zero;
            layoutRect.sizeDelta = Vector2.zero;
        }
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
