/// <summary>Session decisions and victory saves; storage format belongs to the repository.</summary>
public interface ISessionProgress
{
    bool IsBattleGroundUnlocked { get; }
    int BestBattleGroundStreak { get; }
    bool IsIkkiLevelUnlocked(int level);
    bool IsSpecialCardUnlocked(CardData card);
    int RecordIkkiVictory(int level);
    int RecordBattleGroundStreak(int streak);
}

public sealed class GameSessionProgress : ISessionProgress
{
    public bool IsBattleGroundUnlocked => GameProgressStore.IsBattleGroundUnlocked;
    public int BestBattleGroundStreak => GameProgressStore.BestBattleGroundStreak;
    public bool IsIkkiLevelUnlocked(int level) => GameProgressStore.IsIkkiLevelUnlocked(level);
    public bool IsSpecialCardUnlocked(CardData card) => GameProgressStore.IsSpecialCardUnlocked(card);
    public int RecordIkkiVictory(int level) => GameProgressStore.RecordIkkiVictory(level);
    public int RecordBattleGroundStreak(int streak) => GameProgressStore.RecordBattleGroundStreak(streak);
}
