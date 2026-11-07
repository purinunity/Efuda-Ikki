using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 特殊札表示用のカードエリア。
/// クリックでトップ表示カードを順番に切り替える。
/// </summary>
public class SpecialCardArea : CardArea
{
    [SerializeField] private float stackOverlapOffset = 10f;
    [SerializeField] private float topSwitchMoveDuration = 0.28f;
    [SerializeField] private float stackSettleMoveDuration = 0.22f;
    [SerializeField] private float topSwitchArcLift = 32f;
    [SerializeField] private Color usedSpecialCardTint = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color normalSpecialCardTint = Color.white;

    private readonly SpecialCardSelection selection = new SpecialCardSelection();
    private HashSet<Card> usedCards => selection.UsedCards;
    private SpecialCardStackView stackView;
    private SpecialCardStackView View => stackView ?? (stackView = new SpecialCardStackView(this, SwitchTopCard, ApplyCardUsageState));
    private Card selectedTopCard { get => selection.SelectedTopCard; set => selection.SelectedTopCard = value; }

    private bool inputEnabled { get => selection.InputEnabled; set => selection.InputEnabled = value; }
    private bool resetOrderOnNextSetCards;
    private bool tooltipsEnabled = true;
    private bool dimUsedCards = true;

    private void OnEnable()
    {
        if (cardsInArea == null)
        {
            return;
        }

        foreach (Card card in cardsInArea)
        {
            if (card != null)
            {
                SetupCardClickHandler(card);
            }
        }
        RefreshTooltipTargets();
    }

    private void OnDisable()
    {
        StopTopSwitchWithoutChangingHierarchy();
        UnsubscribeAllCardClickHandlers();
    }

    private void OnDestroy()
    {
        StopTopSwitchWithoutChangingHierarchy();
        UnsubscribeAllCardClickHandlers();
    }

    public void SetUsedCards(IEnumerable<Card> cards)
    {
        usedCards.Clear();
        if (cards != null)
        {
            foreach (Card card in cards)
            {
                if (card != null)
                {
                    usedCards.Add(card);
                }
            }
        }

        EnsureSelectedTopCard();
        ApplyTopSelection();
        ApplyUsedVisualState();
        RefreshTooltipTargets();
    }

    public void ResetForNewMatch(bool enableInput)
    {
        StopTopSwitchAndRecoverLayout();
        // A match can end while Unity is rebuilding the standalone player's UI.
        // Recreate our runtime listeners instead of assuming the listeners from
        // the previous match survived that lifecycle transition.
        UnsubscribeAllCardClickHandlers();
        usedCards.Clear();
        selectedTopCard = null;
        inputEnabled = enableInput;
        resetOrderOnNextSetCards = true;
        CardSelectionUtility.ClearSelections(cardsInArea);

        foreach (Card card in cardsInArea)
        {
            if (card != null)
            {
                SetupCardClickHandler(card);
            }
        }

        ApplyUsedVisualState();
        RefreshTooltipTargets();
    }

    /// <summary>
    /// Resets the current choice without changing which special cards are used.
    /// The no-use card is supplied by UIManager because it is displayed in the
    /// player's stack but is intentionally not part of PlayerState.SpecialCards.
    /// </summary>
    public void ResetSelectionTo(Card card)
    {
        StopTopSwitchAndRecoverLayout();
        CardSelectionUtility.ClearSelections(cardsInArea);
        selectedTopCard = card;

        if (selectedTopCard != null)
        {
            selectedTopCard.IsSelected = true;
            if (cardsInArea.Remove(selectedTopCard))
            {
                cardsInArea.Add(selectedTopCard);
            }
        }

        ApplyTopSelection();
        ApplyStackTargetsAndSiblingOrder();
        SnapStackToTargets();
        RefreshTooltipTargets();
    }

    public void SetTooltipsEnabled(bool enabled)
    {
        tooltipsEnabled = enabled;
        RefreshTooltipTargets();
    }

    public void SetUsedCardDimmingEnabled(bool enabled)
    {
        if (dimUsedCards == enabled)
        {
            return;
        }

        dimUsedCards = enabled;
        ApplyUsedVisualState();
    }

