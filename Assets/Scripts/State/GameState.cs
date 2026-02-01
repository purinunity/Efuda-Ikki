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
    public int maxHandTrashTurn { get; set; } = 2; // 手札交換の最大ターン数（ステージで変更可能）
    public int maxHandTrashCount { get; set; } = 2; // 手札交換の最大枚数（ステージで変更可能）
    public int playerCount { get; private set; } = 2; // プレイヤー数
    public int commonCount { get; private set; } = 2; // 共通カード数
    public int playerHandCount { get; private set; } = 5; // プレイヤーの手札枚数

    // デッキ・共通札・捨て札を統合してカードの状態をリセットする。
    // 各カードはデッキへ戻し、表裏や選択状態を初期化する。
    public void CardReset()
    {
        foreach (var card in commonCards)
        {
            AddCardToDeck(card);
        }
        commonCards.Clear();
        foreach (var card in trashCards)
        {
            AddCardToDeck(card);
        }
        trashCards.Clear();
        foreach (var playerState in PlayerStates)
        {
            foreach (var card in playerState.HandCards)
            {
                AddCardToDeck(card);
            }
            playerState.HandCards.Clear();
        }
        foreach (var card in deckCards)
        {
            card.IsFaceUp = false; // 全カードを裏向きに設定
            card.IsSelected = false; // 全カードの選択を解除
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

    // ラウンド終了処理：ラウンド番号と親プレイヤーを進め、手札交換の使用回数をリセットする。
    public void NextRound()
    {
        RoundNumber++;
        CurrentParentIndex = (CurrentParentIndex + 1) % PlayerStates.Count;
        CurrentPlayerIndex = CurrentParentIndex; // 親プレイヤーからスタート
        foreach (var playerState in PlayerStates)
        {
            playerState.ResetHandTrashTurnsUsed();
        }
    }

    // プレイヤー状態リストの初期化
    public void InitializePlayerStates()
    {
        PlayerStates.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            PlayerStates.Add(new PlayerState(i));
        }
    }

    // 共通カードに追加
    public void AddCardToCommon()
    {
        for (int i = 0; i < commonCount; i++)
        {
            if (commonCards.Count >= commonCount) break;
            commonCards.Add(DrawCardFromDeck());
        }
    }

    // カードをデッキに追加する（重複チェックあり）
    public void AddCardToDeck(Card card)
    {
        if (card != null && !deckCards.Contains(card))
        {
            deckCards.Add(card);
            Console.WriteLine($"Card {card.CardData.number} of {card.CardData.suit} added to deck.");
        }
    }

    // ゲーム状態を変更するヘルパー
    public void ChangeState(GameStateType newState)
    {
        CurrentState = newState;
    }
    // 現在のプレイヤーに手札を補充する（playerHandCount になるまでデッキから引く）
    public void AddCardToPlayerHand()
    {
        for (int i = 0; i < playerHandCount; i++)
        {
            if (PlayerStates[CurrentPlayerIndex].HandCards.Count >= playerHandCount) break;
            Card drawCard = DrawCardFromDeck();
            PlayerStates[CurrentPlayerIndex].AddCardToHand(drawCard);
        }
    }

    public void OpenPlayerHands(int playerId)
    {
        if (playerId < 0 || playerId >= PlayerStates.Count)
        {
            Console.WriteLine("Invalid player ID.");
            return;
        }

        foreach (var card in PlayerStates[playerId].HandCards)
        {
            card.IsFaceUp = true; // 指定プレイヤーの手札を表向きに設定
        }
    }

    public void OpenCommonCards()
    {
        foreach (var card in commonCards)
        {
            card.IsFaceUp = true; // 共通札を表向きに設定
        }
    }
    // 現在のプレイヤーが捨てるカードを処理する。
    // 手札から削除して捨て札リストへ追加し、手札交換ターンの使用回数をインクリメントする。
    public void TrashCards(List<Card> cards)
    {
        foreach (var card in cards)
        {
            card.IsFaceUp = true; // 捨てるカードを表向きに設定
            PlayerStates[CurrentPlayerIndex].RemoveCardFromHand(card);
            AddCardToTrash(card);
        }
        PlayerStates[CurrentPlayerIndex].IncrementHandTrashTurnsUsed();
    }

    // 指定したプレイヤーの手札からカードを削除する（IDチェックを行う）
    public void RemoveCardFromPlayerHand(int playerId, Card card)
    {
        if (playerId < 0 || playerId >= PlayerStates.Count)
        {
            Console.WriteLine("Invalid player ID.");
            return;
        }

        PlayerStates[playerId].RemoveCardFromHand(card);
    }

    // デッキをランダムにシャッフルする（Fisher–Yates 風）
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

    // デッキの先頭カードを引いて返す。デッキが空の場合は null を返す。
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
            return null; // 例外を投げる実装に変更してもよい
        }
    }

    // 捨て札リストにカードを追加する（重複を避ける）
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
