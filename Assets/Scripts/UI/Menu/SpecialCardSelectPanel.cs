using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class SpecialCardSelectPanel : MonoBehaviour
{
    [FormerlySerializedAs("allCards")]
    [SerializeField] private Cards specialCardsDeck;
    [SerializeField] private CardArea cardArea; // ゲーム画面と同じCardAreaを使用
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

        // CardAreaに表示するカードを作成
        List<Card> cardsToDisplay = new List<Card>();

        // 特殊札用デッキのカードを UI に表示
        for (int i = 0; i < specialCardsDeck.cardList.Count; i++)
        {
            // CardDataを使用してCardオブジェクトを作成
            Card card = specialCardsDeck.cardList[i];
            if (card != null)
            {
                // 選択UIでは表向きで表示したいので先に表裏・スケールを揃える
                card.ForceSetFaceUp(true);
                card.IsSelected = false;
                // カード選択時のコールバックを設定
                SetupCardSelection(card);
                cardsToDisplay.Add(card);
            }
        }

        // CardAreaにカードをセット
        cardArea.SetCards(cardsToDisplay, 0.5f);
        
        // 全カードを選択可能にする
        foreach (var card in cardsToDisplay)
        {
            card.IsSelectable = true;
        }

        // 初期状態で選択可能状態を更新
        UpdateCardSelectability();
    }

    private void SetupCardSelection(Card card)
    {
        // Cardオブジェクトのクリックイベントをカスタマイズ
        Button cardButton = card.GetComponent<Button>();
        if (cardButton != null)
        {
            // デフォルトのToggleSelectの代わりにカスタム処理を設定
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => OnCardSelected(card));
        }
    }

    private void OnCardSelected(Card card)
    {
        if (!card.IsSelectable) return;

        currentGameModeData = GameModeManager.GetGameModeData();

        // CardDataを使用して選択状態を管理
        if (card.IsSelected)
        {
            // 選択解除
            card.IsSelected = false;
            titleUIManager.RemoveSpecialCard(card);
        }
        else if (currentGameModeData.SelectedSpecialCards.Count < 4)
        {
            // 選択
            card.IsSelected = true;
            titleUIManager.SetSpecialCard(card);
        }
        else
        {
            // 既に4枚選択済みの場合は選択できない
            return;
        }

        card.MoveAndTurnCard(0.3f); // カードのアニメーション
        UpdateSelectedCountDisplay();
        UpdateCardSelectability(); // 選択可能状態を更新
    }

    private void UpdateCardSelectability()
    {
        currentGameModeData = GameModeManager.GetGameModeData();
        int selectedCount = currentGameModeData.SelectedSpecialCards.Count;

        // CardArea内の全カードを確認
        foreach (var card in cardArea.cardsInArea)
        {
            if (card == null) continue;

            // 既に選択されているカード、または4枚に達していない場合は選択可能
            if (card.IsSelected || selectedCount < 4)
            {
                card.IsSelectable = true;
            }
            else
            {
                // 4枚選択済みで、このカードが選択されていない場合は選択不可
                card.IsSelectable = false;
            }
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
