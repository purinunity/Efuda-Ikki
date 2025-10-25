using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// カードを配置・表示するエリアを管理するクラス
public class CardArea : MonoBehaviour
{
    public List<Card> cardsInArea = new List<Card>(); // このエリア内のカードリスト
    public RectTransform areaRect; // カードエリアのRectTransform
    public bool isHorizontal = true;    // 横並びか縦並びか

    // エリアの初期化（カードリストクリア）
    public void Initialize()
    {
        cardsInArea.Clear(); // Clear the list of cards in this area
    }
    // オブジェクト生成時の初期化
    public void Awake()
    {
        Initialize();
    }


    // エリア内のカードを全てクリア
    public void ClearCard()
    {
        cardsInArea.Clear(); // Clear the list after destroying all cards
    }

    // 複数のカードをセット
    public void SetCards(List<Card> cards, float totalDuration = 1.0f)
    {
        cardsInArea.Clear();
        foreach (var card in cards)
        {
            if (card != null && !cardsInArea.Contains(card))
            {
                cardsInArea.Add(card);
            }
        }
        CardsPositionUpdate(totalDuration);
    }

    // カードの位置を更新（横並び・縦並び対応）
    private void CardsPositionUpdate(float totalDuration = 1.0f)
    {
        if (isHorizontal)
        {
            int count = cardsInArea.Count;
            float startOffset = -(areaRect.rect.width / (count + 1)) + (areaRect.rect.width / 2);
            float spacing = areaRect.rect.width / (count + 1);
            for (int i = 0; i < count; i++)
            {
                if (cardsInArea[i] == null) continue;
                cardsInArea[i].gameObject.transform.SetParent(transform); // Set parent to areaRect
                cardsInArea[i].gameObject.transform.SetAsLastSibling(); // Move to the top of the hierarchy
                float offset = startOffset - spacing * i;
                cardsInArea[i].TargetPosition = new Vector2(offset, 0f);
                cardsInArea[i].WaitAndMove(totalDuration / count * i,totalDuration / count);
            }
        }
        else
        {
            int count = cardsInArea.Count;
            float startOffset = -(areaRect.rect.height / (count + 1)) + (areaRect.rect.height / 2);
            float spacing = areaRect.rect.height / (count + 1);
            for (int i = 0; i < count; i++)
            {
                if (cardsInArea[i] == null) continue;
                cardsInArea[i].gameObject.transform.SetParent(transform); // Set parent to areaRect
                cardsInArea[i].gameObject.transform.SetAsLastSibling(); // Move to the top of the hierarchy
                float offset = startOffset - spacing * i;
                cardsInArea[i].TargetPosition = new Vector2(0f, offset);
                cardsInArea[i].WaitAndMove(totalDuration / count * i,totalDuration / count);
            }
        }
    }

    public Card DrawCard()
    {
        if (cardsInArea.Count > 0)
        {
            // int randomIndex = Random.Range(0, cardsInArea.Count);
            Card drawnCard = cardsInArea[0];
            cardsInArea.RemoveAt(0);
            Debug.Log($"Drawn Card: {drawnCard.GetCardNumber()} of {drawnCard.GetCardSuit()}");
            return drawnCard;
        }
        else
        {
            Debug.Log("No cards left to draw.");
            return null; // or throw an exception if preferred
        }
    }

    public Card DrawCard(Card card)
    {
        if (cardsInArea.Contains(card))
        {
            cardsInArea.Remove(card);
            Debug.Log($"Card drawn: {card.GetCardNumber()} of {card.GetCardSuit()}");
            return card;
        }
        else
        {
            Debug.Log("Card not found in the list.");
            return null; // or throw an exception if preferred
        }
    }
    
    public List<Card> GetSelectedCardData()
    {
        List<Card> selectedCards = new List<Card>();
        foreach (var card in cardsInArea)
        {
            if (card.IsSelected)
            {
                selectedCards.Add(card);
                card.IsSelected = false; // Reset selection after getting
            }
        }
        return selectedCards;
    }
}
