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
    [SerializeField] private MatchResultPanel matchResultPanel;
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
    private readonly List<MatchRoundResult> currentMatchResults = new List<MatchRoundResult>();
    private GameUiUpdateService uiUpdateService;
    private ShowdownPresentationService showdownPresentationService;
    private GameEndNavigationService gameEndNavigationService;

    private void Start()
    {
        isGameRunning = false;
        CreatePresentationServices();
    }

    public void StartGameWithMode(GameModeData modeData)
    {
        if (modeData == null)
        {
            Debug.LogWarning("GameModeData is null. Starting with default mode data.");
            modeData = new GameModeData();
        }

        if (modeData.Mode == GameModeData.GameMode.BattleGroundMode &&
            !GameProgressStore.IsBattleGroundUnlocked)
        {
            Debug.LogWarning("Battle Ground mode is locked until the final Ikki boss is defeated.");
            return;
        }

        if (modeData.Mode == GameModeData.GameMode.IkkiMode &&
            !GameProgressStore.IsIkkiLevelUnlocked(modeData.CurrentLevel))
        {
            Debug.LogWarning($"CPU level {modeData.CurrentLevel} is not unlocked.");
            return;
        }

        if (isGameRunning)
        {
            return;
        }

        isGameRunning = true;
        gameOver = false;
        controllers = new Controller[] { playerController, cpuController };
        CreatePresentationServices();

        StartCoroutine(GameModeFlow(modeData));
    }

    private void CreatePresentationServices()
    {
        uiUpdateService = new GameUiUpdateService(gameState, uiManager);
        showdownPresentationService = new ShowdownPresentationService(
            this,
            gameState,
            uiManager,
            characterManager,
            showdownCutInPopup);
        gameEndNavigationService = new GameEndNavigationService(titleUIManager);
    }

    private IEnumerator GameModeFlow(GameModeData modeData)
    {
        InitializeModeProgress(modeData);

        while (isGameRunning)
        {
            yield return StartCoroutine(RunSingleMatch(modeData));
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
                int highestUnlockedLevel = GameProgressStore.RecordIkkiVictory(modeData.CurrentLevel);
                Debug.Log($"Ikki progress saved. Highest unlocked CPU level: {highestUnlockedLevel}.");

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
            int bestStreak = GameProgressStore.RecordBattleGroundStreak(modeData.CurrentWinStreak);
            Debug.Log($"Battle Ground streak: {modeData.CurrentWinStreak}. Best: {bestStreak}.");
            yield return ShowMatchResult(
                modeData.CurrentLevel,
                true,
                "次の対戦へ");
            SelectRandomBattleGroundOpponent(modeData, avoidCurrentLevel: true);
            Debug.Log($"Battle Ground selected random CPU level {modeData.CurrentLevel}.");
        }

        FinishMode(modeData.Mode);
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

    private static void SelectRandomBattleGroundOpponent(
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
            modeData.SetCurrentLevel(Random.Range(minLevel, maxLevel + 1));
            return;
        }

        int currentIndex = modeData.CurrentLevel - minLevel;
        int randomOffset = Random.Range(1, levelCount);
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
            uiUpdateService.WaitForUpdate,
            () => gameOver);
    }

    private IEnumerator ShowDown()
    {
        Debug.Log("ShowDown started.");

        ShowdownFlowService showdownService = CreateShowdownFlowService();
        PreparedShowdown preparedShowdown = showdownService.PrepareShowdown();
        SpecialCardResolver.ShowdownResult showdownResult = preparedShowdown.Result;
        LogShowdownResult(showdownResult);
        int playerLifeBefore = GetLifePoints(0);
        int cpuLifeBefore = GetLifePoints(1);

        yield return showdownPresentationService.Play(showdownResult);
        showdownCutInPopup = showdownPresentationService.CurrentPopup;
        showdownService.MarkSpecialCardsUsed(preparedShowdown.SpecialCardsToConsume);

        if (showdownResult == null)
        {
            yield break;
        }

        if (showdownResult.IsDraw)
        {
            RecordRoundResult(
                showdownResult,
                playerLifeBefore,
                playerLifeBefore,
                cpuLifeBefore,
                cpuLifeBefore);
            Debug.Log("Round ended in a draw.");
            yield break;
        }

        int winner = showdownResult.WinnerIndex;
        int damage = showdownResult.Damage;

        Debug.Log($"Winner is Player {winner}. Damage: {damage}");

        showdownService.ApplyDamage(showdownResult);
        RecordRoundResult(
            showdownResult,
            playerLifeBefore,
            GetLifePoints(0),
            cpuLifeBefore,
            GetLifePoints(1));
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

    private int GetLifePoints(int playerIndex)
    {
        if (gameState?.PlayerStates == null ||
            playerIndex < 0 ||
            playerIndex >= gameState.PlayerStates.Count)
        {
            return 0;
        }

        return gameState.PlayerStates[playerIndex].LifePoints;
    }

    private void RecordRoundResult(
        SpecialCardResolver.ShowdownResult showdownResult,
        int playerLifeBefore,
        int playerLifeAfter,
        int cpuLifeBefore,
        int cpuLifeAfter)
    {
        currentMatchResults.Add(new MatchRoundResult(
            gameState.RoundNumber,
            showdownResult.WinnerIndex,
            showdownResult.Damage,
            playerLifeBefore,
            playerLifeAfter,
            cpuLifeBefore,
            cpuLifeAfter));
    }

    private IEnumerator ShowMatchResult(
        int cpuLevel,
        bool playerWon,
        string buttonLabel)
    {
        matchResultPanel = MatchResultPanel.GetOrCreate(matchResultPanel, uiManager, this);
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
        isGameRunning = false;
        gameOver = false;

        if (mode == GameModeData.GameMode.IkkiMode)
        {
            gameEndNavigationService.ReturnToStageSelectOrStopEditor();
            return;
        }

        gameEndNavigationService.ReturnToTitleOrStopEditor();
    }
}
