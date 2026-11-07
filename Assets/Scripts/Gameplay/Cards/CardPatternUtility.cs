using System.Collections.Generic;
using System.Linq;

public static class CardPatternUtility
{
    public static Dictionary<Number, List<Card>> BuildNumberGroups(IEnumerable<Card> cards)
    {
        Dictionary<Number, List<Card>> groups = new Dictionary<Number, List<Card>>();
        if (cards == null)
        {
            return groups;
        }

        foreach (Card card in cards)
        {
            if (card?.CardData == null)
            {
                continue;
            }

            Number number = card.CardData.number;
            if (!groups.TryGetValue(number, out List<Card> group))
            {
                group = new List<Card>();
                groups[number] = group;
            }

            group.Add(card);
        }

        return groups;
    }

    public static Dictionary<Suit, List<Card>> BuildSuitGroups(IEnumerable<Card> cards)
    {
        Dictionary<Suit, List<Card>> groups = new Dictionary<Suit, List<Card>>();
        if (cards == null)
        {
            return groups;
        }

        foreach (Card card in cards)
        {
            if (card?.CardData == null)
            {
                continue;
            }

            Suit suit = card.CardData.suit;
            if (!groups.TryGetValue(suit, out List<Card> group))
            {
                group = new List<Card>();
                groups[suit] = group;
            }

            group.Add(card);
        }

        return groups;
    }

    public static List<Number> GetNumbersWithGroupCount(
        Dictionary<Number, List<Card>> numberGroups,
        int requiredCount,
        int maxGroups)
    {
        List<Number> numbers = new List<Number>();
        if (numberGroups == null || requiredCount <= 0 || maxGroups <= 0)
        {
            return numbers;
        }

        foreach (KeyValuePair<Number, List<Card>> group in numberGroups)
        {
            if (group.Value == null || group.Value.Count < requiredCount)
            {
                continue;
            }

            numbers.Add(group.Key);
            if (numbers.Count >= maxGroups)
            {
                return numbers;
            }
        }

        return numbers;
    }

    public static List<Number> FindSequence(
        Dictionary<Number, List<Card>> numberGroups,
        int sequenceLength)
    {
        if (numberGroups == null || sequenceLength <= 0)
        {
            return new List<Number>();
        }

        return EfudaIkki.Core.CardPatternAnalyzer
            .FindSequence(numberGroups.Keys.Select(number => (int)number), sequenceLength)
            .Select(number => (Number)number)
            .ToList();
    }

    public static bool HasNumber(IEnumerable<Card> cards, Number number)
    {
        if (cards == null)
        {
            return false;
        }

        foreach (Card card in cards)
        {
            if (card?.CardData != null && card.CardData.number == number)
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryFindMinoritySuit(Dictionary<Suit, List<Card>> suitGroups, out Suit suit)
    {
        suit = default;
        if (suitGroups == null || suitGroups.Count == 0)
        {
            return false;
        }

        bool found = false;
        int bestCount = int.MaxValue;
        foreach (KeyValuePair<Suit, List<Card>> group in suitGroups)
        {
            int count = group.Value != null ? group.Value.Count : 0;
            if (found && count >= bestCount)
            {
                continue;
            }

            found = true;
            bestCount = count;
            suit = group.Key;
        }

        return found;
    }
}
