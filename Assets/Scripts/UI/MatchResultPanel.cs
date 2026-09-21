using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchRoundResult
{
    public int RoundNumber { get; }
    public int WinnerIndex { get; }
    public int Damage { get; }
    public int PlayerLifeBefore { get; }
    public int PlayerLifeAfter { get; }
    public int CpuLifeBefore { get; }
    public int CpuLifeAfter { get; }
    public string PlayerRoleName { get; }
    public int PlayerScore { get; }
    public string CpuRoleName { get; }
    public int CpuScore { get; }
    public IReadOnlyList<Sprite> PlayerHandSprites { get; }
    public IReadOnlyList<Sprite> CpuHandSprites { get; }
    public IReadOnlyList<Sprite> CommonCardSprites { get; }
    public Sprite PlayerSpecialCardSprite { get; }
    public Sprite CpuSpecialCardSprite { get; }

    public MatchRoundResult(
        int roundNumber,
        int winnerIndex,
        int damage,
        int playerLifeBefore,
        int playerLifeAfter,
        int cpuLifeBefore,
        int cpuLifeAfter,
        string playerRoleName = "なし",
        int playerScore = 0,
        string cpuRoleName = "なし",
        int cpuScore = 0,
        IReadOnlyList<Sprite> playerHandSprites = null,
        IReadOnlyList<Sprite> cpuHandSprites = null,
        IReadOnlyList<Sprite> commonCardSprites = null,
        Sprite playerSpecialCardSprite = null,
        Sprite cpuSpecialCardSprite = null)
    {
        RoundNumber = roundNumber;
        WinnerIndex = winnerIndex;
        Damage = damage;
        PlayerLifeBefore = playerLifeBefore;
        PlayerLifeAfter = playerLifeAfter;
        CpuLifeBefore = cpuLifeBefore;
        CpuLifeAfter = cpuLifeAfter;
        PlayerRoleName = playerRoleName ?? "なし";
        PlayerScore = Mathf.Max(0, playerScore);
        CpuRoleName = cpuRoleName ?? "なし";
        CpuScore = Mathf.Max(0, cpuScore);
        PlayerHandSprites = playerHandSprites ?? new Sprite[0];
        CpuHandSprites = cpuHandSprites ?? new Sprite[0];
        CommonCardSprites = commonCardSprites ?? new Sprite[0];
        PlayerSpecialCardSprite = playerSpecialCardSprite;
        CpuSpecialCardSprite = cpuSpecialCardSprite;
    }
}

public sealed class MatchResultPanel : MonoBehaviour
{
    private static readonly Color BackdropColor = new Color(0.03f, 0.035f, 0.04f, 0.92f);
    private static readonly Color PanelColor = new Color(0.1f, 0.11f, 0.12f, 1f);
    private static readonly Color HeaderColor = new Color(0.32f, 0.12f, 0.09f, 1f);
    private static readonly Color RowColor = new Color(0.16f, 0.17f, 0.18f, 1f);
    private static readonly Color AlternateRowColor = new Color(0.2f, 0.21f, 0.22f, 1f);
    private static readonly Color AccentColor = new Color(0.88f, 0.72f, 0.32f, 1f);

    private RectTransform panel;
    private RectTransform content;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI summaryText;
    private Button continueButton;
    private TextMeshProUGUI continueButtonText;
    private Button subscribedContinueButton;
    private TMP_FontAsset font;
    private bool isWaiting;
    private Image resultBackground;
    private MatchResultVisualAssets visualAssets;
    private TextMeshProUGUI playerNameText;
    private TextMeshProUGUI playerRoleText;
    private TextMeshProUGUI playerScoreText;
    private TextMeshProUGUI cpuNameText;
    private TextMeshProUGUI cpuRoleText;
    private TextMeshProUGUI cpuScoreText;
    private Image playerCharacterImage;
    private Image cpuCharacterImage;
    private readonly List<Image> playerHandImages = new List<Image>();
    private readonly List<Image> cpuHandImages = new List<Image>();
    private readonly List<Image> playerCommonImages = new List<Image>();
    private readonly List<Image> cpuCommonImages = new List<Image>();
    private Image playerSpecialImage;
    private Image cpuSpecialImage;

