using System.Collections.Generic;
using NUnit.Framework;

namespace EfudaIkki.Core.Tests
{
    public sealed class SpecialCardEffectPolicyTests
    {
        [TestCase(SpecialEffectKind.Bonus5, 25)]
        [TestCase(SpecialEffectKind.Bonus10, 30)]
        [TestCase(SpecialEffectKind.Bonus15, 35)]
        [TestCase(SpecialEffectKind.Oni, 50)]
        [TestCase(SpecialEffectKind.DoubleScore, 40)]
        public void ScoreEffects_ApplyExpectedScore(SpecialEffectKind kind, int expected)
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Sanju, HandRole.Isso },
                E(0, 0, kind, 100));

            Assert.That(result.Hands[0].Score, Is.EqualTo(expected));
        }

        [Test]
        public void Seal_PreventsLaterEffectByPriority()
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Isso, HandRole.Isso },
                E(0, 1, SpecialEffectKind.Bonus15, 700),
                E(1, 0, SpecialEffectKind.Seal, 100));

            Assert.That(result.Hands[1].Score, Is.EqualTo(5));
            Assert.That(result.EffectSteps[1].WasSealed, Is.True);
            Assert.That(result.EffectSteps[0].Kind, Is.EqualTo(SpecialEffectKind.Seal));
        }

        [Test]
        public void Aiko_ForcesDrawAndZeroDamage()
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Tenshu, HandRole.Isso },
                E(0, 1, SpecialEffectKind.Aiko, 1200));

            Assert.That(result.IsDraw, Is.True);
            Assert.That(result.Damage, Is.Zero);
        }

        [Test]
        public void Swap_ExchangesRoleAndScore()
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Tenshu, HandRole.Isso },
                E(0, 0, SpecialEffectKind.Swap, 400));

            Assert.That(result.Hands[0].CurrentRole, Is.EqualTo(HandRole.Isso));
            Assert.That(result.Hands[0].Score, Is.EqualTo(5));
            Assert.That(result.Hands[1].CurrentRole, Is.EqualTo(HandRole.Tenshu));
            Assert.That(result.Hands[1].Score, Is.EqualTo(45));
        }

        [Test]
        public void Effects_AreAppliedByPriorityRatherThanInputOrder()
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Sanju, HandRole.Isso },
                E(0, 0, SpecialEffectKind.Bonus5, 500),
                E(1, 1, SpecialEffectKind.Swap, 400));

            Assert.That(result.EffectSteps[0].Kind, Is.EqualTo(SpecialEffectKind.Swap));
            Assert.That(result.EffectSteps[1].Kind, Is.EqualTo(SpecialEffectKind.Bonus5));
            Assert.That(result.Hands[0].Score, Is.EqualTo(10));
            Assert.That(result.Hands[1].Score, Is.EqualTo(20));
        }

        [Test]
        public void RainAndSunny_RespectRoleBounds()
        {
            SpecialCardResolutionResult lowerBound = Resolve(
                new[] { HandRole.Miezu, HandRole.Miezu },
                E(0, 0, SpecialEffectKind.Rain, 200));
            SpecialCardResolutionResult upperBound = Resolve(
                new[] { HandRole.Tenshukaku, HandRole.Miezu },
                E(0, 0, SpecialEffectKind.Sunny, 300));

            Assert.That(lowerBound.Hands[1].CurrentRole, Is.EqualTo(HandRole.Miezu));
            Assert.That(upperBound.Hands[0].CurrentRole, Is.EqualTo(HandRole.Tenshukaku));
        }

        [Test]
        public void Curse_ClampsScoreAtZero()
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Miezu, HandRole.Isso },
                E(0, 0, SpecialEffectKind.Curse, 900));

            Assert.That(result.Hands[1].Score, Is.Zero);
        }

        [TestCase(0, 0)]
        [TestCase(1, 40)]
        public void Bet_UsesFixedRandomSource(int rangeValue, int expectedScore)
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Sanju, HandRole.Isso },
                new FixedRandomSource(rangeValue),
                E(0, 0, SpecialEffectKind.Bet, 1100));

            Assert.That(result.Hands[0].Score, Is.EqualTo(expectedScore));
        }

        [TestCase(0, 0)]
        [TestCase(1, 40)]
        public void Festival_UsesFixedRandomSource(int rangeValue, int expectedScore)
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Sanju, HandRole.Isso },
                new FixedRandomSource(rangeValue),
                E(0, 0, SpecialEffectKind.Festival, 800));

            Assert.That(result.Hands[0].Score, Is.EqualTo(expectedScore));
        }

        [Test]
        public void EqualFinalScores_AreDraw()
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Isso, HandRole.Isso });

            Assert.That(result.WinnerIndex, Is.EqualTo(-1));
            Assert.That(result.Damage, Is.Zero);
        }

        [Test]
        public void WinnerDamage_EqualsFinalWinningScore()
        {
            SpecialCardResolutionResult result = Resolve(
                new[] { HandRole.Sanju, HandRole.Isso });

            Assert.That(result.WinnerIndex, Is.EqualTo(0));
            Assert.That(result.Damage, Is.EqualTo(20));
        }

        private static SpecialCardEffectInput E(
            int sourceIndex,
            int owner,
            SpecialEffectKind kind,
            int priority)
        {
            return new SpecialCardEffectInput(sourceIndex, owner, kind, priority, kind.ToString());
        }

        private static SpecialCardResolutionResult Resolve(
            IReadOnlyList<HandRole> roles,
            params SpecialCardEffectInput[] effects)
        {
            return Resolve(roles, new FixedRandomSource(0), effects);
        }

        private static SpecialCardResolutionResult Resolve(
            IReadOnlyList<HandRole> roles,
            IRandomSource randomSource,
            params SpecialCardEffectInput[] effects)
        {
            return SpecialCardEffectPolicy.Resolve(
                new SpecialCardResolutionInput(roles, effects),
                randomSource);
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly int rangeValue;

            public FixedRandomSource(int rangeValue)
            {
                this.rangeValue = rangeValue;
            }

            public int Range(int minInclusive, int maxExclusive)
            {
                return rangeValue < minInclusive
                    ? minInclusive
                    : rangeValue >= maxExclusive ? maxExclusive - 1 : rangeValue;
            }

            public float Value01()
            {
                return rangeValue == 0 ? 0f : 1f;
            }
        }
    }
}
