using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Card click in this area shows cards as a popup preview.
/// This class does not move original cards; it creates preview clones.
/// </summary>
public class PopupPreviewCardArea : CardArea
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CardArea popupCardArea;
    [SerializeField] private Button closeButton;

    [Header("Preview Card")]
    [SerializeField] private Card previewCardPrefab;
    [SerializeField] private bool previewFaceUp = true;
    [SerializeField] private float previewMoveSpeed = 1200f;
    [SerializeField] private float previewTurnSpeed = 1080f;

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

        popupCardArea.SetCardsBySpeed(previewCards, previewMoveSpeed, previewTurnSpeed);
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
