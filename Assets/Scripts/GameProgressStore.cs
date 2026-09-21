using System;
using EfudaIkki.Core;
using UnityEngine;

public static class GameProgressStore
{
    private const int CurrentSchemaVersion = 2;
    private const string ProgressDataKey = "EfudaIkki.Progress";
    private const string BackupDataKey = "EfudaIkki.Progress.Backup";

    public const string IkkiClearedKey = "EfudaIkki.IkkiCleared";
    public const string BestKachinukiStreakKey = "EfudaIkki.BestKachinukiStreak";

    private static IProgressRepository repository = new PlayerPrefsProgressRepository();
    private static bool pendingFlush;

    public static IProgressRepository Repository
    {
        get => repository;
        set
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (ReferenceEquals(repository, value)) return;
            repository = value;
            pendingFlush = false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoLoadAtStartup()
    {
        // Load also migrates/repairs old data and immediately persists the
        // normalized result, so gameplay code never has to call Save itself.
        Load();
    }

    [Serializable]
    public sealed class ProgressData
    {
        public int schemaVersion = CurrentSchemaVersion;
        public int highestUnlockedIkkiLevel = CpuLevelCatalog.MinLevel;
        public int unlockedSpecialCardCount = SpecialCardResolver.InitialUnlockedSpecialCardCount;
        public bool ikkiCleared;
        public int bestKachinukiStreak;
    }

    public static bool IsBattleGroundUnlocked => Load().ikkiCleared;

    /// <summary>
    /// Returns whether progress has been persisted in the current format or in
    /// one of the legacy PlayerPrefs keys. Calling this property never creates
    /// save data.
    /// </summary>
    public static bool HasSaveData =>
        !string.IsNullOrWhiteSpace(repository.GetString(ProgressDataKey, string.Empty)) ||
        !string.IsNullOrWhiteSpace(repository.GetString(BackupDataKey, string.Empty)) ||
        repository.GetInt(IkkiClearedKey, 0) != 0 ||
        repository.GetInt(BestKachinukiStreakKey, 0) != 0;

    public static bool IsKachinukiUnlocked => IsBattleGroundUnlocked;

    public static int HighestUnlockedIkkiLevel => Load().highestUnlockedIkkiLevel;

    public static int UnlockedSpecialCardCount => Load().unlockedSpecialCardCount;

    public static int BestBattleGroundStreak => Load().bestKachinukiStreak;

    public static int BestKachinukiStreak => BestBattleGroundStreak;

    public static bool IsIkkiLevelUnlocked(int level)
    {
        if (level < CpuLevelCatalog.MinLevel || level > CpuLevelCatalog.MaxLevel)
        {
            return false;
        }

        return level <= HighestUnlockedIkkiLevel;
    }

    public static bool IsIkkiLevelCleared(int level)
    {
        if (level < CpuLevelCatalog.MinLevel || level > CpuLevelCatalog.MaxLevel)
        {
            return false;
        }

        ProgressData data = Load();
        return data.ikkiCleared || level < data.highestUnlockedIkkiLevel;
    }

    public static bool IsSpecialCardUnlocked(CardData cardData)
    {
        return SpecialCardResolver.TryGetUnlockOrder(cardData, out int order) &&
               order <= UnlockedSpecialCardCount;
    }

    public static int RecordIkkiVictory(int defeatedLevel)
    {
        if (defeatedLevel < CpuLevelCatalog.MinLevel || defeatedLevel > CpuLevelCatalog.MaxLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(defeatedLevel));
        }

        int clampedLevel = CpuLevelCatalog.ClampLevel(defeatedLevel);
        ProgressData data = LoadForUpdate();
        data.unlockedSpecialCardCount = Mathf.Max(
            data.unlockedSpecialCardCount,
            SpecialCardResolver.GetUnlockedCardCountAfterIkkiVictory(clampedLevel));

        if (clampedLevel >= CpuLevelCatalog.MaxLevel)
        {
            data.highestUnlockedIkkiLevel = CpuLevelCatalog.MaxLevel;
            data.ikkiCleared = true;
        }
        else
        {
            data.highestUnlockedIkkiLevel = Mathf.Max(
                data.highestUnlockedIkkiLevel,
                clampedLevel + 1);
        }

        Save(data);
        return data.highestUnlockedIkkiLevel;
    }

    public static void MarkIkkiCleared()
    {
        ProgressData data = LoadForUpdate();
        data.highestUnlockedIkkiLevel = CpuLevelCatalog.MaxLevel;
        data.ikkiCleared = true;
        Save(data);
    }

    /// <summary>Unlocks every stage, mode, and player special card for debugging.</summary>
    public static ProgressData UnlockAllProgress()
    {
        ProgressData data = LoadForUpdate();
        data.highestUnlockedIkkiLevel = CpuLevelCatalog.MaxLevel;
        data.unlockedSpecialCardCount = SpecialCardResolver.SpecialCardCount;
        data.ikkiCleared = true;
        Save(data);
        return data;
    }

    public static int RecordBattleGroundStreak(int winStreak)
    {
        int clampedStreak = Mathf.Max(0, winStreak);
        ProgressData data = LoadForUpdate();
        data.bestKachinukiStreak = Mathf.Max(data.bestKachinukiStreak, clampedStreak);
        Save(data);
        return data.bestKachinukiStreak;
    }

