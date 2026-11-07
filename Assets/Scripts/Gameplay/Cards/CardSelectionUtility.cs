using System;
using System.Collections.Generic;

public static class CardSelectionUtility
{
    public static List<Card> GetSelectedCards(
        IEnumerable<Card> cards,
        Predicate<Card> canUse = null)
    {
        List<Card> selectedCards = new List<Card>();
        if (cards == null)
        {
            return selectedCards;
        }

        foreach (Card card in cards)
        {
            if (card == null || !card.IsSelected)
            {
                continue;
            }

            if (canUse != null && !canUse(card))
            {
                continue;
            }

            selectedCards.Add(card);
        }

        return selectedCards;
    }

    public static Card GetFirstSelected(
        IEnumerable<Card> cards,
        Predicate<Card> canUse = null)
    {
        if (cards == null)
        {
            return null;
        }

        foreach (Card card in cards)
        {
            if (card == null || !card.IsSelected)
            {
                continue;
            }

            if (canUse != null && !canUse(card))
            {
                continue;
            }

            return card;
        }

        return null;
    }

    public static Dictionary<Card, bool> CaptureSelections(IEnumerable<PlayerState> playerStates)
    {
        Dictionary<Card, bool> selections = new Dictionary<Card, bool>();
        if (playerStates == null)
        {
            return selections;
        }

        foreach (PlayerState playerState in playerStates)
        {
            if (playerState?.SpecialCards == null)
            {
                continue;
            }

            foreach (Card card in playerState.SpecialCards)
            {
                if (card != null && !selections.ContainsKey(card))
                {
                    selections.Add(card, card.IsSelected);
                }
            }
        }

        return selections;
    }

    public static void RestoreSelections(Dictionary<Card, bool> selections)
    {
        if (selections == null)
        {
            return;
        }

        foreach (KeyValuePair<Card, bool> selection in selections)
        {
            if (selection.Key != null)
            {
                selection.Key.IsSelected = selection.Value;
            }
        }
    }

    public static void SelectOnly(IEnumerable<Card> cards, Card selectedCard)
    {
        if (cards == null)
        {
            return;
        }

        foreach (Card card in cards)
        {
            if (card != null)
            {
                card.IsSelected = card == selectedCard;
            }
        }
    }

    public static void ClearSelections(IEnumerable<Card> cards)
    {
        SelectOnly(cards, null);
    }
}
