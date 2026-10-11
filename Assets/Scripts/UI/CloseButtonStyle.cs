using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CloseButtonStyle
{
    public static void Apply(Button button, Sprite normal = null, Sprite highlighted = null)
    {
        if (button == null) return;
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

        // Close controls are image buttons.  Legacy scene labels such as "X"
        // must not become visible when the popup is enabled.
        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            label.gameObject.SetActive(false);
        }
    }
}
