using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EfudaIkki.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static SpecialCardResolver;

public sealed class CatalogCharacterizationTests
{
    private static readonly string[] ExpectedCharacterIds =
    {
        "bartender", "host", "cotton_candy", "boy", "gold_bob",
        "intellectual", "black_long", "bandman", "game_master"
    };

    [Test]
    public void StageCharacters_KeepStableIdsAndOneBasedLevels()
    {
        Assert.That(StageCharacterCatalog.Entries.Count, Is.EqualTo(9));
        Assert.That(StageCharacterCatalog.Entries.Select(entry => entry.Id), Is.EqualTo(ExpectedCharacterIds));
        Assert.That(StageCharacterCatalog.Entries.Select(entry => entry.Level), Is.EqualTo(Enumerable.Range(1, 9)));

        for (int level = 1; level <= 9; level++)
        {
            StageCharacterCatalog.Entry byLevel = StageCharacterCatalog.GetByLevel(level);
            Assert.That(byLevel, Is.Not.Null);
            Assert.That(byLevel.Id, Is.EqualTo(ExpectedCharacterIds[level - 1]));
            Assert.That(StageCharacterCatalog.TryGet(byLevel.Id, out StageCharacterCatalog.Entry byId), Is.True);
            Assert.That(byId, Is.SameAs(byLevel));
        }

        Assert.That(StageCharacterCatalog.GetByLevel(0), Is.Null);
        Assert.That(StageCharacterCatalog.GetByLevel(10), Is.Null);
        Assert.That(StageCharacterCatalog.ClampLevel(-10), Is.EqualTo(1));
        Assert.That(StageCharacterCatalog.ClampLevel(99), Is.EqualTo(9));
    }

    [Test]
    public void SpecialCards_KeepStableIdsPrioritiesAndUnlockOrder()
    {
        var expected = new[]
        {
            Expected(SpecialCardId.Seal, "seal", 100, 4, false),
            Expected(SpecialCardId.Rain, "rain", 200, 6, false),
            Expected(SpecialCardId.Sunny, "sunny", 300, 5, false),
            Expected(SpecialCardId.Swap, "swap", 400, 9, false),
            Expected(SpecialCardId.Bonus5, "bonus_05", 500, 1, false),
            Expected(SpecialCardId.Bonus10, "bonus_10", 600, 2, false),
            Expected(SpecialCardId.Bonus15, "bonus_15", 700, 7, false),
            Expected(SpecialCardId.Oni, "oni", 750, 0, true),
            Expected(SpecialCardId.Festival, "festival", 800, 8, false),
            Expected(SpecialCardId.Curse, "curse", 900, 10, false),
            Expected(SpecialCardId.DoubleScore, "double_score", 1000, 12, false),
            Expected(SpecialCardId.Bet, "bet", 1100, 11, false),
            Expected(SpecialCardId.Aiko, "aiko", 1200, 3, false)
        };

        Assert.That(SpecialCardCatalog.Entries.Count, Is.EqualTo(expected.Length));
        Assert.That(SpecialCardCatalog.UnlockableCardCount, Is.EqualTo(12));
        Assert.That(SpecialCardResolver.SpecialCardCount, Is.EqualTo(12));

        for (int i = 0; i < expected.Length; i++)
        {
            SpecialCardCatalog.Entry entry = SpecialCardCatalog.Entries[i];
            Assert.That(entry.CardId, Is.EqualTo(expected[i].CardId));
            Assert.That(entry.Id, Is.EqualTo(expected[i].StableId));
            Assert.That(entry.Priority, Is.EqualTo(expected[i].Priority));
            Assert.That(entry.UnlockOrder, Is.EqualTo(expected[i].UnlockOrder));
            Assert.That(entry.IsCpuOnly, Is.EqualTo(expected[i].CpuOnly));
            Assert.That(entry.TooltipIndex, Is.EqualTo((int)entry.CardId));
            Assert.That(SpecialCardCatalog.TryGet(entry.CardId, out SpecialCardCatalog.Entry byCardId), Is.True);
            Assert.That(byCardId, Is.SameAs(entry));
            Assert.That(SpecialCardCatalog.TryGet(entry.Id, out SpecialCardCatalog.Entry byStableId), Is.True);
            Assert.That(byStableId, Is.SameAs(entry));
        }

        Assert.That(SpecialCardCatalog.Entries.Select(entry => entry.Id).Distinct().Count(), Is.EqualTo(13));
        Assert.That(SpecialCardCatalog.Entries.Select(entry => entry.Priority), Is.Ordered.Ascending);
        Assert.That(
            SpecialCardCatalog.Entries.Where(entry => !entry.IsCpuOnly).Select(entry => entry.UnlockOrder),
            Is.EquivalentTo(Enumerable.Range(1, 12)));
    }

