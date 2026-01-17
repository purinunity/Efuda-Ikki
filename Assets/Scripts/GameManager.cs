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
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CPUController cpuController;
    [SerializeField] private UIManager uiManager;
    private Controller[] controllers;
    private bool Initialized = false;
    private bool gameOver = false; // ゲーム終了フラグ

    // ゲーム開始時に呼ばれる
    void Start()
    {
        gameOver = false;
        gameState.InitializePlayerStates(); // プレイヤー状態リスト初期化
        controllers = new Controller[] { playerController, cpuController }; // コントローラー配列初期化
        StartCoroutine(GameFlow()); // ゲーム進行コルーチン開始
    }

    // ゲーム進行のメインコルーチン
    IEnumerator GameFlow()
    {
        while (!gameOver)
        {
            Initialized = false;
            StartCoroutine(InitializeGame()); // ゲームの初期化
            yield return new WaitUntil(() => Initialized); // ゲーム初期化完了まで待機
            Debug.Log("ゲーム進行開始");
            Debug.Log($"ラウンド {gameState.RoundNumber} 開始");
            yield return StartCoroutine(Round()); // 1ラウンド進行
            yield return StartCoroutine(ShowDown()); // ショーダウン進行
            if (gameOver) break; // 終了フラグが立ったらループを抜ける
            gameState.NextRound(); // 次のラウンドへ
        }
    }

    // ゲームの初期化処理
    IEnumerator InitializeGame()
    {
        foreach (var card in allCards.cardList)
        {
            gameState.AddCardToDeck(card); // 全カードをデッキに追加
        }
        gameState.CardReset(); // カード状態リセット
        gameState.ShuffleDeck(); // デッキをシャッフル
        yield return UIUpdateWithWaiting(3f); // UI更新(デッキ配布)

        // 各プレイヤーの手札を初期化(親から順に配る)
        for (int i = 0; i < gameState.playerCount; i++)
        {
            gameState.AddCardToPlayerHand();
            yield return UIUpdateWithWaiting(3f); // UI更新(手札配布)
            gameState.NextTurn(); // 次の手番へ
        }

        // 共通カードを追加
        gameState.AddCardToCommon();
        yield return UIUpdateWithWaiting(3f); // UI更新(共通札配布)

        gameState.OpenPlayerHands(0); // プレイヤーの手札を表向きに設定
        gameState.OpenCommonCards(); // 共通札を表向きに設定
        yield return UIUpdateWithWaiting(3f); // UI更新(手札と共通札表向き)

        Initialized = true;
        Debug.Log("ゲーム初期化完了");
        yield break;
    }

    // ラウンド処理：各プレイヤーが手札交換を行うターンを処理します（maxHandTrashTurn 回分）
    IEnumerator Round()
    {
        for (int i = 0; i < gameState.maxHandTrashTurn; i++)
        {
            for (int j = 0; j < gameState.playerCount; j++)
            {
                if (gameOver) yield break; // ゲーム終了時は早期終了
                Controller controller = controllers[gameState.CurrentPlayerIndex]; // 現在のプレイヤーのコントローラーを取得
                bool waiting = true;
                ControllerResponse response = null;
                yield return StartCoroutine(controller.Act(gameState, r => { response = r; waiting = false; }));
                while (waiting) yield return null;
                gameState.TrashCards(response.cardsTrash);
                yield return UIUpdateWithWaiting(3f);// UI更新(手札交換-捨てる)
                
                gameState.AddCardToPlayerHand(); // 捨てた分のカードを補充
                if (gameState.CurrentPlayerIndex == 0)
                {
                    gameState.OpenPlayerHands(0); // プレイヤーの手札を表向きに設定
                }
                yield return UIUpdateWithWaiting(3f);// UI更新(手札交換-加える)
                // 次の手番へ
                gameState.NextTurn();
                if (gameOver) yield break;
            }
        }
    }
    
    // ショーダウン処理：全プレイヤーの手札を表にして役判定→勝者決定→ライフ減算を行う
    IEnumerator ShowDown()
    {
        Debug.Log("ショーダウン開始");
        // 全プレイヤーの手札を表向きに設定
        for (int i = 0; i < gameState.playerCount; i++)
        {
            gameState.OpenPlayerHands(i);
        }
        yield return UIUpdateWithWaiting(5f); // UI更新(ショーダウン)
        // 手札評価
        List<HandInfo> results = new List<HandInfo>();
        for (int i = 0; i < gameState.playerCount; i++)
        {
            var result = HandEvaluator.EvaluateHand(gameState.PlayerStates[i].HandCards, gameState.commonCards);
            results.Add(result);
            Debug.Log($"Player {i} の手札: {result.Name}");
        }

        int winner = HandEvaluator.DetermineWinner(results);

        if (winner == -1)
        {
            Debug.Log("引き分けです！");
            yield break;
        }

        Debug.Log($"勝者は Player {winner} です！");

        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (i != winner)
            {
                gameState.PlayerStates[i].decreaseLifePoints((int)results[winner].Rank);
            }
            Debug.Log($"Player {i} の残りライフポイント: {gameState.PlayerStates[i].LifePoints}");
        }
        Debug.Log("ライフポイント更新完了");
        // どちらかの体力が0以下になっていればゲーム終了処理へ
        if (CheckGameOver())
        {
            StartCoroutine(HandleGameEnd());
            yield break;
        }
        
        yield break;
    }

    // 体力0判定
    private bool CheckGameOver()
    {
        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (gameState.PlayerStates[i].LifePoints <= 0) return true;
        }
        return false;
    }
    // ゲーム終了時の処理
    IEnumerator HandleGameEnd()
    {
        gameOver = true;
        // 勝者判定（体力が残っているプレイヤーを勝者とする）
        int winnerIndex = -1;
        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (gameState.PlayerStates[i].LifePoints > 0)
            {
                winnerIndex = i;
                break;
            }
        }
        if (winnerIndex == -1)
        {
            Debug.Log("両者の体力が0になりました。引き分けでゲーム終了します。");
        }
        else
        {
            Debug.Log($"ゲーム終了！ 勝者は Player {winnerIndex} です！");
        }
        // UI更新を待つ（必要ならUIManagerで表示を行う）
        yield return UIUpdateWithWaiting(5f);

        // エディタの場合は再生停止
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #endif
        yield break;
    }

    // UIManager を呼び出してUI更新を行い、更新完了するまで待機するヘルパー
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
