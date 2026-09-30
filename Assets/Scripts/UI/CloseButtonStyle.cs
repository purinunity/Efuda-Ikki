using UnityEngine;
using UnityEngine.UI;

public static class CloseButtonStyle
{
    public static void Apply(Button button, Sprite normal = null, Sprite highlighted = null)
    {
        if (button == null) return;
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(124f, 124f);
        rect.localScale = new Vector3(0.5f, 0.5f, 1f);

        Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
        if (image != null)
        {
            if (normal != null) image.sprite = normal;
            image.color = Color.white;
            image.preserveAspect = true;
            button.targetGraphic = image;
        }
        if (highlighted != null)
        {
            SpriteState state = button.spriteState;
            state.highlightedSprite = highlighted;
            state.pressedSprite = highlighted;
            state.selectedSprite = highlighted;
            button.spriteState = state;
            button.transition = Selectable.Transition.SpriteSwap;
        }
    }
}
