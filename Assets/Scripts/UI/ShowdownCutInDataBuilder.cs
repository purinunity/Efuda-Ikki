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
            BuildShowdownCardSprites(0),
            BuildShowdownCardSprites(1),
            BuildRoleHighlightFlags(0, GetBaseRoleRank(playerHand)),
            BuildRoleHighlightFlags(1, GetBaseRoleRank(cpuHand)),
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

    private List<bool> BuildRoleHighlightFlags(int playerId, HandRank roleRank)
    {
        List<Card> cards = BuildShowdownCards(playerId);
        HashSet<Card> relatedCards = FindRoleRelatedCards(cards, roleRank);
        List<bool> highlights = new List<bool>();

        foreach (Card card in cards)
        {
            highlights.Add(card != null && relatedCards.Contains(card));
        }

        return highlights;
    }

    private List<Card> BuildShowdownCards(int playerId)
    {
        List<Card> cards = new List<Card>();
        if (gameState == null || gameState.PlayerStates == null || playerId < 0 || playerId >= gameState.PlayerStates.Count)
        {
            return cards;
        }

        PlayerState playerState = gameState.PlayerStates[playerId];
        if (playerState?.HandCards != null)
        {
            cards.AddRange(playerState.HandCards);
        }

        if (gameState.commonCards != null)
        {
            cards.AddRange(gameState.commonCards);
        }

        return cards;
    }

    private HashSet<Card> FindRoleRelatedCards(List<Card> cards, HandRank roleRank)
    {
        HashSet<Card> relatedCards = new HashSet<Card>();
        if (cards == null || roleRank == HandRank.Miezu)
        {
            return relatedCards;
        }

        Dictionary<Number, List<Card>> numberGroups = CardPatternUtility.BuildNumberGroups(cards);
        Dictionary<Suit, List<Card>> suitGroups = CardPatternUtility.BuildSuitGroups(cards);

        switch (roleRank)
        {
            case HandRank.Isso:
                AddNumberGroupByCount(relatedCards, numberGroups, 2, 1);
                break;
            case HandRank.Niso:
                AddNumberGroupByCount(relatedCards, numberGroups, 2, 2);
                break;
            case HandRank.Sanju:
                AddNumberGroupByCount(relatedCards, numberGroups, 3, 1);
                break;
            case HandRank.Yonju:
                AddNumberGroupByCount(relatedCards, numberGroups, 4, 1);
                break;
            case HandRank.Tenshu:
                AddTenshuGroups(relatedCards, suitGroups, 1);
                break;
            case HandRank.Suzi:
                AddSequenceCards(relatedCards, numberGroups, 5);
                break;
            case HandRank.Hikari:
                AddSuitGroupByCount(relatedCards, suitGroups, 5, 1);
                break;
            case HandRank.Nanasuzi:
                AddSequenceCards(relatedCards, numberGroups, 7);
                break;
            case HandRank.Nanahikari:
                AddSuitGroupByCount(relatedCards, suitGroups, 7, 1);
                break;
            case HandRank.Tenshukaku:
                AddTenshuGroups(relatedCards, suitGroups, 2);
                break;
        }

        return relatedCards;
    }

    private static void AddNumberGroupByCount(
        HashSet<Card> relatedCards,
        Dictionary<Number, List<Card>> numberGroups,
        int requiredCount,
        int requiredGroups)
    {
        int addedGroups = 0;
        foreach (KeyValuePair<Number, List<Card>> group in numberGroups)
        {
            if (group.Value.Count < requiredCount)
            {
                continue;
            }

            foreach (Card card in group.Value)
            {
                relatedCards.Add(card);
            }

            addedGroups++;
            if (addedGroups >= requiredGroups)
            {
                return;
            }
        }
    }

    private static void AddSuitGroupByCount(
        HashSet<Card> relatedCards,
        Dictionary<Suit, List<Card>> suitGroups,
        int requiredCount,
        int requiredGroups)
    {
        int addedGroups = 0;
        foreach (KeyValuePair<Suit, List<Card>> group in suitGroups)
        {
            if (group.Value.Count < requiredCount)
            {
                continue;
            }

            foreach (Card card in group.Value)
            {
                relatedCards.Add(card);
            }

            addedGroups++;
            if (addedGroups >= requiredGroups)
            {
                return;
            }
        }
    }

    private static void AddTenshuGroups(
        HashSet<Card> relatedCards,
        Dictionary<Suit, List<Card>> suitGroups,
        int requiredGroups)
    {
        int addedGroups = 0;
        foreach (KeyValuePair<Suit, List<Card>> group in suitGroups)
        {
            if (!CardPatternUtility.HasNumber(group.Value, Number.Jack) ||
                !CardPatternUtility.HasNumber(group.Value, Number.Queen) ||
                !CardPatternUtility.HasNumber(group.Value, Number.King))
            {
                continue;
            }

            AddCardsWithNumber(relatedCards, group.Value, Number.Jack);
            AddCardsWithNumber(relatedCards, group.Value, Number.Queen);
            AddCardsWithNumber(relatedCards, group.Value, Number.King);

            addedGroups++;
            if (addedGroups >= requiredGroups)
            {
                return;
            }
        }
    }

    private static void AddSequenceCards(
        HashSet<Card> relatedCards,
        Dictionary<Number, List<Card>> numberGroups,
        int sequenceLength)
    {
        List<Number> sequence = CardPatternUtility.FindSequence(numberGroups, sequenceLength);
        foreach (Number number in sequence)
        {
            if (!numberGroups.TryGetValue(number, out List<Card> cards))
            {
                continue;
            }

            foreach (Card card in cards)
            {
                relatedCards.Add(card);
            }
        }
    }

    private static void AddCardsWithNumber(HashSet<Card> relatedCards, List<Card> cards, Number number)
    {
        foreach (Card card in cards)
        {
            if (card?.CardData != null && card.CardData.number == number)
            {
                relatedCards.Add(card);
            }
        }
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
