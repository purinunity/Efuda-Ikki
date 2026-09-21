using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HandEvaluator;

public partial class ShowdownCutInPopup
{
    private void ConfigureUiReferences()
    {
        ApplyRuntimeLayout();

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
        ConfigureTextBackdrop(specialCallBackdropImage);
        ConfigureTextBackdrop(resultBackdropImage);
        PlaceBackdropBehindText(specialCallBackdropImage, specialCallText);
        PlaceBackdropBehindText(resultBackdropImage, resultText);
        PlaceImageBehindText(resultStampImage, resultText);
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
        ApplyReadableOverlayText(specialCallText, new Color(1f, 0.9f, 0.35f, 1f));
        ApplyReadableOverlayText(resultText, Color.white);
        ApplyReadableOverlayText(damageText, new Color(1f, 0.92f, 0.72f, 1f));
        ApplyStageSiblingOrder();
    }

    private void ApplyRuntimeLayout()
    {
        if (stage == null)
        {
            return;
        }

        Stretch(stage);
        EnsureDirectStageChild(screenFillImage);
        EnsureDirectStageChild(backgroundImage);
        EnsureDirectStageChild(cpuCharacterBaseImage);
        EnsureDirectStageChild(playerCharacterBaseImage);
        EnsureDirectStageChild(cpuCharacterImage);
        EnsureDirectStageChild(playerCharacterImage);
        EnsureDirectStageChild(cpuLifeDeductionText);
        EnsureDirectStageChild(playerLifeDeductionText);
        EnsureDirectStageChild(cpuRoleImage);
        EnsureDirectStageChild(playerRoleImage);
        EnsureDirectStageChild(cpuScoreText);
        EnsureDirectStageChild(playerScoreText);
        EnsureDirectStageChild(cpuCardImages);
        EnsureDirectStageChild(playerCardImages);
        EnsureDirectStageChild(cpuSpecialCardImage);
        EnsureDirectStageChild(playerSpecialCardImage);
        EnsureDirectStageChild(specialActivationImage);
        EnsureDirectStageChild(specialCallBackdropImage);
        EnsureDirectStageChild(resultBackdropImage);
        EnsureDirectStageChild(specialCallText);
        EnsureDirectStageChild(resultText);
        EnsureDirectStageChild(damageText);
        EnsureDirectStageChild(resultStampImage);
        EnsureDirectStageChild(cpuResultStampImage);
        EnsureDirectStageChild(closeButton);

        if (screenFillImage != null)
        {
            Stretch(screenFillImage.rectTransform);
        }

        if (backgroundImage != null)
        {
            Stretch(backgroundImage.rectTransform);
        }

        SetReferencePixelRect(cpuCharacterBaseImage, CpuCharacterBaseRect);
        SetReferencePixelRect(playerCharacterBaseImage, PlayerCharacterBaseRect);
        SetReferencePixelRect(cpuCharacterImage, CpuCharacterRect);
        SetReferencePixelRect(playerCharacterImage, PlayerCharacterRect);
        SetReferencePixelRect(cpuLifeDeductionText, CpuLifeDeductionRect);
        SetReferencePixelRect(playerLifeDeductionText, PlayerLifeDeductionRect);

        if (cpuRoleImage != null)
        {
            SetReferencePixelRect(cpuRoleImage.rectTransform, GetCpuRoleSpriteRect());
        }

        if (playerRoleImage != null)
        {
            SetReferencePixelRect(playerRoleImage.rectTransform, GetPlayerRoleSpriteRect());
        }

        ApplyShowdownCardLayout(cpuCardImages, CpuHandFrameRect, CpuCommonFrameRect);
        ApplyShowdownCardLayout(playerCardImages, PlayerHandFrameRect, PlayerCommonFrameRect);

        if (cpuSpecialCardImage != null)
        {
            SetReferencePixelRect(cpuSpecialCardImage.rectTransform, CpuSpecialCardSlotRect);
        }

        if (playerSpecialCardImage != null)
        {
            SetReferencePixelRect(playerSpecialCardImage.rectTransform, PlayerSpecialCardSlotRect);
        }

        PlaceScoreAtRoleSwordTip(cpuScoreText, cpuRoleImage, false);
        PlaceScoreAtRoleSwordTip(playerScoreText, playerRoleImage, true);

        SetReferencePixelRect(specialCallBackdropImage, SpecialCallBackdropRect);
        SetReferencePixelRect(specialActivationImage, SpecialActivationRect);
        SetReferencePixelRect(resultBackdropImage, ResultBackdropRect);
        if (resultStampImage != null)
        {
            SetReferencePixelRect(resultStampImage.rectTransform, PlayerResultStampRect);
        }

        if (cpuResultStampImage != null)
        {
            SetReferencePixelRect(cpuResultStampImage.rectTransform, CpuResultStampRect);
        }

        SetReferencePixelRect(specialCallText, SpecialCallTextRect);
        SetReferencePixelRect(resultText, ResultTextRect);
        SetReferencePixelRect(damageText, DamageTextRect);

        if (closeButton != null)
        {
            SetReferencePixelRect(closeButton.GetComponent<RectTransform>(), CloseButtonRect);
        }
    }

