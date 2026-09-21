using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ShowdownCutInPopup
{
    /// <summary>
    /// Builds only the hierarchy references that are absent from the serialized scene view.
    /// The normal path continues to use the scene-owned hierarchy unchanged.
    /// </summary>
    private void BuildUi()
    {
        if (stage == null)
        {
            stage = CreateRect("Stage", transform);
        }
        Stretch(stage);

        if (screenFillImage == null)
        {
            screenFillImage = CreateImage("ScreenFill", stage, null, false);
            Stretch(screenFillImage.rectTransform);
        }
        screenFillImage.color = Color.black;
        screenFillImage.raycastTarget = true;

        if (backgroundImage == null)
        {
            backgroundImage = CreateImage("Background", stage, assetSet != null ? assetSet.cutInBackground : null, false);
            Stretch(backgroundImage.rectTransform);
        }

        if (cpuCharacterBaseImage == null)
        {
            cpuCharacterBaseImage = CreateImage("CpuCharacterBase", stage, assetSet != null ? assetSet.characterBase : null, false);
            SetReferencePixelRect(cpuCharacterBaseImage.rectTransform, CpuCharacterBaseRect);
        }

        if (playerCharacterBaseImage == null)
        {
            playerCharacterBaseImage = CreateImage("PlayerCharacterBase", stage, assetSet != null ? assetSet.characterBase : null, false);
            SetReferencePixelRect(playerCharacterBaseImage.rectTransform, PlayerCharacterBaseRect);
        }

        if (cpuCharacterImage == null)
        {
            cpuCharacterImage = CreateImage("CpuCharacter", stage, null, true);
            SetReferencePixelRect(cpuCharacterImage.rectTransform, CpuCharacterRect);
        }

        if (playerCharacterImage == null)
        {
            playerCharacterImage = CreateImage("PlayerCharacter", stage, null, true);
            SetReferencePixelRect(playerCharacterImage.rectTransform, PlayerCharacterRect);
        }

        if (cpuRoleImage == null)
        {
            cpuRoleImage = CreateImage("CpuRole", stage, null, true);
            SetReferencePixelRect(cpuRoleImage.rectTransform, GetCpuRoleSpriteRect());
        }

        if (playerRoleImage == null)
        {
            playerRoleImage = CreateImage("PlayerRole", stage, null, true);
            SetReferencePixelRect(playerRoleImage.rectTransform, GetPlayerRoleSpriteRect());
        }

        DisableLegacyRoleFallbackTexts();

        if (cpuScoreText == null)
        {
            cpuScoreText = CreateText("CpuScore", stage, 48, Color.black);
            PlaceScoreAtRoleSwordTip(cpuScoreText, cpuRoleImage, false);
        }

        if (playerScoreText == null)
        {
            playerScoreText = CreateText("PlayerScore", stage, 48, Color.black);
            PlaceScoreAtRoleSwordTip(playerScoreText, playerRoleImage, true);
        }

        if (cpuLifeDeductionText == null)
        {
            cpuLifeDeductionText = CreateText("CpuLifeDeduction", stage, 44, LifeDeductionColor);
            SetReferencePixelRect(cpuLifeDeductionText.rectTransform, CpuLifeDeductionRect);
        }

        if (playerLifeDeductionText == null)
        {
            playerLifeDeductionText = CreateText("PlayerLifeDeduction", stage, 44, LifeDeductionColor);
            SetReferencePixelRect(playerLifeDeductionText.rectTransform, PlayerLifeDeductionRect);
        }

        BuildCardImageSlots(ref cpuCardImages, "CpuShowdownCard", CpuHandFrameRect, CpuCommonFrameRect);
        BuildCardImageSlots(ref playerCardImages, "PlayerShowdownCard", PlayerHandFrameRect, PlayerCommonFrameRect);

        if (cpuSpecialCardImage == null)
        {
            cpuSpecialCardImage = CreateImage("CpuSpecialCard", stage, null, true);
            SetReferencePixelRect(cpuSpecialCardImage.rectTransform, CpuSpecialCardSlotRect);
        }

        if (playerSpecialCardImage == null)
        {
            playerSpecialCardImage = CreateImage("PlayerSpecialCard", stage, null, true);
            SetReferencePixelRect(playerSpecialCardImage.rectTransform, PlayerSpecialCardSlotRect);
        }

        if (specialCallBackdropImage == null)
        {
            specialCallBackdropImage = CreateImage("SpecialCallBackdrop", stage, null, false);
            SetReferencePixelRect(specialCallBackdropImage.rectTransform, SpecialCallBackdropRect);
        }

        if (specialActivationImage == null)
        {
            specialActivationImage = CreateImage(
                "SpecialActivation",
                stage,
                assetSet != null ? assetSet.specialActivation : null,
                true);
            SetReferencePixelRect(specialActivationImage.rectTransform, SpecialActivationRect);
        }

        if (resultBackdropImage == null)
        {
            resultBackdropImage = CreateImage("ResultBackdrop", stage, null, false);
            SetReferencePixelRect(resultBackdropImage.rectTransform, ResultBackdropRect);
        }

        if (resultStampImage == null)
        {
            resultStampImage = CreateImage("PlayerResultStamp", stage, null, true);
            SetReferencePixelRect(resultStampImage.rectTransform, PlayerResultStampRect);
        }

        if (cpuResultStampImage == null)
        {
            cpuResultStampImage = CreateImage("CpuResultStamp", stage, null, true);
            SetReferencePixelRect(cpuResultStampImage.rectTransform, CpuResultStampRect);
        }

        if (specialCallText == null)
        {
            specialCallText = CreateText("SpecialCall", stage, 64, Color.white);
            specialCallText.fontStyle = FontStyles.Bold;
            RuntimeUiFactory.SetTextOutline(specialCallText, Color.black, 0.25f);
            SetReferencePixelRect(specialCallText.rectTransform, SpecialCallTextRect);
        }

        if (resultText == null)
        {
            resultText = CreateText("Result", stage, 56, Color.black);
            resultText.fontStyle = FontStyles.Bold;
            SetReferencePixelRect(resultText.rectTransform, ResultTextRect);
        }

        if (damageText == null)
        {
            damageText = CreateText("Damage", stage, 42, Color.black);
            SetReferencePixelRect(damageText.rectTransform, DamageTextRect);
        }

        if (closeButton == null)
        {
            closeButton = CreateButton("CloseButton", stage, "X");
            SetReferencePixelRect(closeButton.GetComponent<RectTransform>(), CloseButtonRect);
        }

        closeButton.gameObject.SetActive(false);
    }

    private void BuildCardImageSlots(
        ref Image[] images,
        string namePrefix,
        Vector4 handFrameRect,
        Vector4 commonFrameRect)
    {
        EnsureImageArraySize(ref images, ShowdownCardCount);
        for (int i = 0; i < ShowdownCardCount; i++)
        {
            if (images[i] == null)
            {
                images[i] = CreateImage($"{namePrefix}{i + 1}", stage, null, true);
                SetReferencePixelRect(
                    images[i].rectTransform,
                    GetShowdownCardSlotRect(handFrameRect, commonFrameRect, i));
            }
        }
    }

    private static void EnsureImageArraySize(ref Image[] images, int requiredCount)
    {
        if (images != null && images.Length == requiredCount)
        {
            return;
        }

        Image[] resized = new Image[requiredCount];
        if (images != null)
        {
            int copyCount = Mathf.Min(images.Length, requiredCount);
            for (int i = 0; i < copyCount; i++)
            {
                resized[i] = images[i];
            }
        }

        images = resized;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        return RuntimeUiFactory.CreateRect(name, parent, true);
    }

    private static Image CreateImage(
        string name,
        Transform parent,
        Sprite sprite,
        bool preserveAspect)
    {
        return RuntimeUiFactory.CreateImage(name, parent, sprite, preserveAspect, true);
    }

    private TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        float maxFontSize,
        Color color)
    {
        TMP_FontAsset font = assetSet != null ? assetSet.textFont : null;
        TextMeshProUGUI text = RuntimeUiFactory.CreateText(
            name,
            parent,
            font,
            null,
            FontStyles.Normal,
            TextAlignmentOptions.Center,
            color,
            true);
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = maxFontSize;
        text.text = string.Empty;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label)
    {
        Button button = RuntimeUiFactory.CreateButton(name, parent, true);
        Image image = button.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        image.raycastTarget = true;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        colors.highlightedColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        colors.pressedColor = new Color(0.02f, 0.02f, 0.02f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", button.transform, 32, Color.white);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform);
        return button;
    }

    private static void ApplyLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            ApplyLayerRecursively(child.gameObject, layer);
        }
    }
}
