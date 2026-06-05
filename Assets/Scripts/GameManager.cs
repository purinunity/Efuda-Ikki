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
    [Tooltip("Legacy fallback count. CPU mode progression now uses CpuLevelCatalog.")]
    [SerializeField] private int cpuSpecialCardCount = 4;
    [Tooltip("Per-character CPU difficulty settings. Character Index matches CPU level - 1.")]
    [SerializeField] private List<CpuCharacterSettings> cpuCharacterSettings = new List<CpuCharacterSettings>();

    private Controller[] controllers;
    private bool gameOver = false;
    private bool isGameRunning = false;
    private int currentMatchWinnerIndex = -1;

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

        if (modeData.Mode == GameModeData.GameMode.KachinukiMode &&
            !GameProgressStore.IsKachinukiUnlocked)
        {
            Debug.LogWarning("Kachinuki mode is locked until Ikki mode is cleared.");
            return;
        }

        if (isGameRunning)
        {
            return;
        }

        isGameRunning = true;
        gameOver = false;
        controllers = new Controller[] { playerController, cpuController };

        StartCoroutine(GameModeFlow(modeData));
    }

    private IEnumerator GameModeFlow(GameModeData modeData)
    {
        InitializeModeProgress(modeData);

        while (isGameRunning)
        {
            yield return StartCoroutine(RunSingleMatch(modeData));
            int winnerIndex = currentMatchWinnerIndex;

            yield return UIUpdateWithWaiting(5f);

            if (winnerIndex != 0)
            {
                Debug.Log($"Mode ended. Winner: Player {winnerIndex}.");
                break;
            }

            if (modeData.Mode == GameModeData.GameMode.IkkiMode)
            {
                if (modeData.CurrentLevel >= CpuLevelCatalog.MaxLevel)
                {
                    GameProgressStore.MarkIkkiCleared();
                    Debug.Log("Ikki mode cleared. Kachinuki mode unlocked.");
                    break;
                }

                modeData.AdvanceLevel(wrap: false);
                Debug.Log($"Ikki mode advanced to CPU level {modeData.CurrentLevel}.");
                continue;
            }

            modeData.IncrementWinStreak();
            int bestStreak = GameProgressStore.RecordKachinukiStreak(modeData.CurrentWinStreak);
            Debug.Log($"Kachinuki streak: {modeData.CurrentWinStreak}. Best: {bestStreak}.");
            modeData.AdvanceLevel(wrap: true);
            Debug.Log($"Kachinuki mode advanced to CPU level {modeData.CurrentLevel}.");
        }

        FinishModeAndReturnToTitle();
    }

    private void InitializeModeProgress(GameModeData modeData)
    {
        if (modeData.Mode == GameModeData.GameMode.KachinukiMode)
        {
            modeData.SetCurrentLevel(CpuLevelCatalog.MinLevel);
            modeData.ResetWinStreak();
            return;
        }

        modeData.SetCurrentLevel(modeData.CurrentLevel);
    }

    private IEnumerator RunSingleMatch(GameModeData modeData)
    {
        gameOver = false;
        currentMatchWinnerIndex = -1;

        gameState.ResetForNewMatch();
        CpuSetupService cpuSetupService = CreateCpuSetupService();
        cpuSetupService.ApplyLevelSettings(modeData.CurrentLevel);
        cpuSetupService.ApplySpecialCardsForLevel(modeData, modeData.CurrentLevel);

        Debug.Log($"{modeData.Mode} match started. CPU level {modeData.CurrentLevel}.");

        while (!gameOver)
        {
            RoundFlowService roundFlowService = CreateRoundFlowService();
            yield return roundFlowService.InitializeRound();

            Debug.Log($"Round {gameState.RoundNumber} started.");

            yield return roundFlowService.RunExchangeRound();
            yield return StartCoroutine(ShowDown());

            if (gameOver)
            {
                break;
            }

            gameState.NextRound();
        }

        currentMatchWinnerIndex = DetermineMatchWinnerIndex();
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

        if (showdownResult == null || showdownResult.IsDraw)
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
            gameOver = true;
            currentMatchWinnerIndex = DetermineMatchWinnerIndex();
            Debug.Log($"Match ended. Winner: Player {currentMatchWinnerIndex}.");
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

    private int DetermineMatchWinnerIndex()
    {
        if (gameState?.PlayerStates == null)
        {
            return -1;
        }

        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (gameState.PlayerStates[i].LifePoints > 0)
            {
                return i;
            }
        }

        return -1;
    }

    private void FinishModeAndReturnToTitle()
    {
        isGameRunning = false;
        gameOver = false;

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
