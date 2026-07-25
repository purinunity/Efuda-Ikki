using static SpecialCardResolver;

public enum CpuFixedCardCondition
{
    Always,
    AtLeastIsso,
    AtLeastNiso,
    AtMostIsso,
    AtMostNiso,
    AtMostSanju,
    NisoThroughSuzi
}

public sealed class CpuLevelDefinition
{
    public int Level { get; }
    public int CharacterIndex => Level - 1;
    public int InitialLifePoints { get; }
    public SpecialCardId FixedCard1 { get; }
    public CpuFixedCardCondition FixedCard1Condition { get; }
    public SpecialCardId FixedCard2 { get; }
    public CpuFixedCardCondition FixedCard2Condition { get; }

    public CpuLevelDefinition(
        int level,
        int initialLifePoints,
        SpecialCardId fixedCard1,
        CpuFixedCardCondition fixedCard1Condition,
        SpecialCardId fixedCard2,
        CpuFixedCardCondition fixedCard2Condition)
    {
        Level = level;
        InitialLifePoints = initialLifePoints;
        FixedCard1 = fixedCard1;
        FixedCard1Condition = fixedCard1Condition;
        FixedCard2 = fixedCard2;
        FixedCard2Condition = fixedCard2Condition;
    }
}

public static class CpuLevelCatalog
{
    public const int MinLevel = 1;
    public const int MaxLevel = 9;

    private static readonly CpuLevelDefinition[] Levels =
    {
        new CpuLevelDefinition(1, 40, SpecialCardId.Sunny, CpuFixedCardCondition.AtLeastNiso, SpecialCardId.Seal, CpuFixedCardCondition.AtLeastNiso),
        new CpuLevelDefinition(2, 45, SpecialCardId.Rain, CpuFixedCardCondition.AtMostNiso, SpecialCardId.Seal, CpuFixedCardCondition.AtLeastNiso),
        new CpuLevelDefinition(3, 55, SpecialCardId.Bonus15, CpuFixedCardCondition.AtLeastIsso, SpecialCardId.Seal, CpuFixedCardCondition.AtLeastNiso),
        new CpuLevelDefinition(4, 70, SpecialCardId.Festival, CpuFixedCardCondition.AtMostSanju, SpecialCardId.Seal, CpuFixedCardCondition.AtLeastNiso),
        new CpuLevelDefinition(5, 90, SpecialCardId.Swap, CpuFixedCardCondition.AtMostIsso, SpecialCardId.Aiko, CpuFixedCardCondition.AtMostIsso),
        new CpuLevelDefinition(6, 115, SpecialCardId.Curse, CpuFixedCardCondition.AtLeastNiso, SpecialCardId.Aiko, CpuFixedCardCondition.AtMostIsso),
        new CpuLevelDefinition(7, 145, SpecialCardId.Bet, CpuFixedCardCondition.NisoThroughSuzi, SpecialCardId.Aiko, CpuFixedCardCondition.AtMostIsso),
        new CpuLevelDefinition(8, 180, SpecialCardId.DoubleScore, CpuFixedCardCondition.AtLeastNiso, SpecialCardId.Aiko, CpuFixedCardCondition.AtMostIsso),
        new CpuLevelDefinition(9, 220, SpecialCardId.Oni, CpuFixedCardCondition.Always, SpecialCardId.Curse, CpuFixedCardCondition.AtLeastNiso)
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
