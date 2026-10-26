using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Self-contained, prefab-authored 4x13 discard preview.</summary>
[DisallowMultipleComponent]
public sealed class DiscardPreviewPanel : MonoBehaviour
{
    [SerializeField] private Button closeButton;
    [SerializeField] private Image[] previewSlots = new Image[52];
    [SerializeField] private ShowdownCutInAssetSet visualAssets;
    [SerializeField] private bool previewFaceUp = true;
    [SerializeField] private bool useSuitRowLayout = true;

    private static readonly Suit[] SuitOrder = { Suit.Birds, Suit.Flowers, Suit.Wind, Suit.Moon };
    private bool wired;

    private void Awake()
    {
        Wire();
    }

    private void OnEnable() => Wire();
    private void OnDisable() => Unwire();
    private void OnDestroy() => Unwire();

    public void Show(IEnumerable<Card> sourceCards, Card focusedCard = null)
    {
        List<Card> source = sourceCards != null
            ? sourceCards.Where(card => card != null && card.CardData != null).ToList()
            : new List<Card>();
        ClearSlots();

        if (useSuitRowLayout)
        {
            foreach (Card card in source)
            {
                if (!TryGetSuitSlotIndex(card.CardData, out int slotIndex)) continue;
                SetSlot(previewSlots[slotIndex], card);
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

    private static int GetSuitOrder(Suit suit)
    {
        for (int i = 0; i < SuitOrder.Length; i++) if (SuitOrder[i] == suit) return i;
        return SuitOrder.Length;
    }

    private static bool TryGetSuitSlotIndex(CardData cardData, out int slotIndex)
    {
        slotIndex = -1;
        if (cardData == null) return false;

        int row = GetSuitOrder(cardData.suit);
        int column = (int)cardData.number - 1;
        if (row < 0 || row >= SuitOrder.Length || column < 0 || column >= 13) return false;

        slotIndex = row * 13 + column;
        return true;
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
            ? (previewFaceUp ? BattleGroundVisualTheme.ResolveFace(card.CardData) : BattleGroundVisualTheme.ResolveBack(card.CardData))
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
