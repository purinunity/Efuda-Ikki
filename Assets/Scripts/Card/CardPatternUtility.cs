using System.Collections.Generic;

public static class CardPatternUtility
{
    private static readonly Number[] SequenceOrder =
    {
        Number.One, Number.Two, Number.Three, Number.Four, Number.Five,
        Number.Six, Number.Seven, Number.Eight, Number.Nine, Number.Ten,
        Number.Jack, Number.Queen, Number.King, Number.One
    };

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
        List<Number> current = new List<Number>();
        if (numberGroups == null || sequenceLength <= 0)
        {
            return current;
        }

        foreach (Number number in SequenceOrder)
        {
            if (numberGroups.ContainsKey(number))
            {
                current.Add(number);
                if (current.Count >= sequenceLength)
                {
                    return current.GetRange(current.Count - sequenceLength, sequenceLength);
                }
            }
            else
            {
                current.Clear();
            }
        }

        return new List<Number>();
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
