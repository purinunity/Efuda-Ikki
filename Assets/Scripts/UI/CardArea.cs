using System;
using System.Collections.Generic;
using UnityEngine;

// カードを保持し、エリア内レイアウトと選択状態を管理する基本クラス
public class CardArea : MonoBehaviour
{
    private const float FallbackCardWidth = 100f;
    private const float FallbackCardHeight = 150f;

    public List<Card> cardsInArea = new List<Card>();
    public RectTransform areaRect;
    public bool isHorizontal = true;
    public bool isVertical = false;

    // true: 後から配置したカードを背面側にする（重なり順を反転）
    public bool reverseOverlapOrder = false;

    public void Initialize()
    {
        cardsInArea.Clear();
    }

    public void Awake()
    {
        Initialize();
    }

    public void ClearCard()
    {
        for (int i = 0; i < cardsInArea.Count; i++)
        {
            var card = cardsInArea[i];
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }

        cardsInArea.Clear();
    }

    public virtual void SetCards(List<Card> cards, float totalDuration = 1.0f)
    {
        RebuildCardsInArea(cards);
        CardsPositionUpdate(totalDuration);
    }

    public virtual void SetCardsBySpeed(List<Card> cards, float moveSpeed, float turnSpeed)
    {
        RebuildCardsInArea(cards);
        CardsPositionUpdateBySpeed(moveSpeed, turnSpeed);
    }

    protected void RebuildCardsInArea(List<Card> cards)
    {
        var previousCards = new HashSet<Card>(cardsInArea);
        var nextCards = new List<Card>();
        var addedCards = new HashSet<Card>();

        if (cards != null)
        {
            foreach (var card in cards)
            {
                if (card != null && addedCards.Add(card))
                {
                    nextCards.Add(card);
                }
            }
        }

        cardsInArea.Clear();
        cardsInArea.AddRange(nextCards);

        foreach (var oldCard in previousCards)
        {
            if (oldCard != null && !cardsInArea.Contains(oldCard))
            {
                oldCard.IsSelected = false;
            }
        }

        foreach (var newCard in cardsInArea)
        {
            if (newCard != null && !previousCards.Contains(newCard))
            {
                newCard.IsSelected = false;
            }
        }
    }

    private void CardsPositionUpdate(float totalDuration = 1.0f)
    {
        ApplyLayout(
            animate: (card, idx, count) =>
            {
                if (!card.MoveComplete)
                {
                    return;
                }

                float perCardDuration = totalDuration / count;
                card.WaitAndMove(perCardDuration * idx, perCardDuration);
            });
    }

    private void CardsPositionUpdateBySpeed(float moveSpeed, float turnSpeed)
    {
        ApplyLayout(
            animate: (card, _, __) =>
            {
                if (!card.MoveComplete)
                {
                    return;
                }

                card.WaitAndMoveBySpeed(0f, moveSpeed, turnSpeed);
            });
    }

    // レイアウト計算と座標反映の共通処理
    private void ApplyLayout(Action<Card, int, int> animate)
    {
        if (areaRect == null || cardsInArea == null || cardsInArea.Count == 0)
        {
            return;
        }

        bool both = isVertical && isHorizontal;
        bool onlyHorizontal = isHorizontal && !isVertical;
        bool onlyVertical = isVertical && !isHorizontal;

        List<Card> validCards = new List<Card>();
        List<float> widths = new List<float>();
        List<float> heights = new List<float>();

        float areaWidth = areaRect.rect.width;
        float areaHeight = areaRect.rect.height;

        foreach (var card in cardsInArea)
        {
            if (card == null)
            {
                continue;
            }

            validCards.Add(card);

            RectTransform rect = card.GetCardRect();
            float w = (rect != null && rect.rect.width > 0f) ? rect.rect.width * rect.localScale.x : FallbackCardWidth;
            float h = (rect != null && rect.rect.height > 0f) ? rect.rect.height * rect.localScale.y : FallbackCardHeight;

            widths.Add(w);
            heights.Add(h);
        }

        int visibleCount = validCards.Count;
        if (visibleCount == 0)
        {
            return;
        }

        List<float> centersX = ComputeCenters(areaWidth, widths);
        List<float> centersY = ComputeCenters(areaHeight, heights, vertical: true);

        for (int idx = 0; idx < visibleCount; idx++)
        {
            var card = validCards[idx];
            float centerX = centersX.Count > idx ? centersX[idx] : 0f;
            float centerY = centersY.Count > idx ? centersY[idx] : 0f;

            card.gameObject.transform.SetParent(areaRect);
            ApplySiblingOrder(card.gameObject.transform);

            if (both)
            {
                card.TargetPosition = new Vector2(centerX, centerY);
            }
            else if (onlyHorizontal)
            {
                card.TargetPosition = new Vector2(centerX, 0f);
            }
            else if (onlyVertical)
            {
                card.TargetPosition = new Vector2(0f, centerY);
            }
            else
            {
                card.TargetPosition = Vector2.zero;
            }

            animate(card, idx, visibleCount);
        }
    }