    private void ApplyShowdownCardLayout(Image[] images, Vector4 handFrameRect, Vector4 commonFrameRect)
    {
        if (images == null)
        {
            return;
        }

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
            {
                continue;
            }

            SetReferencePixelRect(images[i].rectTransform, GetShowdownCardSlotRect(handFrameRect, commonFrameRect, i));
        }
    }

    private static Vector4 GetShowdownCardSlotRect(Vector4 handFrameRect, Vector4 commonFrameRect, int index)
    {
        if (index < HandCardCount)
        {
            const float handCardWidth = 72f;
            const float handCardHeight = 96f;
            const float handCardGap = 16f;
            float handTotalWidth =
                HandCardCount * handCardWidth +
                (HandCardCount - 1) * handCardGap;
            float handStartX =
                handFrameRect.x + (handFrameRect.z - handTotalWidth) * 0.5f;
            float x = handStartX + index * (handCardWidth + handCardGap);
            float y = handFrameRect.y + (handFrameRect.w - handCardHeight) * 0.5f;
            return new Vector4(x, y, handCardWidth, handCardHeight);
        }

        const float commonCardWidth = 72f;
        const float commonCardHeight = 96f;
        const float commonCardGap = 16f;
        int commonIndex = index - HandCardCount;
        float totalWidth = CommonCardCount * commonCardWidth + (CommonCardCount - 1) * commonCardGap;
        float startX = commonFrameRect.x + (commonFrameRect.z - totalWidth) * 0.5f;
        return new Vector4(
            startX + commonIndex * (commonCardWidth + commonCardGap),
            commonFrameRect.y + (commonFrameRect.w - commonCardHeight) * 0.5f,
            commonCardWidth,
            commonCardHeight);
    }

    private void EnsureDirectStageChild(Component component)
    {
        if (component == null || stage == null || component.transform.parent == stage)
        {
            return;
        }

        component.transform.SetParent(stage, false);
    }

    private void EnsureDirectStageChild(Image[] images)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            EnsureDirectStageChild(image);
        }
    }

    private void PlaceScoreAtRoleSwordTip(TextMeshProUGUI scoreText, Image roleImage, bool isPlayer)
    {
        if (scoreText == null || roleImage == null)
        {
            return;
        }

        RectTransform scoreRect = scoreText.rectTransform;
        RectTransform roleRect = roleImage.rectTransform;
        if (scoreRect == null || roleRect == null)
        {
            return;
        }

        if (scoreRect.parent != roleRect)
        {
            scoreRect.SetParent(roleRect, false);
        }

        Vector2 anchor = isPlayer ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        scoreRect.anchorMin = anchor;
        scoreRect.anchorMax = anchor;
        scoreRect.pivot = isPlayer ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        scoreRect.sizeDelta = ScoreTextSize;
        scoreRect.anchoredPosition = isPlayer ? new Vector2(-44f, 0f) : new Vector2(44f, 0f);
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

    private static void PlaceBackdropBehindText(Image backdrop, TextMeshProUGUI text)
    {
        if (backdrop == null || text == null || backdrop.transform.parent != text.transform.parent)
        {
            return;
        }

        backdrop.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
    }

    private static void PlaceImageBehindText(Image image, TextMeshProUGUI text)
    {
        if (image == null || text == null || image.transform.parent != text.transform.parent)
        {
            return;
        }

        image.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
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
        text.fontSizeMin = 30f;
        text.fontSizeMax = 52f;
        text.color = LifeDeductionColor;
        text.fontStyle = FontStyles.Bold;
        RuntimeUiFactory.SetTextOutline(text, Color.black, 0.24f);
    }

    private void ApplyStageSiblingOrder()
    {
        SetAsLastSibling(screenFillImage);
        SetAsLastSibling(backgroundImage);
        SetAsLastSibling(cpuCharacterBaseImage);
        SetAsLastSibling(playerCharacterBaseImage);
        SetAsLastSibling(cpuCharacterImage);
        SetAsLastSibling(playerCharacterImage);
        SetAsLastSibling(cpuRoleImage);
        SetAsLastSibling(playerRoleImage);
        SetAsLastSibling(cpuCardImages);
        SetAsLastSibling(playerCardImages);
        SetAsLastSibling(cpuSpecialCardImage);
        SetAsLastSibling(playerSpecialCardImage);
        SetAsLastSibling(cpuScoreText);
        SetAsLastSibling(playerScoreText);
        SetAsLastSibling(cpuLifeDeductionText);
        SetAsLastSibling(playerLifeDeductionText);
        SetAsLastSibling(specialActivationImage);
        SetAsLastSibling(specialCallBackdropImage);
        SetAsLastSibling(resultBackdropImage);
        SetAsLastSibling(cpuResultStampImage);
        SetAsLastSibling(resultStampImage);
        SetAsLastSibling(specialCallText);
        SetAsLastSibling(resultText);
        SetAsLastSibling(damageText);
        SetAsLastSibling(closeButton);
    }

    private static void SetAsLastSibling(Component component)
    {
        if (component != null)
        {
            component.transform.SetAsLastSibling();
        }
    }

    private static void SetAsLastSibling(Image[] images)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            SetAsLastSibling(image);
        }
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
        image.rectTransform.localScale = isActive ? Vector3.one * 1.08f : Vector3.one;
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
            SetNormalizedRect(
                closeButton.GetComponent<RectTransform>(),
                0.82f,
                0.82f,
                0.95f,
                0.92f);
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

        SetReferencePixelRect(closeButton.GetComponent<RectTransform>(), CloseButtonRect);
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
