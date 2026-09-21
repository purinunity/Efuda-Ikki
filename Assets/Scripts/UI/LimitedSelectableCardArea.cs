using TMPro;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CardArea with selection limit and selected/max UI display.
/// </summary>
public class LimitedSelectableCardArea : CardArea
{
    [SerializeField] private TextMeshProUGUI selectionCountText;
    [SerializeField] private int maxSelectableCount = 1;
    [SerializeField] private float selectMoveDuration = 0.3f;
    private readonly HashSet<Card> managedCards = new HashSet<Card>();
    private readonly HashSet<Card> unavailableCards = new HashSet<Card>();
    private readonly HashSet<Card> currentAreaCards = new HashSet<Card>();

    public override void SetCards(System.Collections.Generic.List<Card> cards, float totalDuration = 1.0f)
    {
        base.SetCards(cards, totalDuration);
        RefreshSelectionState();
    }

    public override void SetCardsBySpeed(System.Collections.Generic.List<Card> cards, float moveSpeed, float turnSpeed)
    {
        base.SetCardsBySpeed(cards, moveSpeed, turnSpeed);
        RefreshSelectionState();
    }

    public void SetMaxSelectableCount(int maxCount)
    {
        maxSelectableCount = Mathf.Max(0, maxCount);
        RefreshSelectionState();
    }

    public void SetSelectionCountText(TextMeshProUGUI text)
    {
        selectionCountText = text;
        RefreshSelectionState();
    }

    public void SetCardAvailability(Card card, bool isAvailable)
    {
        if (card == null)
        {
            return;
        }

        if (isAvailable)
        {
            unavailableCards.Remove(card);
        }
        else
        {
            unavailableCards.Add(card);
            card.IsSelected = false;
        }

        card.IsSelectable = isAvailable;
        RefreshSelectionState();
    }

    public bool CanSelect(Card card)
    {
        if (card == null || !ContainsCard(card))
        {
            return false;
        }

        if (card.IsSelected)
        {
            return true;
        }

        return CountSelectedCards() < maxSelectableCount;
    }

    public bool TryToggleSelection(Card card)
    {
        if (card == null || !ContainsCard(card))
        {
            return false;
        }

        if (!card.MoveComplete)
        {
            // Finish the previous selection movement so a quick second click can
            // immediately switch the card instead of being silently ignored.
            card.SnapToTargetPosition();
        }

        if (!card.IsSelectable)
        {
            RefreshSelectionState();
            return false;
        }

        if (!card.IsSelected && !CanSelect(card))
        {
            RefreshSelectionState();
            return false;
        }

        card.IsSelected = !card.IsSelected;
        card.MoveAndTurnCard(selectMoveDuration);
        RefreshSelectionState();
        return true;
    }

    public void RefreshSelectionState()
    {
        int selectedCount = CountSelectedCards();

        currentAreaCards.Clear();
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            currentAreaCards.Add(card);
            managedCards.Add(card);
            card.IsSelectable = !unavailableCards.Contains(card);
        }

        foreach (var card in managedCards)
        {
            if (card == null) continue;
            if (currentAreaCards.Contains(card)) continue;
            card.IsSelectable = false;
            card.IsSelected = false;
        }

        if (selectionCountText != null)
        {
            selectionCountText.text = selectedCount + "/" + maxSelectableCount;
        }
    }

    private int CountSelectedCards()
    {
        if (cardsInArea == null)
        {
            return 0;
        }

        int count = 0;
        foreach (var card in cardsInArea)
        {
            if (card != null && card.IsSelected)
            {
                count++;
            }
        }
        return count;
    }

    protected virtual bool ContainsCard(Card card)
    {
        return cardsInArea != null && cardsInArea.Contains(card);
    }
}
