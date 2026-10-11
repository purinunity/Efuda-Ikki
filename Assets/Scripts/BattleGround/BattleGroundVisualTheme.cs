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
        }
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
        if (backgroundImage == null)
        {
            backgroundImage = uiManager.BattleBackgroundImage;
            if (backgroundImage != null) originalBackground = backgroundImage.sprite;
        }
        if (roundFrameImage == null)
        {
            roundFrameImage = uiManager.RoundFrameImage;
            if (roundFrameImage != null)
            {
                originalRoundFrame = roundFrameImage.sprite;
                originalRoundPreserveAspect = roundFrameImage.preserveAspect;
                originalRoundRaycastTarget = roundFrameImage.raycastTarget;
            }
        }
    }

    private static void RefreshAllCards()
    {
        foreach (Card card in Object.FindObjectsOfType<Card>(true))
        {
            if (card != null) card.ForceSetFaceUp(card.IsFaceUp);
        }
    }
}
