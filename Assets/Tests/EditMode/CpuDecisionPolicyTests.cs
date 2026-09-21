using System.Collections.Generic;
using NUnit.Framework;

namespace EfudaIkki.Core.Tests
{
    public sealed class CpuDecisionPolicyTests
    {
        [Test]
        public void ExchangePolicy_KeepsPairAndDiscardsOtherHandCards()
        {
            CardValue[] hand =
            {
                C(2, 1), C(2, 2), C(4, 3), C(7, 4), C(9, 1)
            };

            CpuExchangeDecision decision = CpuExchangePolicy.Decide(
                hand,
                new CardValue[0],
                5,
                1f,
                new FixedRandomSource());

            Assert.That(decision.EvaluatedRole, Is.EqualTo(HandRole.Isso));
            Assert.That(decision.DiscardIndexes, Is.EqualTo(new[] { 2, 3, 4 }));
        }

        [Test]
        public void ExchangePolicy_WithFixedRandom_IsReproducible()
        {
            CardValue[] hand =
            {
                C(1, 1), C(3, 2), C(5, 3), C(7, 4), C(9, 1)
            };

            CpuExchangeDecision first = CpuExchangePolicy.Decide(
                hand,
                new CardValue[0],
                3,
                0f,
                new FixedRandomSource(new[] { 2, 4, 2 }, new[] { 1f }));
            CpuExchangeDecision second = CpuExchangePolicy.Decide(
                hand,
                new CardValue[0],
                3,
                0f,
                new FixedRandomSource(new[] { 2, 4, 2 }, new[] { 1f }));

            Assert.That(first.DiscardIndexes, Is.EqualTo(second.DiscardIndexes));
            Assert.That(first.DiscardIndexes.Count, Is.EqualTo(2));
        }

        [Test]
        public void SpecialPolicy_SelectsFirstMatchingFixedCard()
        {
            string[] candidates = { "bonus_05", "seal", "rain" };

            int selected = CpuSpecialCardPolicy.SelectCardIndex(
                candidates,
                HandRole.Niso,
                new CpuFixedSpecialChoice("seal", CpuHandCondition.AtLeastNiso),
                new CpuFixedSpecialChoice("rain", CpuHandCondition.Always),
                1f,
                new FixedRandomSource());

            Assert.That(selected, Is.EqualTo(1));
        }

        [Test]
        public void SpecialPolicy_UsesSecondFixedCardWhenFirstConditionDoesNotMatch()
        {
            string[] candidates = { "bonus_05", "seal", "rain" };

            int selected = CpuSpecialCardPolicy.SelectCardIndex(
                candidates,
                HandRole.Isso,
                new CpuFixedSpecialChoice("seal", CpuHandCondition.AtLeastNiso),
                new CpuFixedSpecialChoice("rain", CpuHandCondition.AtMostIsso),
                1f,
                new FixedRandomSource());

            Assert.That(selected, Is.EqualTo(2));
        }

        [Test]
        public void SpecialPolicy_WithFixedRandom_IsReproducible()
        {
            string[] candidates = { "bonus_05", "bonus_10", "festival" };

            int first = CpuSpecialCardPolicy.SelectCardIndex(
                candidates,
                HandRole.Miezu,
                new CpuFixedSpecialChoice("seal", CpuHandCondition.Always),
                new CpuFixedSpecialChoice("rain", CpuHandCondition.Always),
                0f,
                new FixedRandomSource(new[] { 2 }, new[] { 1f, 0f }));
            int second = CpuSpecialCardPolicy.SelectCardIndex(
                candidates,
                HandRole.Miezu,
                new CpuFixedSpecialChoice("seal", CpuHandCondition.Always),
                new CpuFixedSpecialChoice("rain", CpuHandCondition.Always),
                0f,
                new FixedRandomSource(new[] { 2 }, new[] { 1f, 0f }));

            Assert.That(first, Is.EqualTo(2));
            Assert.That(second, Is.EqualTo(first));
        }

        [TestCase(HandRole.Miezu, CpuHandCondition.AtMostIsso, true)]
        [TestCase(HandRole.Isso, CpuHandCondition.AtLeastIsso, true)]
        [TestCase(HandRole.Miezu, CpuHandCondition.AtLeastIsso, false)]
        [TestCase(HandRole.Niso, CpuHandCondition.AtLeastNiso, true)]
        [TestCase(HandRole.Isso, CpuHandCondition.AtLeastNiso, false)]
        [TestCase(HandRole.Niso, CpuHandCondition.AtMostIsso, false)]
        [TestCase(HandRole.Niso, CpuHandCondition.AtMostNiso, true)]
        [TestCase(HandRole.Sanju, CpuHandCondition.AtMostNiso, false)]
        [TestCase(HandRole.Sanju, CpuHandCondition.AtMostSanju, true)]
        [TestCase(HandRole.Hikari, CpuHandCondition.AtMostSanju, false)]
        [TestCase(HandRole.Niso, CpuHandCondition.NisoThroughSuzi, true)]
        [TestCase(HandRole.Yonju, CpuHandCondition.NisoThroughSuzi, false)]
        [TestCase(HandRole.Tenshukaku, CpuHandCondition.AtLeastNiso, true)]
        public void SpecialConditions_AreStable(
            HandRole role,
            CpuHandCondition condition,
            bool expected)
        {
            Assert.That(CpuSpecialCardPolicy.MatchesCondition(role, condition), Is.EqualTo(expected));
        }

        private static CardValue C(int number, int suit)
        {
            return new CardValue(number, suit);
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly Queue<int> ranges;
            private readonly Queue<float> values;

            public FixedRandomSource(
                IEnumerable<int> ranges = null,
                IEnumerable<float> values = null)
            {
                this.ranges = new Queue<int>(ranges ?? new int[0]);
                this.values = new Queue<float>(values ?? new float[0]);
            }

            public int Range(int minInclusive, int maxExclusive)
            {
                if (ranges.Count == 0)
                {
                    return minInclusive;
                }

                int value = ranges.Dequeue();
                return value < minInclusive
                    ? minInclusive
                    : value >= maxExclusive ? maxExclusive - 1 : value;
            }

            public float Value01()
            {
                return values.Count > 0 ? values.Dequeue() : 0f;
            }
        }
    }
}