    [Test]
    public void SpecialCardLegacyAssetNames_RemainFallbackMappings()
    {
        var createdAssets = new List<CardData>();
        try
        {
            foreach (SpecialCardCatalog.Entry entry in SpecialCardCatalog.Entries)
            {
                Assert.That(entry.LegacyAssetNames, Is.Not.Empty, entry.Id);
                foreach (string legacyName in entry.LegacyAssetNames)
                {
                    CardData cardData = ScriptableObject.CreateInstance<CardData>();
                    cardData.name = legacyName;
                    createdAssets.Add(cardData);

                    Assert.That(SpecialCardCatalog.TryGet(cardData, out SpecialCardCatalog.Entry resolved), Is.True);
                    Assert.That(resolved.CardId, Is.EqualTo(entry.CardId));
                    Assert.That(SpecialCardResolver.TryGetSpecialCardId(cardData, out SpecialCardId id), Is.True);
                    Assert.That(id, Is.EqualTo(entry.CardId));
                }
            }
        }
        finally
        {
            foreach (CardData cardData in createdAssets)
            {
                Object.DestroyImmediate(cardData);
            }
        }
    }

    [Test]
    public void SpecialCardResource_MapsEveryCardDataAndTooltipByStableId()
    {
        ShowdownCutInAssetSet assetSet = AssetDatabase.LoadAssetAtPath<ShowdownCutInAssetSet>(
            "Assets/Resources/ShowdownCutInAssets.asset");
        Assert.That(assetSet, Is.Not.Null);

        FieldInfo bindingsField = typeof(ShowdownCutInAssetSet).GetField(
            "specialCardBindings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(bindingsField, Is.Not.Null);
        var bindings = bindingsField.GetValue(assetSet) as SpecialCardCatalog.AssetBinding[];
        Assert.That(bindings, Is.Not.Null);
        Assert.That(bindings.Length, Is.EqualTo(SpecialCardCatalog.Entries.Count));
        Assert.That(bindings.Select(binding => binding.Id), Is.Unique);
        Assert.That(bindings.Select(binding => binding.CardData), Is.Unique);

        foreach (SpecialCardCatalog.AssetBinding binding in bindings)
        {
            Assert.That(binding, Is.Not.Null);
            Assert.That(binding.CardData, Is.Not.Null, binding.Id);
            Assert.That(SpecialCardCatalog.TryGet(binding.Id, out SpecialCardCatalog.Entry expected), Is.True);
            Assert.That(SpecialCardCatalog.TryGet(binding.CardData, bindings, out SpecialCardCatalog.Entry resolved), Is.True);
            Assert.That(resolved, Is.SameAs(expected));

            // The resource registers CardData identity mappings when it is loaded;
            // name matching remains only the compatibility fallback.
            Assert.That(SpecialCardCatalog.TryGet(binding.CardData, out resolved), Is.True);
            Assert.That(resolved, Is.SameAs(expected));

            if (expected.IsCpuOnly)
            {
                Assert.That(binding.TooltipSprite, Is.Null, binding.Id);
                continue;
            }

            Assert.That(binding.TooltipSprite, Is.Not.Null, binding.Id);
            Assert.That(
                AssetDatabase.GetAssetPath(binding.TooltipSprite).Replace('\\', '/'),
                Is.EqualTo($"Assets/Sprite/production/ui/tooltips/special_cards/{expected.Id}.png"));
            Assert.That(assetSet.GetSpecialCardTooltipSprite(binding.CardData), Is.SameAs(binding.TooltipSprite));
        }
    }

    [Test]
    public void RegisteredCardDataIdentity_IsPreferredWhenAssetNameIsUnknown()
    {
        CardData cardData = ScriptableObject.CreateInstance<CardData>();
        cardData.name = "renamed_card_without_legacy_alias";
        try
        {
            var binding = new SpecialCardCatalog.AssetBinding();
            SetPrivateField(binding, "id", "seal");
            SetPrivateField(binding, "cardData", cardData);
            SpecialCardCatalog.RegisterBindings(new[] { binding });

            Assert.That(SpecialCardCatalog.TryGet(cardData, out SpecialCardCatalog.Entry entry), Is.True);
            Assert.That(entry.Id, Is.EqualTo("seal"));
            Assert.That(entry.CardId, Is.EqualTo(SpecialCardId.Seal));
        }
        finally
        {
            Object.DestroyImmediate(cardData);
        }
    }

    public static IEnumerable<TestCaseData> CpuLevelCases()
    {
        yield return Level(1, 40, SpecialCardId.Sunny, CpuFixedCardCondition.AtLeastNiso, SpecialCardId.Seal, CpuFixedCardCondition.AtLeastNiso);
        yield return Level(2, 45, SpecialCardId.Rain, CpuFixedCardCondition.AtMostNiso, SpecialCardId.Seal, CpuFixedCardCondition.AtLeastNiso);
        yield return Level(3, 55, SpecialCardId.Bonus15, CpuFixedCardCondition.AtLeastIsso, SpecialCardId.Seal, CpuFixedCardCondition.AtLeastNiso);
        yield return Level(4, 70, SpecialCardId.Festival, CpuFixedCardCondition.AtMostSanju, SpecialCardId.Seal, CpuFixedCardCondition.AtLeastNiso);
        yield return Level(5, 90, SpecialCardId.Swap, CpuFixedCardCondition.AtMostIsso, SpecialCardId.Aiko, CpuFixedCardCondition.AtMostIsso);
        yield return Level(6, 115, SpecialCardId.Curse, CpuFixedCardCondition.AtLeastNiso, SpecialCardId.Aiko, CpuFixedCardCondition.AtMostIsso);
        yield return Level(7, 145, SpecialCardId.Bet, CpuFixedCardCondition.NisoThroughSuzi, SpecialCardId.Aiko, CpuFixedCardCondition.AtMostIsso);
        yield return Level(8, 180, SpecialCardId.DoubleScore, CpuFixedCardCondition.AtLeastNiso, SpecialCardId.Aiko, CpuFixedCardCondition.AtMostIsso);
        yield return Level(9, 220, SpecialCardId.Oni, CpuFixedCardCondition.Always, SpecialCardId.Curse, CpuFixedCardCondition.AtLeastNiso);
    }

    [TestCaseSource(nameof(CpuLevelCases))]
    public void CpuLevelCatalog_KeepsCurrentCharacterHpAndFixedCardSettings(
        int level,
        int life,
        SpecialCardId fixedCard1,
        CpuFixedCardCondition condition1,
        SpecialCardId fixedCard2,
        CpuFixedCardCondition condition2)
    {
        CpuLevelDefinition definition = CpuLevelCatalog.GetLevel(level);

        Assert.That(definition.Level, Is.EqualTo(level));
        Assert.That(definition.CharacterIndex, Is.EqualTo(level - 1));
        Assert.That(definition.InitialLifePoints, Is.EqualTo(life));
        Assert.That(definition.FixedCard1, Is.EqualTo(fixedCard1));
        Assert.That(definition.FixedCard1Condition, Is.EqualTo(condition1));
        Assert.That(definition.FixedCard2, Is.EqualTo(fixedCard2));
        Assert.That(definition.FixedCard2Condition, Is.EqualTo(condition2));
        Assert.That(CpuLevelCatalog.GetByCharacterIndex(level - 1), Is.SameAs(definition));
    }

    [Test]
    public void CpuLevelCatalog_ClampsOutsideRangeAndDifficultyDefaultsStayStable()
    {
        Assert.That(CpuLevelCatalog.GetLevel(int.MinValue).Level, Is.EqualTo(1));
        Assert.That(CpuLevelCatalog.GetLevel(int.MaxValue).Level, Is.EqualTo(9));

        var settings = new CpuDifficultySettings();
        Assert.That(settings.ExchangeDecisionStrength, Is.EqualTo(1f));
        Assert.That(settings.SpecialCardDecisionStrength, Is.EqualTo(1f));
        Assert.That(settings.SpecialCardWinUtility, Is.EqualTo(10000));
        Assert.That(settings.SpecialCardDrawUtility, Is.EqualTo(0));
        Assert.That(settings.SpecialCardDamageUtilityWeight, Is.EqualTo(1));
        Assert.That(settings.ThinkingDelaySeconds, Is.EqualTo(1f));
    }

    [Test]
    public void CpuFixedConditionEnumValues_StayAlignedWithCorePolicy()
    {
        CpuFixedCardCondition[] legacyConditions =
        {
            CpuFixedCardCondition.Always,
            CpuFixedCardCondition.AtLeastIsso,
            CpuFixedCardCondition.AtLeastNiso,
            CpuFixedCardCondition.AtMostIsso,
            CpuFixedCardCondition.AtMostNiso,
            CpuFixedCardCondition.AtMostSanju,
            CpuFixedCardCondition.NisoThroughSuzi
        };

        for (int value = 0; value < legacyConditions.Length; value++)
        {
            Assert.That((int)legacyConditions[value], Is.EqualTo(value));
            Assert.That(((CpuHandCondition)value).ToString(), Is.EqualTo(legacyConditions[value].ToString()));
        }
    }

    private static SpecialCardExpectation Expected(
        SpecialCardId cardId,
        string stableId,
        int priority,
        int unlockOrder,
        bool cpuOnly)
    {
        return new SpecialCardExpectation(cardId, stableId, priority, unlockOrder, cpuOnly);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static TestCaseData Level(
        int level,
        int life,
        SpecialCardId fixedCard1,
        CpuFixedCardCondition condition1,
        SpecialCardId fixedCard2,
        CpuFixedCardCondition condition2)
    {
        return new TestCaseData(level, life, fixedCard1, condition1, fixedCard2, condition2)
            .SetName($"CpuLevel_{level}");
    }

    private sealed class SpecialCardExpectation
    {
        public SpecialCardId CardId { get; }
        public string StableId { get; }
        public int Priority { get; }
        public int UnlockOrder { get; }
        public bool CpuOnly { get; }

        public SpecialCardExpectation(
            SpecialCardId cardId,
            string stableId,
            int priority,
            int unlockOrder,
            bool cpuOnly)
        {
            CardId = cardId;
            StableId = stableId;
            Priority = priority;
            UnlockOrder = unlockOrder;
            CpuOnly = cpuOnly;
        }
    }
}

public sealed class StageSelectPanelTests
{
    private IProgressRepository originalRepository;

    [SetUp]
    public void SetUp()
    {
        originalRepository = GameProgressStore.Repository;
        GameProgressStore.Repository = new UnlockedProgressRepository();
        GameModeManager.ResetGameModeData();
    }

    [TearDown]
    public void TearDown()
    {
        GameModeManager.ResetGameModeData();
        GameProgressStore.Repository = originalRepository;
    }

    [Test]
    public void StableIdButtonListeners_AreIdempotentAndRemovedOnDisable()
    {
        var root = new GameObject("StageSelectPanelTest");
        try
        {
            StageSelectPanel panel = root.AddComponent<StageSelectPanel>();
            panel.titleUIManager = root.AddComponent<TitleUIManager>();
            panel.stageButtons = new Button[StageCharacterCatalog.Entries.Count];
            for (int i = 0; i < panel.stageButtons.Length; i++)
            {
                var buttonObject = new GameObject(
                    $"StageButton{i + 1}",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));
                buttonObject.transform.SetParent(root.transform, false);
                panel.stageButtons[i] = buttonObject.GetComponent<Button>();
            }

            var backObject = new GameObject(
                "BackButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            backObject.transform.SetParent(root.transform, false);
            panel.backButton = backObject.GetComponent<Button>();

            InvokePrivate(panel, "CacheUnlockedCharacterSprites");
            InvokePrivate(panel, "BuildActiveCharacterBindings");
            InvokePrivate(panel, "RegisterButtonListeners");
            InvokePrivate(panel, "RegisterButtonListeners");

            IDictionary handlers = GetPrivateField<IDictionary>(panel, "characterClickHandlers");
            Assert.That(handlers.Count, Is.EqualTo(StageCharacterCatalog.Entries.Count));

            panel.stageButtons[4].onClick.Invoke();
            Assert.That(GameModeManager.GetGameModeData().CurrentLevel, Is.EqualTo(5));

            InvokePrivate(panel, "OnDisable");
            Assert.That(handlers.Count, Is.Zero);

            GameModeManager.SetGameModeData(new GameModeData(GameModeData.GameMode.IkkiMode));
            panel.stageButtons[7].onClick.Invoke();
            Assert.That(GameModeManager.GetGameModeData().CurrentLevel, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, methodName);
        method.Invoke(target, null);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(target);
    }

    private sealed class UnlockedProgressRepository : IProgressRepository
    {
        private const string Json =
            "{\"schemaVersion\":2,\"highestUnlockedIkkiLevel\":9," +
            "\"unlockedSpecialCardCount\":12,\"ikkiCleared\":true," +
            "\"bestKachinukiStreak\":0}";

        public string GetString(string key, string defaultValue) => Json;
        public int GetInt(string key, int defaultValue) => defaultValue;
        public void SetString(string key, string value) { }
        public void SetInt(string key, int value) { }
        public void Save() { }
    }
}
