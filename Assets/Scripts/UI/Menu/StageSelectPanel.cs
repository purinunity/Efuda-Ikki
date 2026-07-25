using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectPanel : MonoBehaviour
{
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
    [SerializeField] private Color lockedCharacterSilhouetteColor = Color.black;

    private void Start()
    {
        ApplyCharacterFrameLayout();
        HideLegacyTextLabels();
        RefreshProgression();

        // 9つのステージボタンをセットアップ
        for (int i = 0; i < 9; i++)
        {
            int stageNumber = i; // クロージャ用
            if (stageButtons[i] != null)
            {
                stageButtons[i].onClick.AddListener(() => SelectStage(stageNumber));
            }
        }

        // 戻るボタン
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => titleUIManager.BackFromStageSelect());
        }
    }

    private void SelectStage(int stageNumber)
    {
        int level = stageNumber + 1;
        if (!GameProgressStore.IsIkkiLevelUnlocked(level))
        {
            return;
        }

        titleUIManager.SelectStage(stageNumber);
    }

    public void RefreshProgression()
    {
        if (stageButtons == null)
        {
            return;
        }

        int highestUnlockedLevel = GameProgressStore.HighestUnlockedIkkiLevel;
        for (int i = 0; i < stageButtons.Length; i++)
        {
            Button button = stageButtons[i];
            if (button == null)
            {
                continue;
            }

            int level = i + 1;
            bool unlocked = level >= CpuLevelCatalog.MinLevel &&
                            level <= CpuLevelCatalog.MaxLevel &&
                            level <= highestUnlockedLevel;
            button.interactable = unlocked;

            Image image = button.targetGraphic as Image;
            if (image == null)
            {
                image = button.GetComponent<Image>();
            }

            if (image != null)
            {
                image.color = unlocked
                    ? unlockedCharacterColor
                    : lockedCharacterSilhouetteColor;
            }

            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
        }
    }

    private void OnValidate()
    {
        ApplyCharacterFrameLayout();
        HideLegacyTextLabels();
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

            SetTextLabelsActive(button.transform, !hideLegacyTextLabels);
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
