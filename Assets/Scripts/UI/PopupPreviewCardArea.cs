using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Card click in this area shows cards as a popup preview.
/// This class does not move original cards; it creates preview clones.
/// </summary>
public class PopupPreviewCardArea : CardArea
{
    private static readonly Vector2 DiscardPopupReferenceSize = new Vector2(768f, 656f);
    private static readonly Rect[] DiscardRowSlots =
    {
        new Rect(160f, 16f, 592f, 144f),
        new Rect(160f, 176f, 592f, 144f),
        new Rect(160f, 336f, 592f, 144f),
        new Rect(160f, 496f, 592f, 144f)
    };
    private const float DiscardSlotHorizontalPadding = 18f;
    private const float DiscardSlotVerticalPadding = 14f;
    private const float MaxPreviewCardScale = 0.25f;

    private static readonly Suit[] PreviewSuitOrder =
    {
        Suit.Flowers,
        Suit.Birds,
        Suit.Wind,
        Suit.Moon
    };

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CardArea popupCardArea;
    [SerializeField] private Button closeButton;

    [Header("Preview Card")]
    [SerializeField] private Card previewCardPrefab;
    [SerializeField] private bool previewFaceUp = true;
    [SerializeField] private float previewMoveSpeed = 1200f;
    [SerializeField] private float previewTurnSpeed = 1080f;
    [SerializeField] private bool useSuitRowLayout = true;
    [SerializeField] private float closeButtonReservedHeight = 96f;
    [SerializeField] private float popupBottomPadding = 24f;

    private readonly List<Card> previewCards = new List<Card>();
    private readonly Dictionary<Card, UnityAction> sourceCardClickHandlers = new Dictionary<Card, UnityAction>();

