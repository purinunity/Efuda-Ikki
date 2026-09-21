using System.Collections.Generic;
using EfudaIkki.Core;
using NUnit.Framework;
using UnityEngine;
using static SpecialCardResolver;

public sealed class SpecialCardResolverTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void EqualHandsWithoutSpecialCards_AreDrawWithNoDamage()
    {
        GameState gameState = CreateGameState(PairHand(), PairHand());

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(gameState, new FixedRandomSource());

        Assert.That(result.IsDraw, Is.True);
        Assert.That(result.WinnerIndex, Is.EqualTo(-1));
        Assert.That(result.Damage, Is.EqualTo(0));
        Assert.That(result.EffectSteps, Is.Empty);
    }

    [Test]
    public void LegacyResolveOverload_AcceptsNullFestivalOverride()
    {
        GameState gameState = CreateGameState(PairHand(), PairHand());

        ShowdownResult result = SpecialCardResolver.Resolve(gameState, null);

        Assert.That(result.IsDraw, Is.True);
        Assert.That(result.Damage, Is.Zero);
    }

    [Test]
    public void SpecialCardEnumValues_StayAlignedWithCoreEffects()
    {
        SpecialCardId[] orderedIds =
        {
            SpecialCardId.Aiko,
            SpecialCardId.Seal,
            SpecialCardId.Bonus5,
            SpecialCardId.Curse,
            SpecialCardId.Bonus10,
            SpecialCardId.Bonus15,
            SpecialCardId.DoubleScore,
            SpecialCardId.Bet,
            SpecialCardId.Rain,
            SpecialCardId.Festival,
            SpecialCardId.Sunny,
            SpecialCardId.Swap,
            SpecialCardId.Oni
        };

        for (int value = 0; value < orderedIds.Length; value++)
        {
            Assert.That((int)orderedIds[value], Is.EqualTo(value));
            Assert.That((SpecialEffectKind)value, Is.EqualTo((SpecialEffectKind)(int)orderedIds[value]));
            Assert.That(((SpecialEffectKind)value).ToString(), Is.EqualTo(orderedIds[value].ToString()));
        }
    }

    [TestCase(SpecialCardId.Bonus5, 10)]
    [TestCase(SpecialCardId.Bonus10, 15)]
    [TestCase(SpecialCardId.Bonus15, 20)]
    [TestCase(SpecialCardId.Oni, 35)]
    public void AdditiveCards_AddExpectedScoreAndWinnerDamage(SpecialCardId cardId, int expectedScore)
    {
        GameState gameState = CreateGameState(PairHand(), PairHand());
        SelectSpecialCard(gameState, 0, cardId);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(gameState, new FixedRandomSource());

        Assert.That(result.Hands[0].Score, Is.EqualTo(expectedScore));
        Assert.That(result.Hands[1].Score, Is.EqualTo(5));
        Assert.That(result.WinnerIndex, Is.EqualTo(0));
        Assert.That(result.Damage, Is.EqualTo(expectedScore));
    }

    [Test]
    public void Curse_ReducesOpponentButNeverBelowZero()
    {
        GameState gameState = CreateGameState(PairHand(), PairHand());
        SelectSpecialCard(gameState, 0, SpecialCardId.Curse);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(gameState, new FixedRandomSource());

        Assert.That(result.Hands[0].Score, Is.EqualTo(5));
        Assert.That(result.Hands[1].Score, Is.EqualTo(0));
        Assert.That(result.WinnerIndex, Is.EqualTo(0));
        Assert.That(result.Damage, Is.EqualTo(5));
    }

    [Test]
    public void DoubleScore_DoublesCurrentScore()
    {
        GameState gameState = CreateGameState(PairHand(), PairHand());
        SelectSpecialCard(gameState, 0, SpecialCardId.DoubleScore);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(gameState, new FixedRandomSource());

        Assert.That(result.Hands[0].Score, Is.EqualTo(10));
        Assert.That(result.WinnerIndex, Is.EqualTo(0));
        Assert.That(result.Damage, Is.EqualTo(10));
    }

    [TestCase(0, 0, 1, 5)]
    [TestCase(1, 10, 0, 10)]
    public void Bet_UsesInjectedRandomSource(int randomValue, int expectedPlayerScore, int winner, int damage)
    {
        GameState gameState = CreateGameState(PairHand(), PairHand());
        SelectSpecialCard(gameState, 0, SpecialCardId.Bet);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(
            gameState,
            new FixedRandomSource(randomValue));

        Assert.That(result.Hands[0].Score, Is.EqualTo(expectedPlayerScore));
        Assert.That(result.WinnerIndex, Is.EqualTo(winner));
        Assert.That(result.Damage, Is.EqualTo(damage));
    }

    [TestCase(0, 0, 1, 5)]
    [TestCase(1, 25, 0, 25)]
    public void Festival_UsesInjectedRandomSourceAndClampsAtZero(
        int randomValue,
        int expectedPlayerScore,
        int winner,
        int damage)
    {
        GameState gameState = CreateGameState(PairHand(), PairHand());
        SelectSpecialCard(gameState, 0, SpecialCardId.Festival);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(
            gameState,
            new FixedRandomSource(randomValue));

        Assert.That(result.Hands[0].Score, Is.EqualTo(expectedPlayerScore));
        Assert.That(result.WinnerIndex, Is.EqualTo(winner));
        Assert.That(result.Damage, Is.EqualTo(damage));
    }

    [Test]
    public void Sunny_RaisesOwnerOneCatalogRank()
    {
        GameState gameState = CreateGameState(PairHand(), PairHand());
        SelectSpecialCard(gameState, 0, SpecialCardId.Sunny);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(gameState, new FixedRandomSource());

        Assert.That(result.Hands[0].CurrentRank, Is.EqualTo(HandEvaluator.HandRank.Niso));
        Assert.That(result.Hands[0].Score, Is.EqualTo(10));
        Assert.That(result.WinnerIndex, Is.EqualTo(0));
    }

    [Test]
    public void Rain_LowersOpponentOneCatalogRank()
    {
        GameState gameState = CreateGameState(PairHand(), TwoPairHand());
        SelectSpecialCard(gameState, 0, SpecialCardId.Rain);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(gameState, new FixedRandomSource());

        Assert.That(result.Hands[1].CurrentRank, Is.EqualTo(HandEvaluator.HandRank.Isso));
        Assert.That(result.Hands[1].Score, Is.EqualTo(5));
        Assert.That(result.IsDraw, Is.True);
        Assert.That(result.Damage, Is.EqualTo(0));
    }

    [Test]
    public void Swap_ExchangesRoleAndScoreSnapshots()
    {
        GameState gameState = CreateGameState(TwoPairHand(), PairHand());
        SelectSpecialCard(gameState, 0, SpecialCardId.Swap);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(gameState, new FixedRandomSource());

        Assert.That(result.Hands[0].CurrentRank, Is.EqualTo(HandEvaluator.HandRank.Isso));
        Assert.That(result.Hands[0].Score, Is.EqualTo(5));
        Assert.That(result.Hands[1].CurrentRank, Is.EqualTo(HandEvaluator.HandRank.Niso));
        Assert.That(result.Hands[1].Score, Is.EqualTo(10));
        Assert.That(result.WinnerIndex, Is.EqualTo(1));
        Assert.That(result.Damage, Is.EqualTo(10));
    }

    [Test]
    public void Aiko_ForcesDrawAndZeroDamage()
    {
        GameState gameState = CreateGameState(TwoPairHand(), PairHand());
        SelectSpecialCard(gameState, 0, SpecialCardId.Aiko);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(gameState, new FixedRandomSource());

        Assert.That(result.IsDraw, Is.True);
        Assert.That(result.Damage, Is.EqualTo(0));
    }

    [Test]
    public void Seal_HasPriorityAndMarksEveryLaterEffectAsSealed()
    {
        GameState gameState = CreateGameState(TwoPairHand(), PairHand());
        SelectSpecialCard(gameState, 0, SpecialCardId.Seal);
        SelectSpecialCard(gameState, 1, SpecialCardId.Aiko);

        ShowdownResult result = SpecialCardResolver.ResolveWithRandom(gameState, new FixedRandomSource());

        Assert.That(result.EffectSteps.Count, Is.EqualTo(2));
        Assert.That(result.EffectSteps[0].OwnerPlayerId, Is.EqualTo(0));
        Assert.That(result.EffectSteps[0].WasSealed, Is.False);
        Assert.That(result.EffectSteps[1].OwnerPlayerId, Is.EqualTo(1));
        Assert.That(result.EffectSteps[1].WasSealed, Is.True);
        Assert.That(result.WinnerIndex, Is.EqualTo(0), "Aiko must not apply after Seal.");
        Assert.That(result.Damage, Is.EqualTo(10));
    }

    [Test]
    public void RankAdjustment_StopsAtBothCatalogBounds()
    {
        var minimum = new ResolvedHand(
            0,
            new HandEvaluator.HandInfo(HandEvaluator.HandRank.Miezu, string.Empty));
        var maximum = new ResolvedHand(
            1,
            new HandEvaluator.HandInfo(
                HandEvaluator.HandRank.Tenshukaku,
                HandRoleCatalog.GetDisplayName(HandEvaluator.HandRank.Tenshukaku)));

        Assert.That(minimum.StepDownRank(), Is.False);
        Assert.That(minimum.CurrentRank, Is.EqualTo(HandEvaluator.HandRank.Miezu));
        Assert.That(minimum.Score, Is.EqualTo(0));
        Assert.That(maximum.StepUpRank(), Is.False);
        Assert.That(maximum.CurrentRank, Is.EqualTo(HandEvaluator.HandRank.Tenshukaku));
        Assert.That(maximum.Score, Is.EqualTo(90));
    }

    private GameState CreateGameState(CardSpec[] playerHand, CardSpec[] cpuHand)
    {
        var gameState = new GameState();
        gameState.InitializePlayerStates();
        AddHand(gameState.PlayerStates[0], playerHand);
        AddHand(gameState.PlayerStates[1], cpuHand);
        return gameState;
    }

    private void AddHand(PlayerState playerState, IEnumerable<CardSpec> specs)
    {
        foreach (CardSpec spec in specs)
        {
            playerState.AddCardToHand(CreateCard(spec.Number, spec.Suit, $"basic_{spec.Number}_{spec.Suit}"));
        }
    }

    private void SelectSpecialCard(GameState gameState, int playerId, SpecialCardId cardId)
    {
        Assert.That(SpecialCardCatalog.TryGet(cardId, out SpecialCardCatalog.Entry entry), Is.True);
        string assetName = entry.LegacyAssetNames[0];
        Card card = CreateCard(Number.Joker, Suit.Joker, assetName);
        gameState.PlayerStates[playerId].SetSpecialCardsForMatch(new[] { card });
        card.IsSelected = true;
    }

    private Card CreateCard(Number number, Suit suit, string assetName)
    {
        CardData cardData = ScriptableObject.CreateInstance<CardData>();
        cardData.name = assetName;
        cardData.number = number;
        cardData.suit = suit;
        createdObjects.Add(cardData);

        var gameObject = new GameObject($"Test Card {assetName}");
        createdObjects.Add(gameObject);
        Card card = gameObject.AddComponent<Card>();
        card.SetCardData(cardData);
        card.IsFaceUp = true;
        return card;
    }

    private static CardSpec[] PairHand()
    {
        return new[]
        {
            C(Number.Two, Suit.Flowers), C(Number.Two, Suit.Birds),
            C(Number.Five, Suit.Wind), C(Number.Eight, Suit.Moon), C(Number.Ten, Suit.Flowers)
        };
    }

    private static CardSpec[] TwoPairHand()
    {
        return new[]
        {
            C(Number.Two, Suit.Flowers), C(Number.Two, Suit.Birds),
            C(Number.Five, Suit.Wind), C(Number.Five, Suit.Moon), C(Number.Nine, Suit.Flowers)
        };
    }

    private static CardSpec C(Number number, Suit suit)
    {
        return new CardSpec(number, suit);
    }

    private readonly struct CardSpec
    {
        public Number Number { get; }
        public Suit Suit { get; }

        public CardSpec(Number number, Suit suit)
        {
            Number = number;
            Suit = suit;
        }
    }

    private sealed class FixedRandomSource : IRandomSource
    {
        private readonly Queue<int> values;

        public FixedRandomSource(params int[] values)
        {
            this.values = new Queue<int>(values ?? new int[0]);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            int value = values.Count > 0 ? values.Dequeue() : minInclusive;
            Assert.That(value, Is.GreaterThanOrEqualTo(minInclusive));
            Assert.That(value, Is.LessThan(maxExclusive));
            return value;
        }

        public float Value01()
        {
            return 0f;
        }
    }
}
