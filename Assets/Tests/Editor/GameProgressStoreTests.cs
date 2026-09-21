using System.Collections.Generic;
using EfudaIkki.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GameProgressStoreTests
{
    private const string ProgressDataKey = "EfudaIkki.Progress";
    private const string BackupDataKey = "EfudaIkki.Progress.Backup";
    private IProgressRepository originalRepository;
    private MemoryProgressRepository repository;

    [SetUp]
    public void SetUp()
    {
        originalRepository = GameProgressStore.Repository;
        repository = new MemoryProgressRepository();
        GameProgressStore.Repository = repository;
    }

    [TearDown]
    public void TearDown()
    {
        GameProgressStore.Repository = originalRepository;
    }

    [Test]
    public void EmptyRepository_LoadsAndAutoSavesCurrentDefaults()
    {
        Assert.That(GameProgressStore.HasSaveData, Is.False);

        GameProgressStore.ProgressData data = GameProgressStore.Load();

        Assert.That(data.schemaVersion, Is.EqualTo(2));
        Assert.That(data.highestUnlockedIkkiLevel, Is.EqualTo(1));
        Assert.That(data.unlockedSpecialCardCount, Is.EqualTo(4));
        Assert.That(data.ikkiCleared, Is.False);
        Assert.That(data.bestKachinukiStreak, Is.EqualTo(0));
        Assert.That(repository.SaveCallCount, Is.EqualTo(1));
        Assert.That(GameProgressStore.HasSaveData, Is.True);
        StringAssert.Contains("\"schemaVersion\":2", repository.Strings[ProgressDataKey]);
    }

    [Test]
    public void LegacyKeys_AreStillReadWithOriginalMeaning()
    {
        repository.Ints[GameProgressStore.IkkiClearedKey] = 1;
        repository.Ints[GameProgressStore.BestKachinukiStreakKey] = 7;

        GameProgressStore.ProgressData data = GameProgressStore.Load();

        Assert.That(data.highestUnlockedIkkiLevel, Is.EqualTo(9));
        Assert.That(data.unlockedSpecialCardCount, Is.EqualTo(12));
        Assert.That(data.ikkiCleared, Is.True);
        Assert.That(data.bestKachinukiStreak, Is.EqualTo(7));
        Assert.That(repository.SaveCallCount, Is.EqualTo(1));
        Assert.That(repository.Strings.ContainsKey(ProgressDataKey), Is.True);
    }

    [Test]
    public void CorruptJson_FallsBackToDefaultsAndRepairsSaveData()
    {
        repository.Strings[ProgressDataKey] = "{ definitely-not-json";
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
            "Failed to load progress data.*"));

        GameProgressStore.ProgressData data = GameProgressStore.Load();

        Assert.That(data.highestUnlockedIkkiLevel, Is.EqualTo(1));
        Assert.That(data.unlockedSpecialCardCount, Is.EqualTo(4));
        Assert.That(repository.SaveCallCount, Is.EqualTo(1));
        StringAssert.Contains("\"schemaVersion\":2", repository.Strings[ProgressDataKey]);
    }

    [Test]
    public void OutOfRangeJson_IsNormalizedAndAutoSavedOnce()
    {
        repository.Strings[ProgressDataKey] =
            "{\"schemaVersion\":1,\"highestUnlockedIkkiLevel\":99," +
            "\"unlockedSpecialCardCount\":-5,\"ikkiCleared\":false," +
            "\"bestKachinukiStreak\":-8}";

        GameProgressStore.ProgressData data = GameProgressStore.Load();

        Assert.That(data.schemaVersion, Is.EqualTo(2));
        Assert.That(data.highestUnlockedIkkiLevel, Is.EqualTo(9));
        Assert.That(data.unlockedSpecialCardCount, Is.EqualTo(12));
        Assert.That(data.bestKachinukiStreak, Is.Zero);
        Assert.That(repository.SaveCallCount, Is.EqualTo(1));

        GameProgressStore.Load();
        Assert.That(repository.SaveCallCount, Is.EqualTo(1),
            "Already-normalized data must not be rewritten on every read.");
    }

    [Test]
    public void VictoryProgress_PreservesJsonFieldsAndSynchronizesLegacyKeys()
    {
        Assert.That(GameProgressStore.RecordIkkiVictory(1), Is.EqualTo(2));

        Assert.That(repository.Strings.ContainsKey(ProgressDataKey), Is.True);
        string json = repository.Strings[ProgressDataKey];
        StringAssert.Contains("\"schemaVersion\":2", json);
        StringAssert.Contains("\"highestUnlockedIkkiLevel\":2", json);
        StringAssert.Contains("\"unlockedSpecialCardCount\":5", json);
        StringAssert.Contains("\"ikkiCleared\":false", json);
        StringAssert.Contains("\"bestKachinukiStreak\":0", json);
        Assert.That(repository.Ints[GameProgressStore.IkkiClearedKey], Is.EqualTo(0));
        Assert.That(repository.Ints[GameProgressStore.BestKachinukiStreakKey], Is.EqualTo(0));
        Assert.That(repository.SaveCallCount, Is.EqualTo(1));

        GameProgressStore.ProgressData roundTrip = GameProgressStore.Load();
        Assert.That(roundTrip.highestUnlockedIkkiLevel, Is.EqualTo(2));
        Assert.That(roundTrip.unlockedSpecialCardCount, Is.EqualTo(5));
    }

    [Test]
    public void FinalVictoryAndBattleGroundStreak_AreMonotonic()
    {
        GameProgressStore.RecordIkkiVictory(9);
        Assert.That(GameProgressStore.IsBattleGroundUnlocked, Is.True);
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(9));
        Assert.That(GameProgressStore.UnlockedSpecialCardCount, Is.EqualTo(12));

        Assert.That(GameProgressStore.RecordBattleGroundStreak(8), Is.EqualTo(8));
        Assert.That(GameProgressStore.RecordBattleGroundStreak(3), Is.EqualTo(8));
        Assert.That(GameProgressStore.RecordBattleGroundStreak(-4), Is.EqualTo(8));
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.EqualTo(8));
    }

    [Test]
    public void ResetProgress_RestoresDefaultsAndAutoSavesCompatibilityKeys()
    {
        GameProgressStore.RecordIkkiVictory(9);
        GameProgressStore.RecordBattleGroundStreak(12);
        int saveCallsBeforeReset = repository.SaveCallCount;

        GameProgressStore.ProgressData reset = GameProgressStore.ResetProgress();

        Assert.That(reset.highestUnlockedIkkiLevel, Is.EqualTo(1));
        Assert.That(reset.unlockedSpecialCardCount, Is.EqualTo(4));
        Assert.That(reset.ikkiCleared, Is.False);
        Assert.That(reset.bestKachinukiStreak, Is.Zero);
        Assert.That(repository.Ints[GameProgressStore.IkkiClearedKey], Is.Zero);
        Assert.That(repository.Ints[GameProgressStore.BestKachinukiStreakKey], Is.Zero);
        Assert.That(repository.SaveCallCount, Is.EqualTo(saveCallsBeforeReset + 1));
    }

    [Test]
    public void ProgressSurvivesRepositoryRestartWithoutManualSave()
    {
        for (int level = 1; level <= 9; level++) GameProgressStore.RecordIkkiVictory(level);
        GameProgressStore.RecordBattleGroundStreak(17);
        GameProgressStore.Repository = repository.Restart();

        Assert.That(GameProgressStore.IsBattleGroundUnlocked, Is.True);
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(9));
        Assert.That(GameProgressStore.UnlockedSpecialCardCount, Is.EqualTo(12));
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.EqualTo(17));
    }

    [TestCase("{ broken")]
    [TestCase("{}")]
    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{\"unrelated\":true}")]
    public void InvalidPrimary_RecoversLatestProgressFromBackup(string invalidJson)
    {
        GameProgressStore.RecordIkkiVictory(5);
        GameProgressStore.RecordBattleGroundStreak(8);
        repository.Strings[ProgressDataKey] = invalidJson;
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Failed to load progress data.*"));

        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(6));
        Assert.That(GameProgressStore.UnlockedSpecialCardCount, Is.EqualTo(9));
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.EqualTo(8));
        Assert.That(repository.Strings[ProgressDataKey], Is.EqualTo(repository.Strings[BackupDataKey]));
    }

    [Test]
    public void MissingPrimary_RecoversBackupAndHasSaveDataDoesNotWrite()
    {
        GameProgressStore.RecordIkkiVictory(3);
        repository.Strings.Remove(ProgressDataKey);
        int before = repository.SaveCallCount;
        Assert.That(GameProgressStore.HasSaveData, Is.True);
        Assert.That(repository.SaveCallCount, Is.EqualTo(before));
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(4));
        Assert.That(repository.SaveCallCount, Is.EqualTo(before + 1));
    }

    [Test]
    public void InvalidPrimaryAndBackup_RecoverLegacyProgress()
    {
        repository.Strings[ProgressDataKey] = "{}";
        repository.Strings[BackupDataKey] = "not json";
        repository.Ints[GameProgressStore.IkkiClearedKey] = 1;
        repository.Ints[GameProgressStore.BestKachinukiStreakKey] = 23;
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Failed to load progress data.*"));

        Assert.That(GameProgressStore.IsBattleGroundUnlocked, Is.True);
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.EqualTo(23));
        Assert.That(repository.Strings[BackupDataKey], Is.EqualTo(repository.Strings[ProgressDataKey]));
    }

    [Test]
    public void OldJsonWithoutCardCount_MigratesAndPersistsUnlocks()
    {
        repository.Strings[ProgressDataKey] = "{\"highestUnlockedIkkiLevel\":5,\"bestKachinukiStreak\":6}";
        Assert.That(GameProgressStore.UnlockedSpecialCardCount, Is.EqualTo(8));
        GameProgressStore.Repository = repository.Restart();
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(5));
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.EqualTo(6));
    }

    [Test]
    public void RepeatedReadsAndLowerRecords_DoNotFlushAgain()
    {
        GameProgressStore.RecordIkkiVictory(5);
        GameProgressStore.RecordBattleGroundStreak(10);
        int before = repository.SaveCallCount;
        for (int i = 0; i < 20; i++) GameProgressStore.Load();
        GameProgressStore.RecordIkkiVictory(1);
        GameProgressStore.RecordBattleGroundStreak(10);
        GameProgressStore.RecordBattleGroundStreak(-1);
        Assert.That(repository.SaveCallCount, Is.EqualTo(before));
    }

    [TestCase(0)]
    [TestCase(10)]
    public void InvalidVictoryLevel_DoesNotUnlockOrWriteProgress(int level)
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => GameProgressStore.RecordIkkiVictory(level));
        Assert.That(GameProgressStore.HasSaveData, Is.False);
        Assert.That(repository.SaveCallCount, Is.Zero);
    }

    [Test]
    public void NewerSchema_IsReadableButNeverOverwrittenByOlderBuild()
    {
        const string future = "{\"schemaVersion\":99,\"highestUnlockedIkkiLevel\":5,\"futureField\":123}";
        repository.Strings[ProgressDataKey] = future;
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(5));
        Assert.Throws<System.InvalidOperationException>(() => GameProgressStore.RecordIkkiVictory(5));
        Assert.Throws<System.InvalidOperationException>(() => GameProgressStore.RecordBattleGroundStreak(8));
        Assert.That(repository.Strings[ProgressDataKey], Is.EqualTo(future));
        Assert.That(repository.SaveCallCount, Is.Zero);
    }

    [Test]
    public void ResetProgress_ReplacesBackupSoDeletedProgressCannotReturn()
    {
        GameProgressStore.MarkIkkiCleared();
        GameProgressStore.RecordBattleGroundStreak(20);
        GameProgressStore.ResetProgress();
        repository = repository.Restart();
        GameProgressStore.Repository = repository;
        repository.Strings.Remove(ProgressDataKey);

        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(1));
        Assert.That(GameProgressStore.UnlockedSpecialCardCount, Is.EqualTo(4));
        Assert.That(GameProgressStore.IsBattleGroundUnlocked, Is.False);
        Assert.That(GameProgressStore.BestBattleGroundStreak, Is.Zero);
    }

    [Test]
    public void FailedFlush_IsRetriedEvenWhenInMemoryValuesAlreadyMatch()
    {
        repository.FailNextSave = true;
        Assert.Throws<System.IO.IOException>(() => GameProgressStore.RecordIkkiVictory(2));
        GameProgressStore.Load();
        GameProgressStore.Repository = repository.Restart();
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(3));
    }

    [Test]
    public void MutatingLoadedSnapshot_DoesNotChangeSavedProgress()
    {
        GameProgressStore.ProgressData snapshot = GameProgressStore.Load();
        snapshot.ikkiCleared = true;
        snapshot.highestUnlockedIkkiLevel = 9;
        Assert.That(GameProgressStore.IsBattleGroundUnlocked, Is.False);
        Assert.That(GameProgressStore.HighestUnlockedIkkiLevel, Is.EqualTo(1));
    }

    private sealed class MemoryProgressRepository : IProgressRepository
    {
        public Dictionary<string, string> Strings { get; } = new Dictionary<string, string>();
        public Dictionary<string, int> Ints { get; } = new Dictionary<string, int>();
        public int SaveCallCount { get; private set; }
        public bool FailNextSave;
        private readonly Dictionary<string, string> savedStrings = new Dictionary<string, string>();
        private readonly Dictionary<string, int> savedInts = new Dictionary<string, int>();

        public MemoryProgressRepository Restart()
        {
            var restarted = new MemoryProgressRepository();
            foreach (var pair in savedStrings) restarted.Strings[pair.Key] = pair.Value;
            foreach (var pair in savedInts) restarted.Ints[pair.Key] = pair.Value;
            return restarted;
        }

        public string GetString(string key, string defaultValue)
        {
            return Strings.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public int GetInt(string key, int defaultValue)
        {
            return Ints.TryGetValue(key, out int value) ? value : defaultValue;
        }

        public void SetString(string key, string value)
        {
            Strings[key] = value;
        }

        public void SetInt(string key, int value)
        {
            Ints[key] = value;
        }

        public void Save()
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new System.IO.IOException("Simulated storage failure");
            }
            SaveCallCount++;
            savedStrings.Clear();
            savedInts.Clear();
            foreach (var pair in Strings) savedStrings[pair.Key] = pair.Value;
            foreach (var pair in Ints) savedInts[pair.Key] = pair.Value;
        }
    }
}
