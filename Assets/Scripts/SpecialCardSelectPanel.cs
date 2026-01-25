using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SpecialCardSelectPanel : MonoBehaviour
{
    [SerializeField] private Cards allCards;
    [SerializeField] private Transform cardButtonContainer;
    [SerializeField] private TextMeshProUGUI selectedCountText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;
    public TitleUIManager titleUIManager;

    private List<Button> cardButtons = new List<Button>();
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

        CreateSpecialCardButtons();
        UpdateSelectedCountDisplay();
    }

    private void CreateSpecialCardButtons()
    {
        if (allCards == null || cardButtonContainer == null) return;

        // 既存のボタンをクリア
        foreach (Transform child in cardButtonContainer)
        {
            Destroy(child.gameObject);
        }
        cardButtons.Clear();

        // 最初の12枚のカードをボタンとして表示
        for (int i = 0; i < Mathf.Min(12, allCards.cardList.Count); i++)
        {
            Card card = allCards.cardList[i];
            Button button = CreateCardButton(card, i);
            cardButtons.Add(button);
        }
    }

    private Button CreateCardButton(Card card, int index)
    {
        GameObject buttonObj = new GameObject($"CardButton_{index}");
        buttonObj.transform.SetParent(cardButtonContainer);
        Button button = buttonObj.AddComponent<Button>();
        Image image = buttonObj.AddComponent<Image>();
        image.color = Color.white;

        int cardIndex = index;
        button.onClick.AddListener(() => ToggleCardSelection(card, button));

        return button;
    }

    private void ToggleCardSelection(Card card, Button button)
    {
        currentGameModeData = GameModeManager.GetGameModeData();

        if (currentGameModeData.SelectedSpecialCards.Contains(card))
        {
            titleUIManager.RemoveSpecialCard(card);
            SetButtonSelected(button, false);
        }
        else if (currentGameModeData.SelectedSpecialCards.Count < 4)
        {
            titleUIManager.SetSpecialCard(card);
            SetButtonSelected(button, true);
        }

        UpdateSelectedCountDisplay();
    }

    private void SetButtonSelected(Button button, bool isSelected)
    {
        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = isSelected ? Color.yellow : Color.white;
        }
    }

    private void UpdateSelectedCountDisplay()
    {
        if (selectedCountText != null)
        {
            currentGameModeData = GameModeManager.GetGameModeData();
            int count = currentGameModeData.SelectedSpecialCards.Count;
            selectedCountText.text = $"{count}/4";
        }
    }
}