    public void SetInputEnabled(bool enabled)
    {
        if (inputEnabled == enabled)
        {
            // Reapply the actual Button/Image state after scene activation or a
            // platform-specific UI rebuild even when the logical flag is unchanged.
            ApplyUsedVisualState();
            return;
        }

        inputEnabled = enabled;
        ApplyUsedVisualState();
    }

    public override void SetCards(List<Card> cards, float totalDuration = 1.0f)
    {
        RebuildCardsInAreaKeepingSelection(cards);
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            SetupCardClickHandler(card);
        }
        ApplyUsedVisualState();
        CardsStackedPositionUpdate(totalDuration);
        RefreshTooltipTargets();
    }

    public override void SetCardsBySpeed(List<Card> cards, float moveSpeed, float turnSpeed)
    {
        RebuildCardsInAreaKeepingSelection(cards);
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            SetupCardClickHandler(card);
        }
        ApplyUsedVisualState();
        CardsStackedPositionUpdateBySpeed(moveSpeed, turnSpeed);
        RefreshTooltipTargets();
    }

    private void RebuildCardsInAreaKeepingSelection(List<Card> cards)
    {
        var previousCards = new HashSet<Card>(cardsInArea);
        var incomingCards = new List<Card>();
        var incomingSet = new HashSet<Card>();

        if (cards != null)
        {
            foreach (var card in cards)
            {
                if (card != null && incomingSet.Add(card))
                {
                    incomingCards.Add(card);
                }
            }
        }

        var nextCards = new List<Card>();
        if (resetOrderOnNextSetCards)
        {
            nextCards.AddRange(incomingCards);
            incomingSet.Clear();
            resetOrderOnNextSetCards = false;
        }
        else
        {
            foreach (var card in cardsInArea)
            {
                if (card != null && incomingSet.Remove(card))
                {
                    nextCards.Add(card);
                }
            }

            foreach (var card in incomingCards)
            {
                if (incomingSet.Contains(card))
                {
                    nextCards.Add(card);
                }
            }
        }

        foreach (var oldCard in previousCards)
        {
            if (oldCard != null && !nextCards.Contains(oldCard))
            {
                RemoveCardClickHandler(oldCard);
                oldCard.IsSelected = false;
                oldCard.UseSelectedYOffset = true;
            }
        }

        foreach (var newCard in nextCards)
        {
            if (newCard != null && !previousCards.Contains(newCard))
            {
                newCard.IsSelected = false;
            }
        }

        Card preselectedTopCard = null;
        foreach (Card card in nextCards)
        {
            if (card != null && card.IsSelected && IsCardAvailable(card))
            {
                preselectedTopCard = card;
            }
        }

        cardsInArea.Clear();
        cardsInArea.AddRange(nextCards);
        DisableSelectedYOffsetForStackCards();

        if (preselectedTopCard != null)
        {
            selectedTopCard = preselectedTopCard;
        }

        EnsureSelectedTopCard();
        ApplyTopSelection();
        RefreshTooltipTargets();
    }

    private void CardsStackedPositionUpdate(float totalDuration = 1.0f)
    {
        if (areaRect == null || cardsInArea == null || cardsInArea.Count == 0) return;

        int visibleCount = cardsInArea.Count;
        for (int idx = 0; idx < visibleCount; idx++)
        {
            var card = cardsInArea[idx];
            if (card == null) continue;
            card.UseSelectedYOffset = false;

            if (HasEnoughAuthoredSlots(visibleCount))
            {
                BindCardToAuthoredSlot(card, idx);
            }
            else
            {
                card.gameObject.transform.SetParent(areaRect);
                card.gameObject.transform.SetSiblingIndex(idx);
                card.TargetPosition = GetStackPosition(idx, visibleCount);
            }

            if (!card.MoveComplete || card.NeedsAnimationForCurrentTarget())
            {
                card.WaitAndMove(totalDuration / visibleCount * idx, totalDuration / visibleCount);
            }
        }
    }

    private void CardsStackedPositionUpdateBySpeed(float moveSpeed, float turnSpeed)
    {
        if (areaRect == null || cardsInArea == null || cardsInArea.Count == 0) return;

        int visibleCount = cardsInArea.Count;
        for (int idx = 0; idx < visibleCount; idx++)
        {
            var card = cardsInArea[idx];
            if (card == null) continue;
            card.UseSelectedYOffset = false;

            if (HasEnoughAuthoredSlots(visibleCount))
            {
                BindCardToAuthoredSlot(card, idx);
            }
            else
            {
                card.gameObject.transform.SetParent(areaRect);
                card.gameObject.transform.SetSiblingIndex(idx);
                card.TargetPosition = GetStackPosition(idx, visibleCount);
            }

            if (!card.MoveComplete || card.NeedsAnimationForCurrentTarget())
            {
                card.WaitAndMoveBySpeed(0, moveSpeed, turnSpeed);
            }
        }
    }

    private void SetupCardClickHandler(Card card) => View.Bind(card);

    private void RemoveCardClickHandler(Card card) => View.Unbind(card);

    private void UnsubscribeAllCardClickHandlers() => View.UnbindAll();

    private void StopTopSwitchAndRecoverLayout()
    {
        StopTopSwitchWithoutChangingHierarchy();

        ApplyStackTargetsAndSiblingOrder();
        SnapStackToTargets();
    }

    private void StopTopSwitchWithoutChangingHierarchy() => View.Stop();

    private void SwitchTopCard(Card selectedCard)
    {
        if (selectedCard == null || !cardsInArea.Contains(selectedCard) || !inputEnabled) return;
        if (View.IsSwitching || !AreStackCardsMoveComplete()) StopTopSwitchAndRecoverLayout();
        if (!selection.TrySwitch(cardsInArea, selectedCard)) return;
        ApplyStackTargetsAndSiblingOrder();
        RefreshTooltipTargets();
        View.PlaySwitch(cardsInArea, selectedTopCard, topSwitchMoveDuration, stackSettleMoveDuration,
            topSwitchArcLift, AreStackCardsMoveComplete, () =>
            {
                ApplyStackTargetsAndSiblingOrder();
                SnapStackToTargets();
            });
    }

    private void ApplyTopSelection() => selection.Apply(cardsInArea);

    private void RefreshTooltipTargets() => View.RefreshTooltips(cardsInArea, tooltipsEnabled);

    private void DisableSelectedYOffsetForStackCards()
    {
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            card.UseSelectedYOffset = false;
        }
    }

    private void ApplyStackTargetsAndSiblingOrder()
    {
        if (areaRect == null || cardsInArea == null)
        {
            return;
        }

        for (int idx = 0; idx < cardsInArea.Count; idx++)
        {
            Card card = cardsInArea[idx];
            if (card == null)
            {
                continue;
            }

            card.UseSelectedYOffset = false;
            if (HasEnoughAuthoredSlots(cardsInArea.Count))
            {
                BindCardToAuthoredSlot(card, idx);
            }
            else
            {
                card.gameObject.transform.SetParent(areaRect);
                card.gameObject.transform.SetSiblingIndex(idx);
                card.TargetPosition = GetStackPosition(idx, cardsInArea.Count);
            }
        }
    }

    private Vector2 GetStackPosition(int index, int visibleCount)
    {
        int depthFromTop = Mathf.Max(0, visibleCount - 1 - index);
        return new Vector2(
            -stackOverlapOffset * depthFromTop * 0.1f,
            stackOverlapOffset * depthFromTop * 0.1f
        );
    }

    private void SnapStackToTargets()
    {
        foreach (Card card in cardsInArea)
        {
            if (card == null)
            {
                continue;
            }

            card.UseSelectedYOffset = false;
            card.SnapToTargetPosition();
        }
    }

    private void EnsureSelectedTopCard() => selection.EnsureSelected(cardsInArea);

    private Card FindTopAvailableCard() => selection.FindTopAvailable(cardsInArea);

    private bool IsCardAvailable(Card card) => selection.IsAvailable(card);

    private void ApplyUsedVisualState()
    {
        foreach (Card card in cardsInArea)
        {
            ApplyCardUsageState(card);
        }
    }

    private void ApplyCardUsageState(Card card)
    {
        View.RenderUsage(card, usedCards.Contains(card), inputEnabled, dimUsedCards,
            usedSpecialCardTint, normalSpecialCardTint);
    }

    private bool AreStackCardsMoveComplete()
    {
        foreach (Card card in cardsInArea)
        {
            if (card != null && !card.MoveComplete)
            {
                return false;
            }
        }

        return true;
    }

    public void SetStackOverlapOffset(float offset)
    {
        stackOverlapOffset = offset;
    }
}
