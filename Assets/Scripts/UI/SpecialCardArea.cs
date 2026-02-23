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
    private float stackOverlapOffset = 10f;
    private readonly Dictionary<Card, UnityAction> clickHandlers = new Dictionary<Card, UnityAction>();

    public override void SetCards(List<Card> cards, float totalDuration = 1.0f)
    {
        RebuildCardsInArea(cards);
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            SetupCardClickHandler(card);
        }
        CardsStackedPositionUpdate(totalDuration);
    }

    public override void SetCardsBySpeed(List<Card> cards, float moveSpeed, float turnSpeed)
    {
        RebuildCardsInArea(cards);
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            SetupCardClickHandler(card);
        }
        CardsStackedPositionUpdateBySpeed(moveSpeed, turnSpeed);
    }

    private void CardsStackedPositionUpdate(float totalDuration = 1.0f)
    {
        if (areaRect == null || cardsInArea == null || cardsInArea.Count == 0) return;

        int visibleCount = cardsInArea.Count;
        for (int idx = 0; idx < visibleCount; idx++)
        {
            var card = cardsInArea[idx];
            if (card == null) continue;

            card.gameObject.transform.SetParent(areaRect);
            card.gameObject.transform.SetSiblingIndex(idx);

            Vector2 stackPosition = new Vector2(
                stackOverlapOffset * idx * 0.1f,
                -stackOverlapOffset * idx * 0.1f
            );
            card.TargetPosition = stackPosition;

            if (card.MoveComplete)
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

            card.gameObject.transform.SetParent(areaRect);
            card.gameObject.transform.SetSiblingIndex(idx);

            Vector2 stackPosition = new Vector2(
                stackOverlapOffset * idx * 0.1f,
                -stackOverlapOffset * idx * 0.1f
            );
            card.TargetPosition = stackPosition;

            if (card.MoveComplete)
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
        cardButton.interactable = true;
    }

    private void SwitchTopCard(Card selectedCard)
    {
        if (selectedCard == null || !cardsInArea.Contains(selectedCard)) return;

        int topIndex = cardsInArea.Count - 1;
        int selectedIndex = cardsInArea.IndexOf(selectedCard);

        if (selectedIndex == topIndex && cardsInArea.Count > 1)
        {
            // トップを押したら末尾循環: 次カードをトップにする
            cardsInArea.RemoveAt(topIndex);
            cardsInArea.Insert(0, selectedCard);
        }
        else
        {
            // トップ以外を押したらそのカードをトップへ
            cardsInArea.RemoveAt(selectedIndex);
            cardsInArea.Add(selectedCard);
        }

        for (int idx = 0; idx < cardsInArea.Count; idx++)
        {
            var card = cardsInArea[idx];
            if (card != null)
            {
                card.gameObject.transform.SetSiblingIndex(idx);
            }
        }

        StartCoroutine(AnimateCardToTopWithArc(selectedCard));
    }

    private IEnumerator AnimateCardToTopWithArc(Card card)
    {
        Vector2 currentPos = card.TargetPosition;
        Vector2 arcTopPos = new Vector2(currentPos.x, currentPos.y + 120f);
        card.TargetPosition = arcTopPos;
        card.MoveAndTurnCard(0.18f);
        yield return new WaitUntil(() => card.MoveComplete);

        int finalIndex = cardsInArea.Count - 1;
        Vector2 finalPos = new Vector2(
            stackOverlapOffset * finalIndex * 0.1f,
            -stackOverlapOffset * finalIndex * 0.1f
        );
        card.TargetPosition = finalPos;
        card.MoveAndTurnCard(0.22f);
        yield return new WaitUntil(() => card.MoveComplete);
    }

    public void SetStackOverlapOffset(float offset)
    {
        stackOverlapOffset = offset;
    }
}
