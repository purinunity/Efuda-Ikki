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
        SyncSelectedCardsToModeData();
    }

    private void CreateSpecialCardUI()
    {
        if (specialCardsDeck == null || selectionCardArea == null) return;

        currentGameModeData = GameModeManager.GetGameModeData();
        List<Card> cardsToDisplay = new List<Card>();
        for (int i = 0; i < specialCardsDeck.cardList.Count; i++)
        {
            Card card = specialCardsDeck.cardList[i];
            if (card == null) continue;

            card.ForceSetFaceUp(true);
            bool isPreSelected = currentGameModeData != null &&
                                 currentGameModeData.SelectedSpecialCardDatas != null &&
                                 currentGameModeData.SelectedSpecialCardDatas.Contains(card.CardData);
            card.IsSelected = isPreSelected;
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
