using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reuses four authored suit rows, centering each row's actual cards.</summary>
[DisallowMultipleComponent]
public sealed class DiscardPreviewPanel : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Image[] previewSlots = new Image[52];
    [SerializeField] private ShowdownCutInAssetSet visualAssets;
    [SerializeField] private bool previewFaceUp = true;
    [SerializeField] private bool useSuitRowLayout = true;

    private static readonly Suit[] SuitOrder = { Suit.Birds, Suit.Flowers, Suit.Wind, Suit.Moon };
    private const float PreferredCardGapRatio = 0.15f;
    private bool wired;
    private Vector2[] authoredAnchorMin;
    private Vector2[] authoredAnchorMax;
    private float[] authoredCenters;
    private float[] authoredWidths;

    private void Awake()
    {
        Wire();
    }

    private void OnEnable() => Wire();
    private void OnDisable()
    {
        RestoreAnchors();
        Unwire();
    }
    private void OnDestroy() => Unwire();

    public void Show(IEnumerable<Card> sourceCards, Card focusedCard = null)
    {
        List<Card> source = sourceCards != null
            ? sourceCards.Where(card => card != null && card.CardData != null).ToList()
            : new List<Card>();
        ClearSlots();

        if (useSuitRowLayout)
        {
            RestoreAnchors();
            for (int row = 0; TryCacheAuthoredAnchors() && row < SuitOrder.Length; row++)
            {
                List<Card> cards = source.Where(card => card.CardData.suit == SuitOrder[row] &&
                        (int)card.CardData.number >= 1 && (int)card.CardData.number <= 13)
                    .Distinct().OrderBy(card => (int)card.CardData.number).Take(13).ToList();
                int first = row * 13;
                float center = (authoredCenters[first] + authoredCenters[first + 12]) * 0.5f;
                float cardWidth = 0f;
                for (int slot = first; slot < first + 13; slot++)
                    cardWidth = Mathf.Max(cardWidth, authoredWidths[slot]);
                float span = Mathf.Abs(authoredCenters[first + 12] - authoredCenters[first]);
                float spacing = cards.Count > 1
                    ? Mathf.Min(cardWidth * (1f + PreferredCardGapRatio), span / (cards.Count - 1))
                    : 0f;
                for (int i = 0; i < cards.Count; i++)
                {
                    int index = first + i;
                    float offset = center + (i - (cards.Count - 1) * 0.5f) * spacing - authoredCenters[index];
                    RectTransform rect = previewSlots[index].rectTransform;
                    rect.anchorMin = authoredAnchorMin[index] + new Vector2(offset, 0f);
                    rect.anchorMax = authoredAnchorMax[index] + new Vector2(offset, 0f);
                    SetSlot(previewSlots[index], cards[i]);
                }
            }
        }
        else
        {
            List<Card> ordered = new List<Card>();
            if (focusedCard != null && source.Contains(focusedCard)) ordered.Add(focusedCard);
            ordered.AddRange(source.Where(card => card != focusedCard));
            for (int index = 0; index < previewSlots.Length && index < ordered.Count; index++)
                SetSlot(previewSlots[index], ordered[index]);
        }

        gameObject.SetActive(true);
    }

    public void Close()
    {
        RestoreAnchors();
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void Wire()
    {
        if (wired || closeButton == null) return;
        CloseButtonStyle.Apply(closeButton,
            visualAssets != null ? visualAssets.closeButton : null,
            visualAssets != null ? visualAssets.closeButtonHighlighted : null);
        closeButton.onClick.AddListener(Close);
        wired = true;
    }

    private void Unwire()
    {
        if (!wired) return;
        closeButton?.onClick.RemoveListener(Close);
        wired = false;
    }

    private bool TryCacheAuthoredAnchors()
    {
        if (authoredAnchorMin != null) return true;
        if (previewSlots == null || previewSlots.Length != 52 || previewSlots.Any(slot => slot == null)) return false;
        authoredAnchorMin = new Vector2[previewSlots.Length];
        authoredAnchorMax = new Vector2[previewSlots.Length];
        authoredCenters = new float[previewSlots.Length];
        authoredWidths = new float[previewSlots.Length];
        for (int i = 0; i < previewSlots.Length; i++)
        {
            RectTransform rect = previewSlots[i].rectTransform;
            authoredAnchorMin[i] = rect.anchorMin;
            authoredAnchorMax[i] = rect.anchorMax;
            float parentWidth = (rect.parent as RectTransform)?.rect.width ?? 0f;
            authoredWidths[i] = parentWidth > 0f
                ? rect.rect.width / parentWidth
                : Mathf.Abs(rect.anchorMax.x - rect.anchorMin.x);
            authoredCenters[i] = (rect.anchorMin.x + rect.anchorMax.x) * 0.5f +
                (parentWidth > 0f ? rect.anchoredPosition.x / parentWidth : 0f);
        }
        return true;
    }

    private void RestoreAnchors()
    {
        if (authoredAnchorMin == null) return;
        for (int i = 0; i < previewSlots.Length; i++)
        {
            if (previewSlots[i] == null) continue;
            previewSlots[i].rectTransform.anchorMin = authoredAnchorMin[i];
            previewSlots[i].rectTransform.anchorMax = authoredAnchorMax[i];
        }
    }

    private void ClearSlots()
    {
        if (previewSlots == null) return;
        foreach (Image slot in previewSlots) SetSlot(slot, null);
    }

    private void SetSlot(Image slot, Card card)
    {
        if (slot == null) return;

        bool visible = card != null && card.CardData != null;
        slot.sprite = visible
            ? (previewFaceUp ? card.DisplayFaceSprite : card.DisplayBackSprite)
            : null;
        slot.color = visible ? Color.white : Color.clear;
        slot.preserveAspect = true;
        slot.raycastTarget = false;
        slot.gameObject.SetActive(visible);
    }

    private void OnValidate()
    {
        if (closeButton == null || visualAssets == null || previewSlots == null ||
            previewSlots.Length != 52 || previewSlots.Any(slot => slot == null))
            Debug.LogWarning("DiscardPreviewPanel: assign Close, assets, and all 52 authored slots.", this);
    }
}
