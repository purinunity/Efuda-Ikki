
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 複数のカードオブジェクトを管理するクラス
public class Cards : MonoBehaviour
{
    public List<Card> cardList { get; private set; } // 管理するカードリスト
    public CardsData CardsData; // 全カードデータ
    [SerializeField] private Card cardPrefab; // カードのプレハブ

    // カードリストの初期化処理
    public void Initialize()
    {
        foreach (var card in cardList)
        {
            card.gameObject.transform.SetParent(this.transform); // Set parent to this Cards object
            card.Initialize(); // Assuming Card has an Initialize method
        }
        // Initialize the card list or any other setup if needed
        Debug.Log("Cards initialized.");
    }

    // オブジェクト生成時の初期化
    public void Awake()
    {
        cardList = new List<Card>();
        foreach (var cardData in CardsData.cards)
        {
            Card card = Instantiate(cardPrefab, this.transform);
            card.SetCardData(cardData);
            card.Initialize();
            cardList.Add(card);
        }
        Debug.Log($"Cards initialized. Count: {cardList.Count}");
        Initialize();
    }

    // 指定したカードデータに対応するCardオブジェクトを取得
    public Card GetCard(CardData cardData)
    {
        foreach (var card in cardList)
        {
            if (card.CardData == cardData)
            {
                return card;
            }
        }
        Debug.LogWarning($"Card with data {cardData.number} of {cardData.suit} not found.");
        return null;
    }

    // 複数のカードデータに対応するCardオブジェクトリストを取得
    public List<Card> GetCards(List<CardData> cardDataList)
    {
        List<Card> cards = new List<Card>();
        foreach (var cardData in cardDataList)
        {
            Card card = GetCard(cardData);
            if (card != null)
            {
                cards.Add(card);
            }
        }
        return cards;
    }
}
