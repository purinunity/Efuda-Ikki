using UnityEngine;
using UnityEngine.UI;

public sealed class SpecialCardTooltip : MonoBehaviour
{
    private static SpecialCardTooltip instance;

    [SerializeField] private Vector2 pointerOffset = new Vector2(24f, -24f);
    [SerializeField] private Vector2 tooltipSize = new Vector2(512f, 328f);
    [SerializeField] private float screenMargin = 12f;

    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Image tooltipImage;
    private object currentOwner;

    public static void Show(object owner, Sprite sprite, Vector2 screenPosition)
    {
        if (sprite == null)
        {
            return;
        }

        SpecialCardTooltip tooltip = EnsureInstance(FindOwnerCanvas(owner));
        if (tooltip != null)
        {
            tooltip.ShowInternal(owner, sprite, screenPosition);
        }
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

        if (owner != null &&
            instance.currentOwner != null &&
            !ReferenceEquals(owner, instance.currentOwner))
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

    private static SpecialCardTooltip EnsureInstance(Canvas preferredCanvas)
    {
        if (instance != null)
        {
            instance.AttachToCanvas(preferredCanvas);
            return instance;
        }

        instance = FindObjectOfType<SpecialCardTooltip>();
        if (instance != null)
        {
            instance.Initialize();
            instance.AttachToCanvas(preferredCanvas);
            return instance;
        }

        Canvas canvas = preferredCanvas != null ? preferredCanvas : FindRootCanvas();
        if (canvas == null)
        {
            return null;
        }

        GameObject tooltipObject = new GameObject(
            "SpecialCardTooltip",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image));
        tooltipObject.transform.SetParent(canvas.transform, false);

        instance = tooltipObject.AddComponent<SpecialCardTooltip>();
        instance.rootCanvas = canvas;
        instance.Initialize();
        return instance;
    }

    private static Canvas FindOwnerCanvas(object owner)
    {
        Component ownerComponent = owner as Component;
        if (ownerComponent == null)
        {
            return null;
        }

        Canvas ownerCanvas = ownerComponent.GetComponentInParent<Canvas>();
        return ownerCanvas != null && ownerCanvas.rootCanvas != null
            ? ownerCanvas.rootCanvas
            : ownerCanvas;
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

    private void AttachToCanvas(Canvas canvas)
    {
        if (canvas == null || rootCanvas == canvas)
        {
            return;
        }

        rootCanvas = canvas;
        transform.SetParent(rootCanvas.transform, false);
        gameObject.layer = rootCanvas.gameObject.layer;
    }

    private void Initialize()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.sizeDelta = tooltipSize;
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

        if (tooltipImage == null)
        {
            tooltipImage = GetComponent<Image>();
            tooltipImage.color = Color.white;
            tooltipImage.preserveAspect = true;
            tooltipImage.raycastTarget = false;
        }
    }

    private void ShowInternal(object owner, Sprite sprite, Vector2 screenPosition)
    {
        Initialize();
        currentOwner = owner;
        tooltipImage.sprite = sprite;
        rectTransform.sizeDelta = tooltipSize;
        transform.SetAsLastSibling();

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        SetPositionInternal(screenPosition);
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

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            camera,
            out Vector2 localPoint);

        Vector2 topLeftLocal = new Vector2(
            -canvasRect.rect.width * canvasRect.pivot.x,
            canvasRect.rect.height * (1f - canvasRect.pivot.y));
        Vector2 anchoredPosition = localPoint - topLeftLocal + pointerOffset;
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 imageSize = rectTransform.sizeDelta;

        anchoredPosition.x = Mathf.Clamp(
            anchoredPosition.x,
            screenMargin,
            Mathf.Max(screenMargin, canvasSize.x - imageSize.x - screenMargin));
        anchoredPosition.y = Mathf.Clamp(
            anchoredPosition.y,
            Mathf.Min(-screenMargin, -canvasSize.y + imageSize.y + screenMargin),
            -screenMargin);

        rectTransform.anchoredPosition = anchoredPosition;
    }
}
