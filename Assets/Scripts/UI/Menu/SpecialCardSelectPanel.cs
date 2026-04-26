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
        RefreshSelectionFromModeData();
        SyncSelectedCardsToModeData();
    }

    private void CreateSpecialCardUI()
    {
        if (specialCardsDeck == null || selectionCardArea == null) return;

        List<Card> cardsToDisplay = new List<Card>();
        for (int i = 0; i < specialCardsDeck.cardList.Count; i++)
        {
            Card card = specialCardsDeck.cardList[i];
            if (card == null) continue;

            card.ForceSetFaceUp(true);
            SetupCardSelection(card);
            cardsToDisplay.Add(card);
        }

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

        foreach (var card in selectionCardArea.cardsInArea)
        {
            if (card == null || card.CardData == null) continue;
            card.IsSelected = selectedCards.Contains(card.CardData);
            card.IsSelectable = true;
        }

        selectionCardArea.RefreshSelectionState();
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
            if (!card.IsSelected) continue;
            currentGameModeData.AddSpecialCard(card.CardData, maxSelectableSpecialCards);
        }
    }
}