    private void OnEnable()
    {
        WireContinueButton();
    }

    private void OnDisable()
    {
        UnwireContinueButton();
        isWaiting = false;
    }

    private void OnDestroy()
    {
        UnwireContinueButton();
    }

    public static MatchResultPanel GetOrCreate(
        MatchResultPanel current,
        UIManager uiManager,
        MonoBehaviour owner)
    {
        if (current != null)
        {
            current.Initialize();
            return current;
        }

        MatchResultPanel existing = Object.FindObjectOfType<MatchResultPanel>(true);
        if (existing != null)
        {
            existing.Initialize();
            return existing;
        }

        Canvas canvas = FindGameCanvas(uiManager, owner);
        if (canvas == null)
        {
            Debug.LogWarning("A Canvas was not found for the match result panel.");
            return null;
        }

        GameObject root = new GameObject(
            "MatchResultPanel",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(MatchResultPanel));
        root.transform.SetParent(canvas.transform, false);
        MatchResultPanel result = root.GetComponent<MatchResultPanel>();
        result.Initialize();
        return result;
    }

    public IEnumerator Show(
        int cpuLevel,
        IReadOnlyList<MatchRoundResult> results,
        bool playerWon,
        string buttonLabel,
        string summaryOverride = null)
    {
        Initialize();
        Populate(cpuLevel, results, playerWon, buttonLabel, summaryOverride);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        isWaiting = true;

        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }

