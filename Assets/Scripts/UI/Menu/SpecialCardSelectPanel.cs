using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class SpecialCardSelectPanel : MonoBehaviour
{
    [FormerlySerializedAs("allCards")]
    [SerializeField] private Cards specialCardsDeck;
    [SerializeField] private CardArea cardArea;
    [SerializeField] private TextMeshProUGUI selectedCountText;
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
        UpdateSelectedCountDisplay();
    }

    private void CreateSpecialCardUI()
    {
        if (specialCardsDeck == null || cardArea == null) return;

        List<Card> cardsToDisplay = new List<Card>();
        for (int i = 0; i < specialCardsDeck.cardList.Count; i++)
        {
            Card card = specialCardsDeck.cardList[i];
            if (card == null) continue;

            card.ForceSetFaceUp(true);
            card.IsSelected = false;
            SetupCardSelection(card);
            cardsToDisplay.Add(card);
        }

        cardArea.SetCards(cardsToDisplay, 0.5f);
        foreach (var card in cardsToDisplay)
        {
            card.IsSelectable = true;
        }

        UpdateCardSelectability();
    }

    private void SetupCardSelection(Card card)
    {
        Button cardButton = card.GetComponent<Button>();
        if (cardButton == null) return;

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => OnCardSelected(card));
    }

    private void OnCardSelected(Card card)
    {
        if (!card.IsSelectable) return;

        currentGameModeData = GameModeManager.GetGameModeData();

        if (card.IsSelected)
        {
            card.IsSelected = false;
            titleUIManager.RemoveSpecialCard(card.CardData);
        }
        else if (currentGameModeData.SelectedSpecialCardDatas.Count < 4)
        {
            card.IsSelected = true;
            titleUIManager.SetSpecialCard(card.CardData);
        }
        else
        {
            return;
        }

        card.MoveAndTurnCard(0.3f);
        UpdateSelectedCountDisplay();
        UpdateCardSelectability();
    }

    private void UpdateCardSelectability()
    {
        currentGameModeData = GameModeManager.GetGameModeData();
        int selectedCount = currentGameModeData.SelectedSpecialCardDatas.Count;

        foreach (var card in cardArea.cardsInArea)
        {
            if (card == null) continue;
            card.IsSelectable = card.IsSelected || selectedCount < 4;
        }
    }

    private void UpdateSelectedCountDisplay()
    {
        if (selectedCountText == null) return;

        currentGameModeData = GameModeManager.GetGameModeData();
        int count = currentGameModeData.SelectedSpecialCardDatas.Count;
        selectedCountText.text = $"{count}/4";
    }
}
