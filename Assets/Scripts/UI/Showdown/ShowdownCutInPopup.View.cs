using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HandEvaluator;

public partial class ShowdownCutInPopup
{

    private static bool HasImageSlots(Image[] images, int requiredCount)
    {
        if (images == null || images.Length < requiredCount)
        {
            return false;
        }

        for (int i = 0; i < requiredCount; i++)
        {
            if (images[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void SetLifeDeductionActive(TextMeshProUGUI text, bool active)
    {
        SetTextActive(text, active);
    }

    private void SetActive(Image image, bool isActive)
    {
        if (image != null)
        {
            image.gameObject.SetActive(isActive);
        }
    }

    private static void SetTextActive(TextMeshProUGUI text, bool isActive)
    {
        if (text != null)
        {
            text.gameObject.SetActive(isActive);
        }
    }

    private void SetBackground(Sprite sprite, Color fillColor)
    {
        if (screenFillImage != null)
        {
            screenFillImage.color = fillColor;
        }

        backgroundImage.sprite = sprite;
        backgroundImage.color = sprite != null ? Color.white : new Color(0f, 0f, 0f, 0.85f);
    }

    private void SetCharacters(Data data)
    {
        SetImage(playerCharacterImage, data.PlayerCharacterSprite);
        SetImage(cpuCharacterImage, data.CpuCharacterSprite);
        SetImage(playerCharacterBaseImage, assetSet != null ? assetSet.characterBase : null);
        SetImage(cpuCharacterBaseImage, assetSet != null ? assetSet.characterBase : null);
    }

    private void SetRole(Image image, TextMeshProUGUI fallbackText, bool isPlayer, string roleName, HandRank roleRank)
    {
        Sprite roleSprite = assetSet != null ? assetSet.GetRoleSprite(isPlayer, roleRank) : null;
        SetImage(image, roleSprite);
        if (fallbackText != null)
        {
            fallbackText.text = roleName;
            fallbackText.gameObject.SetActive(roleSprite == null);
        }
    }

    private void SetCardImages(Image[] images, IReadOnlyList<Sprite> sprites, IReadOnlyList<bool> highlights)
    {
        CardRowView.Render(images, sprites, highlights, roleCardDimColor, SetImage);
    }

    private void HighlightSpecialCard(int ownerPlayerId)
    {
        ApplySpecialCardHighlight(playerSpecialCardImage, ownerPlayerId == 0);
        ApplySpecialCardHighlight(cpuSpecialCardImage, ownerPlayerId == 1);
    }

    private void ResetSpecialCardHighlights()
    {
        ApplySpecialCardHighlight(playerSpecialCardImage, false, dimInactive: false);
        ApplySpecialCardHighlight(cpuSpecialCardImage, false, dimInactive: false);
    }

    private void ApplySpecialCardHighlight(Image image, bool isActive, bool dimInactive = true)
    {
        if (image == null || !image.gameObject.activeSelf)
        {
            return;
        }

        image.color = isActive ? activeSpecialCardTint : dimInactive ? inactiveSpecialCardTint : Color.white;
        Vector3 authoredScale = image == playerSpecialCardImage
            ? playerSpecialCardInitialScale
            : image == cpuSpecialCardImage ? cpuSpecialCardInitialScale : image.rectTransform.localScale;
        image.rectTransform.localScale = isActive ? authoredScale * 1.08f : authoredScale;
    }

    private void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
        image.gameObject.SetActive(sprite != null);
    }

    private static RectTransform GetActiveRect(Image image)
    {
        if (image == null || !image.gameObject.activeSelf)
        {
            return null;
        }

        return image.rectTransform;
    }

    private static void SetAnchoredPosition(RectTransform rectTransform, Vector2 position)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }
    }

    private static void SetLocalScale(RectTransform rectTransform, Vector3 scale)
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = scale;
        }
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

    private void SetCloseButtonLabel(string label)
    {
        if (closeButton == null)
        {
            return;
        }

        TextMeshProUGUI labelText = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            labelText.text = label;
            labelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
        }
    }

    private void CacheCloseButtonDefaultVisual()
    {
        if (closeButtonDefaultVisualCached || closeButton == null)
        {
            return;
        }

        Image image = closeButton.targetGraphic as Image;
        if (image == null)
        {
            image = closeButton.GetComponent<Image>();
        }

        if (image != null)
        {
            closeButtonDefaultSprite = image.sprite;
            closeButtonDefaultImageType = image.type;
            closeButtonDefaultColor = image.color;
        }

        closeButtonDefaultTransition = closeButton.transition;
        closeButtonDefaultColors = closeButton.colors;
        closeButtonDefaultVisualCached = true;
    }

    private void ConfigureCloseButtonVisual(bool showNextButton)
    {
        if (closeButton == null)
        {
            return;
        }

        CacheCloseButtonDefaultVisual();
        Image image = closeButton.targetGraphic as Image;
        if (image == null)
        {
            image = closeButton.GetComponent<Image>();
        }

        TextMeshProUGUI labelText =
            closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (showNextButton)
        {
            closeButton.transition = closeButtonDefaultTransition;
            closeButton.colors = closeButtonDefaultColors;
            if (image != null)
            {
                image.sprite = closeButtonDefaultSprite;
                image.type = closeButtonDefaultImageType;
                image.preserveAspect = false;
                image.color = closeButtonDefaultColor;
                image.CrossFadeColor(
                    closeButtonDefaultColors.normalColor,
                    0f,
                    true,
                    true);
            }

            if (labelText != null)
            {
                labelText.gameObject.SetActive(true);
            }

            SetCloseButtonLabel("次へ");
            return;
        }

        if (image != null && assetSet != null && assetSet.closeButton != null)
        {
            image.sprite = assetSet.closeButton;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        Sprite highlightedSprite = assetSet != null
            ? assetSet.closeButtonHighlighted
            : null;
        if (highlightedSprite != null)
        {
            SpriteState spriteState = closeButton.spriteState;
            spriteState.highlightedSprite = highlightedSprite;
            spriteState.pressedSprite = highlightedSprite;
            spriteState.selectedSprite = highlightedSprite;
            closeButton.spriteState = spriteState;
        }

        ColorBlock closeColors = closeButton.colors;
        closeColors.normalColor = Color.white;
        closeColors.highlightedColor = Color.white;
        closeColors.selectedColor = Color.white;
        closeColors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        closeColors.disabledColor = Color.white;
        closeColors.colorMultiplier = 1f;
        closeButton.transition = highlightedSprite != null
            ? Selectable.Transition.SpriteSwap
            : Selectable.Transition.ColorTint;
        closeButton.colors = closeColors;
        if (image != null)
        {
            image.CrossFadeColor(Color.white, 0f, true, true);
        }

        if (labelText != null)
        {
            labelText.gameObject.SetActive(false);
        }

        CloseButtonStyle.Apply(
            closeButton,
            assetSet != null ? assetSet.closeButton : null,
            highlightedSprite);
        SetCloseButtonLabel(string.Empty);
    }

    private static string BuildWinnerText(Data data)
    {
        if (data.IsDraw)
        {
            return "引き分け";
        }

        return data.WinnerIndex == 0 ? "プレイヤー勝利" : "CPU勝利";
    }
}
