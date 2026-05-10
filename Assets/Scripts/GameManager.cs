using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameState gameState = new GameState();

    [Header("Card Decks")]
    [SerializeField] private Cards allCards;

    [Header("Controllers")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CPUController cpuController;

    [Header("UI")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ShowdownCutInPopup showdownCutInPopup;
    [SerializeField] private TitleUIManager titleUIManager;
    [SerializeField] private CharacterManager characterManager;

    [Header("Special Card Decks")]
    [SerializeField] private Cards specialCardsDeck1;
    [SerializeField] private Cards specialCardsDeck2;

    [Header("CPU Character Settings")]
    [Tooltip("Used when a character does not have configured special cards.")]
    [SerializeField] private int cpuSpecialCardCount = 4;
    [Tooltip("Per-character CPU difficulty and special-card loadout settings. Character Index matches the selected stage number.")]
    [SerializeField] private List<CpuCharacterSettings> cpuCharacterSettings = new List<CpuCharacterSettings>();

    private Controller[] controllers;
    private bool gameOver = false;
    private bool isGameRunning = false;

    private void Start()
    {
        isGameRunning = false;
    }

    public void StartGameWithMode(GameModeData modeData)
    {
        if (modeData == null)
        {
            Debug.LogWarning("GameModeData is null. Starting with default mode data.");
            modeData = new GameModeData();
        }

        if (isGameRunning)
        {
            return;
        }

        isGameRunning = true;
        gameOver = false;
        gameState.InitializePlayerStates();
        controllers = new Controller[] { playerController, cpuController };

        int stageNumber = modeData.Mode == GameModeData.GameMode.KatinukiMode ? modeData.SelectedStage : 0;
        CpuSetupService cpuSetupService = CreateCpuSetupService();
        cpuSetupService.ApplyStageSettings(stageNumber);
        cpuSetupService.ApplySpecialCards(modeData, stageNumber);

        if (modeData.Mode == GameModeData.GameMode.KatinukiMode)
        {
            Debug.Log($"Katinuki mode started with stage {modeData.SelectedStage}.");
            StartCoroutine(GameFlow());
        }
        else if (modeData.Mode == GameModeData.GameMode.BattleGroundMode)
        {
            Debug.Log("BattleGround mode started.");
            StartCoroutine(GameFlow());
        }
    }

    private CpuSetupService CreateCpuSetupService()
    {
        return new CpuSetupService(
            gameState,
            cpuController,
            characterManager,
            specialCardsDeck1,
            specialCardsDeck2,
            cpuSpecialCardCount,
            cpuCharacterSettings);
    }

    private IEnumerator GameFlow()
    {
        while (!gameOver)
        {
            RoundFlowService roundFlowService = CreateRoundFlowService();
            yield return roundFlowService.InitializeRound();

            Debug.Log("Game flow started.");
            Debug.Log($"Round {gameState.RoundNumber} started.");

            yield return roundFlowService.RunExchangeRound();
            yield return StartCoroutine(ShowDown());

            if (gameOver)
            {
                break;
            }

            gameState.NextRound();
        }
    }

    private RoundFlowService CreateRoundFlowService()
    {
        return new RoundFlowService(
            gameState,
            allCards,
            controllers,
            UIUpdateWithWaiting,
            () => gameOver);
    }

    private IEnumerator ShowDown()
    {
        Debug.Log("ShowDown started.");

        ShowdownFlowService showdownService = CreateShowdownFlowService();
        PreparedShowdown preparedShowdown = showdownService.PrepareShowdown();
        SpecialCardResolver.ShowdownResult showdownResult = preparedShowdown.Result;
        LogShowdownResult(showdownResult);

        yield return StartCoroutine(PlayShowdownCutIn(showdownResult));
        showdownService.MarkSpecialCardsUsed(preparedShowdown.SpecialCardsToConsume);

        if (showdownResult.IsDraw)
        {
            Debug.Log("Round ended in a draw.");
            yield break;
        }

        int winner = showdownResult.WinnerIndex;
        int damage = showdownResult.Damage;

        Debug.Log($"Winner is Player {winner}. Damage: {damage}");

        showdownService.ApplyDamage(showdownResult);
        LogLifePoints();

        if (showdownService.CheckGameOver())
        {
            StartCoroutine(HandleGameEnd());
        }
    }

    private ShowdownFlowService CreateShowdownFlowService()
    {
        return new ShowdownFlowService(gameState, cpuController);
    }

    private void LogShowdownResult(SpecialCardResolver.ShowdownResult showdownResult)
    {
        if (showdownResult == null)
        {
            return;
        }

        foreach (SpecialCardResolver.ResolvedHand hand in showdownResult.Hands)
        {
            Debug.Log(
                $"Player {hand.PlayerId} hand: {hand.BaseHand.Rank} -> {hand.DisplayName} ({hand.Score})");
        }

        foreach (string logLine in showdownResult.Logs)
        {
            Debug.Log(logLine);
        }
    }

    private void LogLifePoints()
    {
        for (int i = 0; i < gameState.playerCount; i++)
        {
            Debug.Log($"Player {i} life: {gameState.PlayerStates[i].LifePoints}");
        }
    }

    private IEnumerator PlayShowdownCutIn(SpecialCardResolver.ShowdownResult showdownResult)
    {
        ShowdownCutInPopupProvider popupProvider = CreateShowdownCutInPopupProvider();
        ShowdownCutInPopup popup = popupProvider.GetOrCreate();
        showdownCutInPopup = popupProvider.CurrentPopup;

        if (popup == null)
        {
            yield break;
        }

        ShowdownCutInPopup.Data cutInData = BuildShowdownCutInData(showdownResult);
        yield return StartCoroutine(popup.Play(cutInData));
    }

    private ShowdownCutInPopupProvider CreateShowdownCutInPopupProvider()
    {
        return new ShowdownCutInPopupProvider(this, uiManager, showdownCutInPopup);
    }

    private ShowdownCutInPopup.Data BuildShowdownCutInData(SpecialCardResolver.ShowdownResult showdownResult)
    {
        return new ShowdownCutInDataBuilder(gameState, characterManager).Build(showdownResult);
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
