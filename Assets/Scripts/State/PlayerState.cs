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
            Console.WriteLine($"Card {card.CardData.number} of {card.CardData.suit} added to player {PlayerId}'s hand.");
        }
        else
        {
            Console.WriteLine($"Card {card?.CardData.number} of {card?.CardData.suit} is already in player {PlayerId}'s hand or is null.");
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