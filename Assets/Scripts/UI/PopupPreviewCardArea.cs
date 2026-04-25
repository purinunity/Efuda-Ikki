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

        popupRoot.SetActive(true);
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

        var rowHeights = rows.Select(GetMaxCardHeight).ToList();
        var rowCentersY = ComputeCenters(popupCardArea.areaRect.rect.height, rowHeights, true);
        int siblingIndex = 0;

        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var rowCards = rows[rowIndex];
            if (rowCards.Count == 0)
            {
                continue;
            }

            var rowWidths = rowCards.Select(GetCardWidth).ToList();
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
                card.TargetPosition = new Vector2(rowCentersX[cardIndex], rowCentersY[rowIndex]);

                if (card.MoveComplete)
                {
                    card.WaitAndMoveBySpeed(0f, previewMoveSpeed, previewTurnSpeed);
                }
            }
        }
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
