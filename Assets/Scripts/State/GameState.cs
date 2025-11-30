using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ゲームの状態を表す列挙型
// （RoundReady→RoundStart→TurnStart→Turn→TurnStart→Turn→TurnStart→Turn→TurnStart→Turn→ShowDown→Result）
public enum GameStateType
{
    RoundReady,
    RoundStart,
    TurnStart,
    ShowDown,
    Result
}

// ゲーム全体の状態を管理するクラス
public class GameState
{
    public GameStateType CurrentState { get; private set; } // 現在のゲーム状態
    public List<PlayerState> PlayerStates { get; private set; } = new List<PlayerState>(); // プレイヤー状態リスト
    public int CurrentPlayerIndex { get; private set; } = 0; // 現在のターンプレイヤー
    public int CurrentParentIndex { get; private set; } = 0; // 現在の親プレイヤー
    public int RoundNumber { get; private set; } = 1; // 現在のラウンド数
    public List<Card> deckCards { get; private set; } = new List<Card>(); // デッキのカード
    public List<Card> commonCards { get; private set; } = new List<Card>(); // 共通カード
    public List<Card> trashCards { get; private set; } = new List<Card>(); // 捨て札

    public void CardReset()
    {
        deckCards.Clear();
        commonCards.Clear();
        trashCards.Clear();
        foreach (var playerState in PlayerStates)
        {
            playerState.HandCards.Clear();
        }
    }

    // コンストラクタ
    public GameState()
    {
        CurrentState = GameStateType.RoundReady;
    }

    // 現在のプレイヤー状態を取得
    public PlayerState GetPlayerState()
    {
        return PlayerStates[CurrentPlayerIndex];
    }

    // 次のターンへ進める
    public void NextTurn()
    {
        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % PlayerStates.Count;
    }

    public void NextRound()
    {
        RoundNumber++;
        CurrentParentIndex = (CurrentParentIndex + 1) % PlayerStates.Count;
        CurrentPlayerIndex = CurrentParentIndex; // 親プレイヤーからスタート
    }

    // プレイヤー状態リストの初期化
    public void InitializePlayerStates(int playerCount)
    {
        PlayerStates.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            PlayerStates.Add(new PlayerState(i));
        }
    }

    // 共通カードに追加
    public void AddCardToCommon(Card card)
    {
        if (card != null && !commonCards.Contains(card))
        {
            commonCards.Add(card);
            Console.WriteLine($"Card {card.CardData.number} of {card.CardData.suit} added to common cards.");
        }
        else
        {
            Console.WriteLine($"Card {card?.CardData.number} of {card?.CardData.suit} is already in common cards or is null.");
        }
    }

    public void AddCardToDeck(Card card)
    {
        if (card != null && !deckCards.Contains(card))
        {
            deckCards.Add(card);
            Console.WriteLine($"Card {card.CardData.number} of {card.CardData.suit} added to deck.");
        }
        else
        {
            Console.WriteLine($"Card {card?.CardData.number} of {card?.CardData.suit} is already in deck or is null.");
        }
    }

    public void ChangeState(GameStateType newState)
    {
        CurrentState = newState;
    }

    public void AddCardToPlayerHand(int playerId, Card card)
    {
        if (playerId < 0 || playerId >= PlayerStates.Count)
        {
            Console.WriteLine("Invalid player ID.");
            return;
        }

        PlayerStates[playerId].AddCardToHand(card);
    }

    public void RemoveCardFromPlayerHand(int playerId, Card card)
    {
        if (playerId < 0 || playerId >= PlayerStates.Count)
        {
            Console.WriteLine("Invalid player ID.");
            return;
        }

        PlayerStates[playerId].RemoveCardFromHand(card);
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < deckCards.Count; i++)
        {
            Card temp = deckCards[i];
            int randomIndex = UnityEngine.Random.Range(i, deckCards.Count);
            deckCards[i] = deckCards[randomIndex];
            deckCards[randomIndex] = temp;
        }
    }

    public Card DrawCardFromDeck()
    {
        if (deckCards.Count > 0)
        {
            Card drawnCard = deckCards[0];
            deckCards.RemoveAt(0);
            return drawnCard;
        }
        else
        {
            Console.WriteLine("No cards left to draw.");
            return null; // or throw an exception if preferred
        }
    }

    public void AddCardToTrash(Card card)
    {
        if (card != null && !trashCards.Contains(card))
        {
            trashCards.Add(card);
            Console.WriteLine($"Card {card.CardData.number} of {card.CardData.suit} added to trash.");
        }
        else
        {
            Console.WriteLine($"Card {card?.CardData.number} of {card?.CardData.suit} is already in trash or is null.");
        }
    }
}
