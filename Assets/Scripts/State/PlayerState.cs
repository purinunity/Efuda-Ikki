using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    public const int DefaultLifePoints = 100;

    public int PlayerId { get; private set; }
    public int LifePoints { get; private set; } = DefaultLifePoints;
    public List<Card> HandCards { get; private set; }
    public List<Card> SpecialCards { get; private set; }
    private readonly HashSet<Card> usedSpecialCards = new HashSet<Card>();
    public IReadOnlyCollection<Card> UsedSpecialCards => usedSpecialCards;
    public int HandTrashTurnsUsed { get; private set; } = 0;

    public PlayerState(int playerId)
    {
        PlayerId = playerId;
        HandCards = new List<Card>();
        SpecialCards = new List<Card>();
    }

    public void IncrementHandTrashTurnsUsed()
    {
        HandTrashTurnsUsed++;
    }

    public void ResetHandTrashTurnsUsed()
    {
        HandTrashTurnsUsed = 0;
    }

    public void SetLifePoints(int lifePoints)
    {
        LifePoints = Mathf.Max(0, lifePoints);
    }

    public void ResetForMatch(int initialLifePoints = DefaultLifePoints)
    {
        SetLifePoints(initialLifePoints);
        HandCards.Clear();
        HandTrashTurnsUsed = 0;
        ResetSpecialCardUsage();
    }

    public void SetSpecialCardsForMatch(IEnumerable<Card> cards)
    {
        CardSelectionUtility.ClearSelections(SpecialCards);
        SpecialCards = cards != null ? new List<Card>(cards) : new List<Card>();
        ResetSpecialCardUsage();
    }

    public void ResetSpecialCardUsage()
    {
        usedSpecialCards.Clear();
        CardSelectionUtility.ClearSelections(SpecialCards);
    }

    public bool IsSpecialCardUsed(Card card)
    {
        return card != null && usedSpecialCards.Contains(card);
    }

    public bool MarkSpecialCardUsed(Card card)
    {
        if (card == null || SpecialCards == null || !SpecialCards.Contains(card))
        {
            return false;
        }

        card.IsSelected = false;
        return usedSpecialCards.Add(card);
    }

    public void AddCardToHand(Card card)
    {
        if (card != null && !HandCards.Contains(card))
        {
            HandCards.Add(card);
            SortHand();
        }
        else
        {
            Debug.LogWarning($"Card {card?.CardData.number} of {card?.CardData.suit} is already in player {PlayerId}'s hand or is null.");
        }
    }

    private void SortHand()
    {
        HandCards.Sort((a, b) =>
        {
            if (a == null || a.CardData == null) return -1;
            if (b == null || b.CardData == null) return 1;

            int suitA = SuitOrder(a.CardData.suit);
            int suitB = SuitOrder(b.CardData.suit);
            if (suitA != suitB) return suitA.CompareTo(suitB);

            int numA = (int)a.CardData.number;
            int numB = (int)b.CardData.number;
            return numA.CompareTo(numB);
        });
    }

    private int SuitOrder(Suit suit)
    {
        switch (suit)
        {
            case Suit.Flowers: return 0;
            case Suit.Birds: return 1;
            case Suit.Wind: return 2;
            case Suit.Moon: return 3;
            default: return 4;
        }
    }

    public void RemoveCardFromHand(Card card)
    {
        if (card != null && HandCards.Contains(card))
        {
            HandCards.Remove(card);
        }
        else
        {
            Debug.LogWarning($"Card {card?.CardData.number} of {card?.CardData.suit} is not in player {PlayerId}'s hand or is null.");
        }
    }

    public void decreaseLifePoints(int amount)
    {
        SetLifePoints(LifePoints - amount);
    }
}
