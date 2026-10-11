using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private BattleGroundRewardPanel battleGroundRewardPanel;
    private CoinTossPanel coinTossPanel;
    private HandRevealPanel handRevealPanel;
    private BattleGroundRunState battleGroundRun;
    private BattleGroundRewardService battleGroundRewards;
    private BattleGroundOpponentSelector battleGroundOpponents;
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

    public void RequestBattleGroundSurrender()
    {
        if (!IsRunning || battleGroundRun == null) return;
        gameOver = true;
        currentMatchWinnerIndex = 1;
        if (playerController != null) playerController.CancelPendingInput();
        if (uiManager != null) uiManager.SetPlayerSpecialCardInputEnabled(false);
        handRevealPanel?.CancelDisplay();
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
        if (battleGroundRewardPanel != null) battleGroundRewardPanel.CancelDisplay();
        if (coinTossPanel != null) coinTossPanel.CancelDisplay();
        if (handRevealPanel != null) handRevealPanel.CancelDisplay();
        gameState.SetWaitingForHandReveal(false);
        BattleGroundVisualTheme.Deactivate(characterManager);
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
                if (battleGroundRun != null)
                {
                    yield return ShowMatchResult(
                        modeData.CurrentLevel,
                        false,
                        "タイトルへ",
                        $"今回 {battleGroundRun.WinStreak}人抜き　最高 {GameProgressStore.BestBattleGroundStreak}人抜き");
                }
                else
                {
                    yield return ShowMatchResult(
                        modeData.CurrentLevel,
                        false,
                        "キャラクター選択へ");
                }
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
            uiManager?.SetBattleGroundWinCount(modeData.CurrentWinStreak);
            PlayerState winningPlayer = gameState.PlayerStates != null && gameState.PlayerStates.Count > 0
                ? gameState.PlayerStates[0]
                : null;
            battleGroundRun.CaptureVictory(winningPlayer);
            modeData.ReplaceSpecialCards(battleGroundRun.SpecialCards);
            int bestStreak =
                GameProgressStore.RecordBattleGroundStreak(modeData.CurrentWinStreak);
            Debug.Log(
                $"Battle Ground streak: {modeData.CurrentWinStreak}. Best: {bestStreak}.");
            yield return ShowMatchResult(
                modeData.CurrentLevel,
                true,
                "次の対戦へ");
            IEnumerator rewardRoutine = ShowBattleGroundReward(modeData);
            while (rewardRoutine.MoveNext())
            {
                yield return rewardRoutine.Current;
            }
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
            battleGroundRun = new BattleGroundRunState(modeData.SelectedSpecialCardDatas);
            battleGroundRewards = new BattleGroundRewardService(randomRange);
            battleGroundOpponents = new BattleGroundOpponentSelector(randomRange);
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

        battleGroundOpponents = battleGroundOpponents ?? new BattleGroundOpponentSelector(randomRange);
        modeData.SetCurrentLevel(
            battleGroundOpponents.Select(modeData.CurrentLevel, avoidCurrentLevel));
    }

    private IEnumerator RunSingleMatch(GameModeData modeData)
    {
        gameOver = false;
        currentMatchWinnerIndex = -1;
        currentMatchResults.Clear();

        gameState.ResetForNewMatch();
        uiManager?.ResetSpecialCardAreasForNewMatch();
        CpuSetupService cpuSetupService = CreateCpuSetupService();
        cpuSetupService.ApplyLevelSettings(modeData.CurrentLevel);
        if (battleGroundRun != null && gameState.PlayerStates.Count >= 2)
        {
            gameState.PlayerStates[0].SetLifePoints(battleGroundRun.PlayerLife);
            gameState.PlayerStates[1].SetLifePoints(PlayerState.DefaultLifePoints);
            modeData.ReplaceSpecialCards(battleGroundRun.SpecialCards);
        }
        cpuSetupService.ApplySpecialCardsForLevel(modeData, modeData.CurrentLevel);
        if (battleGroundRun != null)
        {
            BattleGroundVisualTheme.Activate(modeData.CurrentLevel, uiManager, characterManager);
        }

        int initialParentIndex = randomRange(0, gameState.playerCount);
        gameState.SetInitialParent(initialParentIndex);
        uiManager?.SetParentMarkerVisible(false);
        PrepareMatchUiForCoinToss();
        uiUpdateService.UpdateImmediately();
        if (!IsRunning)
        {
            yield break;
        }

        coinTossPanel = CoinTossPanel.GetOrCreate(uiManager, owner);
        if (coinTossPanel != null)
        {
            yield return coinTossPanel.Show(initialParentIndex);
            if (!IsRunning)
            {
                yield break;
            }
        }

        uiManager?.SetParentMarkerVisible(true);

        Debug.Log($"{modeData.Mode} match started. CPU level {modeData.CurrentLevel}.");

        while (IsRunning && !gameOver)
        {
            RoundFlowService roundFlowService = CreateRoundFlowService();
            yield return roundFlowService.InitializeRound();
            if (!IsRunning)
            {
                yield break;
            }
            if (gameOver)
            {
                break;
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
            if (gameOver)
            {
                break;
            }

            gameState.SetWaitingForHandReveal(true);
            uiManager?.SetPlayerSpecialCardInputEnabled(true);
            yield return uiUpdateService.WaitForUpdate(0f);
            handRevealPanel = HandRevealPanel.GetOrCreate(uiManager, owner);
            if (handRevealPanel != null)
            {
                yield return handRevealPanel.WaitForConfirmation();
                if (!IsRunning || gameOver || !handRevealPanel.WasConfirmed)
                {
                    yield break;
                }
            }
            gameState.SetWaitingForHandReveal(false);
            gameState.LockSpecialCardSelection();
            uiManager?.SetPlayerSpecialCardInputEnabled(false);

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

    private void PrepareMatchUiForCoinToss()
    {
        if (gameState == null || allCards == null || allCards.cardList == null)
        {
            return;
        }

        // A new match reuses the scene's Card objects. Return every card to the
        // deck before rendering so the coin-toss background cannot show the
        // previous opponent's hand, common cards, trash, or score state.
        foreach (Card card in allCards.cardList)
        {
            gameState.AddCardToDeck(card);
        }

        gameState.CardReset();
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
        string buttonLabel,
        string summaryOverride = null)
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
            buttonLabel,
            summaryOverride);
    }

    private IEnumerator ShowBattleGroundReward(GameModeData modeData)
    {
        if (battleGroundRun == null || battleGroundRewards == null) yield break;

        List<CardData> unlockedCards = GetUnlockedPlayerSpecialCards();
        bool canChooseCard =
            battleGroundRewards.BuildCandidates(battleGroundRun, unlockedCards).Count > 0;
        battleGroundRewardPanel = BattleGroundRewardPanel.GetOrCreate(uiManager, owner);
        if (battleGroundRewardPanel == null)
        {
            battleGroundRewards.ApplyHerb(battleGroundRun);
            yield break;
        }

        BattleGroundRewardPanel.Choice choice = BattleGroundRewardPanel.Choice.None;
        yield return battleGroundRewardPanel.Show(
            battleGroundRun.PlayerLife,
            battleGroundRun.WinStreak,
            canChooseCard,
            selected => choice = selected);

        if (choice == BattleGroundRewardPanel.Choice.SpecialCard && canChooseCard)
        {
            CardData reward =
                battleGroundRewards.GrantRandomSpecialCard(battleGroundRun, unlockedCards);
            yield return battleGroundRewardPanel.RevealCard(reward);
        }
        else
        {
            battleGroundRewards.ApplyHerb(battleGroundRun);
        }

        modeData.ReplaceSpecialCards(battleGroundRun.SpecialCards);
    }

    private List<CardData> GetUnlockedPlayerSpecialCards()
    {
        var result = new List<CardData>();
        if (playerSpecialCardsDeck == null || playerSpecialCardsDeck.cardList == null)
        {
            return result;
        }

        foreach (Card card in playerSpecialCardsDeck.cardList)
        {
            CardData data = card != null ? card.CardData : null;
            if (data == null || SpecialCardResolver.IsNoUseSpecialCard(data)) continue;
            if (GameProgressStore.IsSpecialCardUnlocked(data) && !result.Contains(data))
            {
                result.Add(data);
            }
        }

        return result;
    }

    private void FinishMode(GameModeData.GameMode mode)
    {
        IsRunning = false;
        gameOver = false;
        battleGroundRun = null;
        BattleGroundVisualTheme.Deactivate(characterManager);

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

public sealed class CoinTossPanel : MonoBehaviour
{
    private const float SpinDuration = 2.1f;
    private const float ResultDuration = 0.9f;

    private Image coinImage;
    private TextMeshProUGUI resultText;
    private Sprite parentSprite;
    private Sprite childSprite;
    private bool cancelled;
    private bool initialized;

    public static CoinTossPanel GetOrCreate(UIManager uiManager, MonoBehaviour owner)
    {
        CoinTossPanel existing = UnityEngine.Object.FindObjectOfType<CoinTossPanel>(true);
        if (existing != null)
        {
            existing.Initialize();
            return existing;
        }

        Canvas canvas = uiManager != null && uiManager.deck != null
            ? uiManager.deck.GetComponentInParent<Canvas>()
            : uiManager != null ? uiManager.GetComponentInParent<Canvas>() : null;
        if (canvas == null && owner != null) canvas = owner.GetComponentInParent<Canvas>();
        if (canvas == null) return null;

        GameObject root = new GameObject(
            "CoinTossPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(CoinTossPanel));
        root.transform.SetParent(canvas.transform, false);
        RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>());
        CoinTossPanel panel = root.GetComponent<CoinTossPanel>();
        panel.Initialize();
        return panel;
    }

    public IEnumerator Show(int parentPlayerIndex)
    {
        Initialize();
        cancelled = false;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        resultText.text = string.Empty;

        float elapsed = 0f;
        while (!cancelled && elapsed < SpinDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / SpinDuration);
            float turns = Mathf.Lerp(0f, 7f, 1f - Mathf.Pow(1f - progress, 1.7f));
            float angle = turns * 360f;
            float face = Mathf.Cos(angle * Mathf.Deg2Rad);
            coinImage.sprite = face >= 0f ? parentSprite : childSprite;
            // Z軸の傾きは演出の終端で必ず0度へ戻す。角度に比例させると
            // 終了直前に上下逆の状態が残り、確定した面を判別しづらくなる。
            float roll = Mathf.Sin(progress * Mathf.PI) * 24f;
            coinImage.rectTransform.localRotation = Quaternion.Euler(0f, angle, roll);
            float bounce = Mathf.Sin(progress * Mathf.PI) * 48f;
            coinImage.rectTransform.anchoredPosition = new Vector2(0f, bounce);
            yield return null;
        }

        if (!cancelled)
        {
            coinImage.sprite = parentPlayerIndex == 0 ? parentSprite : childSprite;
            coinImage.rectTransform.localRotation = Quaternion.identity;
            coinImage.rectTransform.localScale = Vector3.one;
            coinImage.rectTransform.anchoredPosition = Vector2.zero;
            resultText.text = parentPlayerIndex == 0 ? "あなたが親" : "CPUが親";

            float resultElapsed = 0f;
            while (!cancelled && resultElapsed < ResultDuration)
            {
                resultElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        gameObject.SetActive(false);
    }

    public void CancelDisplay()
    {
        cancelled = true;
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        parentSprite = Resources.Load<Sprite>("CoinToss/parent_coin");
        childSprite = Resources.Load<Sprite>("CoinToss/child_coin");
        Sprite backgroundSprite = Resources.Load<Sprite>("CoinToss/coin_background");

        Image overlay = GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.78f);
        overlay.raycastTarget = true;

        RectTransform frame = RuntimeUiFactory.CreateRect("CoinFrame", transform);
        frame.anchorMin = new Vector2(0.31f, 0.12f);
        frame.anchorMax = new Vector2(0.69f, 0.88f);
        frame.offsetMin = frame.offsetMax = Vector2.zero;
        Image frameImage = RuntimeUiFactory.GetOrAdd<Image>(frame.gameObject);
        frameImage.sprite = backgroundSprite;
        frameImage.color = backgroundSprite != null ? Color.white : new Color(0.18f, 0.18f, 0.18f, 1f);
        frameImage.preserveAspect = true;
        frameImage.raycastTarget = false;

        RectTransform coinRect = RuntimeUiFactory.CreateRect("Coin", transform);
        coinRect.anchorMin = new Vector2(0.42f, 0.25f);
        coinRect.anchorMax = new Vector2(0.58f, 0.75f);
        coinRect.offsetMin = coinRect.offsetMax = Vector2.zero;
        coinImage = RuntimeUiFactory.GetOrAdd<Image>(coinRect.gameObject);
        coinImage.sprite = parentSprite;
        coinImage.color = Color.white;
        coinImage.preserveAspect = true;
        coinImage.raycastTarget = false;

        TMP_FontAsset font = UnityEngine.Object.FindObjectOfType<TextMeshProUGUI>(true)?.font ?? TMP_Settings.defaultFontAsset;
        resultText = RuntimeUiFactory.CreateText(
            "Result",
            transform,
            font,
            34,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            Color.black);
        resultText.rectTransform.anchorMin = new Vector2(0.3f, 0.12f);
        resultText.rectTransform.anchorMax = new Vector2(0.7f, 0.24f);
        resultText.rectTransform.offsetMin = resultText.rectTransform.offsetMax = Vector2.zero;
        resultText.enableAutoSizing = true;
        resultText.fontSizeMin = 20f;
        resultText.fontSizeMax = 34f;
        resultText.raycastTarget = false;

        gameObject.SetActive(false);
    }
}
