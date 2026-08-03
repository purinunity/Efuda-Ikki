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

    public MatchRoundResult(
        int roundNumber,
        int winnerIndex,
        int damage,
        int playerLifeBefore,
        int playerLifeAfter,
        int cpuLifeBefore,
        int cpuLifeAfter)
    {
        RoundNumber = roundNumber;
        WinnerIndex = winnerIndex;
        Damage = damage;
        PlayerLifeBefore = playerLifeBefore;
        PlayerLifeAfter = playerLifeAfter;
        CpuLifeBefore = cpuLifeBefore;
        CpuLifeAfter = cpuLifeAfter;
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
    private TMP_FontAsset font;
    private bool isWaiting;

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
        string buttonLabel)
    {
        Initialize();
        Populate(cpuLevel, results, playerWon, buttonLabel);
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

    private void Initialize()
    {
        if (panel != null)
        {
            return;
        }

        ResolveFont();
        RectTransform root = GetComponent<RectTransform>();
        Stretch(root);

        Image backdrop = GetOrAdd<Image>(gameObject);
        backdrop.color = BackdropColor;
        backdrop.raycastTarget = true;

        panel = CreateRect("Panel", root);
        panel.anchorMin = new Vector2(0.1f, 0.08f);
        panel.anchorMax = new Vector2(0.9f, 0.92f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;
        GetOrAdd<Image>(panel.gameObject).color = PanelColor;

        RectTransform header = CreateRect("Header", panel);
        SetAnchoredBand(header, 0f, 1f, 0.84f, 1f);
        GetOrAdd<Image>(header.gameObject).color = HeaderColor;

        titleText = CreateText("Title", header, 40, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(titleText.rectTransform);

        summaryText = CreateText("Summary", panel, 24, FontStyles.Normal, TextAlignmentOptions.Center);
        SetAnchoredBand(summaryText.rectTransform, 0.04f, 0.96f, 0.76f, 0.84f);

        CreateColumnHeader();
        CreateScrollArea();
        CreateContinueButton();
        gameObject.SetActive(false);
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
        RectTransform buttonRect = CreateRect("ContinueButton", panel);
        buttonRect.anchorMin = new Vector2(0.68f, 0.04f);
        buttonRect.anchorMax = new Vector2(0.95f, 0.14f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image image = GetOrAdd<Image>(buttonRect.gameObject);
        image.color = AccentColor;
        continueButton = GetOrAdd<Button>(buttonRect.gameObject);
        continueButton.targetGraphic = image;
        continueButton.onClick.AddListener(() => isWaiting = false);

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

    private void Populate(
        int cpuLevel,
        IReadOnlyList<MatchRoundResult> results,
        bool playerWon,
        string buttonLabel)
    {
        titleText.text = playerWon ? "勝利" : "敗北";
        int roundCount = results != null ? results.Count : 0;
        summaryText.text = $"CPUレベル{cpuLevel}　全{roundCount}局";
        continueButtonText.text = buttonLabel;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject oldRow = content.GetChild(i).gameObject;
            oldRow.SetActive(false);
            Destroy(oldRow);
        }

        if (results == null)
        {
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            CreateResultRow(results[i], i);
        }
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
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
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
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
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
