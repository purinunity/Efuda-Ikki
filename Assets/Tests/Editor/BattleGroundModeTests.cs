using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleGroundModeTests
{
    private readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object item in created) Object.DestroyImmediate(item);
        created.Clear();
    }

    [Test]
    public void RunState_CarriesLifeAndOnlyUnusedCardsAfterVictory()
    {
        CardData firstData = MakeData("first");
        CardData secondData = MakeData("second");
        Card first = MakeCard(firstData);
        Card second = MakeCard(secondData);
        var player = new PlayerState(0);
        player.SetSpecialCardsForMatch(new[] { first, second });
        player.SetLifePoints(63);
        player.MarkSpecialCardUsed(first);
        var run = new BattleGroundRunState(new[] { firstData, secondData });

        run.CaptureVictory(player);

        Assert.That(run.PlayerLife, Is.EqualTo(63));
        Assert.That(run.WinStreak, Is.EqualTo(1));
        Assert.That(run.SpecialCards, Is.EquivalentTo(new[] { secondData }));
    }

    [Test]
    public void Herb_HealsFiftyAndCapsAtOneHundred()
    {
        var run = new BattleGroundRunState(null);
        var player = new PlayerState(0);
        player.SetLifePoints(72);
        run.CaptureVictory(player);

        int life = new BattleGroundRewardService().ApplyHerb(run);

        Assert.That(life, Is.EqualTo(100));
    }

    [Test]
    public void RunState_StartsAtOneHundredAndKeepsAtMostFourUniqueCards()
    {
        CardData a = MakeData("a"); CardData b = MakeData("b");
        CardData c = MakeData("c"); CardData d = MakeData("d"); CardData e = MakeData("e");

        var run = new BattleGroundRunState(new[] { a, b, a, c, d, e });

        Assert.That(run.PlayerLife, Is.EqualTo(100));
        Assert.That(run.WinStreak, Is.Zero);
        Assert.That(run.SpecialCards, Is.EqualTo(new[] { a, b, c, d }));
    }

    [Test]
    public void SpecialReward_DoesNotDuplicateAndNeverExceedsFourCards()
    {
        CardData a = MakeData("a"); CardData b = MakeData("b");
        CardData c = MakeData("c"); CardData d = MakeData("d"); CardData e = MakeData("e");
        var run = new BattleGroundRunState(new[] { a, b, c });
        var rewards = new BattleGroundRewardService((min, max) => max - 1);

        CardData reward = rewards.GrantRandomSpecialCard(run, new[] { a, b, c, d, e });
        CardData blocked = rewards.GrantRandomSpecialCard(run, new[] { a, b, c, d, e });

        Assert.That(reward, Is.SameAs(e));
        Assert.That(blocked, Is.Null);
        Assert.That(run.SpecialCards.Count, Is.EqualTo(4));
    }

    [Test]
    public void SpecialReward_HasNoCandidatesWhenEveryUnlockedCardIsOwned()
    {
        CardData a = MakeData("a"); CardData b = MakeData("b");
        var run = new BattleGroundRunState(new[] { a, b });

        List<CardData> candidates = new BattleGroundRewardService().BuildCandidates(run, new[] { a, b, a });

        Assert.That(candidates, Is.Empty);
    }

    [Test]
    public void OpponentSelector_UsesLevelsOneThroughNineAndExcludesPrevious()
    {
        var first = new BattleGroundOpponentSelector((min, max) => max - 1);
        var next = new BattleGroundOpponentSelector((min, max) => min);

        Assert.That(first.Select(1, false), Is.EqualTo(CpuLevelCatalog.MaxLevel));
        Assert.That(next.Select(5, true), Is.EqualTo(6));
        Assert.That(next.Select(5, true), Is.Not.EqualTo(5));
    }

    [Test]
    public void VisualAsset_ContainsEveryBattleGroundMapping()
    {
        BattleGroundVisualAssets assets = Resources.Load<BattleGroundVisualAssets>("BattleGroundVisualAssets");
        Assert.That(assets, Is.Not.Null);
        Assert.That(assets.battleBackground, Is.Not.Null);
        Assert.That(assets.roundCounterFrame, Is.Not.Null);
        Assert.That(assets.normalCardBack, Is.Not.Null);
        Assert.That(assets.specialCardBack, Is.Not.Null);
        Assert.That(assets.playerCharacter, Is.Not.Null);
        Assert.That(assets.cpuCharacters, Has.Length.EqualTo(9));
        Assert.That(assets.specialCards, Has.Length.EqualTo(13));
    }

    private CardData MakeData(string name)
    {
        CardData data = ScriptableObject.CreateInstance<CardData>();
        data.name = name; created.Add(data); return data;
    }

    private Card MakeCard(CardData data)
    {
        GameObject go = new GameObject(data.name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Card));
        created.Add(go);
        Card card = go.GetComponent<Card>(); card.SetCardData(data); return card;
    }
}
