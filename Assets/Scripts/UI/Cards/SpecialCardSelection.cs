using System.Collections.Generic;

/// <summary>Selection and availability rules, independent of button subscriptions and motion.</summary>
internal sealed class SpecialCardSelection
{
    public HashSet<Card> UsedCards { get; } = new HashSet<Card>();
    public Card SelectedTopCard { get; set; }
    public bool InputEnabled { get; set; } = true;

    public bool IsAvailable(Card card) => card != null && !UsedCards.Contains(card);

    public Card FindTopAvailable(IReadOnlyList<Card> cards)
    {
        if (cards == null) return null;
        for (int i = cards.Count - 1; i >= 0; i--)
            if (IsAvailable(cards[i])) return cards[i];
        return null;
    }

    public void EnsureSelected(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            SelectedTopCard = null;
            return;
        }
        if (!IsAvailable(SelectedTopCard) || !cards.Contains(SelectedTopCard))
            SelectedTopCard = FindTopAvailable(cards);
        if (SelectedTopCard != null && cards.Remove(SelectedTopCard)) cards.Add(SelectedTopCard);
    }

    public void Apply(IReadOnlyList<Card> cards)
    {
        foreach (Card card in cards)
        {
            if (card == null) continue;
            card.UseSelectedYOffset = false;
            card.IsSelected = card == SelectedTopCard && IsAvailable(card);
        }
    }

    public bool TrySwitch(List<Card> cards, Card card)
    {
        int top = cards.Count - 1;
        int index = cards.IndexOf(card);
        bool used = !IsAvailable(card);
        if (used && index != top) return false;
        if (index == top && cards.Count > 1)
        {
            cards.RemoveAt(top);
            cards.Insert(0, card);
        }
        else
        {
            if (used) return false;
            cards.RemoveAt(index);
            cards.Add(card);
        }
        SelectedTopCard = FindTopAvailable(cards);
        EnsureSelected(cards);
        Apply(cards);
        return true;
    }
}
