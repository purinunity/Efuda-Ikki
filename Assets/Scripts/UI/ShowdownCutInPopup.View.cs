using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HandEvaluator;

public partial class ShowdownCutInPopup
{
    private void ConfigureUiReferences()
    {
        ConfigureRequestedFrames();

        if (screenFillImage != null)
        {
            screenFillImage.color = Color.black;
            screenFillImage.raycastTarget = true;
        }

        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = false;
            if (backgroundImage.sprite == null && assetSet != null)
            {
                backgroundImage.sprite = assetSet.cutInBackground;
            }
        }

        ConfigureImage(playerCharacterBaseImage, preserveAspect: false);
        ConfigureImage(cpuCharacterBaseImage, preserveAspect: false);
        ConfigureImage(playerCharacterImage, preserveAspect: true);
        ConfigureImage(cpuCharacterImage, preserveAspect: true);
        ConfigureImage(playerRoleImage, preserveAspect: true);
        ConfigureImage(cpuRoleImage, preserveAspect: true);
        ConfigureImageArray(playerCardImages, preserveAspect: true);
        ConfigureImageArray(cpuCardImages, preserveAspect: true);
        ConfigureImage(playerSpecialCardImage, preserveAspect: true);
        ConfigureImage(cpuSpecialCardImage, preserveAspect: true);
        ConfigureImage(specialActivationImage, preserveAspect: true);
        ConfigureImage(resultStampImage, preserveAspect: true);
        ConfigureImage(cpuResultStampImage, preserveAspect: true);
        ConfigureSpecialCallPanel();
        ConfigureTextBackdrop(resultBackdropImage);
        DisableLegacyRoleFallbackTexts();

        ApplyTextDefaults(playerScoreText);
        ApplyTextDefaults(cpuScoreText);
        ApplyTextDefaults(playerLifeDeductionText);
        ApplyTextDefaults(cpuLifeDeductionText);
        ApplyTextDefaults(specialCallText);
        ApplyTextDefaults(resultText);
        ApplyTextDefaults(damageText);
        ConfigureScoreText(cpuScoreText);
        ConfigureScoreText(playerScoreText);
        ConfigureLifeDeductionText(cpuLifeDeductionText);
        ConfigureLifeDeductionText(playerLifeDeductionText);
        ConfigureSpecialCallText();
        ApplyReadableOverlayText(resultText, Color.white);
        ApplyReadableOverlayText(damageText, new Color(1f, 0.92f, 0.72f, 1f));
    }

    private void ConfigureRequestedFrames()
    {
        if (assetSet == null) return;
        if (specialCallBackdropImage != null && assetSet.roleFrame != null)
        {
            specialCallBackdropImage.sprite = assetSet.roleFrame;
            specialCallBackdropImage.color = Color.white;
            specialCallBackdropImage.preserveAspect = true;
        }
        ConfigureLifeDeductionFrame(playerLifeDeductionFrameImage, assetSet.lifeDeductionFrame);
        ConfigureLifeDeductionFrame(cpuLifeDeductionFrameImage, assetSet.lifeDeductionFrame);
    }

    private static void ConfigureLifeDeductionFrame(Image image, Sprite sprite)
    {
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void ConfigureSpecialCallPanel()
    {
        if (specialCallBackdropImage == null)
        {
            return;
        }

        if (assetSet != null && assetSet.roleFrame != null)
        {
            // 汎用panelは色や縦横比を加工せず、素材本来の表示を使用する。
            specialCallBackdropImage.sprite = assetSet.roleFrame;
            specialCallBackdropImage.color = Color.white;
            specialCallBackdropImage.preserveAspect = true;
            specialCallBackdropImage.type = Image.Type.Simple;
            specialCallBackdropImage.raycastTarget = false;
            return;
        }

        ConfigureTextBackdrop(specialCallBackdropImage);
    }

    private void ConfigureSpecialCallText()
    {
        if (specialCallText == null)
        {
            return;
        }

        if (assetSet != null && assetSet.roleFrame != null)
        {
            specialCallText.color = Color.black;
            // The supplied 4096px SDF atlas is clearest without synthetic bold.
            // Give the glyphs enough room so auto-size does not downsample them.
            specialCallText.fontStyle = FontStyles.Normal;
            specialCallText.alignment = TextAlignmentOptions.Center;
            specialCallText.enableAutoSizing = true;
            specialCallText.fontSizeMin = 30f;
            specialCallText.fontSizeMax = 60f;
            specialCallText.extraPadding = true;
            specialCallText.characterSpacing = 1f;
            RuntimeUiFactory.SetTextOutline(specialCallText, Color.black, 0f);
            return;
        }

        ApplyReadableOverlayText(specialCallText, new Color(1f, 0.9f, 0.35f, 1f));
    }

    private void ConfigureImage(Image image, bool preserveAspect)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
    }

    private void ConfigureImageArray(Image[] images, bool preserveAspect)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            ConfigureImage(image, preserveAspect);
        }
    }

    private void ConfigureTextBackdrop(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.color = new Color(0f, 0f, 0f, 0.68f);
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

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

    private void ApplyTextDefaults(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        if (assetSet != null && assetSet.textFont != null)
        {
            text.font = assetSet.textFont;
        }

        text.raycastTarget = false;
    }

    private void ApplyReadableOverlayText(TextMeshProUGUI text, Color color)
    {
        if (text == null)
        {
            return;
        }

        text.color = color;
        text.fontStyle = FontStyles.Bold;
        RuntimeUiFactory.SetTextOutline(text, Color.black, 0.28f);
    }

    private static void ConfigureScoreText(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 30f;
        text.fontSizeMax = 68f;
        text.color = Color.black;
        text.fontStyle = FontStyles.Bold;
        RuntimeUiFactory.SetTextOutline(text, Color.black, 0f);
    }

    private static void ConfigureLifeDeductionText(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 15f;
        text.fontSizeMax = 26f;
        text.color = LifeDeductionColor;
        text.fontStyle = FontStyles.Bold;
        RuntimeUiFactory.SetTextOutline(text, Color.black, 0.24f);
    }

    private void SetLifeDeductionActive(TextMeshProUGUI text, bool active)
    {
        SetTextActive(text, active);
        Image frame = text == playerLifeDeductionText
            ? playerLifeDeductionFrameImage
            : text == cpuLifeDeductionText ? cpuLifeDeductionFrameImage : null;
        SetActive(frame, active);
    }

    private void DisableLegacyRoleFallbackTexts()
    {
        if (stage == null)
        {
            return;
        }

        DisableStageChild("PlayerRoleText");
        DisableStageChild("CpuRoleText");
    }

    private void DisableStageChild(string childName)
    {
        Transform child = stage.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
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
        if (images == null)
        {
            return;
        }

        bool hasHighlights = HasAnyHighlight(highlights);
        for (int i = 0; i < images.Length; i++)
        {
            Sprite sprite = sprites != null && i < sprites.Count ? sprites[i] : null;
            SetImage(images[i], sprite);
            if (images[i] != null && sprite != null && hasHighlights)
            {
                bool highlighted = highlights != null && i < highlights.Count && highlights[i];
                images[i].color = highlighted ? Color.white : roleCardDimColor;
            }
        }
    }

    private static bool HasAnyHighlight(IReadOnlyList<bool> highlights)
    {
        if (highlights == null)
        {
            return false;
        }

        for (int i = 0; i < highlights.Count; i++)
        {
            if (highlights[i])
            {
                return true;
            }
        }

        return false;
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
