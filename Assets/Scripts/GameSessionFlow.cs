using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns mode, match, round, and showdown orchestration.
/// GameManager remains the serialized Unity composition root and delegates here.
/// </summary>
public sealed class GameSessionFlow
{
    private readonly MonoBehaviour owner;
    private readonly GameState gameState;
    private readonly Cards allCards;
    private readonly PlayerController playerController;
    private readonly CPUController cpuController;
    private readonly UIManager uiManager;
    private readonly CharacterManager characterManager;
    private readonly Cards playerSpecialCardsDeck;
    private readonly Cards cpuSpecialCardsDeck;
    private readonly int legacyCpuSpecialCardCount;
    private readonly List<CpuCharacterSettings> cpuCharacterSettings;
    private readonly TitleUIManager titleUIManager;
    private readonly Action<ShowdownCutInPopup> popupChanged;
    private readonly Action<MatchResultPanel> resultPanelChanged;
    private readonly Func<int, int, int> randomRange;

    private readonly List<MatchRoundResult> currentMatchResults =
        new List<MatchRoundResult>();

    private Controller[] controllers;
    private GameUiUpdateService uiUpdateService;
    private ShowdownPresentationService showdownPresentationService;
    private GameEndNavigationService navigationService;
    private ShowdownCutInPopup showdownCutInPopup;
    private MatchResultPanel matchResultPanel;
    private bool gameOver;
    private int currentMatchWinnerIndex = -1;

    public bool IsRunning { get; private set; }

    public GameSessionFlow(
        MonoBehaviour owner,
        GameState gameState,
        Cards allCards,
        PlayerController playerController,
        CPUController cpuController,
        UIManager uiManager,
        CharacterManager characterManager,
        Cards playerSpecialCardsDeck,
        Cards cpuSpecialCardsDeck,
        int legacyCpuSpecialCardCount,
        List<CpuCharacterSettings> cpuCharacterSettings,
        ShowdownCutInPopup initialPopup,
        MatchResultPanel initialResultPanel,
        TitleUIManager titleUIManager,
        Action<ShowdownCutInPopup> popupChanged = null,
        Action<MatchResultPanel> resultPanelChanged = null,
        Func<int, int, int> randomRange = null)
    {
        this.owner = owner;
        this.gameState = gameState ?? new GameState();
        this.allCards = allCards;
        this.playerController = playerController;
        this.cpuController = cpuController;
        this.uiManager = uiManager;
        this.characterManager = characterManager;
        this.playerSpecialCardsDeck = playerSpecialCardsDeck;
        this.cpuSpecialCardsDeck = cpuSpecialCardsDeck;
        this.legacyCpuSpecialCardCount = legacyCpuSpecialCardCount;
        this.cpuCharacterSettings = cpuCharacterSettings;
        showdownCutInPopup = initialPopup;
        matchResultPanel = initialResultPanel;
        this.titleUIManager = titleUIManager;
        this.popupChanged = popupChanged;
        this.resultPanelChanged = resultPanelChanged;
        this.randomRange = randomRange ?? UnityEngine.Random.Range;
        CreateServices();
    }

    public bool TryStart(GameModeData modeData, out IEnumerator routine)
    {
        routine = null;

        if (IsRunning)
        {
            return false;
        }

        if (modeData == null)
        {
            Debug.LogWarning("GameModeData is null. Starting with default mode data.");
            modeData = new GameModeData();
        }

        if (!CanStart(modeData))
        {
            return false;
        }

        IsRunning = true;
        gameOver = false;
        controllers = new Controller[] { playerController, cpuController };
        CreateServices();
        routine = RunMode(modeData);
        return true;
    }

    public void Cancel()
    {
        IsRunning = false;
        gameOver = true;
        // Unity objects can have a managed reference after their native object
        // is destroyed. Use Unity's null check during scene teardown.
        if (playerController != null) playerController.CancelPendingInput();
        if (uiManager != null) uiManager.SetPlayerSpecialCardInputEnabled(false);
        if (matchResultPanel != null) matchResultPanel.CancelDisplay();
        if (showdownCutInPopup != null) showdownCutInPopup.CancelDisplay();
        ShowdownCutInPopup currentPopup = showdownPresentationService?.CurrentPopup;
        if (currentPopup != null && currentPopup != showdownCutInPopup) currentPopup.CancelDisplay();
    }

    private bool CanStart(GameModeData modeData)
    {
        if (modeData.Mode == GameModeData.GameMode.BattleGroundMode &&
            !GameProgressStore.IsBattleGroundUnlocked)
        {
            Debug.LogWarning("Battle Ground mode is locked until the final Ikki boss is defeated.");
            return false;
        }

        if (modeData.Mode == GameModeData.GameMode.IkkiMode &&
            !GameProgressStore.IsIkkiLevelUnlocked(modeData.CurrentLevel))
        {
            Debug.LogWarning($"CPU level {modeData.CurrentLevel} is not unlocked.");
            return false;
        }

        return true;
    }

