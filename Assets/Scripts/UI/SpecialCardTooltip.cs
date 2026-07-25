using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SpecialCardTooltip : MonoBehaviour
{
    private static SpecialCardTooltip instance;

    [SerializeField] private Vector2 pointerOffset = new Vector2(24f, -24f);
    [SerializeField] private Vector2 padding = new Vector2(18f, 14f);
    [SerializeField] private float spacing = 8f;
    [SerializeField] private float maxTextWidth = 420f;
    [SerializeField] private float screenMargin = 12f;
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.92f);
    [SerializeField] private Color titleColor = new Color(1f, 0.88f, 0.45f, 1f);
    [SerializeField] private Color bodyColor = Color.white;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private object currentOwner;

    public static void Show(object owner, string title, string body, Vector2 screenPosition)
    {
        SpecialCardTooltip tooltip = EnsureInstance();
        if (tooltip == null)
        {
            return;
        }

        tooltip.ShowInternal(owner, title, body, screenPosition);
    }

    public static void SetPosition(Vector2 screenPosition)
    {
        if (instance == null || instance.canvasGroup == null || instance.canvasGroup.alpha <= 0f)
        {
            return;
        }

        instance.SetPositionInternal(screenPosition);
    }

    public static void Hide(object owner)
    {
        if (instance == null)
        {
            return;
        }

        if (owner != null && instance.currentOwner != null && !ReferenceEquals(owner, instance.currentOwner))
        {
            return;
        }

        instance.currentOwner = null;
        if (instance.canvasGroup != null)
        {
            instance.canvasGroup.alpha = 0f;
            instance.canvasGroup.blocksRaycasts = false;
            instance.canvasGroup.interactable = false;
        }
    }

    private static SpecialCardTooltip EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindObjectOfType<SpecialCardTooltip>();
        if (instance != null)
        {
            instance.Initialize();
            return instance;
        }

        Canvas canvas = FindRootCanvas();
        if (canvas == null)
        {
            return null;
        }

        GameObject tooltipObject = new GameObject("SpecialCardTooltip", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        tooltipObject.transform.SetParent(canvas.transform, false);

        instance = tooltipObject.AddComponent<SpecialCardTooltip>();
        instance.rootCanvas = canvas;
        instance.Initialize();
        return instance;
    }

    private static Canvas FindRootCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas fallback = null;

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = canvas;
            }

            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                return canvas;
            }
        }

        return fallback;
    }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        Image background = GetComponent<Image>();
        if (background != null)
        {
            background.color = backgroundColor;
            background.raycastTarget = false;
        }

        if (titleText == null)
        {
            titleText = CreateText("Title", 30f, FontStyles.Bold, titleColor);
        }

        if (bodyText == null)
        {
            bodyText = CreateText("Body", 24f, FontStyles.Normal, bodyColor);
        }
    }

    private TextMeshProUGUI CreateText(string objectName, float fontSize, FontStyles style, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(transform, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.TopLeft;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);

        return text;
    }

    private void ShowInternal(object owner, string title, string body, Vector2 screenPosition)
    {
        Initialize();
        currentOwner = owner;

        titleText.text = title;
        bodyText.text = body;
        UpdateLayoutSize();
        transform.SetAsLastSibling();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        SetPositionInternal(screenPosition);
    }

    private void UpdateLayoutSize()
    {
        float textWidth = maxTextWidth;
        Vector2 titleSize = titleText.GetPreferredValues(titleText.text, textWidth, 0f);
        Vector2 bodySize = bodyText.GetPreferredValues(bodyText.text, textWidth, 0f);
        float contentWidth = Mathf.Min(textWidth, Mathf.Max(titleSize.x, bodySize.x));
        float width = contentWidth + padding.x * 2f;
        float titleHeight = titleText.GetPreferredValues(titleText.text, contentWidth, 0f).y;
        float bodyHeight = bodyText.GetPreferredValues(bodyText.text, contentWidth, 0f).y;
        float height = padding.y * 2f + titleHeight + spacing + bodyHeight;

        rectTransform.sizeDelta = new Vector2(width, height);

        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(padding.x, -padding.y);
        titleRect.sizeDelta = new Vector2(contentWidth, titleHeight);

        RectTransform bodyRect = bodyText.GetComponent<RectTransform>();
        bodyRect.anchoredPosition = new Vector2(padding.x, -padding.y - titleHeight - spacing);
        bodyRect.sizeDelta = new Vector2(contentWidth, bodyHeight);
    }

    private void SetPositionInternal(Vector2 screenPosition)
    {
        if (rootCanvas == null || rectTransform == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            return;
        }

        Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out localPoint);

        Vector2 topLeftLocal = new Vector2(
            -canvasRect.rect.width * canvasRect.pivot.x,
            canvasRect.rect.height * (1f - canvasRect.pivot.y));

        Vector2 anchoredPosition = localPoint - topLeftLocal + pointerOffset;
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 tooltipSize = rectTransform.sizeDelta;

        anchoredPosition.x = Mathf.Clamp(
            anchoredPosition.x,
            screenMargin,
            Mathf.Max(screenMargin, canvasSize.x - tooltipSize.x - screenMargin));
        anchoredPosition.y = Mathf.Clamp(
            anchoredPosition.y,
            Mathf.Min(-screenMargin, -canvasSize.y + tooltipSize.y + screenMargin),
            -screenMargin);

        rectTransform.anchoredPosition = anchoredPosition;
    }
}
