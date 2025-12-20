using System;
using System.Collections.Generic;
using UnityEngine;

// プレイヤーの状態を管理するクラス
public class PlayerState
{
    public int PlayerId { get; private set; } // プレイヤーID
    public int LifePoints { get; private set; } = 100; // ライフポイント（初期値100）
    public List<Card> HandCards { get; private set; } // 手札

    // コンストラクタ
    public PlayerState(int playerId)
    {
        PlayerId = playerId;
        HandCards = new List<Card>();
    }

    // 手札にカードを追加
    public void AddCardToHand(Card card)
    {
        if (card != null && !HandCards.Contains(card))
        {
            HandCards.Add(card);
            // 追加後に手札をソート（花鳥風月の順、同じスートは数字の小さい順）
            SortHand();
            Console.WriteLine($"Card {card.CardData.number} of {card.CardData.suit} added to player {PlayerId}'s hand.");
        }
        else
        {
            Console.WriteLine($"Card {card?.CardData.number} of {card?.CardData.suit} is already in player {PlayerId}'s hand or is null.");
        }
    }

    // 手札を花鳥風月の順にソート、同じスートなら数字の小さい順
    private void SortHand()
    {
        HandCards.Sort((a, b) =>
        {
            if (a == null || a.CardData == null) return -1;
            if (b == null || b.CardData == null) return 1;

            int suitA = SuitOrder(a.CardData.suit);
            int suitB = SuitOrder(b.CardData.suit);
            if (suitA != suitB) return suitA.CompareTo(suitB);

            int numA = (int)a.CardData.number;
            int numB = (int)b.CardData.number;
            return numA.CompareTo(numB);
        });
    }

    private int SuitOrder(Suit suit)
    {
        switch (suit)
        {
            case Suit.Flowers: return 0; // 花
            case Suit.Birds: return 1;   // 鳥
            case Suit.Wind: return 2;    // 風
            case Suit.Moon: return 3;    // 月
            default: return 4; // Joker等は最後に
        }
    }

    // 手札からカードを削除
    public void RemoveCardFromHand(Card card)
    {
        if (card != null && HandCards.Contains(card))
        {
            HandCards.Remove(card);
            Console.WriteLine($"Card {card.CardData.number} of {card.CardData.suit} removed from player {PlayerId}'s hand.");
        }
        else
        {
            Console.WriteLine($"Card {card?.CardData.number} of {card?.CardData.suit} is not in player {PlayerId}'s hand or is null.");
        }
    }

    // ライフポイントを減らす
    public void decreaseLifePoints(int amount)
    {
        LifePoints -= amount;
        if (LifePoints < 0) LifePoints = 0; // マイナスにならないよう制御
        Console.WriteLine($"Player {PlayerId} life points decreased by {amount}. Current life points: {LifePoints}");
    }
}