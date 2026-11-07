using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal interface ICardVisualTheme
{
    Sprite ResolveFace(CardData data);
    Sprite ResolveBack(CardData data);
}

/// <summary>One session's theme and explicit targets. No scene-wide or static state.</summary>
public sealed class BattleGroundVisualTheme : ICardVisualTheme
{
    private readonly BattleGroundVisualAssets assets;
    private readonly HashSet<Card> cards;
    private readonly Image backgroundImage;
    private readonly Image roundFrameImage;
    private readonly CharacterManager characters;
    private Sprite originalBackground;
    private Sprite originalRoundFrame;
    private bool originalRoundPreserveAspect;
    private bool originalRoundRaycastTarget;

    public bool IsActive { get; private set; }

    public BattleGroundVisualTheme(BattleGroundVisualAssets assets, IEnumerable<Card> cards,
        Image backgroundImage, Image roundFrameImage, CharacterManager characters)
    {
        this.assets = assets;
        this.cards = cards != null ? new HashSet<Card>(cards) : new HashSet<Card>();
        this.backgroundImage = backgroundImage;
        this.roundFrameImage = roundFrameImage;
        this.characters = characters;
    }

    public void Activate(int cpuLevel)
    {
        if (assets == null) return;
        if (!IsActive)
        {
            if (backgroundImage != null) originalBackground = backgroundImage.sprite;
            if (roundFrameImage != null)
            {
                originalRoundFrame = roundFrameImage.sprite;
                originalRoundPreserveAspect = roundFrameImage.preserveAspect;
                originalRoundRaycastTarget = roundFrameImage.raycastTarget;
            }
        }
        IsActive = true;
        if (backgroundImage != null && assets.battleBackground != null) backgroundImage.sprite = assets.battleBackground;
        if (roundFrameImage != null && assets.roundCounterFrame != null)
        {
            roundFrameImage.sprite = assets.roundCounterFrame;
            roundFrameImage.preserveAspect = true;
            roundFrameImage.raycastTarget = false;
        }
        Sprite cpu = assets.cpuCharacters != null && assets.cpuCharacters.Length > 0
            ? assets.cpuCharacters[Mathf.Clamp(cpuLevel - 1, 0, assets.cpuCharacters.Length - 1)] : null;
        if (characters != null) characters.ApplyBattleGroundSprites(assets.playerCharacter, cpu);
        foreach (Card card in cards)
        {
            if (card == null) continue;
            card.SetVisualTheme(this);
            card.ForceSetFaceUp(card.IsFaceUp);
        }
    }

    public void Deactivate()
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
        if (characters != null) characters.RestoreModeSprites();
        foreach (Card card in cards)
        {
            if (card != null && card.ClearVisualTheme(this)) card.ForceSetFaceUp(card.IsFaceUp);
        }
    }

    public Sprite ResolveFace(CardData data)
    {
        if (!IsActive || assets == null || data == null) return data != null ? data.Image : null;
        if (SpecialCardResolver.IsNoUseSpecialCard(data)) return assets.noSpecialCard ?? data.Image;
        if (SpecialCardResolver.TryGetSpecialCardId(data, out SpecialCardResolver.SpecialCardId id))
            return assets.GetSpecialCard(id) ?? data.Image;
        return data.Image;
    }

    public Sprite ResolveBack(CardData data)
    {
        if (!IsActive || assets == null || data == null) return data != null ? data.BackImage : null;
        bool special = SpecialCardResolver.TryGetSpecialCardId(data, out _) || SpecialCardResolver.IsNoUseSpecialCard(data);
        Sprite themed = special ? assets.specialCardBack : assets.normalCardBack;
        return themed != null ? themed : data.BackImage;
    }
}