        while (isWaiting)
        {
            yield return null;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Releases a pending result wait when the owning game session is cancelled.
    /// Safe to call when the panel has not been initialized or is already hidden.
    /// </summary>
    public void CancelDisplay()
    {
        isWaiting = false;
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void Initialize()
    {
        if (panel != null)
        {
            return;
        }

        ResolveFont();
        RectTransform root = GetComponent<RectTransform>();
        Stretch(root);

        resultBackground = GetOrAdd<Image>(gameObject);
        resultBackground.color = Color.white;
        resultBackground.raycastTarget = true;
        resultBackground.preserveAspect = false;
        visualAssets = Resources.Load<MatchResultVisualAssets>("MatchResultVisualAssets");

        panel = CreateRect("Panel", root);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        GetOrAdd<Image>(panel.gameObject).color = Color.clear;

        CreateArtworkFields();
        CreateContinueButton();
        gameObject.SetActive(false);
    }

    private void CreateArtworkFields()
    {
        summaryText = CreateText("Summary", panel, 21, FontStyles.Bold, TextAlignmentOptions.Center);
        SetAnchoredBand(summaryText.rectTransform, 0.2f, 0.8f, 0.60f, 0.66f);
        summaryText.color = Color.black;
        summaryText.enableAutoSizing = true;
        summaryText.fontSizeMin = 14f;
        summaryText.fontSizeMax = 21f;

        // Keep portraits inside the character frames with matching inset on
        // both rows. PreserveAspect then centers each portrait in this area.
        playerCharacterImage = CreateArtworkImage("PlayerCharacter", 0.115f, 0.228f, 0.369f, 0.601f);
        cpuCharacterImage = CreateArtworkImage("CpuCharacter", 0.115f, 0.228f, 0.092f, 0.296f);
        CreateCardRow("Player", 0.403f, 0.571f, playerHandImages, playerCommonImages, out playerSpecialImage);
        CreateCardRow("Cpu", 0.097f, 0.264f, cpuHandImages, cpuCommonImages, out cpuSpecialImage);
    }

    private void CreateCardRow(
        string prefix,
        float minY,
        float maxY,
        List<Image> handImages,
        List<Image> commonImages,
        out Image specialImage)
    {
        const float handStart = 0.286f;
        const float handWidth = 0.061f;
        const float handGap = 0.003f;
        for (int i = 0; i < 5; i++)
        {
            float minX = handStart + i * (handWidth + handGap);
            handImages.Add(CreateArtworkImage($"{prefix}Hand{i + 1}", minX, minX + handWidth, minY, maxY));
        }

        // The right white frame spans x=0.641..0.891. Divide it into three
        // equal card slots with identical outer margins and gaps.
        const float rightFrameMin = 0.641f;
        const float rightPadding = 0.014f;
        const float rightGap = 0.014f;
        const float rightCardWidth = 0.06467f;
        float rightMinY = minY + 0.007f;
        float rightMaxY = maxY - 0.007f;
        for (int i = 0; i < 2; i++)
        {
            float minX = rightFrameMin + rightPadding + i * (rightCardWidth + rightGap);
            commonImages.Add(CreateArtworkImage(
                $"{prefix}Common{i + 1}",
                minX,
                minX + rightCardWidth,
                rightMinY,
                rightMaxY));
        }

        float specialMinX = rightFrameMin + rightPadding + 2f * (rightCardWidth + rightGap);
        specialImage = CreateArtworkImage(
            $"{prefix}Special",
            specialMinX,
            specialMinX + rightCardWidth,
            rightMinY,
            rightMaxY);
    }

    private Image CreateArtworkImage(string name, float minX, float maxX, float minY, float maxY)
    {
        RectTransform rect = CreateRect(name, panel);
        SetAnchoredBand(rect, minX, maxX, minY, maxY);
        Image image = GetOrAdd<Image>(rect.gameObject);
        image.color = Color.clear;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateArtworkText(
        string name, float minX, float maxX, float minY, float maxY, int maxSize)
    {
        TextMeshProUGUI text = CreateText(name, panel, maxSize, FontStyles.Bold, TextAlignmentOptions.Center);
        SetAnchoredBand(text.rectTransform, minX, maxX, minY, maxY);
        text.color = Color.black;
        text.enableWordWrapping = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = 15f;
        text.fontSizeMax = maxSize;
        text.margin = new Vector4(8f, 4f, 8f, 4f);
        return text;
    }

    private void CreateColumnHeader()
    {
        RectTransform header = CreateRect("ColumnHeader", panel);
        SetAnchoredBand(header, 0.05f, 0.95f, 0.68f, 0.76f);
        GetOrAdd<Image>(header.gameObject).color = new Color(0.09f, 0.085f, 0.075f, 1f);
        AddHorizontalLayout(header.gameObject, 12, 16);

        CreateCell("局", header, 90, 0);
        CreateCell("結果", header, 90, 0);
        CreateCell("ダメージ", header, 150, 0);
        CreateCell("体力の推移", header, 0, 1);
    }

    private void CreateScrollArea()
    {
        RectTransform scrollRoot = CreateRect("RoundResults", panel);
        SetAnchoredBand(scrollRoot, 0.05f, 0.95f, 0.18f, 0.68f);
        Image scrollBackground = GetOrAdd<Image>(scrollRoot.gameObject);
        scrollBackground.color = new Color(0.07f, 0.065f, 0.06f, 1f);

        ScrollRect scrollRect = GetOrAdd<ScrollRect>(scrollRoot.gameObject);
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 32f;

        RectTransform viewport = CreateRect("Viewport", scrollRoot);
        Stretch(viewport);
        viewport.offsetMax = new Vector2(-20f, 0f);
        GetOrAdd<RectMask2D>(viewport.gameObject);

        content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        VerticalLayoutGroup verticalLayout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        verticalLayout.spacing = 2f;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        CreateScrollbar(scrollRoot, scrollRect);
    }

    private void CreateScrollbar(RectTransform parent, ScrollRect scrollRect)
    {
        RectTransform bar = CreateRect("Scrollbar", parent);
        bar.anchorMin = new Vector2(1f, 0f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(1f, 0.5f);
        bar.sizeDelta = new Vector2(14f, 0f);
        bar.anchoredPosition = Vector2.zero;
        GetOrAdd<Image>(bar.gameObject).color = new Color(0.15f, 0.14f, 0.12f, 1f);

        Scrollbar scrollbar = GetOrAdd<Scrollbar>(bar.gameObject);
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        RectTransform slidingArea = CreateRect("SlidingArea", bar);
        Stretch(slidingArea);
        slidingArea.offsetMin = new Vector2(2f, 2f);
        slidingArea.offsetMax = new Vector2(-2f, -2f);

        RectTransform handle = CreateRect("Handle", slidingArea);
        Stretch(handle);
        GetOrAdd<Image>(handle.gameObject).color = AccentColor;
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalScrollbarSpacing = 6f;
    }

    private void CreateContinueButton()
    {
        continueButton = RuntimeUiFactory.CreateButton("ContinueButton", panel);
        RectTransform buttonRect = continueButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.76f, 0.01f);
        buttonRect.anchorMax = new Vector2(0.97f, 0.085f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image image = continueButton.GetComponent<Image>();
        image.color = AccentColor;
        continueButton.targetGraphic = image;
        WireContinueButton();

        continueButtonText = CreateText(
            "Label",
            buttonRect,
            25,
            FontStyles.Bold,
            TextAlignmentOptions.Center);
        continueButtonText.color = Color.black;
        continueButtonText.enableAutoSizing = true;
        continueButtonText.fontSizeMin = 18f;
        continueButtonText.fontSizeMax = 25f;
        continueButtonText.margin = new Vector4(8f, 0f, 8f, 0f);
        Stretch(continueButtonText.rectTransform);
    }

    private void WireContinueButton()
    {
        UnwireContinueButton();
        if (continueButton == null)
        {
            return;
        }

        subscribedContinueButton = continueButton;
        continueButton.onClick.AddListener(HandleContinueClicked);
    }

    private void UnwireContinueButton()
    {
        if (subscribedContinueButton != null)
        {
            subscribedContinueButton.onClick.RemoveListener(HandleContinueClicked);
        }

        subscribedContinueButton = null;
    }

    private void HandleContinueClicked()
    {
        isWaiting = false;
    }

    private void Populate(
        int cpuLevel,
        IReadOnlyList<MatchRoundResult> results,
        bool playerWon,
        string buttonLabel,
        string summaryOverride)
    {
        if (resultBackground != null)
        {
            resultBackground.sprite = visualAssets != null
                ? (playerWon ? visualAssets.winBackground : visualAssets.loseBackground)
                : null;
            resultBackground.color = resultBackground.sprite != null ? Color.white : BackdropColor;
        }
        int roundCount = results != null ? results.Count : 0;
        GameModeData modeData = GameModeManager.GetGameModeData();
        bool battleGround = modeData != null && modeData.Mode == GameModeData.GameMode.BattleGroundMode;
        summaryText.text = !string.IsNullOrEmpty(summaryOverride)
            ? summaryOverride
            : battleGround
                ? $"今回 {modeData.CurrentWinStreak}人抜き　最高 {GameProgressStore.BestBattleGroundStreak}人抜き"
                : $"CPUレベル {cpuLevel}　全{roundCount}局";
        continueButtonText.text = buttonLabel;
        MatchRoundResult playerBest = FindBestResult(results, true);
        MatchRoundResult cpuBest = FindBestResult(results, false);
        CharacterManager characters = Object.FindObjectOfType<CharacterManager>(true);
        SetArtworkSprite(playerCharacterImage, characters != null ? characters.GetPlayerSprite() : null);
        SetArtworkSprite(cpuCharacterImage, characters != null ? characters.GetCpuSprite() : null);
        PopulateCardRow(playerBest, true, playerHandImages, playerCommonImages, playerSpecialImage);
        PopulateCardRow(cpuBest, false, cpuHandImages, cpuCommonImages, cpuSpecialImage);
    }

    private static void PopulateCardRow(
        MatchRoundResult result,
        bool player,
        List<Image> handImages,
        List<Image> commonImages,
        Image specialImage)
    {
        SetArtworkSprites(handImages, result != null
            ? (player ? result.PlayerHandSprites : result.CpuHandSprites)
            : null);
        SetArtworkSprites(commonImages, result != null ? result.CommonCardSprites : null);
        SetArtworkSprite(specialImage, result != null
            ? (player ? result.PlayerSpecialCardSprite : result.CpuSpecialCardSprite)
            : null);
    }

    private static void SetArtworkSprites(IReadOnlyList<Image> images, IReadOnlyList<Sprite> sprites)
    {
        for (int i = 0; i < images.Count; i++)
        {
            SetArtworkSprite(images[i], sprites != null && i < sprites.Count ? sprites[i] : null);
        }
    }

    private static void SetArtworkSprite(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
    }

    private static MatchRoundResult FindBestResult(IReadOnlyList<MatchRoundResult> results, bool player)
    {
        MatchRoundResult best = null;
        if (results == null) return null;
        foreach (MatchRoundResult result in results)
        {
            if (result == null) continue;
            int score = player ? result.PlayerScore : result.CpuScore;
            int bestScore = best == null ? -1 : player ? best.PlayerScore : best.CpuScore;
            if (score > bestScore) best = result;
        }
        return best;
    }

    private void CreateResultRow(MatchRoundResult result, int index)
    {
        RectTransform row = CreateRect($"Round{result.RoundNumber}", content);
        Image background = GetOrAdd<Image>(row.gameObject);
        background.color = index % 2 == 0 ? RowColor : AlternateRowColor;

        LayoutElement rowLayout = GetOrAdd<LayoutElement>(row.gameObject);
        rowLayout.minHeight = 60f;
        rowLayout.preferredHeight = 60f;
        AddHorizontalLayout(row.gameObject, 12, 16);

        CreateCell($"第{result.RoundNumber}局", row, 90, 0);
        CreateCell(GetResultLabel(result.WinnerIndex), row, 90, 0);
        CreateCell(result.WinnerIndex < 0 ? "なし" : $"{result.Damage}点", row, 150, 0);
        CreateCell(BuildLifeTransition(result), row, 0, 1);
    }

    private TextMeshProUGUI CreateCell(
        string value,
        RectTransform parent,
        float preferredWidth,
        float flexibleWidth)
    {
        TextMeshProUGUI text = CreateText(
            "Cell",
            parent,
            22,
            FontStyles.Normal,
            TextAlignmentOptions.Center);
        text.text = value;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement layout = GetOrAdd<LayoutElement>(text.gameObject);
        layout.preferredWidth = preferredWidth;
        layout.flexibleWidth = flexibleWidth;
        return text;
    }

    private static string GetResultLabel(int winnerIndex)
    {
        if (winnerIndex == 0)
        {
            return "勝";
        }

        return winnerIndex == 1 ? "負" : "分";
    }

    private static string BuildLifeTransition(MatchRoundResult result)
    {
        if (result.WinnerIndex == 0)
        {
            return $"CPU　{result.CpuLifeBefore} → {result.CpuLifeAfter}";
        }

        if (result.WinnerIndex == 1)
        {
            return $"プレイヤー　{result.PlayerLifeBefore} → {result.PlayerLifeAfter}";
        }

        return "変化なし";
    }

    private void ResolveFont()
    {
        TextMeshProUGUI existingText = Object.FindObjectOfType<TextMeshProUGUI>(true);
        font = existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;
    }

    private TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        int fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = RuntimeUiFactory.CreateText(
            name,
            parent,
            font,
            fontSize,
            fontStyle,
            alignment,
            Color.white);
        text.font = font;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        return RuntimeUiFactory.CreateRect(name, parent);
    }

    private static void AddHorizontalLayout(GameObject target, int spacing, int padding)
    {
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(target);
        layout.spacing = spacing;
        layout.padding = new RectOffset(padding, padding, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
    }

    private static void SetAnchoredBand(
        RectTransform rect,
        float minX,
        float maxX,
        float minY,
        float maxY)
    {
        RuntimeUiFactory.SetAnchoredBand(rect, minX, maxX, minY, maxY);
    }

    private static void Stretch(RectTransform rect)
    {
        RuntimeUiFactory.Stretch(rect);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        return RuntimeUiFactory.GetOrAdd<T>(target);
    }

    private static Canvas FindGameCanvas(UIManager uiManager, MonoBehaviour owner)
    {
        Canvas canvas =
            FindCanvas(uiManager != null ? uiManager.deck : null) ??
            FindCanvas(uiManager != null ? uiManager.common : null) ??
            FindCanvas(uiManager != null ? uiManager.player1 : null) ??
            (uiManager != null ? uiManager.GetComponentInParent<Canvas>() : null);

        return canvas != null
            ? canvas
            : owner != null ? owner.GetComponentInParent<Canvas>() : null;
    }

    private static Canvas FindCanvas(Component component)
    {
        return component != null ? component.GetComponentInParent<Canvas>() : null;
    }
}
