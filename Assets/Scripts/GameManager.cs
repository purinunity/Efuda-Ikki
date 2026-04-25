using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HandEvaluator;

public class GameManager : MonoBehaviour
{
    public GameState gameState = new GameState();

    [SerializeField] private Cards allCards;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CPUController cpuController;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TitleUIManager titleUIManager;
    [SerializeField] private CharacterManager characterManager;
    [SerializeField] private Cards specialCardsDeck1;
    [SerializeField] private Cards specialCardsDeck2;

    private Controller[] controllers;
    private bool initialized = false;
    private bool gameOver = false;
    private bool isGameRunning = false;

    private void Start()
    {
        isGameRunning = false;
    }

    public void StartGameWithMode(GameModeData modeData)
    {
        if (isGameRunning)
        {
            return;
        }

        isGameRunning = true;
        gameOver = false;
        gameState.InitializePlayerStates();
        controllers = new Controller[] { playerController, cpuController };

        ApplySpecialCards();

        if (modeData.Mode == GameModeData.GameMode.KatinukiMode)
        {
            ApplyStageSettings(modeData.SelectedStage);
            Debug.Log($"Katinuki mode started with stage {modeData.SelectedStage}.");
            StartCoroutine(GameFlow());
        }
        else if (modeData.Mode == GameModeData.GameMode.BattleGroundMode)
        {
            Debug.Log("BattleGround mode started.");
            ApplyStageSettings(0);
            StartCoroutine(GameFlow());
        }
    }

    private void ApplyStageSettings(int stageNumber)
    {
        if (characterManager != null)
        {
            characterManager.SetCPUImage(stageNumber);
        }

        gameState.maxHandTrashTurn = 2;
        gameState.maxHandTrashCount = 5;

        Debug.Log($"Stage {stageNumber}: exchange limit fixed to 2 turns / 5 cards.");
    }

    private void ApplySpecialCards()
    {
        GameModeData modeData = GameModeManager.GetGameModeData();

        if (modeData.SelectedSpecialCardDatas == null || modeData.SelectedSpecialCardDatas.Count == 0)
        {
            Debug.Log("No special cards selected.");
            return;
        }

        if (gameState.PlayerStates != null && gameState.PlayerStates.Count > 0)
        {
            if (specialCardsDeck1 == null)
            {
                Debug.LogWarning("specialCardsDeck1 is not assigned.");
                return;
            }

            gameState.PlayerStates[0].SpecialCards = specialCardsDeck1.GetCards(modeData.SelectedSpecialCardDatas);
        }
    }

    private IEnumerator GameFlow()
    {
        while (!gameOver)
        {
            initialized = false;
            StartCoroutine(InitializeGame());
            yield return new WaitUntil(() => initialized);

            Debug.Log("Game flow started.");
            Debug.Log($"Round {gameState.RoundNumber} started.");

            yield return StartCoroutine(Round());
            yield return StartCoroutine(ShowDown());

            if (gameOver)
            {
                break;
            }

            gameState.NextRound();
        }
    }

    private IEnumerator InitializeGame()
    {
        foreach (var card in allCards.cardList)
        {
            gameState.AddCardToDeck(card);
        }

        gameState.CardReset();
        gameState.ShuffleDeck();
        yield return UIUpdateWithWaiting(3f);

        for (int i = 0; i < gameState.playerCount; i++)
        {
            gameState.AddCardToPlayerHand();
            yield return UIUpdateWithWaiting(3f);
            gameState.NextTurn();
        }

        gameState.AddCardToCommon();
        yield return UIUpdateWithWaiting(3f);

        gameState.OpenPlayerHands(0);
        gameState.OpenCommonCards();
        yield return UIUpdateWithWaiting(3f);

        initialized = true;
        Debug.Log("Game initialized.");
    }

    private IEnumerator Round()
    {
        for (int i = 0; i < gameState.maxHandTrashTurn; i++)
        {
            for (int j = 0; j < gameState.playerCount; j++)
            {
                if (gameOver)
                {
                    yield break;
                }

                Controller controller = controllers[gameState.CurrentPlayerIndex];
                bool waiting = true;
                ControllerResponse response = null;

                yield return StartCoroutine(controller.Act(gameState, r =>
                {
                    response = r;
                    waiting = false;
                }));

                while (waiting)
                {
                    yield return null;
                }

                gameState.TrashCards(response.cardsTrash);
                yield return UIUpdateWithWaiting(3f);

                gameState.AddCardToPlayerHand();
                if (gameState.CurrentPlayerIndex == 0)
                {
                    gameState.OpenPlayerHands(0);
                }

                yield return UIUpdateWithWaiting(3f);
                gameState.NextTurn();

                if (gameOver)
                {
                    yield break;
                }
            }
        }
    }

    private IEnumerator ShowDown()
    {
        Debug.Log("ShowDown started.");

        for (int i = 0; i < gameState.playerCount; i++)
        {
            gameState.OpenPlayerHands(i);
        }

        yield return UIUpdateWithWaiting(5f);

        List<HandInfo> results = new List<HandInfo>();
        for (int i = 0; i < gameState.playerCount; i++)
        {
            HandInfo result = HandEvaluator.EvaluateHand(gameState.PlayerStates[i].HandCards, gameState.commonCards);
            results.Add(result);
            Debug.Log($"Player {i} hand: {result.Name}");
        }

        int winner = HandEvaluator.DetermineWinner(results);
        if (winner == -1)
        {
            Debug.Log("Round ended in a draw.");
            yield break;
        }

        Debug.Log($"Winner is Player {winner}.");

        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (i != winner)
            {
                gameState.PlayerStates[i].decreaseLifePoints((int)results[winner].Rank);
            }

            Debug.Log($"Player {i} life: {gameState.PlayerStates[i].LifePoints}");
        }

        if (CheckGameOver())
        {
            StartCoroutine(HandleGameEnd());
        }
    }

    private bool CheckGameOver()
    {
        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (gameState.PlayerStates[i].LifePoints <= 0)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator HandleGameEnd()
    {
        gameOver = true;

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
            Debug.Log("Game ended with no remaining players.");
        }
        else
        {
            Debug.Log($"Game over. Winner: Player {winnerIndex}.");
        }

        yield return UIUpdateWithWaiting(5f);

        isGameRunning = false;

        if (titleUIManager != null)
        {
            titleUIManager.ShowTitleScreen();
            GameModeManager.ResetGameModeData();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    private IEnumerator UIUpdateWithWaiting(float duration = 5f)
    {
        uiManager.UIUpdate(gameState, duration);

        while (uiManager.UIUpdateInProgress)
        {
            yield return null;
        }

        Debug.Log("UI update completed.");
    }
}