    private void CreateServices()
    {
        uiUpdateService = new GameUiUpdateService(gameState, uiManager);
        showdownPresentationService = new ShowdownPresentationService(
            owner,
            gameState,
            uiManager,
            characterManager,
            showdownCutInPopup);
        navigationService = new GameEndNavigationService(titleUIManager);
    }

    private IEnumerator RunMode(GameModeData modeData)
    {
        InitializeModeProgress(modeData);

        while (IsRunning)
        {
            yield return RunSingleMatch(modeData);
            if (!IsRunning)
            {
                yield break;
            }

            int winnerIndex = currentMatchWinnerIndex;
            if (winnerIndex != 0)
            {
                Debug.Log($"Mode ended. Winner: Player {winnerIndex}.");
                yield return ShowMatchResult(
                    modeData.CurrentLevel,
                    false,
                    modeData.Mode == GameModeData.GameMode.IkkiMode
                        ? "キャラクター選択へ"
                        : "タイトルへ");
                break;
            }

            if (modeData.Mode == GameModeData.GameMode.IkkiMode)
            {
                int highestUnlockedLevel =
                    GameProgressStore.RecordIkkiVictory(modeData.CurrentLevel);
                Debug.Log(
                    $"Ikki progress saved. Highest unlocked CPU level: {highestUnlockedLevel}.");

                if (modeData.CurrentLevel >= CpuLevelCatalog.MaxLevel)
                {
                    Debug.Log("Ikki mode cleared. Battle Ground mode unlocked.");
                }
                else
                {
                    Debug.Log($"CPU level {highestUnlockedLevel} unlocked.");
                }

                yield return ShowMatchResult(
                    modeData.CurrentLevel,
                    true,
                    "キャラクター選択へ");
                break;
            }

            modeData.IncrementWinStreak();
            int bestStreak =
                GameProgressStore.RecordBattleGroundStreak(modeData.CurrentWinStreak);
            Debug.Log(
                $"Battle Ground streak: {modeData.CurrentWinStreak}. Best: {bestStreak}.");
            yield return ShowMatchResult(
                modeData.CurrentLevel,
                true,
                "次の対戦へ");
            SelectRandomBattleGroundOpponent(modeData, avoidCurrentLevel: true);
            Debug.Log(
                $"Battle Ground selected random CPU level {modeData.CurrentLevel}.");
        }

        if (IsRunning)
        {
            FinishMode(modeData.Mode);
        }
    }

    private void InitializeModeProgress(GameModeData modeData)
    {
        if (modeData.Mode == GameModeData.GameMode.BattleGroundMode)
        {
            SelectRandomBattleGroundOpponent(modeData, avoidCurrentLevel: false);
            modeData.ResetWinStreak();
            return;
        }

        modeData.SetCurrentLevel(modeData.CurrentLevel);
    }

    private void SelectRandomBattleGroundOpponent(
        GameModeData modeData,
        bool avoidCurrentLevel)
    {
        if (modeData == null)
        {
            return;
        }

        int minLevel = CpuLevelCatalog.MinLevel;
        int maxLevel = CpuLevelCatalog.MaxLevel;
        int levelCount = maxLevel - minLevel + 1;

        if (!avoidCurrentLevel ||
            levelCount <= 1 ||
            modeData.CurrentLevel < minLevel ||
            modeData.CurrentLevel > maxLevel)
        {
            modeData.SetCurrentLevel(randomRange(minLevel, maxLevel + 1));
            return;
        }

        int currentIndex = modeData.CurrentLevel - minLevel;
        int randomOffset = randomRange(1, levelCount);
        int nextLevel = minLevel + (currentIndex + randomOffset) % levelCount;
        modeData.SetCurrentLevel(nextLevel);
    }

    private IEnumerator RunSingleMatch(GameModeData modeData)
    {
        gameOver = false;
        currentMatchWinnerIndex = -1;
        currentMatchResults.Clear();

        gameState.ResetForNewMatch();
        CpuSetupService cpuSetupService = CreateCpuSetupService();
        cpuSetupService.ApplyLevelSettings(modeData.CurrentLevel);
        cpuSetupService.ApplySpecialCardsForLevel(modeData, modeData.CurrentLevel);

        Debug.Log($"{modeData.Mode} match started. CPU level {modeData.CurrentLevel}.");

        while (IsRunning && !gameOver)
        {
            RoundFlowService roundFlowService = CreateRoundFlowService();
            yield return roundFlowService.InitializeRound();
            if (!IsRunning)
            {
                yield break;
            }

            Debug.Log($"Round {gameState.RoundNumber} started.");
            yield return roundFlowService.RunExchangeRound();
            if (!IsRunning || roundFlowService.WasCancelled)
            {
                if (roundFlowService.WasCancelled)
                {
                    Cancel();
                }

                yield break;
            }

            yield return RunShowdown();
            if (!gameOver)
            {
                gameState.NextRound();
            }
        }

        if (IsRunning && currentMatchWinnerIndex < 0)
        {
            currentMatchWinnerIndex = DetermineMatchWinnerIndex();
        }
    }

