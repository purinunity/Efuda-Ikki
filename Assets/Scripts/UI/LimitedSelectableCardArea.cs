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

    private void Start()
    {
    }

    private void LateUpdate()
    {
        RefreshSelectionState();
    }

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
            RefreshSelectionState();
            return false;
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

        var areaCards = new HashSet<Card>();
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            areaCards.Add(card);
            managedCards.Add(card);
            card.IsSelectable = true;
        }

        foreach (var card in managedCards)
        {
            if (card == null) continue;
            if (areaCards.Contains(card)) continue;
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
