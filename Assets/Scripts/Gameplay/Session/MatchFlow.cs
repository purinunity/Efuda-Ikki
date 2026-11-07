using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMatchFlow
{
    int WinnerIndex { get; }
    bool WasCancelled { get; }
    IReadOnlyList<MatchRoundResult> Results { get; }
    IEnumerator Run(GameModeData mode, BattleGroundRunState run);
    void Cancel();
    void Surrender();
}

/// <summary>Owns one match, delegating exchange rules and showdown commits to existing services.</summary>
public sealed class MatchFlow : IMatchFlow
{
    private readonly GameState state;
    private readonly Cards allCards;
    private readonly Controller[] controllers;
    private readonly PlayerController player;
    private readonly CPUController cpu;
    private readonly UIManager ui;
    private readonly Cards specialCards;
    private readonly CpuSetupService setup;
    private readonly ISessionPresentation presentation;
    private readonly BattleGroundVisualTheme theme;
    private readonly Func<int, int, int> randomRange;
    private readonly List<MatchRoundResult> results = new List<MatchRoundResult>();
    private GameUiUpdateService uiUpdates;
    private bool gameOver;
    private int generation;

    public int WinnerIndex { get; private set; } = -1;
    public bool WasCancelled { get; private set; }
    public IReadOnlyList<MatchRoundResult> Results => results;

    public MatchFlow(GameState state, Cards allCards, PlayerController player, CPUController cpu,
        UIManager ui, Cards specialCards, CpuSetupService setup, ISessionPresentation presentation,
        BattleGroundVisualTheme theme, Func<int, int, int> randomRange)
    {
        this.state = state;
        this.allCards = allCards;
        this.player = player;
        this.cpu = cpu;
        controllers = new Controller[] { player, cpu };
        this.ui = ui;
        this.specialCards = specialCards;
        this.setup = setup;
        this.presentation = presentation;
        this.theme = theme;
        this.randomRange = randomRange ?? UnityEngine.Random.Range;
    }

    public void Cancel()
    {
        generation++;
        WasCancelled = true;
        gameOver = true;
        if (player != null) player.CancelPendingInput();
        if (ui != null) ui.SetPlayerSpecialCardInputEnabled(false);
        state.SetWaitingForHandReveal(false);
    }

    public void Surrender()
    {
        gameOver = true;
        WinnerIndex = 1;
        if (player != null) player.CancelPendingInput();
        if (ui != null) ui.SetPlayerSpecialCardInputEnabled(false);
        presentation.CancelHandReveal();
    }

    private bool IsCurrent(int version) => generation == version && !WasCancelled;

    public IEnumerator Run(GameModeData mode, BattleGroundRunState run)
    {
        int version = ++generation;
        WasCancelled = false;
        return RunMatch(mode, run, version);
    }