    private CpuSetupService CreateCpuSetupService()
    {
        return new CpuSetupService(
            gameState,
            cpuController,
            characterManager,
            playerSpecialCardsDeck,
            cpuSpecialCardsDeck,
            legacyCpuSpecialCardCount,
            cpuCharacterSettings);
    }

    private RoundFlowService CreateRoundFlowService()
    {
        return new RoundFlowService(
            gameState,
            allCards,
            controllers,
            uiUpdateService.WaitForUpdate,
            () => gameOver || !IsRunning);
    }

    private IEnumerator RunShowdown()
    {
        Debug.Log("ShowDown started.");

        ShowdownFlowService showdownService =
            new ShowdownFlowService(gameState, cpuController);
        PreparedShowdown preparedShowdown = showdownService.PrepareShowdown();
        LogShowdownResult(preparedShowdown.Result);

        // Presentation intentionally precedes Commit: the cut-in shows the life transition,
        // while Commit is the single point that mutates cards, life, and match state.
        yield return showdownPresentationService.Play(preparedShowdown.Result);
        showdownCutInPopup = showdownPresentationService.CurrentPopup;
        popupChanged?.Invoke(showdownCutInPopup);

        ShowdownCommitResult commit =
            showdownService.Commit(preparedShowdown, gameState.RoundNumber);
        if (commit.RoundResult != null)
        {
            currentMatchResults.Add(commit.RoundResult);
        }

        SpecialCardResolver.ShowdownResult showdownResult = preparedShowdown.Result;
        if (showdownResult == null)
        {
            yield break;
        }

        if (showdownResult.IsDraw)
        {
            Debug.Log("Round ended in a draw.");
        }
        else
        {
            Debug.Log(
                $"Winner is Player {showdownResult.WinnerIndex}. Damage: {showdownResult.Damage}");
            LogLifePoints();
        }

        if (commit.IsGameOver)
        {
            gameOver = true;
            currentMatchWinnerIndex = commit.MatchWinnerIndex;
            Debug.Log($"Match ended. Winner: Player {currentMatchWinnerIndex}.");
        }
    }

    private int DetermineMatchWinnerIndex()
    {
        return new ShowdownFlowService(gameState, cpuController)
            .DetermineMatchWinnerIndex();
    }

    private IEnumerator ShowMatchResult(
        int cpuLevel,
        bool playerWon,
        string buttonLabel)
    {
        matchResultPanel = MatchResultPanel.GetOrCreate(
            matchResultPanel,
            uiManager,
            owner);
        resultPanelChanged?.Invoke(matchResultPanel);

        if (matchResultPanel == null)
        {
            yield break;
        }

        yield return matchResultPanel.Show(
            cpuLevel,
            currentMatchResults,
            playerWon,
            buttonLabel);
    }

    private void FinishMode(GameModeData.GameMode mode)
    {
        IsRunning = false;
        gameOver = false;

        if (mode == GameModeData.GameMode.IkkiMode)
        {
            navigationService.ReturnToStageSelectOrStopEditor();
            return;
        }

        navigationService.ReturnToTitleOrStopEditor();
    }

    private static void LogShowdownResult(
        SpecialCardResolver.ShowdownResult showdownResult)
    {
        if (showdownResult == null)
        {
            return;
        }

        foreach (SpecialCardResolver.ResolvedHand hand in showdownResult.Hands)
        {
            Debug.Log(
                $"Player {hand.PlayerId} hand: {hand.BaseHand.Rank} -> " +
                $"{hand.DisplayName} ({hand.Score})");
        }

        foreach (string logLine in showdownResult.Logs)
        {
            Debug.Log(logLine);
        }
    }

    private void LogLifePoints()
    {
        if (gameState?.PlayerStates == null)
        {
            return;
        }

        for (int i = 0; i < gameState.PlayerStates.Count; i++)
        {
            PlayerState playerState = gameState.PlayerStates[i];
            if (playerState != null)
            {
                Debug.Log($"Player {i} life: {playerState.LifePoints}");
            }
        }
    }
}
