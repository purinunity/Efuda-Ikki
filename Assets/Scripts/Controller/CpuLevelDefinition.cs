using System.Collections.Generic;
using static SpecialCardResolver;

public enum CpuSpecialCardUsageMode
{
    Random,
    Judgment,
    Strategy
}

public enum CpuSpecialCardSlot
{
    Buff = 0,
    Attack = 1,
    Defense = 2,
    Flex = 3
}

public sealed class CpuLevelDefinition
{
    private readonly SpecialCardId[] specialCardIds;

    public int Level { get; }
    public int CharacterIndex => Level - 1;
    public int InitialLifePoints { get; }
    public CpuSpecialCardUsageMode UsageMode { get; }
    public IReadOnlyList<SpecialCardId> SpecialCardIds => specialCardIds;

    public CpuLevelDefinition(
        int level,
        CpuSpecialCardUsageMode usageMode,
        int initialLifePoints,
        SpecialCardId buffCard,
        SpecialCardId attackCard,
        SpecialCardId defenseCard,
        SpecialCardId flexCard)
    {
        Level = level;
        UsageMode = usageMode;
        InitialLifePoints = initialLifePoints;
        specialCardIds = new[]
        {
            buffCard,
            attackCard,
            defenseCard,
            flexCard
        };
    }

    public SpecialCardId GetCardId(CpuSpecialCardSlot slot)
    {
        int index = (int)slot;
        if (index < 0 || index >= specialCardIds.Length)
        {
            return specialCardIds[(int)CpuSpecialCardSlot.Flex];
        }

        return specialCardIds[index];
    }
}

public static class CpuLevelCatalog
{
    public const int MinLevel = 1;
    public const int MaxLevel = 9;

    private static readonly CpuLevelDefinition[] Levels =
    {
        new CpuLevelDefinition(1, CpuSpecialCardUsageMode.Random, 70, SpecialCardId.Bonus15, SpecialCardId.Seal, SpecialCardId.Aiko, SpecialCardId.Bonus10),
        new CpuLevelDefinition(2, CpuSpecialCardUsageMode.Random, 80, SpecialCardId.Bonus15, SpecialCardId.Rain, SpecialCardId.Aiko, SpecialCardId.Bonus10),
        new CpuLevelDefinition(3, CpuSpecialCardUsageMode.Random, 90, SpecialCardId.Festival, SpecialCardId.Rain, SpecialCardId.Aiko, SpecialCardId.Bonus15),
        new CpuLevelDefinition(4, CpuSpecialCardUsageMode.Judgment, 100, SpecialCardId.Sunny, SpecialCardId.Rain, SpecialCardId.Aiko, SpecialCardId.Festival),
        new CpuLevelDefinition(5, CpuSpecialCardUsageMode.Judgment, 100, SpecialCardId.Sunny, SpecialCardId.Curse, SpecialCardId.Aiko, SpecialCardId.Rain),
        new CpuLevelDefinition(6, CpuSpecialCardUsageMode.Judgment, 100, SpecialCardId.Sunny, SpecialCardId.Curse, SpecialCardId.Aiko, SpecialCardId.Bet),
        new CpuLevelDefinition(7, CpuSpecialCardUsageMode.Strategy, 110, SpecialCardId.DoubleScore, SpecialCardId.Curse, SpecialCardId.Bet, SpecialCardId.Sunny),
        new CpuLevelDefinition(8, CpuSpecialCardUsageMode.Strategy, 120, SpecialCardId.DoubleScore, SpecialCardId.Seal, SpecialCardId.Bet, SpecialCardId.Swap),
        new CpuLevelDefinition(9, CpuSpecialCardUsageMode.Strategy, 140, SpecialCardId.DoubleScore, SpecialCardId.Curse, SpecialCardId.Bet, SpecialCardId.Swap)
    };

    public static CpuLevelDefinition GetLevel(int level)
    {
        int index = ClampLevel(level) - 1;
        return Levels[index];
    }

    public static CpuLevelDefinition GetByCharacterIndex(int characterIndex)
    {
        return GetLevel(characterIndex + 1);
    }

    public static int ClampLevel(int level)
    {
        if (level < MinLevel)
        {
            return MinLevel;
        }

        return level > MaxLevel ? MaxLevel : level;
    }
}
