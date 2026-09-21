using System;
using System.Collections;
using System.Reflection;
using EfudaIkki.Core;
using NUnit.Framework;
using UnityEngine;

// Uses the production PlayerPrefs adapter with unique keys; never resets or
// overwrites the player's save. Session tests stop before result UI is shown.
public sealed class ProgressAutoSaveIntegrationTests
{
    private IProgressRepository originalRepository;
    private string prefix;
    private static readonly string[] Keys =
    {
        "EfudaIkki.Progress", "EfudaIkki.Progress.Backup",
        GameProgressStore.IkkiClearedKey, GameProgressStore.BestKachinukiStreakKey
    };

    [SetUp]
    public void SetUp()
    {
        originalRepository = GameProgressStore.Repository;
        prefix = "EfudaIkki.Tests." + Guid.NewGuid().ToString("N") + ".";
        GameProgressStore.Repository = new PlayerPrefsProgressRepository(prefix);
    }

    [TearDown]
    public void TearDown()
    {
        GameProgressStore.Repository = originalRepository;
        foreach (string key in Keys) PlayerPrefs.DeleteKey(prefix + key);
        PlayerPrefs.Save();
    }

    [Test]
    public void PlayerPrefs_NewRepositoryRestoresAllProgressFields()
    {
        for (int level = 1; level <= 9; level++) GameProgressStore.RecordIkkiVictory(level);
        GameProgressStore.RecordBattleGroundStreak(14);
        GameProgressStore.Repository = new PlayerPrefsProgressRepository(prefix);

        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(9));
        Assert.That(GameProgressStore.UnlockedSpecialCardCount, Is.EqualTo(12));
        Assert.That(GameProgressStore.IsBattleGroundUnlocked, Is.True);
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.EqualTo(14));
        Assert.That(PlayerPrefs.GetString(prefix + Keys[0]), Is.EqualTo(PlayerPrefs.GetString(prefix + Keys[1])));
    }

    [Test]
    public void StartupHook_AutoLoadsAndCreatesInitialSave()
    {
        typeof(GameProgressStore).GetMethod("AutoLoadAtStartup", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, null);
        Assert.That(PlayerPrefs.HasKey(prefix + Keys[0]), Is.True);
        Assert.That(PlayerPrefs.HasKey(prefix + Keys[1]), Is.True);
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(1));
    }

    [Test]
    public void PlayerPrefs_ResetSurvivesRepositoryRecreation()
    {
        GameProgressStore.MarkIkkiCleared();
        GameProgressStore.RecordBattleGroundStreak(9);
        GameProgressStore.ResetProgress();
        GameProgressStore.Repository = new PlayerPrefsProgressRepository(prefix);
        Assert.That(GameProgressStore.IsBattleGroundUnlocked, Is.False);
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(1));
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.Zero);
    }

    [TestCase(1)]
    [TestCase(9)]
    public void IkkiSession_WinningSavesBeforeResultScreen(int level)
    {
        if (level > 1) GameProgressStore.RecordIkkiVictory(level - 1);
        var mode = new GameModeData(GameModeData.GameMode.IkkiMode);
        mode.SetCurrentLevel(level);
        GameSessionFlow flow = CreateFlow();
        Assert.That(flow.TryStart(mode, out IEnumerator routine), Is.True);
        Assert.That(routine.MoveNext(), Is.True); // Match coroutine is now yielded.
        SetMatchWinner(flow, 0); // Supply a completed match at the orchestration boundary.
        Assert.That(routine.MoveNext(), Is.True); // Result UI yielded, not acknowledged.

        GameProgressStore.Repository = new PlayerPrefsProgressRepository(prefix);
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(Math.Min(level + 1, 9)));
        Assert.That(GameProgressStore.IsBattleGroundUnlocked, Is.EqualTo(level == 9));
        Assert.That(GameProgressStore.UnlockedSpecialCardCount, Is.EqualTo(Math.Min(4 + level, 12)));
        flow.Cancel();
    }

    [Test]
    public void BattleGroundSession_EachWinSavesBestStreakBeforeNextMatch()
    {
        GameProgressStore.MarkIkkiCleared();
        var mode = new GameModeData(GameModeData.GameMode.BattleGroundMode);
        GameSessionFlow flow = CreateFlow();
        Assert.That(flow.TryStart(mode, out IEnumerator routine), Is.True);
        Assert.That(routine.MoveNext(), Is.True);
        SetMatchWinner(flow, 0);
        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.EqualTo(1));
        Assert.That(routine.MoveNext(), Is.True); // Next match.
        SetMatchWinner(flow, 0);
        Assert.That(routine.MoveNext(), Is.True);
        GameProgressStore.Repository = new PlayerPrefsProgressRepository(prefix);
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.EqualTo(2));
        flow.Cancel();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LosingOrCancellingSession_DoesNotChangeProgress(bool cancel)
    {
        GameProgressStore.RecordIkkiVictory(2);
        GameProgressStore.RecordBattleGroundStreak(7);
        string before = PlayerPrefs.GetString(prefix + Keys[0]);
        GameSessionFlow flow = CreateFlow();
        Assert.That(flow.TryStart(new GameModeData(), out IEnumerator routine), Is.True);
        Assert.That(routine.MoveNext(), Is.True);
        if (cancel) flow.Cancel();
        else SetMatchWinner(flow, 1);
        routine.MoveNext();
        Assert.That(PlayerPrefs.GetString(prefix + Keys[0]), Is.EqualTo(before));
        flow.Cancel();
    }

    private static GameSessionFlow CreateFlow()
    {
        return new GameSessionFlow(null, new GameState(), null, null, null, null,
            null, null, null, 0, null, null, null, null, randomRange: (min, max) => min);
    }

    private static void SetMatchWinner(GameSessionFlow flow, int index)
    {
        typeof(GameSessionFlow).GetField("currentMatchWinnerIndex", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(flow, index);
    }
}
