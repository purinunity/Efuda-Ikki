using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class SpecialCardSelectPanel : MonoBehaviour
{
    [FormerlySerializedAs("allCards")]
    [SerializeField] private Cards specialCardsDeck;
    [SerializeField] private TextMeshProUGUI selectedCountText;
    [SerializeField] private LimitedSelectableCardArea selectionCardArea;
    [SerializeField] private int maxSelectableSpecialCards = 4;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Color unlockedCardColor = Color.white;
    [SerializeField] private Color lockedCardColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [Header("Scene-authored slots")]
    [SerializeField] private RectTransform[] selectableCardSlots;
    [SerializeField] private List<Image> selectedDisplayImages = new List<Image>();
    public TitleUIManager titleUIManager;

    private GameModeData currentGameModeData;
    private readonly Dictionary<Card, Button> subscribedCardButtons =
        new Dictionary<Card, Button>();
    private Button subscribedStartGameButton;
    private Button subscribedBackButton;
    private bool started;

    private void Start()
    {
        started = true;
        SubscribePanelButtons();
        CreateSpecialCardUI();
        ValidateDisplaySlots();
        ConfigureSelectionLimiter();
        ResetSelectionForOpen();
    }

    private void OnEnable()
    {
        if (!started)
        {
            return;
        }

        SubscribePanelButtons();
        SubscribeDisplayedCards();
    }

    private void OnDisable()
    {
        UnsubscribePanelButtons();
        UnsubscribeAllCards();
    }

    private void OnDestroy()
    {
        UnsubscribePanelButtons();
        UnsubscribeAllCards();
    }

    private void CreateSpecialCardUI()
    {
        if (specialCardsDeck == null || selectionCardArea == null) return;

        List<Card> cardsToDisplay = new List<Card>();
        for (int i = 0; i < specialCardsDeck.cardList.Count; i++)
        {
            Card card = specialCardsDeck.cardList[i];
            if (card == null) continue;
            if (SpecialCardResolver.IsNoUseSpecialCard(card.CardData) ||
                SpecialCardResolver.IsCpuOnlySpecialCard(card.CardData))
            {
                card.gameObject.SetActive(false);
                continue;
            }

            card.gameObject.SetActive(true);
            card.ForceSetFaceUp(true);
            SetupCardSelection(card);
            cardsToDisplay.Add(card);
        }

        cardsToDisplay.Sort(CompareByUnlockOrder);
        selectionCardArea.SetCards(cardsToDisplay, 0.5f);
        LayoutCardsInGrid(cardsToDisplay);
        foreach (var card in cardsToDisplay)
        {
            card.IsSelectable = true;
        }

    }

    private void SetupCardSelection(Card card)
    {
        if (card == null)
        {
            return;
        }

        Button cardButton = card.GetComponent<Button>();
        if (cardButton == null) return;

        if (subscribedCardButtons.TryGetValue(card, out Button subscribedButton))
        {
            if (subscribedButton == cardButton)
            {
                return;
            }

            subscribedButton?.onClick.RemoveListener(HandleCardSelectionChanged);
        }

        subscribedCardButtons[card] = cardButton;
        cardButton.onClick.AddListener(HandleCardSelectionChanged);
    }

    private void LayoutCardsInGrid(IReadOnlyList<Card> cards)
    {
        if (selectionCardArea == null || cards == null || cards.Count == 0)
        {
            return;
        }

        for (int index = 0; index < cards.Count; index++)
        {
            Card card = cards[index];
            if (card == null)
            {
                continue;
            }

            if (selectableCardSlots == null || index >= selectableCardSlots.Length ||
                selectableCardSlots[index] == null)
            {
                Debug.LogError($"SpecialCardSelectPanel: selectable card slot {index + 1} is not assigned.", this);
                continue;
            }

            RectTransform slot = selectableCardSlots[index];

            // The slot owns the layout.  Cards remain runtime content, while their
            // position, anchor and ordering can be edited directly in the scene.
            card.transform.SetParent(slot, false);
            FitCardToSlot(card, slot);
            card.UseSelectedYOffset = false;
            card.TargetPosition = Vector2.zero;
            card.SnapToTargetPosition();
        }

        RefreshSelectionVisuals(cards);
    }

    private static void FitCardToSlot(Card card, RectTransform slot)
    {
        if (card == null || slot == null)
        {
            return;
        }

        RectTransform cardRect = card.GetComponent<RectTransform>();
        Image cardImage = card.GetComponent<Image>();
        if (cardRect == null || cardImage == null || cardImage.sprite == null)
        {
            return;
        }

        // Card.ForceSetFaceUp uses the sprite's native size.  Derive the scale
        // from the scene-authored slot so changing a slot in the Inspector is
        // enough to change both placement and card size.
        cardImage.SetNativeSize();
        Vector2 nativeSize = cardRect.rect.size;
        Vector2 slotSize = slot.rect.size;
        if (nativeSize.x <= 0f || nativeSize.y <= 0f || slotSize.x <= 0f || slotSize.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(slotSize.x / nativeSize.x, slotSize.y / nativeSize.y);
        card.DisplayScale = scale;
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.localScale = Vector3.one * scale;
    }

    private void ValidateDisplaySlots()
    {
        if (selectedDisplayImages == null || selectedDisplayImages.Count < maxSelectableSpecialCards)
        {
            Debug.LogError("SpecialCardSelectPanel: selected display slots are not assigned.", this);
        }
    }

    private void RefreshSelectionVisuals(IReadOnlyList<Card> cards)
    {
        int displayIndex = 0;
        if (cards != null)
        {
            foreach (Card card in cards)
            {
                if (card == null || card.CardData == null) continue;
                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null && card.IsFaceUp)
                {
                    Sprite selectedSprite = card.IsSelected
                        ? GetSelectedCardSprite(card.CardData)
                        : null;
                    cardImage.sprite = selectedSprite != null
                        ? selectedSprite
                        : BattleGroundVisualTheme.ResolveFace(card.CardData);
                    cardImage.color = Color.white;
                }

                if (!card.IsSelected || displayIndex >= selectedDisplayImages.Count) continue;
                Image displayImage = selectedDisplayImages[displayIndex++];
                displayImage.sprite = BattleGroundVisualTheme.ResolveFace(card.CardData);
                displayImage.color = Color.white;
            }
        }

        for (int i = displayIndex; i < selectedDisplayImages.Count; i++)
        {
            selectedDisplayImages[i].sprite = null;
            selectedDisplayImages[i].color = Color.clear;
        }
    }

    private static Sprite GetSelectedCardSprite(CardData cardData)
    {
        if (!SpecialCardResolver.TryGetSpecialCardId(cardData, out SpecialCardResolver.SpecialCardId id))
        {
            return null;
        }

        string fileName;
        switch (id)
        {
            case SpecialCardResolver.SpecialCardId.Aiko: fileName = "aiko_s"; break;
            case SpecialCardResolver.SpecialCardId.Seal: fileName = "seal_s"; break;
            case SpecialCardResolver.SpecialCardId.Bonus5: fileName = "bonus_05_s"; break;
            case SpecialCardResolver.SpecialCardId.Curse: fileName = "curse_s"; break;
            case SpecialCardResolver.SpecialCardId.Bonus10: fileName = "bonus_10_s"; break;
            case SpecialCardResolver.SpecialCardId.Bonus15: fileName = "bonus_15_s"; break;
            case SpecialCardResolver.SpecialCardId.DoubleScore: fileName = "double_score_s"; break;
            case SpecialCardResolver.SpecialCardId.Bet: fileName = "bet_s"; break;
            case SpecialCardResolver.SpecialCardId.Rain: fileName = "rain_s"; break;
            case SpecialCardResolver.SpecialCardId.Festival: fileName = "festival_s"; break;
            case SpecialCardResolver.SpecialCardId.Sunny: fileName = "sunny_s"; break;
            case SpecialCardResolver.SpecialCardId.Swap: fileName = "swap_s"; break;
            default: return null;
        }

        return Resources.Load<Sprite>("SpecialCardSelection/" + fileName);
    }

    private void SubscribeDisplayedCards()
    {
        if (selectionCardArea == null || selectionCardArea.cardsInArea == null)
        {
            return;
        }

        foreach (Card card in selectionCardArea.cardsInArea)
        {
            SetupCardSelection(card);
        }
    }

    private void UnsubscribeAllCards()
    {
        foreach (KeyValuePair<Card, Button> pair in subscribedCardButtons)
        {
            pair.Value?.onClick.RemoveListener(HandleCardSelectionChanged);
        }

        subscribedCardButtons.Clear();
    }

    private void SubscribePanelButtons()
    {
        UnsubscribePanelButtons();
        if (startGameButton != null)
        {
            subscribedStartGameButton = startGameButton;
            startGameButton.onClick.AddListener(HandleStartGameClicked);
        }

        if (backButton != null)
        {
            subscribedBackButton = backButton;
            backButton.onClick.AddListener(HandleBackClicked);
        }
    }

    private void UnsubscribePanelButtons()
    {
        subscribedStartGameButton?.onClick.RemoveListener(HandleStartGameClicked);
        subscribedBackButton?.onClick.RemoveListener(HandleBackClicked);
        subscribedStartGameButton = null;
        subscribedBackButton = null;
    }

    private void HandleStartGameClicked()
    {
        titleUIManager?.StartGame();
    }

    private void HandleBackClicked()
    {
        titleUIManager?.BackFromSpecialCardSelect();
    }

    private void ConfigureSelectionLimiter()
    {
        if (selectionCardArea == null)
        {
            return;
        }
        selectionCardArea.SetSelectionCountText(selectedCountText);
        selectionCardArea.SetMaxSelectableCount(maxSelectableSpecialCards);
        selectionCardArea.RefreshSelectionState();
        LayoutCardsInGrid(selectionCardArea.cardsInArea);
    }

    public void RefreshSelectionFromModeData()
    {
        currentGameModeData = GameModeManager.GetGameModeData();
        if (selectionCardArea == null || selectionCardArea.cardsInArea == null)
        {
            return;
        }

        HashSet<CardData> selectedCards = new HashSet<CardData>();
        if (currentGameModeData != null && currentGameModeData.SelectedSpecialCardDatas != null)
        {
            foreach (var cardData in currentGameModeData.SelectedSpecialCardDatas)
            {
                if (cardData != null)
                {
                    selectedCards.Add(cardData);
                }
            }
        }

        int unlockedSpecialCardCount = GameProgressStore.UnlockedSpecialCardCount;
        foreach (var card in selectionCardArea.cardsInArea)
        {
            if (card == null || card.CardData == null) continue;
            if (SpecialCardResolver.IsNoUseSpecialCard(card.CardData)) continue;

            bool unlocked = SpecialCardResolver.TryGetUnlockOrder(card.CardData, out int order) &&
                            order <= unlockedSpecialCardCount;
            card.IsSelected = unlocked && selectedCards.Contains(card.CardData);
            selectionCardArea.SetCardAvailability(card, unlocked);
            SetCardLockVisual(card, unlocked);
        }

        selectionCardArea.RefreshSelectionState();
        LayoutCardsInGrid(selectionCardArea.cardsInArea);
    }

    public void ResetSelectionForOpen()
    {
        currentGameModeData = GameModeManager.GetGameModeData();
        currentGameModeData?.ClearSpecialCards();

        if (selectionCardArea?.cardsInArea != null)
        {
            foreach (Card card in selectionCardArea.cardsInArea)
            {
                if (card == null)
                {
                    continue;
                }

                card.IsSelected = false;
            }
        }

        RefreshSelectionFromModeData();

        if (selectionCardArea?.cardsInArea == null)
        {
            return;
        }

        foreach (Card card in selectionCardArea.cardsInArea)
        {
            card?.SnapToTargetPosition();
        }
    }

    private void SyncSelectedCardsToModeData()
    {
        currentGameModeData = GameModeManager.GetGameModeData();
        if (currentGameModeData == null)
        {
            return;
        }

        currentGameModeData.ClearSpecialCards();

        if (selectionCardArea == null || selectionCardArea.cardsInArea == null)
        {
            return;
        }

        foreach (var card in selectionCardArea.cardsInArea)
        {
            if (card == null || card.CardData == null) continue;
            if (SpecialCardResolver.IsNoUseSpecialCard(card.CardData)) continue;
            if (!GameProgressStore.IsSpecialCardUnlocked(card.CardData)) continue;
            if (!card.IsSelected) continue;
            currentGameModeData.AddSpecialCard(card.CardData, maxSelectableSpecialCards);
        }
    }

    private void HandleCardSelectionChanged()
    {
        if (selectionCardArea != null)
        {
            LayoutCardsInGrid(selectionCardArea.cardsInArea);
        }

        SyncSelectedCardsToModeData();
    }

    private static int CompareByUnlockOrder(Card left, Card right)
    {
        int leftOrder = GetUnlockOrderOrLast(left);
        int rightOrder = GetUnlockOrderOrLast(right);
        return leftOrder.CompareTo(rightOrder);
    }

    private static int GetUnlockOrderOrLast(Card card)
    {
        return card != null &&
               SpecialCardResolver.TryGetUnlockOrder(card.CardData, out int order)
            ? order
            : int.MaxValue;
    }

    private void SetCardLockVisual(Card card, bool unlocked)
    {
        if (card == null)
        {
            return;
        }

        Image image = card.GetComponent<Image>();
        if (image != null)
        {
            image.color = unlocked ? unlockedCardColor : Color.white;
        }

        card.ForceSetFaceUp(unlocked);

        Button button = card.GetComponent<Button>();
        if (button != null)
        {
            // Keep pointer events active for tooltips; selection is blocked by IsSelectable.
            button.interactable = true;
        }

        SpecialCardTooltipTarget tooltipTarget = card.GetComponent<SpecialCardTooltipTarget>();
        if (tooltipTarget != null)
        {
            tooltipTarget.SetTooltipEnabled(unlocked);
        }
    }
}
