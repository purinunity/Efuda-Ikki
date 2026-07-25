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
    public TitleUIManager titleUIManager;

    private GameModeData currentGameModeData;
    private readonly HashSet<Card> subscribedCards = new HashSet<Card>();

    private void Start()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(() => titleUIManager.StartGame());
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(() => titleUIManager.BackFromSpecialCardSelect());
        }

        CreateSpecialCardUI();
        ConfigureSelectionLimiter();
        ResetSelectionForOpen();
    }

    private void CreateSpecialCardUI()
    {
        if (specialCardsDeck == null || selectionCardArea == null) return;

        List<Card> cardsToDisplay = new List<Card>();
        for (int i = 0; i < specialCardsDeck.cardList.Count; i++)
        {
            Card card = specialCardsDeck.cardList[i];
            if (card == null) continue;
            if (SpecialCardResolver.IsNoUseSpecialCard(card.CardData)) continue;
            if (SpecialCardResolver.IsCpuOnlySpecialCard(card.CardData)) continue;

            card.ForceSetFaceUp(true);
            SetupCardSelection(card);
            cardsToDisplay.Add(card);
        }

        cardsToDisplay.Sort(CompareByUnlockOrder);
        selectionCardArea.SetCards(cardsToDisplay, 0.5f);
        foreach (var card in cardsToDisplay)
        {
            card.IsSelectable = true;
        }

    }

    private void SetupCardSelection(Card card)
    {
        if (card == null || !subscribedCards.Add(card))
        {
            return;
        }

        Button cardButton = card.GetComponent<Button>();
        if (cardButton == null) return;

        cardButton.onClick.AddListener(SyncSelectedCardsToModeData);
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
            image.color = unlocked ? unlockedCardColor : lockedCardColor;
        }

        Button button = card.GetComponent<Button>();
        if (button != null)
        {
            // Keep pointer events active for tooltips; selection is blocked by IsSelectable.
            button.interactable = true;
        }
    }
}