    private IEnumerator RunMatch(GameModeData mode, BattleGroundRunState run, int version)
    {
        if (!IsCurrent(version)) yield break;
        gameOver = false;
        WinnerIndex = -1;
        results.Clear();
        uiUpdates = new GameUiUpdateService(state, ui, mode);

        state.ResetForNewMatch();
        setup.ApplyLevelSettings(mode.CurrentLevel);
        if (run != null && state.PlayerStates.Count >= 2)
        {
            state.PlayerStates[0].SetLifePoints(run.PlayerLife);
            state.PlayerStates[1].SetLifePoints(PlayerState.DefaultLifePoints);
            mode.ReplaceSpecialCards(run.SpecialCards);
        }
        setup.ApplySpecialCardsForLevel(mode, mode.CurrentLevel);
        ui?.ResetSpecialCardAreasForNewMatch();
        ResetSpecialSelection();
        if (run != null) theme?.Activate(mode.CurrentLevel);

        int parent = randomRange(0, state.playerCount);
        state.SetInitialParent(parent);
        ui?.SetParentMarkerVisible(false);
        PrepareCoinTossCards();
        uiUpdates.UpdateImmediately();
        ui?.SetPlayerSpecialCardInputEnabled(true);
        if (!IsCurrent(version)) yield break;
        yield return presentation.CoinToss(parent);
        if (!IsCurrent(version)) yield break;
        ui?.SetParentMarkerVisible(true);
        Debug.Log($"{mode.Mode} match started. CPU level {mode.CurrentLevel}.");

        while (IsCurrent(version) && !gameOver)
        {
            var round = new RoundFlowService(state, allCards, controllers,
                uiUpdates.WaitForUpdate, () => gameOver || !IsCurrent(version));
            yield return round.InitializeRound();
            if (!IsCurrent(version)) yield break;
            if (gameOver) break;
            Debug.Log($"Round {state.RoundNumber} started.");
            yield return round.RunExchangeRound();
            if (!IsCurrent(version) || round.WasCancelled)
            {
                if (round.WasCancelled) Cancel();
                yield break;
            }
            if (gameOver) break;

            state.SetWaitingForHandReveal(true);
            ui?.SetPlayerSpecialCardInputEnabled(true);
            yield return uiUpdates.WaitForUpdate(0f);
            if (!IsCurrent(version)) yield break;
            if (gameOver) break;
            yield return presentation.HandReveal();
            if (!IsCurrent(version)) yield break;
            if (gameOver) break;
            if (!presentation.HandRevealConfirmed)
            {
                Cancel();
                yield break;
            }
            state.SetWaitingForHandReveal(false);
            state.LockSpecialCardSelection();
            ui?.SetPlayerSpecialCardInputEnabled(false);

            yield return RunShowdown(version);
            if (!IsCurrent(version)) yield break;
            if (!gameOver)
            {
                state.NextRound();
                ResetSpecialSelection();
            }
        }
        state.SetWaitingForHandReveal(false);
        if (IsCurrent(version) && WinnerIndex < 0)
            WinnerIndex = new ShowdownFlowService(state, cpu).DetermineMatchWinnerIndex();
    }

    private void ResetSpecialSelection()
    {
        state.ResetSpecialCardSelections();
        ui?.ResetSpecialCardSelectionToNoUse();
    }

    private void PrepareCoinTossCards()
    {
        if (allCards == null || allCards.cardList == null) return;
        foreach (Card card in allCards.cardList) state.AddCardToDeck(card);
        state.CardReset();
    }

    private IEnumerator RunShowdown(int version)
    {
        Debug.Log("ShowDown started.");
        var service = new ShowdownFlowService(state, cpu, specialCards);
        PreparedShowdown prepared = service.PrepareShowdown();
        LogShowdown(prepared.Result);
        // Show the life transition first. Only Commit mutates life and consumes cards.
        yield return presentation.Showdown(prepared.Result);
        if (!IsCurrent(version) || gameOver) yield break;
        ShowdownCommitResult commit = service.Commit(prepared, state.RoundNumber);
        if (commit.RoundResult != null) results.Add(commit.RoundResult);
        if (prepared.Result == null) yield break;
        if (prepared.Result.IsDraw) Debug.Log("Round ended in a draw.");
        else
        {
            Debug.Log($"Winner is Player {prepared.Result.WinnerIndex}. Damage: {prepared.Result.Damage}");
            for (int i = 0; i < state.PlayerStates.Count; i++)
                if (state.PlayerStates[i] != null) Debug.Log($"Player {i} life: {state.PlayerStates[i].LifePoints}");
        }
        if (commit.IsGameOver)
        {
            gameOver = true;
            WinnerIndex = commit.MatchWinnerIndex;
            Debug.Log($"Match ended. Winner: Player {WinnerIndex}.");
        }
    }

    private static void LogShowdown(SpecialCardResolver.ShowdownResult result)
    {
        if (result == null) return;
        foreach (SpecialCardResolver.ResolvedHand hand in result.Hands)
            Debug.Log($"Player {hand.PlayerId} hand: {hand.BaseHand.Rank} -> {hand.DisplayName} ({hand.Score})");
        foreach (string line in result.Logs) Debug.Log(line);
    }
}
