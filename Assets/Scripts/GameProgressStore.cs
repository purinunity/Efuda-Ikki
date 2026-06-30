using UnityEngine;

public static class GameProgressStore
{
    public const string IkkiClearedKey = "EfudaIkki.IkkiCleared";
    public const string BestKachinukiStreakKey = "EfudaIkki.BestKachinukiStreak";

    public static bool IsKachinukiUnlocked => PlayerPrefs.GetInt(IkkiClearedKey, 0) == 1;

    public static int BestKachinukiStreak =>
        Mathf.Max(0, PlayerPrefs.GetInt(BestKachinukiStreakKey, 0));

    public static void MarkIkkiCleared()
    {
        PlayerPrefs.SetInt(IkkiClearedKey, 1);
        PlayerPrefs.Save();
    }

    public static int RecordKachinukiStreak(int winStreak)
    {
        int clampedStreak = Mathf.Max(0, winStreak);
        int best = Mathf.Max(BestKachinukiStreak, clampedStreak);
        PlayerPrefs.SetInt(BestKachinukiStreakKey, best);
        PlayerPrefs.Save();
        return best;
    }
}
