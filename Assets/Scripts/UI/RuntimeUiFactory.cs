using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared primitives for UI that must be constructed when no scene hierarchy is available.
/// Callers remain responsible for applying their feature-specific styling and layout.
/// </summary>
internal static class RuntimeUiFactory
{
    public static void SetTextOutline(TextMeshProUGUI text, Color color, float width)
    {
        if (text == null) return;

        // Inactive hierarchies defer TMP.Awake. Its outline setter accesses
        // the cached renderer directly; the public getter initializes it safely
        // without activating the popup or invoking its lifecycle callbacks.
        if (text.canvasRenderer == null) return;
        if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
        if (text.fontSharedMaterial == null && text.font != null)
        {
            text.fontSharedMaterial = text.font.material;
        }
        if (text.fontSharedMaterial == null) return;

        text.outlineColor = color;
        text.outlineWidth = width;
    }

    public static RectTransform CreateRect(
        string name,
        Transform parent,
        bool inheritParentLayer = false)
    {
        GameObject target = new GameObject(name, typeof(RectTransform));
        target.transform.SetParent(parent, false);
        ApplyParentLayer(target, parent, inheritParentLayer);
        return target.GetComponent<RectTransform>();
    }

    public static Image CreateImage(
        string name,
        Transform parent,
        Sprite sprite,
        bool preserveAspect,
        bool inheritParentLayer = false)
    {
        GameObject target = new GameObject(name, typeof(RectTransform), typeof(Image));
        target.transform.SetParent(parent, false);
        ApplyParentLayer(target, parent, inheritParentLayer);

        Image image = target.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        float? fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment,
        Color color,
        bool inheritParentLayer = false)
    {
        RectTransform rect = CreateRect(name, parent, inheritParentLayer);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
        {
            text.font = font;
        }

        if (fontSize.HasValue)
        {
            text.fontSize = fontSize.Value;
        }
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    public static Button CreateButton(
        string name,
        Transform parent,
        bool inheritParentLayer = false)
    {
        GameObject target = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        target.transform.SetParent(parent, false);
        ApplyParentLayer(target, parent, inheritParentLayer);
        return target.GetComponent<Button>();
    }

    public static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    public static void Stretch(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void SetAnchoredBand(
        RectTransform rect,
        float minX,
        float maxX,
        float minY,
        float maxY)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void SetNormalizedRect(
        RectTransform rect,
        float xMin,
        float yMin,
        float xMax,
        float yMax)
    {
        SetAnchoredBand(rect, xMin, xMax, yMin, yMax);
    }

    public static void SetReferencePixelRect(
        RectTransform rectTransform,
        Vector4 rect,
        float referenceWidth,
        float referenceHeight)
    {
        if (rectTransform == null)
        {
            return;
        }

        float x = rect.x;
        float yFromTop = rect.y;
        float width = Mathf.Max(0f, rect.z);
        float height = Mathf.Max(0f, rect.w);

        rectTransform.anchorMin = new Vector2(
            x / referenceWidth,
            1f - (yFromTop + height) / referenceHeight);
        rectTransform.anchorMax = new Vector2(
            (x + width) / referenceWidth,
            1f - yFromTop / referenceHeight);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private static void ApplyParentLayer(
        GameObject target,
        Transform parent,
        bool inheritParentLayer)
    {
        if (inheritParentLayer && parent != null)
        {
            target.layer = parent.gameObject.layer;
        }
    }
}
