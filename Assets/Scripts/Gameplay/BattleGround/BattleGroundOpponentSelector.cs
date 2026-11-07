using System;

public sealed class BattleGroundOpponentSelector
{
    private readonly Func<int, int, int> randomRange;

    public BattleGroundOpponentSelector(Func<int, int, int> randomRange = null)
    {
        this.randomRange = randomRange ?? UnityEngine.Random.Range;
    }

    public int Select(int previousLevel, bool excludePrevious)
    {
        int min = CpuLevelCatalog.MinLevel;
        int max = CpuLevelCatalog.MaxLevel;
        int count = max - min + 1;
        if (!excludePrevious || previousLevel < min || previousLevel > max || count <= 1)
        {
            return randomRange(min, max + 1);
        }

        int offset = randomRange(1, count);
        return min + (previousLevel - min + offset) % count;
    }
}
