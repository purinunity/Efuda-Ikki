using System;
using System.Collections.Generic;
using System.Linq;
using EfudaIkki.Core;

/// <summary>
/// Unity-facing compatibility facade for the pure EfudaIkki.Core hand evaluator.
/// Existing public types and enum values are intentionally preserved.
/// </summary>
public static class HandEvaluator
{
    public enum HandRank
    {
        Miezu = 0,
        Isso = 1,
        Niso = 2,
        Sanju = 3,
        Hikari = 4,
        Suzi = 5,
        Yonju = 6,
        Tenshu = 7,
        Nanahikari = 8,
        Nanasuzi = 9,
        Tenshukaku = 10
    }

    public class HandInfo
    {
        private readonly int[] contributingCardIndexes;

        public HandRank Rank { get; private set; }
        public string Name { get; private set; }
        public int Score => HandRoleCatalog.GetScore(Rank);
        public IReadOnlyList<int> ContributingCardIndexes => contributingCardIndexes;

        public HandInfo(HandRank rank, string name)
            : this(rank, name, null)
        {
        }

        public HandInfo(HandRank rank, string name, IEnumerable<int> contributingCardIndexes)
        {
            Rank = rank;
            Name = name;
            this.contributingCardIndexes = contributingCardIndexes == null
                ? Array.Empty<int>()
                : contributingCardIndexes.ToArray();
        }
    }

    public static HandInfo EvaluateHand(List<Card> playerHand, List<Card> commonCards)
    {
        // Preserve the existing UI contract: an incomplete or hidden hand has no role label.
        if (playerHand == null || playerHand.Count != 5)
        {
            return new HandInfo(HandRank.Miezu, "");
        }

        if (playerHand.Any(card => !card.IsFaceUp))
        {
            return new HandInfo(HandRank.Miezu, "");
        }

        if (commonCards != null && commonCards.Any(card => !card.IsFaceUp))
        {
            return new HandInfo(HandRank.Miezu, "");
        }

        List<Card> allCards = playerHand.Concat(commonCards ?? Enumerable.Empty<Card>()).ToList();
        HandEvaluationResult result = CoreHandEvaluator.Evaluate(ToCoreValues(allCards));
        HandRank rank = ToLegacyRank(result.Role);
        return new HandInfo(rank, HandRoleCatalog.GetDisplayName(rank), result.ContributingCardIndexes);
    }

    /// <summary>
    /// Returns indexes into <paramref name="cards"/> which visually constitute the requested role.
    /// This keeps cut-in highlighting on the same rules used for evaluation.
    /// </summary>
    public static IReadOnlyList<int> GetContributingCardIndexes(
        IReadOnlyList<Card> cards,
        HandRank rank)
    {
        return CoreHandEvaluator.FindContributingCardIndexes(
            ToCoreValues(cards),
            (HandRole)(int)rank);
    }

    public static int DetermineWinner(List<HandInfo> handInfos)
    {
        List<int> winners = new List<int>();
        int highestScore = HandRoleCatalog.GetScore(HandRank.Miezu);

        for (int i = 0; i < handInfos.Count; i++)
        {
            int score = handInfos[i] != null ? handInfos[i].Score : 0;
            if (score > highestScore)
            {
                highestScore = score;
                winners = new List<int> { i };
            }
            else if (score == highestScore)
            {
                winners.Add(i);
            }
        }

        return winners.Count == 1 ? winners[0] : -1;
    }

    private static List<CardValue> ToCoreValues(IReadOnlyList<Card> cards)
    {
        var values = new List<CardValue>();
        if (cards == null)
        {
            return values;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            Card card = cards[i];
            values.Add(card?.CardData == null
                ? CardValue.Invalid
                : new CardValue((int)card.CardData.number, (int)card.CardData.suit));
        }

        return values;
    }

    private static HandRank ToLegacyRank(HandRole role)
    {
        return (HandRank)(int)role;
    }
}