    private List<float> ComputeCenters(float areaSize, List<float> sizes, bool vertical = false)
    {
        List<float> centers = new List<float>();
        int n = sizes.Count;

        if (n == 0)
        {
            return centers;
        }

        float total = 0f;
        for (int i = 0; i < n; i++)
        {
            total += sizes[i];
        }

        if (n == 1)
        {
            centers.Add(0f);
            return centers;
        }

        if (total <= areaSize)
        {
            // 収まる場合: 等間隔に配置
            float space = (areaSize - total) / (n + 1);
            float cursor = vertical ? areaSize / 2f - space : -areaSize / 2f + space;

            for (int i = 0; i < n; i++)
            {
                float half = sizes[i] / 2f;
                float center = vertical ? cursor - half : cursor + half;
                float min = -areaSize / 2f + half;
                float max = areaSize / 2f - half;

                if (min > max)
                {
                    center = 0f;
                }
                else
                {
                    center = Mathf.Clamp(center, min, max);
                }

                centers.Add(center);

                if (vertical)
                {
                    cursor = center - half - space;
                }
                else
                {
                    cursor = center + half + space;
                }
            }
        }
        else
        {
            // 収まらない場合: オーバー分を重なりとして按分
            float overlap = (total - areaSize) / (n - 1);
            float center = vertical ? areaSize / 2f - sizes[0] / 2f : -areaSize / 2f + sizes[0] / 2f;

            for (int i = 0; i < n; i++)
            {
                float half = sizes[i] / 2f;
                float min = -areaSize / 2f + half;
                float max = areaSize / 2f - half;
                float centerClamped = (min > max) ? 0f : Mathf.Clamp(center, min, max);

                centers.Add(centerClamped);

                if (i + 1 < n)
                {
                    if (vertical)
                    {
                        center = center - (half + sizes[i + 1] / 2f) + overlap;
                    }
                    else
                    {
                        center = center + (half + sizes[i + 1] / 2f) - overlap;
                    }
                }
            }
        }

        return centers;
    }

    private void ApplySiblingOrder(Transform cardTransform)
    {
        if (reverseOverlapOrder)
        {
            cardTransform.SetAsFirstSibling();
            return;
        }

        cardTransform.SetAsLastSibling();
    }

    public Card DrawCard()
    {
        if (cardsInArea.Count > 0)
        {
            Card drawnCard = cardsInArea[0];
            cardsInArea.RemoveAt(0);
            Debug.Log($"Drawn Card: {drawnCard.GetCardNumber()} of {drawnCard.GetCardSuit()}");
            return drawnCard;
        }

        Debug.Log("No cards left to draw.");
        return null;
    }

    public Card DrawCard(Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("DrawCard called with null card.");
            return null;
        }

        if (cardsInArea.Remove(card))
        {
            Debug.Log($"Card drawn: {card.GetCardNumber()} of {card.GetCardSuit()}");
            return card;
        }

        Debug.Log("Card not found in the list.");
        return null;
    }

    public List<Card> GetSelectedCardData()
    {
        List<Card> selectedCards = new List<Card>(cardsInArea.Count);

        foreach (var card in cardsInArea)
        {
            if (card != null && card.IsSelected)
            {
                selectedCards.Add(card);
            }
        }

        return selectedCards;
    }
}
