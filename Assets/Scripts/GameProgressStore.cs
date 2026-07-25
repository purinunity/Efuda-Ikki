using System;
using UnityEngine;

public static class GameProgressStore
{
    private const int CurrentSchemaVersion = 2;
    private const string ProgressDataKey = "EfudaIkki.Progress";

    public const string IkkiClearedKey = "EfudaIkki.IkkiCleared";
    public const string BestKachinukiStreakKey = "EfudaIkki.BestKachinukiStreak";

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

    public static bool IsSpecialCardUnlocked(CardData cardData)
    {
        return SpecialCardResolver.TryGetUnlockOrder(cardData, out int order) &&
               order <= UnlockedSpecialCardCount;
    }

    public static int RecordIkkiVictory(int defeatedLevel)
    {
        int clampedLevel = CpuLevelCatalog.ClampLevel(defeatedLevel);
        ProgressData data = Load();
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
        ProgressData data = Load();
        data.highestUnlockedIkkiLevel = CpuLevelCatalog.MaxLevel;
        data.ikkiCleared = true;
        Save(data);
    }

    public static int RecordBattleGroundStreak(int winStreak)
    {
        int clampedStreak = Mathf.Max(0, winStreak);
        ProgressData data = Load();
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
        ProgressData data = null;
        string json = PlayerPrefs.GetString(ProgressDataKey, string.Empty);

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                data = JsonUtility.FromJson<ProgressData>(json);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"Failed to load progress data. Legacy data will be used. {exception.Message}");
            }
        }

        if (data == null)
        {
            data = LoadLegacyData();
        }

        Normalize(data);
        return data;
    }

    private static ProgressData LoadLegacyData()
    {
        bool ikkiCleared = PlayerPrefs.GetInt(IkkiClearedKey, 0) == 1;
        return new ProgressData
        {
            highestUnlockedIkkiLevel = ikkiCleared
                ? CpuLevelCatalog.MaxLevel
                : CpuLevelCatalog.MinLevel,
            unlockedSpecialCardCount = ikkiCleared
                ? SpecialCardResolver.SpecialCardCount
                : SpecialCardResolver.InitialUnlockedSpecialCardCount,
            ikkiCleared = ikkiCleared,
            bestKachinukiStreak = PlayerPrefs.GetInt(BestKachinukiStreakKey, 0)
        };
    }

    private static void Save(ProgressData data)
    {
        Normalize(data);
        PlayerPrefs.SetString(ProgressDataKey, JsonUtility.ToJson(data));

        // Keep the original keys synchronized for compatibility with existing builds.
        PlayerPrefs.SetInt(IkkiClearedKey, data.ikkiCleared ? 1 : 0);
        PlayerPrefs.SetInt(BestKachinukiStreakKey, data.bestKachinukiStreak);
        PlayerPrefs.Save();
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
