// ゲーム全体の管理を行うクラス
// ゲームの初期化、進行、プレイヤー・CPUの制御を担当
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static HandEvaluator;

public class GameManager : MonoBehaviour
{
    public GameState gameState = new GameState();
    [SerializeField] private Cards allCards; // 全カード管理
    [SerializeField] private int playerCount = 2;
    [SerializeField] private int commonCount = 2;
    [SerializeField] private int playerHandCount = 5;
    [SerializeField] private int maxHandTrashTurn = 2;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CPUController cpuController;
    [SerializeField] private UIManager uiManager;
    private Controller[] controllers;
    private bool Initialized = false;

    // ゲーム開始時に呼ばれる
    void Start()
    {
        gameState.InitializePlayerStates(playerCount); // プレイヤー状態リスト初期化
        controllers = new Controller[] { playerController, cpuController }; // コントローラー配列初期化
        StartCoroutine(GameFlow()); // ゲーム進行コルーチン開始
    }

    // ゲーム進行のメインコルーチン
    System.Collections.IEnumerator GameFlow()
    {
        while (true)
        {
            Initialized = false;
            StartCoroutine(InitializeGame()); // ゲームの初期化
            yield return new WaitUntil(() => Initialized); // ゲーム初期化完了まで待機
            Debug.Log("ゲーム進行開始");
            Debug.Log($"ラウンド {gameState.RoundNumber} 開始");
            yield return StartCoroutine(Round()); // 1ラウンド進行
            gameState.NextRound(); // 次のラウンドへ
        }
    }

    // ゲームの初期化処理
    System.Collections.IEnumerator InitializeGame()
    {
        gameState.CardReset(); // ゲーム状態リセット
        foreach (var card in allCards.cardList)
        {
            card.IsFaceUp = false; // 全カードを裏向きに設定
            card.IsSelected = false; // 全カードの選択を解除
            gameState.deckCards.Add(card); // 全カードをデッキに追加
        }
        gameState.ShuffleDeck(); // デッキをシャッフル
        yield return UIUpdateWithWaiting(3f); // UI更新(デッキ配布)

        // 各プレイヤーの手札を初期化(親から順に配る)
        for (int i = 0; i < playerCount; i++)
        {
            for (int j = 0; j < playerHandCount; j++)
            {
                gameState.AddCardToPlayerHand((i + gameState.CurrentParentIndex) % playerCount, gameState.DrawCardFromDeck()); // プレイヤーにカードを配る
            }
            yield return UIUpdateWithWaiting(3f); // UI更新(手札配布)
        }

        // 共通カードを追加
        for (int j = 0; j < commonCount; j++)
        {
            gameState.AddCardToCommon(gameState.DrawCardFromDeck()); // 共通カードを追加
        }
        yield return UIUpdateWithWaiting(3f); // UI更新(共通札配布)

        foreach (var playerHand in gameState.PlayerStates[0].HandCards)
        {
            playerHand.IsFaceUp = true; // プレイヤーの手札を表向きに設定
        }

        foreach (var card in gameState.commonCards)
        {
            card.IsFaceUp = true; // 共通札を表向きに設定
        }

        yield return UIUpdateWithWaiting(3f); // UI更新(手札と共通札表向き)

        Initialized = true;
        Debug.Log("ゲーム初期化完了");
        yield break;
    }
    
    System.Collections.IEnumerator Round()
    {
        for (int i = 0; i < maxHandTrashTurn; i++)
        {
            for (int j = 0; j < playerCount; j++)
            {
                Controller controller = controllers[gameState.CurrentPlayerIndex]; // 現在のプレイヤーのコントローラーを取得
                bool waiting = true;
                ControllerResponse response = null;
                yield return StartCoroutine(controller.Act(gameState, r => { response = r; waiting = false; }));
                while (waiting) yield return null;
                foreach (var card in response.cardsTrash)
                {
                    card.IsFaceUp = true; // 捨てるカードを表向きに設定
                    gameState.AddCardToTrash(card);
                    gameState.RemoveCardFromPlayerHand(gameState.CurrentPlayerIndex, card);
                    var drawCard = gameState.DrawCardFromDeck();
                    if (gameState.CurrentPlayerIndex == 0) drawCard.IsFaceUp = true; // プレイヤーの引くカードは表向きに設定
                    gameState.AddCardToPlayerHand(gameState.CurrentPlayerIndex, drawCard);
                }
                yield return UIUpdateWithWaiting(3f);// UI更新(手札交換)
                // 次の手番へ
                gameState.NextTurn();
            }
        }

        // スコア判定
        List<(int playerIndex, HandInfo handInfo)> playerScores = new List<(int, HandInfo)>();
        int originalIndex = gameState.CurrentPlayerIndex;
        for (int i = 0; i < playerCount; i++)
        {
            PlayerState playerState = gameState.PlayerStates[i];
            var handInfo = EvaluateHandRank(playerState.HandCards, gameState.commonCards);
            playerScores.Add((i, handInfo));

        }
        // スコアの高い順にソート
        playerScores.Sort((a, b) => CompareHands(b.handInfo, a.handInfo));

        // デバッグ表示例
        foreach (var entry in playerScores)
        {
            Debug.Log($"Player {entry.playerIndex}: {entry.handInfo.Name} (Rank {entry.handInfo.Rank})");
        }
    }

    IEnumerator UIUpdateWithWaiting(float duration = 5f)
    {
        uiManager.UIUpdate(gameState,duration);
        while (uiManager.UIUpdateInProgress)
        {
            // Debug.Log("UI更新待機中...");
            yield return null; // 状態更新完了まで待機
        }
        Debug.Log("UI更新完了");
        yield break;
    }
}
