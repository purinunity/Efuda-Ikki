using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Owns mode lifetime and victory ordering; match, run and views have separate owners.</summary>
public sealed class GameSessionFlow
{
    private readonly GameState state;
    private readonly PlayerController player;
    private readonly UIManager ui;
    private readonly Cards specialCards;
    private readonly ISessionProgress progress;
    private readonly ISessionPresentation presentation;
    private readonly BattleGroundVisualTheme theme;
    private readonly GameEndNavigationService navigation;
    private readonly Func<IMatchFlow> createMatch;
    private readonly Func<int, int, int> randomRange;
    private IMatchFlow match;
    private BattleGroundFlow battleGround;
    private int generation;

    public bool IsRunning { get; private set; }

    // Existing callers keep their serialized composition and constructor arguments.
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
        Func<int, int, int> randomRange = null,
        GameplayUiReferences gameplayUiReferences = null,
        IMatchFlow matchFlow = null,
        ISessionPresentation presentation = null,
        ISessionProgress progress = null)
    {
        state = gameState ?? new GameState();
        player = playerController;
        ui = uiManager;
        specialCards = playerSpecialCardsDeck;
        this.randomRange = randomRange ?? UnityEngine.Random.Range;
        this.progress = progress ?? new GameSessionProgress();
        this.presentation = presentation ?? new GameplaySessionPresentation(owner, state,
            ui, characterManager, specialCards, initialPopup, initialResultPanel,
            gameplayUiReferences, popupChanged, resultPanelChanged);
        navigation = new GameEndNavigationService(titleUIManager);
        theme = CreateTheme(allCards, specialCards, cpuSpecialCardsDeck, ui, characterManager);
        createMatch = () => matchFlow ?? new MatchFlow(state, allCards, player, cpuController,
            ui, specialCards, new CpuSetupService(state, cpuController, characterManager,
                specialCards, cpuSpecialCardsDeck, legacyCpuSpecialCardCount, cpuCharacterSettings),
            this.presentation, theme, this.randomRange);
    }

    public bool TryStart(GameModeData modeData, out IEnumerator routine)
    {
        routine = null;
        if (IsRunning) return false;
        if (modeData == null)
        {
            Debug.LogWarning("GameModeData is null. Starting with default mode data.");
            modeData = new GameModeData();
        }
        if (!CanStart(modeData)) return false;
        match = createMatch();
        battleGround = null;
        IsRunning = true;
        int version = ++generation;
        routine = RunMode(modeData, version);
        return true;
    }

    public void RequestBattleGroundSurrender()
    {
        if (IsRunning && battleGround != null) match.Surrender();
    }

    public void Cancel()
    {
        generation++;
        IsRunning = false;
        match?.Cancel();
        // Cancellation is also valid before TryStart or after native scene teardown.
        if (player != null) player.CancelPendingInput();
        if (ui != null) ui.SetPlayerSpecialCardInputEnabled(false);
        state.SetWaitingForHandReveal(false);
        presentation.Cancel();
        theme.Deactivate();
    }

    private bool IsCurrent(int version) => IsRunning && generation == version;

    private bool CanStart(GameModeData mode)
    {
        if (mode.Mode == GameModeData.GameMode.BattleGroundMode && !progress.IsBattleGroundUnlocked)
        {
            Debug.LogWarning("Battle Ground mode is locked until the final Ikki boss is defeated.");
            return false;
        }
        if (mode.Mode == GameModeData.GameMode.IkkiMode && !progress.IsIkkiLevelUnlocked(mode.CurrentLevel))
        {
            Debug.LogWarning($"CPU level {mode.CurrentLevel} is not unlocked.");
            return false;
        }
        return true;
    }

    private IEnumerator RunMode(GameModeData mode, int version)
    {
        // A stale routine must not initialize a run after cancellation/restart.
        if (!IsCurrent(version)) yield break;
        try
        {
            if (mode.Mode == GameModeData.GameMode.BattleGroundMode)
            {
                battleGround = new BattleGroundFlow(specialCards, progress, presentation, randomRange);
                battleGround.Initialize(mode);
            }
            else mode.SetCurrentLevel(mode.CurrentLevel);

            while (IsCurrent(version))
            {
                yield return match.Run(mode, battleGround?.Run);
                if (!IsCurrent(version)) yield break;
                if (match.WasCancelled)
                {
                    Cancel();
                    yield break;
                }

                int winner = match.WinnerIndex;
                if (winner != 0)
                {
                    Debug.Log($"Mode ended. Winner: Player {winner}.");
                    yield return presentation.ShowResult(mode.CurrentLevel, match.Results, false,
                        battleGround != null ? "タイトルへ" : "キャラクター選択へ",
                        battleGround != null ? BattleGroundSummary() : string.Empty);
                    if (!IsCurrent(version)) yield break;
                    break;
                }

                if (mode.Mode == GameModeData.GameMode.IkkiMode)
                {
                    int unlocked = progress.RecordIkkiVictory(mode.CurrentLevel);
                    Debug.Log($"Ikki progress saved. Highest unlocked CPU level: {unlocked}.");
                    yield return presentation.ShowResult(mode.CurrentLevel, match.Results, true,
                        "キャラクター選択へ", string.Empty);
                    if (!IsCurrent(version)) yield break;
                    break;
                }

                PlayerState winningPlayer = state.PlayerStates != null && state.PlayerStates.Count > 0
                    ? state.PlayerStates[0] : null;
                battleGround.CaptureVictory(mode, winningPlayer);
                ui?.SetBattleGroundWinCount(mode.CurrentWinStreak);
                int best = progress.RecordBattleGroundStreak(mode.CurrentWinStreak);
                Debug.Log($"Battle Ground streak: {mode.CurrentWinStreak}. Best: {best}.");
                yield return presentation.ShowResult(mode.CurrentLevel, match.Results, true,
                    "次の対戦へ", BattleGroundSummary());
                if (!IsCurrent(version)) yield break;
                IEnumerator reward = battleGround.ShowReward(mode, () => !IsCurrent(version));
                while (reward.MoveNext())
                {
                    yield return reward.Current;
                    if (!IsCurrent(version)) yield break;
                }
                if (!IsCurrent(version)) yield break;
                battleGround.SelectNextOpponent(mode);
                Debug.Log($"Battle Ground selected random CPU level {mode.CurrentLevel}.");
            }

            if (IsCurrent(version))
            {
                IsRunning = false;
                theme.Deactivate();
                if (mode.Mode == GameModeData.GameMode.IkkiMode) navigation.ReturnToStageSelectOrStopEditor();
                else navigation.ReturnToTitleOrStopEditor();
            }
        }
        finally
        {
            // Only this generation may clean up; an old disposed iterator cannot cancel a new run.
            if (generation == version) Cancel();
        }
    }

    private string BattleGroundSummary() =>
        $"今回 {battleGround.Run.WinStreak}人抜き　最高 {progress.BestBattleGroundStreak}人抜き";

    private static BattleGroundVisualTheme CreateTheme(Cards normal, Cards playerSpecial,
        Cards cpuSpecial, UIManager ui, CharacterManager characters)
    {
        var targets = new List<Card>();
        foreach (Cards pool in new[] { normal, playerSpecial, cpuSpecial })
            if (pool != null && pool.cardList != null) targets.AddRange(pool.cardList);
        return new BattleGroundVisualTheme(Resources.Load<BattleGroundVisualAssets>("BattleGroundVisualAssets"),
            targets, ui != null ? ui.BattleBackgroundImage : null,
            ui != null ? ui.RoundFrameImage : null, characters);
    }
}