    private void Start()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePopup);
            closeButton.onClick.AddListener(ClosePopup);
            closeButton.transform.SetAsLastSibling();
        }
    }

    public override void SetCards(List<Card> cards, float totalDuration = 1.0f)
    {
        base.SetCards(cards, totalDuration);
        BindSourceCardClickHandlers();
    }

    public override void SetCardsBySpeed(List<Card> cards, float moveSpeed, float turnSpeed)
    {
        base.SetCardsBySpeed(cards, moveSpeed, turnSpeed);
        BindSourceCardClickHandlers();
    }

    public void ShowPopup(Card focusedCard = null)
    {
        if (popupRoot == null || popupCardArea == null || previewCardPrefab == null)
        {
            Debug.LogWarning("PopupPreviewCardArea: popup references are not assigned.");
            return;
        }

        if (focusedCard != null && !focusedCard.MoveComplete)
        {
            return;
        }

        popupRoot.SetActive(true);
        FitPopupCardAreaToBackground();
        if (closeButton != null)
        {
            closeButton.transform.SetAsLastSibling();
        }

        RebuildPreviewCards(focusedCard);
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void BindSourceCardClickHandlers()
    {
        UnbindSourceCardClickHandlers();

        foreach (var source in cardsInArea)
        {
            if (source == null) continue;

            Button sourceButton = source.GetComponent<Button>();
            if (sourceButton == null) continue;

            Card captured = source;
            UnityAction action = () => ShowPopup(captured);
            sourceCardClickHandlers[captured] = action;
            sourceButton.onClick.AddListener(action);
        }
    }

    private void UnbindSourceCardClickHandlers()
    {
        foreach (var pair in sourceCardClickHandlers)
        {
            if (pair.Key == null) continue;

            Button sourceButton = pair.Key.GetComponent<Button>();
            if (sourceButton == null) continue;
            sourceButton.onClick.RemoveListener(pair.Value);
        }
        sourceCardClickHandlers.Clear();
    }

    private void RebuildPreviewCards(Card focusedCard)
    {
        ClearPreviewCards();

        var orderedSources = BuildOrderedSources(focusedCard);

        foreach (var source in orderedSources)
        {
            if (source == null || source.CardData == null) continue;

            Card preview = Instantiate(previewCardPrefab, popupCardArea.transform);
            preview.SetCardData(source.CardData);
            preview.Initialize();
            preview.IsSelectable = false;
            preview.IsSelected = false;
            preview.ForceSetFaceUp(previewFaceUp);

            Button previewButton = preview.GetComponent<Button>();
            if (previewButton != null)
            {
                // Keep full opacity; avoid Disabled Color fade on UI Button.
                previewButton.onClick.RemoveAllListeners();
                previewButton.interactable = true;
                previewButton.transition = Selectable.Transition.None;
            }

            previewCards.Add(preview);
        }

        if (useSuitRowLayout)
        {
            LayoutPreviewCardsBySuitRows();
        }
        else
        {
            popupCardArea.SetCardsBySpeed(previewCards, previewMoveSpeed, previewTurnSpeed);
        }
    }

    private List<Card> BuildOrderedSources(Card focusedCard)
    {
        if (!useSuitRowLayout)
        {
            var orderedSources = new List<Card>();
            if (focusedCard != null && cardsInArea.Contains(focusedCard))
            {
                orderedSources.Add(focusedCard);
            }

            foreach (var source in cardsInArea)
            {
                if (source == null) continue;
                if (source == focusedCard) continue;
                orderedSources.Add(source);
            }

            return orderedSources;
        }

        var sortedCards = cardsInArea
            .Where(card => card != null && card.CardData != null)
            .OrderBy(card => GetSuitSortOrder(card.CardData.suit))
            .ThenBy(card => (int)card.CardData.number)
            .ToList();

        return sortedCards;
    }

    private void LayoutPreviewCardsBySuitRows()
    {
        if (popupCardArea == null)
        {
            return;
        }

        if (popupCardArea.areaRect == null)
        {
            popupCardArea.SetCardsBySpeed(previewCards, previewMoveSpeed, previewTurnSpeed);
            return;
        }

        popupCardArea.cardsInArea.Clear();
        popupCardArea.cardsInArea.AddRange(previewCards);

        var rows = BuildSuitRows(previewCards);
        if (rows.Count == 0)
        {
            return;
        }

        if (TryLayoutPreviewCardsInDiscardSlots(rows))
        {
            return;
        }

        float contentHeight = Mathf.Max(
            0f,
            popupCardArea.areaRect.rect.height - Mathf.Max(0f, closeButtonReservedHeight) - Mathf.Max(0f, popupBottomPadding));
        float contentYOffset = (Mathf.Max(0f, popupBottomPadding) - Mathf.Max(0f, closeButtonReservedHeight)) * 0.5f;
        var rowHeights = rows.Select(GetMaxCardHeight).ToList();
        var rowCentersY = ComputeCenters(contentHeight, rowHeights, true);
        int siblingIndex = 0;

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var rowCards = rows[rowIndex];
            if (rowCards.Count == 0)
            {
                continue;
            }

            var rowWidths = rowCards.Select(card => GetCardWidth(card)).ToList();
            var rowCentersX = ComputeCenters(popupCardArea.areaRect.rect.width, rowWidths, false);

            for (int cardIndex = 0; cardIndex < rowCards.Count; cardIndex++)
            {
                var card = rowCards[cardIndex];
                if (card == null)
                {
                    continue;
                }

                card.transform.SetParent(popupCardArea.areaRect);
                card.transform.SetSiblingIndex(siblingIndex++);
                card.TargetPosition = new Vector2(rowCentersX[cardIndex], rowCentersY[rowIndex] + contentYOffset);

                if (card.MoveComplete)
                {
                    card.WaitAndMoveBySpeed(0f, previewMoveSpeed, previewTurnSpeed);
                }
            }
        }
    }

    private bool TryLayoutPreviewCardsInDiscardSlots(List<List<Card>> rows)
    {
        RectTransform targetArea = popupCardArea.areaRect;
        if (targetArea == null || rows == null || rows.Count == 0)
        {
            return false;
        }

        Rect areaRect = targetArea.rect;
        if (areaRect.width <= 0f || areaRect.height <= 0f)
        {
            return false;
        }

        int siblingIndex = 0;
        int rowCount = Mathf.Min(rows.Count, DiscardRowSlots.Length);

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var rowCards = rows[rowIndex];
            if (rowCards == null || rowCards.Count == 0)
            {
                continue;
            }

            Rect slot = GetScaledDiscardSlot(DiscardRowSlots[rowIndex], areaRect.size);
            Vector2 slotPadding = GetScaledDiscardPadding(areaRect.size);
            Vector2 usableSize = new Vector2(
                Mathf.Max(1f, slot.width - slotPadding.x * 2f),
                Mathf.Max(1f, slot.height - slotPadding.y * 2f));
            float cardScale = GetCardScaleToFit(rowCards, usableSize);
            var rowWidths = rowCards.Select(card => GetCardWidth(card, cardScale)).ToList();
            var rowCentersX = ComputeCenters(usableSize.x, rowWidths, false);

            for (int cardIndex = 0; cardIndex < rowCards.Count; cardIndex++)
            {
                var card = rowCards[cardIndex];
                if (card == null)
                {
                    continue;
                }

                RectTransform cardRect = card.GetCardRect();
                if (cardRect != null)
                {
                    cardRect.localScale = new Vector3(cardScale, cardScale, cardScale);
                }

                card.UseSelectedYOffset = false;
                card.transform.SetParent(targetArea);
                card.transform.SetSiblingIndex(siblingIndex++);
                float centerX = rowCentersX.Count > cardIndex ? rowCentersX[cardIndex] : 0f;
                card.TargetPosition = new Vector2(slot.center.x + centerX, slot.center.y);

                if (card.MoveComplete)
                {
                    card.WaitAndMoveBySpeed(0f, previewMoveSpeed, previewTurnSpeed);
                }
            }
        }

        return true;
    }

    private void FitPopupCardAreaToBackground()
    {
        if (popupRoot == null || popupCardArea == null || popupCardArea.areaRect == null)
        {
            return;
        }

        RectTransform popupRootRect = popupRoot.GetComponent<RectTransform>();
        RectTransform targetArea = popupCardArea.areaRect;
        if (popupRootRect == null || targetArea.parent != popupRootRect)
        {
            return;
        }

        targetArea.anchorMin = Vector2.zero;
        targetArea.anchorMax = Vector2.one;
        targetArea.pivot = new Vector2(0.5f, 0.5f);
        targetArea.anchoredPosition = Vector2.zero;
        targetArea.sizeDelta = Vector2.zero;
        targetArea.localScale = Vector3.one;
    }

    private Rect GetScaledDiscardSlot(Rect referenceSlot, Vector2 areaSize)
    {
        float scaleX = areaSize.x / DiscardPopupReferenceSize.x;
        float scaleY = areaSize.y / DiscardPopupReferenceSize.y;
        float x = (referenceSlot.x - DiscardPopupReferenceSize.x * 0.5f) * scaleX;
        float y = (DiscardPopupReferenceSize.y * 0.5f - referenceSlot.y - referenceSlot.height) * scaleY;
        return new Rect(x, y, referenceSlot.width * scaleX, referenceSlot.height * scaleY);
    }

    private Vector2 GetScaledDiscardPadding(Vector2 areaSize)
    {
        return new Vector2(
            DiscardSlotHorizontalPadding * areaSize.x / DiscardPopupReferenceSize.x,
            DiscardSlotVerticalPadding * areaSize.y / DiscardPopupReferenceSize.y);
    }

    private float GetCardScaleToFit(List<Card> rowCards, Vector2 usableSize)
    {
        float maxWidth = 0f;
        float maxHeight = 0f;

        foreach (var card in rowCards)
        {
            if (card == null)
            {
                continue;
            }

            RectTransform rect = card.GetCardRect();
            maxWidth = Mathf.Max(maxWidth, rect != null && rect.rect.width > 0f ? rect.rect.width : 100f);
            maxHeight = Mathf.Max(maxHeight, rect != null && rect.rect.height > 0f ? rect.rect.height : 150f);
        }

        if (maxWidth <= 0f || maxHeight <= 0f)
        {
            return MaxPreviewCardScale;
        }

        float widthScale = usableSize.x / maxWidth;
        float heightScale = usableSize.y / maxHeight;
        return Mathf.Max(0.01f, Mathf.Min(MaxPreviewCardScale, widthScale, heightScale));
    }

    private List<List<Card>> BuildSuitRows(List<Card> cards)
    {
        var rows = new List<List<Card>>();

        foreach (var suit in PreviewSuitOrder)
        {
            var row = cards
                .Where(card => card != null && card.CardData != null && card.CardData.suit == suit)
                .OrderBy(card => (int)card.CardData.number)
                .ToList();

            rows.Add(row);
        }

        var extras = cards
            .Where(card => card != null && card.CardData != null && !PreviewSuitOrder.Contains(card.CardData.suit))
            .OrderBy(card => GetSuitSortOrder(card.CardData.suit))
            .ThenBy(card => (int)card.CardData.number)
            .ToList();

        if (extras.Count > 0)
        {
            rows.Add(extras);
        }

        return rows;
    }

    private float GetCardWidth(Card card)
    {
        if (card == null)
        {
            return 100f;
        }

        RectTransform rect = card.GetCardRect();
        if (rect == null || rect.rect.width <= 0f)
        {
            return 100f;
        }

        return rect.rect.width * rect.localScale.x;
    }

    private float GetCardWidth(Card card, float scale)
    {
        if (card == null)
        {
            return 100f * scale;
        }

        RectTransform rect = card.GetCardRect();
        if (rect == null || rect.rect.width <= 0f)
        {
            return 100f * scale;
        }

        return rect.rect.width * scale;
    }

    private float GetMaxCardHeight(List<Card> cards)
    {
        float maxHeight = 0f;

        foreach (var card in cards)
        {
            if (card == null)
            {
                continue;
            }

            RectTransform rect = card.GetCardRect();
            if (rect == null || rect.rect.height <= 0f)
            {
                maxHeight = Mathf.Max(maxHeight, 150f);
                continue;
            }

            maxHeight = Mathf.Max(maxHeight, rect.rect.height * rect.localScale.y);
        }

        return maxHeight > 0f ? maxHeight : 150f;
    }

    private List<float> ComputeCenters(float areaSize, List<float> sizes, bool vertical)
    {
        var centers = new List<float>();
        int count = sizes.Count;

        if (count == 0)
        {
            return centers;
        }

        float total = 0f;
        for (int i = 0; i < count; i++)
        {
            total += sizes[i];
        }

        if (count == 1)
        {
            centers.Add(0f);
            return centers;
        }

        if (total <= areaSize)
        {
            float space = (areaSize - total) / (count + 1);
            float cursor = vertical ? areaSize / 2f - space : -areaSize / 2f + space;

            for (int i = 0; i < count; i++)
            {
                float half = sizes[i] / 2f;
                float center = vertical ? cursor - half : cursor + half;
                float min = -areaSize / 2f + half;
                float max = areaSize / 2f - half;

                center = min > max ? 0f : Mathf.Clamp(center, min, max);
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
            float overlap = (total - areaSize) / (count - 1);
            float center = vertical ? areaSize / 2f - sizes[0] / 2f : -areaSize / 2f + sizes[0] / 2f;

            for (int i = 0; i < count; i++)
            {
                float half = sizes[i] / 2f;
                float min = -areaSize / 2f + half;
                float max = areaSize / 2f - half;
                float clamped = min > max ? 0f : Mathf.Clamp(center, min, max);
                centers.Add(clamped);

                if (i + 1 < count)
                {
                    float nextHalf = sizes[i + 1] / 2f;
                    if (vertical)
                    {
                        center = center - (half + nextHalf) + overlap;
                    }
                    else
                    {
                        center = center + (half + nextHalf) - overlap;
                    }
                }
            }
        }

        return centers;
    }

    private int GetSuitSortOrder(Suit suit)
    {
        switch (suit)
        {
            case Suit.Flowers:
                return 0;
            case Suit.Birds:
                return 1;
            case Suit.Wind:
                return 2;
            case Suit.Moon:
                return 3;
            default:
                return 4;
        }
    }

    private void ClearPreviewCards()
    {
        for (int i = 0; i < previewCards.Count; i++)
        {
            if (previewCards[i] != null)
            {
                Destroy(previewCards[i].gameObject);
            }
        }
        previewCards.Clear();
    }

    private void OnDestroy()
    {
        UnbindSourceCardClickHandlers();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePopup);
        }
        ClearPreviewCards();
    }
}
