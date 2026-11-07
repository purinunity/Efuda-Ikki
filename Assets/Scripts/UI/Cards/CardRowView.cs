using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Updates an authored row of card images, without creating or laying out UI.</summary>
internal static class CardRowView
{
    public static void Render(Image[] images, IReadOnlyList<Sprite> sprites, IReadOnlyList<bool> highlights,
        Color dimColor, Action<Image, Sprite> setImage)
    {
        if (images == null) return;
        bool hasHighlights = false;
        if (highlights != null)
            for (int i = 0; i < highlights.Count; i++)
                if (highlights[i]) { hasHighlights = true; break; }
        for (int i = 0; i < images.Length; i++)
        {
            Sprite sprite = sprites != null && i < sprites.Count ? sprites[i] : null;
            setImage(images[i], sprite);
            if (images[i] != null && sprite != null && hasHighlights)
                images[i].color = highlights != null && i < highlights.Count && highlights[i] ? Color.white : dimColor;
        }
    }
}
