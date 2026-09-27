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
    [SerializeField, Min(1)] private int gridColumnCount = 4;
    [SerializeField, Range(0.05f, 0.5f)] private float gridCardScale = 0.234375f;
    [SerializeField, Range(0.05f, 0.5f)] private float selectedCardScale = 0.1171875f;
    public TitleUIManager titleUIManager;

    private GameModeData currentGameModeData;
    private readonly Dictionary<Card, Button> subscribedCardButtons =
        new Dictionary<Card, Button>();
    private Button subscribedStartGameButton;
    private Button subscribedBackButton;
    private bool started;
    private readonly List<Image> selectedDisplayImages = new List<Image>();

    private void Start()
    {
        started = true;
        SubscribePanelButtons();
        CreateSpecialCardUI();
        CreateSelectedDisplayImages();
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
        if (selectionCardArea == null || selectionCardArea.areaRect == null ||
            cards == null || cards.Count == 0)
        {
            return;
        }

        Rect rect = selectionCardArea.areaRect.rect;
        int columns = Mathf.Clamp(gridColumnCount, 1, cards.Count);
        for (int index = 0; index < cards.Count; index++)
        {
            Card card = cards[index];
            if (card == null)
            {
                continue;
            }

            float x;
            float y;
            int column = index % columns;
            int row = index / columns;
            // Keep every selectable card in the left grid. The top-right slots
            // are display-only copies and never receive pointer input.
            x = rect.xMin + rect.width * ((112f + 128f * column) / 1024f);
            y = rect.yMin + rect.height * ((128f + 160f * row) / 576f);
            card.DisplayScale = gridCardScale;

            card.transform.SetParent(selectionCardArea.areaRect, false);
            card.UseSelectedYOffset = false;
            card.TargetPosition = new Vector2(x, y);
            card.SnapToTargetPosition();
        }

        RefreshSelectionVisuals(cards);
    }

    private void CreateSelectedDisplayImages()
    {
        if (selectionCardArea == null || selectionCardArea.areaRect == null || selectedDisplayImages.Count > 0)
        {
            return;
        }

        Rect rect = selectionCardArea.areaRect.rect;
        for (int i = 0; i < maxSelectableSpecialCards; i++)
        {
            var display = new GameObject($"SelectedSpecialCard{i + 1}", typeof(RectTransform), typeof(Image));
            display.transform.SetParent(selectionCardArea.areaRect, false);
            RectTransform displayRect = display.GetComponent<RectTransform>();
            displayRect.anchorMin = displayRect.anchorMax = new Vector2(0.5f, 0.5f);
            displayRect.pivot = new Vector2(0.5f, 0.5f);
            displayRect.anchoredPosition = new Vector2(
                rect.xMin + rect.width * ((664f + 80f * i) / 1024f),
                rect.yMin + rect.height * (400f / 576f));
            Image image = display.GetComponent<Image>();
            image.color = Color.clear;
            image.preserveAspect = true;
            image.raycastTarget = false;
            selectedDisplayImages.Add(image);
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
                displayImage.SetNativeSize();
                displayImage.rectTransform.localScale = Vector3.one * selectedCardScale;
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