    public static int RecordKachinukiStreak(int winStreak)
    {
        return RecordBattleGroundStreak(winStreak);
    }

    public static ProgressData Load()
    {
        return LoadInternal(autoRepair: true);
    }

    private static ProgressData LoadForUpdate()
    {
        // The caller will persist the updated value, so avoid writing the
        // default/migrated value immediately before the actual update.
        return LoadInternal(autoRepair: false);
    }

    private static ProgressData LoadInternal(bool autoRepair)
    {
        string json = repository.GetString(ProgressDataKey, string.Empty);
        if (!TryReadData(json, out ProgressData data))
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("Failed to load progress data. Recovering from backup or legacy data.");
            }
            if (!TryReadData(repository.GetString(BackupDataKey, string.Empty), out data))
            {
                data = LoadLegacyData();
            }
        }

        // An older build may read the known fields of a newer save, but must
        // never silently downgrade it or discard fields it does not understand.
        int sourceVersion = data.schemaVersion;
        Normalize(data);
        if (sourceVersion > CurrentSchemaVersion)
        {
            data.schemaVersion = sourceVersion;
            return data;
        }
        if (autoRepair)
        {
            Save(data);
        }

        return data;
    }

    private static bool TryReadData(string json, out ProgressData data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(json)) return false;

        string trimmed = json.Trim();
        if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}")) return false;

        try
        {
            // Explicit defaults also support old saves without the card-count
            // field. The sentinel rejects empty/unrelated JSON objects.
            var candidate = new ProgressData
            {
                schemaVersion = 0,
                highestUnlockedIkkiLevel = int.MinValue
            };
            JsonUtility.FromJsonOverwrite(trimmed, candidate);
            if (candidate.highestUnlockedIkkiLevel == int.MinValue) return false;
            data = candidate;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Restores the initial progression and persists it immediately.
    /// Intended for an explicit "delete save data" action and automated tests.
    /// </summary>
    public static ProgressData ResetProgress()
    {
        ProgressData data = new ProgressData();
        Save(data);
        return data;
    }

    private static ProgressData LoadLegacyData()
    {
        bool ikkiCleared = repository.GetInt(IkkiClearedKey, 0) == 1;
        return new ProgressData
        {
            highestUnlockedIkkiLevel = ikkiCleared
                ? CpuLevelCatalog.MaxLevel
                : CpuLevelCatalog.MinLevel,
            unlockedSpecialCardCount = ikkiCleared
                ? SpecialCardResolver.SpecialCardCount
                : SpecialCardResolver.InitialUnlockedSpecialCardCount,
            ikkiCleared = ikkiCleared,
            bestKachinukiStreak = repository.GetInt(BestKachinukiStreakKey, 0)
        };
    }

    private static void Save(ProgressData data)
    {
        if (data.schemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidOperationException("Progress was saved by a newer game version and cannot be overwritten.");
        }

        Normalize(data);
        string json = JsonUtility.ToJson(data);
        if (!pendingFlush && repository.GetString(ProgressDataKey, string.Empty) == json &&
            repository.GetString(BackupDataKey, string.Empty) == json &&
            repository.GetInt(IkkiClearedKey, -1) == (data.ikkiCleared ? 1 : 0) &&
            repository.GetInt(BestKachinukiStreakKey, -1) == data.bestKachinukiStreak)
        {
            return;
        }

        pendingFlush = true;
        repository.SetString(ProgressDataKey, json);
        // Keep a recovery copy of the latest committed progression, including
        // resets, so recovery cannot resurrect progress the player deleted.
        repository.SetString(BackupDataKey, json);

        // Keep the original keys synchronized for compatibility with existing builds.
        repository.SetInt(IkkiClearedKey, data.ikkiCleared ? 1 : 0);
        repository.SetInt(BestKachinukiStreakKey, data.bestKachinukiStreak);
        repository.Save();
        pendingFlush = false;
    }

    private static void Normalize(ProgressData data)
    {
        data.schemaVersion = CurrentSchemaVersion;
        data.highestUnlockedIkkiLevel = Mathf.Clamp(
            data.highestUnlockedIkkiLevel,
            CpuLevelCatalog.MinLevel,
            CpuLevelCatalog.MaxLevel);
        int unlockedCountFromIkkiProgress =
            SpecialCardResolver.InitialUnlockedSpecialCardCount +
            Mathf.Max(0, data.highestUnlockedIkkiLevel - CpuLevelCatalog.MinLevel);
        data.unlockedSpecialCardCount = Mathf.Clamp(
            Mathf.Max(data.unlockedSpecialCardCount, unlockedCountFromIkkiProgress),
            SpecialCardResolver.InitialUnlockedSpecialCardCount,
            SpecialCardResolver.SpecialCardCount);
        data.bestKachinukiStreak = Mathf.Max(0, data.bestKachinukiStreak);

        if (data.ikkiCleared)
        {
            data.highestUnlockedIkkiLevel = CpuLevelCatalog.MaxLevel;
            data.unlockedSpecialCardCount = SpecialCardResolver.SpecialCardCount;
        }
    }

}
