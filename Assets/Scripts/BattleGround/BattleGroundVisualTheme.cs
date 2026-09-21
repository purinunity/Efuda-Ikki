using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class BattleGroundVisualTheme
{
    private static BattleGroundVisualAssets assets;
    private static Image backgroundImage;
    private static Image roundFrameImage;
    private static Sprite originalBackground;
    private static Sprite originalRoundFrame;
    private static bool originalRoundPreserveAspect;
    private static bool originalRoundRaycastTarget;
    private static Vector2 originalRoundSize;
    private static TextMeshProUGUI roundText;
    private static Vector2 originalTextAnchorMin;
    private static Vector2 originalTextAnchorMax;
    private static Vector2 originalTextOffsetMin;
    private static Vector2 originalTextOffsetMax;
    private static Vector3 originalTextScale;
    private static bool originalTextAutoSizing;
    private static float originalTextFontSize;
    private static float originalTextFontSizeMin;
    private static float originalTextFontSizeMax;
    private static bool originalTextWordWrapping;
    private static bool originalTextRaycastTarget;
    private static TextAlignmentOptions originalTextAlignment;
    private static bool roundLayoutCaptured;

    public static bool IsActive { get; private set; }

    public static void Activate(int cpuLevel, UIManager uiManager, CharacterManager characters)
    {
        assets = assets != null ? assets : Resources.Load<BattleGroundVisualAssets>("BattleGroundVisualAssets");
        if (assets == null) return;
        IsActive = true;
        ResolveSceneImages(uiManager);
        if (backgroundImage != null && assets.battleBackground != null) backgroundImage.sprite = assets.battleBackground;
        if (roundFrameImage != null && assets.roundCounterFrame != null)
        {
            roundFrameImage.sprite = assets.roundCounterFrame;
            roundFrameImage.preserveAspect = true;
            roundFrameImage.raycastTarget = false;
            float spriteHeight = assets.roundCounterFrame.rect.height;
            if (spriteHeight > 0f)
            {
                float aspect = assets.roundCounterFrame.rect.width / spriteHeight;
                roundFrameImage.rectTransform.sizeDelta = new Vector2(
                    originalRoundSize.y * aspect,
                    originalRoundSize.y);
            }
            ConfigureBattleGroundCounterText();
        }
        Sprite cpu = assets.cpuCharacters != null && assets.cpuCharacters.Length > 0
            ? assets.cpuCharacters[Mathf.Clamp(cpuLevel - 1, 0, assets.cpuCharacters.Length - 1)]
            : null;
        characters?.ApplyBattleGroundSprites(assets.playerCharacter, cpu);
        RefreshAllCards();
    }

    public static void Deactivate(CharacterManager characters)
    {
        if (!IsActive) return;
        IsActive = false;
        if (backgroundImage != null) backgroundImage.sprite = originalBackground;
        if (roundFrameImage != null)
        {
            roundFrameImage.sprite = originalRoundFrame;
            roundFrameImage.preserveAspect = originalRoundPreserveAspect;
            roundFrameImage.raycastTarget = originalRoundRaycastTarget;
            roundFrameImage.rectTransform.sizeDelta = originalRoundSize;
        }
        RestoreCounterText();
        characters?.RestoreModeSprites();
        RefreshAllCards();
    }

    public static Sprite ResolveFace(CardData data)
    {
        if (!IsActive || assets == null || data == null) return data != null ? data.Image : null;
        if (SpecialCardResolver.IsNoUseSpecialCard(data)) return assets.noSpecialCard ?? data.Image;
        if (SpecialCardResolver.TryGetSpecialCardId(data, out SpecialCardResolver.SpecialCardId id))
        {
            return assets.GetSpecialCard(id) ?? data.Image;
        }
        return data.Image;
    }

    public static Sprite ResolveBack(CardData data)
    {
        if (!IsActive || assets == null || data == null) return data != null ? data.BackImage : null;
        bool special = SpecialCardResolver.TryGetSpecialCardId(data, out _)
            || SpecialCardResolver.IsNoUseSpecialCard(data);
        Sprite themed = special ? assets.specialCardBack : assets.normalCardBack;
        return themed != null ? themed : data.BackImage;
    }

    private static void ResolveSceneImages(UIManager uiManager)
    {
        if (uiManager == null) return;
        Canvas canvas = uiManager.deck != null
            ? uiManager.deck.GetComponentInParent<Canvas>()
            : uiManager.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        if (canvas != null && backgroundImage == null)
        {
            Transform panel = canvas.transform.Find("Panel");
            backgroundImage = panel != null ? panel.GetComponent<Image>() : null;
            if (backgroundImage != null) originalBackground = backgroundImage.sprite;
        }
        if (roundFrameImage == null)
        {
            roundLayoutCaptured = false;
            roundText = null;
            Transform round = canvas.transform.Find("CharacterManager/round");
            roundFrameImage = round != null ? round.GetComponent<Image>() : null;
            if (roundFrameImage != null)
            {
                originalRoundFrame = roundFrameImage.sprite;
                CaptureRoundLayout();
            }
        }
    }

    private static void CaptureRoundLayout()
    {
        if (roundFrameImage == null || roundLayoutCaptured) return;
        roundLayoutCaptured = true;
        originalRoundPreserveAspect = roundFrameImage.preserveAspect;
        originalRoundRaycastTarget = roundFrameImage.raycastTarget;
        originalRoundSize = roundFrameImage.rectTransform.sizeDelta;
        roundText = roundFrameImage.GetComponentInChildren<TextMeshProUGUI>(true);
        if (roundText == null) return;

        RectTransform rect = roundText.rectTransform;
        originalTextAnchorMin = rect.anchorMin;
        originalTextAnchorMax = rect.anchorMax;
        originalTextOffsetMin = rect.offsetMin;
        originalTextOffsetMax = rect.offsetMax;
        originalTextScale = rect.localScale;
        originalTextAutoSizing = roundText.enableAutoSizing;
        originalTextFontSize = roundText.fontSize;
        originalTextFontSizeMin = roundText.fontSizeMin;
        originalTextFontSizeMax = roundText.fontSizeMax;
        originalTextWordWrapping = roundText.enableWordWrapping;
        originalTextRaycastTarget = roundText.raycastTarget;
        originalTextAlignment = roundText.alignment;
    }

    private static void ConfigureBattleGroundCounterText()
    {
        if (roundText == null && roundFrameImage != null)
        {
            roundText = roundFrameImage.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (roundText == null) return;

        RectTransform rect = roundText.rectTransform;
        // Keep the counter inside the dark center of the decorative frame.
        // The sprite includes a wide transparent/decorative border, so using
        // almost the entire RectTransform makes the glyphs spill over it.
        rect.anchorMin = new Vector2(0.2f, 0.05f);
        rect.anchorMax = new Vector2(0.8f, 0.56f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        roundText.enableWordWrapping = false;
        roundText.raycastTarget = false;
        // TMP auto sizing does not account correctly for this world-space UI's
        // inherited scale. Use a size that fits both the height and three-digit
        // win counts inside the counter frame.
        roundText.enableAutoSizing = false;
        roundText.fontSize = 1.35f;
        roundText.fontSizeMin = 1.35f;
        roundText.fontSizeMax = 1.35f;
        roundText.alignment = TextAlignmentOptions.Center;
        roundText.ForceMeshUpdate();
    }

    private static void RestoreCounterText()
    {
        if (roundText == null || !roundLayoutCaptured) return;
        RectTransform rect = roundText.rectTransform;
        rect.anchorMin = originalTextAnchorMin;
        rect.anchorMax = originalTextAnchorMax;
        rect.offsetMin = originalTextOffsetMin;
        rect.offsetMax = originalTextOffsetMax;
        rect.localScale = originalTextScale;
        roundText.enableAutoSizing = originalTextAutoSizing;
        roundText.fontSize = originalTextFontSize;
        roundText.fontSizeMin = originalTextFontSizeMin;
        roundText.fontSizeMax = originalTextFontSizeMax;
        roundText.enableWordWrapping = originalTextWordWrapping;
        roundText.raycastTarget = originalTextRaycastTarget;
        roundText.alignment = originalTextAlignment;
    }

    private static void RefreshAllCards()
    {
        foreach (Card card in Object.FindObjectsOfType<Card>(true))
        {
            if (card != null) card.ForceSetFaceUp(card.IsFaceUp);
        }
    }
}
