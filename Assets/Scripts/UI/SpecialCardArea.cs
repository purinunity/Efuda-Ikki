using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [SerializeField] private Color usedSpecialCardTint = new Color(0.45f, 0.45f, 0.45f, 0.65f);
    [SerializeField] private Color normalSpecialCardTint = Color.white;

    private readonly Dictionary<Card, UnityAction> clickHandlers = new Dictionary<Card, UnityAction>();
    private readonly HashSet<Card> usedCards = new HashSet<Card>();
    private Card selectedTopCard;
    private Coroutine topSwitchCoroutine;
    private bool inputEnabled = true;

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
    }

    public void SetInputEnabled(bool enabled)
    {
        if (inputEnabled == enabled)
        {
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

        foreach (var oldCard in previousCards)
        {
            if (oldCard != null && !nextCards.Contains(oldCard))
            {
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

            card.gameObject.transform.SetParent(areaRect);
            card.gameObject.transform.SetSiblingIndex(idx);

            card.TargetPosition = GetStackPosition(idx, visibleCount);

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

            card.gameObject.transform.SetParent(areaRect);
            card.gameObject.transform.SetSiblingIndex(idx);

            card.TargetPosition = GetStackPosition(idx, visibleCount);

            if (!card.MoveComplete || card.NeedsAnimationForCurrentTarget())
            {
                card.WaitAndMoveBySpeed(0, moveSpeed, turnSpeed);
            }
        }
    }

    private void SetupCardClickHandler(Card card)
    {
        Button cardButton = card.GetComponent<Button>();
        if (cardButton == null) return;

        if (clickHandlers.TryGetValue(card, out var oldHandler))
        {
            cardButton.onClick.RemoveListener(oldHandler);
        }

        UnityAction newHandler = () => SwitchTopCard(card);
        clickHandlers[card] = newHandler;
        cardButton.onClick.AddListener(newHandler);
        ApplyCardUsageState(card);
    }

    private void SwitchTopCard(Card selectedCard)
    {
        if (selectedCard == null || !cardsInArea.Contains(selectedCard)) return;
        if (!inputEnabled) return;
        if (topSwitchCoroutine != null || !AreStackCardsMoveComplete()) return;

        int topIndex = cardsInArea.Count - 1;
        int selectedIndex = cardsInArea.IndexOf(selectedCard);
        bool selectedCardUsed = !IsCardAvailable(selectedCard);

        if (selectedCardUsed && selectedIndex != topIndex)
        {
            return;
        }

        if (selectedIndex == topIndex && cardsInArea.Count > 1)
        {
            // トップを押したら末尾循環: 次カードをトップにする
            cardsInArea.RemoveAt(topIndex);
            cardsInArea.Insert(0, selectedCard);
        }
        else
        {
            if (selectedCardUsed)
            {
                return;
            }

            // トップ以外を押したらそのカードをトップへ
            cardsInArea.RemoveAt(selectedIndex);
            cardsInArea.Add(selectedCard);
        }

        selectedTopCard = FindTopAvailableCard();
        EnsureSelectedTopCard();
        ApplyTopSelection();
        ApplyStackTargetsAndSiblingOrder();

        if (topSwitchCoroutine != null)
        {
            StopCoroutine(topSwitchCoroutine);
        }

        topSwitchCoroutine = StartCoroutine(AnimateStackSwitch(selectedTopCard));
    }

    private void ApplyTopSelection()
    {
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            card.UseSelectedYOffset = false;
            card.IsSelected = card == selectedTopCard && IsCardAvailable(card);
        }
    }

    private void DisableSelectedYOffsetForStackCards()
    {
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            card.UseSelectedYOffset = false;
        }
    }

    private IEnumerator AnimateCardToTopWithArc(Card card)
    {
        if (card == null)
        {
            yield break;
        }

        card.MoveAlongArcToTarget(topSwitchMoveDuration, topSwitchArcLift);
        yield return new WaitUntil(() => card.MoveComplete);
        card.SnapToTargetPosition();
    }

    private IEnumerator AnimateStackSwitch(Card featuredCard)
    {
        for (int idx = 0; idx < cardsInArea.Count; idx++)
        {
            Card card = cardsInArea[idx];
            if (card == null || card == featuredCard)
            {
                continue;
            }

            if (!card.MoveComplete || card.NeedsAnimationForCurrentTarget())
            {
                card.MoveAndTurnCard(stackSettleMoveDuration);
            }
        }

        yield return AnimateCardToTopWithArc(featuredCard);
        yield return new WaitUntil(AreStackCardsMoveComplete);
        ApplyStackTargetsAndSiblingOrder();
        SnapStackToTargets();
        topSwitchCoroutine = null;
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
            card.gameObject.transform.SetParent(areaRect);
            card.gameObject.transform.SetSiblingIndex(idx);
            card.TargetPosition = GetStackPosition(idx, cardsInArea.Count);
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

    private void EnsureSelectedTopCard()
    {
        if (cardsInArea == null || cardsInArea.Count == 0)
        {
            selectedTopCard = null;
            return;
        }

        if (!IsCardAvailable(selectedTopCard) || !cardsInArea.Contains(selectedTopCard))
        {
            selectedTopCard = FindTopAvailableCard();
        }

        if (selectedTopCard != null && cardsInArea.Remove(selectedTopCard))
        {
            cardsInArea.Add(selectedTopCard);
        }
    }

    private Card FindTopAvailableCard()
    {
        if (cardsInArea == null)
        {
            return null;
        }

        for (int idx = cardsInArea.Count - 1; idx >= 0; idx--)
        {
            Card card = cardsInArea[idx];
            if (IsCardAvailable(card))
            {
                return card;
            }
        }

        return null;
    }

    private bool IsCardAvailable(Card card)
    {
        return card != null && !usedCards.Contains(card);
    }

    private void ApplyUsedVisualState()
    {
        foreach (Card card in cardsInArea)
        {
            ApplyCardUsageState(card);
        }
    }

    private void ApplyCardUsageState(Card card)
    {
        if (card == null)
        {
            return;
        }

        bool used = usedCards.Contains(card);
        Image image = card.GetComponent<Image>();
        if (image != null)
        {
            image.color = used ? usedSpecialCardTint : normalSpecialCardTint;
        }

        Button cardButton = card.GetComponent<Button>();
        if (cardButton != null)
        {
            // 見た目と入力可否は SwitchTopCard 側で管理する。
            // 使用済み札が山の一番上でも、クリックで次の札へ送れるようにする。
            cardButton.interactable = true;
        }
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
