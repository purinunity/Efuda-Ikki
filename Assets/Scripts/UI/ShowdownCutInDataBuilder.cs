using System.Collections.Generic;
using UnityEngine;
using static HandEvaluator;

public sealed class ShowdownCutInDataBuilder
{
    private readonly GameState gameState;
    private readonly CharacterManager characterManager;

    public ShowdownCutInDataBuilder(GameState gameState, CharacterManager characterManager)
    {
        this.gameState = gameState;
        this.characterManager = characterManager;
    }

    public ShowdownCutInPopup.Data Build(SpecialCardResolver.ShowdownResult showdownResult)
    {
        SpecialCardResolver.ResolvedHand playerHand = FindResolvedHand(showdownResult, 0);
        SpecialCardResolver.ResolvedHand cpuHand = FindResolvedHand(showdownResult, 1);
        List<Sprite> playerCards = BuildShowdownCardSprites(0);
        List<Sprite> cpuCards = BuildShowdownCardSprites(1);
        int playerLifeBefore = GetLifePoints(0);
        int cpuLifeBefore = GetLifePoints(1);
        int playerLifeAfter = playerLifeBefore;
        int cpuLifeAfter = cpuLifeBefore;

        if (showdownResult != null && !showdownResult.IsDraw)
        {
            if (showdownResult.WinnerIndex == 0)
            {
                cpuLifeAfter = Mathf.Max(0, cpuLifeBefore - showdownResult.Damage);
            }
            else if (showdownResult.WinnerIndex == 1)
            {
                playerLifeAfter = Mathf.Max(0, playerLifeBefore - showdownResult.Damage);
            }
        }

        return new ShowdownCutInPopup.Data(
            characterManager != null ? characterManager.GetPlayerSprite() : null,
            characterManager != null ? characterManager.GetCpuSprite() : null,
            GetBaseRoleName(playerHand),
            GetBaseRoleName(cpuHand),
            GetBaseRoleRank(playerHand),
            GetBaseRoleRank(cpuHand),
            GetBaseScore(playerHand),
            GetBaseScore(cpuHand),
            GetFinalRoleName(playerHand),
            GetFinalRoleName(cpuHand),
            GetFinalRoleRank(playerHand),
            GetFinalRoleRank(cpuHand),
            GetFinalScore(playerHand),
            GetFinalScore(cpuHand),
            playerCards,
            cpuCards,
            BuildRoleHighlightFlags(playerCards.Count, playerHand?.BaseHand),
            BuildRoleHighlightFlags(cpuCards.Count, cpuHand?.BaseHand),
            GetSelectedSpecialCardSprite(0),
            GetSelectedSpecialCardSprite(1),
            BuildEffectStepData(showdownResult),
            showdownResult.WinnerIndex,
            showdownResult.Damage,
            playerLifeBefore,
            cpuLifeBefore,
            playerLifeAfter,
            cpuLifeAfter);
    }

    private int GetLifePoints(int playerId)
    {
        if (gameState?.PlayerStates == null ||
            playerId < 0 ||
            playerId >= gameState.PlayerStates.Count ||
            gameState.PlayerStates[playerId] == null)
        {
            return 0;
        }

        return gameState.PlayerStates[playerId].LifePoints;
    }

    private List<Sprite> BuildShowdownCardSprites(int playerId)
    {
        List<Sprite> sprites = new List<Sprite>();
        if (gameState == null || gameState.PlayerStates == null || playerId < 0 || playerId >= gameState.PlayerStates.Count)
        {
            return sprites;
        }

        PlayerState playerState = gameState.PlayerStates[playerId];
        if (playerState?.HandCards != null)
        {
            foreach (Card card in playerState.HandCards)
            {
                AddCardSprite(sprites, card);
            }
        }

        if (gameState.commonCards != null)
        {
            foreach (Card card in gameState.commonCards)
            {
                AddCardSprite(sprites, card);
            }
        }

        return sprites;
    }

    private Sprite GetSelectedSpecialCardSprite(int playerId)
    {
        if (gameState == null || gameState.PlayerStates == null || playerId < 0 || playerId >= gameState.PlayerStates.Count)
        {
            return null;
        }

        PlayerState playerState = gameState.PlayerStates[playerId];
        if (playerState?.SpecialCards == null)
        {
            return null;
        }

        Card selectedCard = CardSelectionUtility.GetFirstSelected(
            playerState.SpecialCards,
            card => !playerState.IsSpecialCardUsed(card));

        return selectedCard != null && selectedCard.CardData != null ? selectedCard.CardData.Image : null;
    }

    private static void AddCardSprite(List<Sprite> sprites, Card card)
    {
        if (sprites == null)
        {
            return;
        }

        sprites.Add(card != null && card.CardData != null ? card.CardData.Image : null);
    }

    private static List<bool> BuildRoleHighlightFlags(
        int cardCount,
        HandInfo baseHand)
    {
        var contributingIndexes = new HashSet<int>(
            baseHand?.ContributingCardIndexes ?? new int[0]);
        var highlights = new List<bool>(Mathf.Max(0, cardCount));
        for (int index = 0; index < cardCount; index++)
        {
            highlights.Add(contributingIndexes.Contains(index));
        }

        return highlights;
    }

    private List<ShowdownCutInPopup.Data.EffectStepData> BuildEffectStepData(
        SpecialCardResolver.ShowdownResult showdownResult)
    {
        List<ShowdownCutInPopup.Data.EffectStepData> steps =
            new List<ShowdownCutInPopup.Data.EffectStepData>();

        if (showdownResult?.EffectSteps == null)
        {
            return steps;
        }

        foreach (SpecialCardResolver.EffectStep step in showdownResult.EffectSteps)
        {
            steps.Add(new ShowdownCutInPopup.Data.EffectStepData(
                step.OwnerPlayerId,
                step.Card != null && step.Card.CardData != null ? step.Card.CardData.Image : null,
                step.EffectName,
                step.Message,
                step.WasSealed,
                step.PlayerRoleName,
                step.CpuRoleName,
                step.PlayerRoleRank,
                step.CpuRoleRank,
                step.PlayerScore,
                step.CpuScore));
        }

        return steps;
    }

    private static SpecialCardResolver.ResolvedHand FindResolvedHand(
        SpecialCardResolver.ShowdownResult showdownResult,
        int playerId)
    {
        if (showdownResult == null || showdownResult.Hands == null)
        {
            return null;
        }

        foreach (SpecialCardResolver.ResolvedHand hand in showdownResult.Hands)
        {
            if (hand != null && hand.PlayerId == playerId)
            {
                return hand;
            }
        }

        return null;
    }

    private static HandRank GetBaseRoleRank(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? HandRoleCatalog.GetAt(hand.BaseRoleIndex).Rank : HandRank.Miezu;
    }

    private static HandRank GetFinalRoleRank(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? hand.CurrentRank : HandRank.Miezu;
    }

    private static string GetBaseRoleName(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? hand.BaseDisplayName : "不見";
    }

    private static string GetFinalRoleName(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? hand.DisplayName : "不見";
    }

    private static int GetBaseScore(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? hand.BaseScore : 0;
    }

    private static int GetFinalScore(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? hand.Score : 0;
    }
}
