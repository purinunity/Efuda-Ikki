using TMPro;
using UnityEngine;
/// <summary>Safe shared styling helpers for scene-authored UI.</summary>
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
}
