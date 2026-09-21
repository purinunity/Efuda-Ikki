using System.Collections.Generic;
using NUnit.Framework;

namespace EfudaIkki.Core.Tests
{
    public sealed class CoreHandEvaluatorTests
    {
        private const int Flowers = 1;
        private const int Birds = 2;
        private const int Wind = 3;
        private const int Moon = 4;

        public static IEnumerable<TestCaseData> EveryRoleCases()
        {
            yield return RoleCase(HandRole.Miezu, new[]
            {
                C(1, Flowers), C(3, Birds), C(5, Wind), C(8, Moon), C(10, Flowers)
            });
            yield return RoleCase(HandRole.Isso, new[]
            {
                C(2, Flowers), C(2, Birds), C(5, Wind), C(8, Moon), C(10, Flowers)
            }, 0, 1);
            yield return RoleCase(HandRole.Niso, new[]
            {
                C(2, Flowers), C(2, Birds), C(5, Wind), C(5, Moon), C(9, Flowers)
            }, 0, 1, 2, 3);
            yield return RoleCase(HandRole.Sanju, new[]
            {
                C(3, Flowers), C(3, Birds), C(3, Wind), C(7, Moon), C(10, Flowers)
            }, 0, 1, 2);
            yield return RoleCase(HandRole.Hikari, new[]
            {
                C(1, Flowers), C(3, Flowers), C(5, Flowers), C(8, Flowers), C(10, Flowers)
            }, 0, 1, 2, 3, 4);
            yield return RoleCase(HandRole.Suzi, new[]
            {
                C(1, Flowers), C(2, Birds), C(3, Wind), C(4, Moon), C(5, Flowers)
            }, 0, 1, 2, 3, 4);
            yield return RoleCase(HandRole.Yonju, new[]
            {
                C(4, Flowers), C(4, Birds), C(4, Wind), C(4, Moon), C(9, Flowers)
            }, 0, 1, 2, 3);
            yield return RoleCase(HandRole.Tenshu, new[]
            {
                C(11, Flowers), C(12, Flowers), C(13, Flowers), C(2, Flowers), C(7, Flowers)
            }, 0, 1, 2);
            yield return RoleCase(HandRole.Nanahikari, new[]
            {
                C(1, Flowers), C(1, Flowers), C(3, Flowers), C(3, Flowers),
                C(5, Flowers), C(8, Flowers), C(10, Flowers)
            }, 0, 1, 2, 3, 4, 5, 6);
            yield return RoleCase(HandRole.Nanasuzi, new[]
            {
                C(1, Flowers), C(2, Birds), C(3, Wind), C(4, Moon),
                C(5, Flowers), C(6, Birds), C(7, Wind)
            }, 0, 1, 2, 3, 4, 5, 6);
            yield return RoleCase(HandRole.Tenshukaku, new[]
            {
                C(11, Flowers), C(12, Flowers), C(13, Flowers),
                C(11, Birds), C(12, Birds), C(13, Birds)
            }, 0, 1, 2, 3, 4, 5);
        }

        [TestCaseSource(nameof(EveryRoleCases))]
        public void Evaluate_ReturnsExpectedRoleAndContributingIndexes(
            HandRole expectedRole,
            CardValue[] cards,
            int[] expectedIndexes)
        {
            HandEvaluationResult result = CoreHandEvaluator.Evaluate(cards);

            Assert.That(result.Role, Is.EqualTo(expectedRole));
            Assert.That(result.ContributingCardIndexes, Is.EqualTo(expectedIndexes));
            Assert.That(result.DisplayName, Is.EqualTo(HandRoleRules.GetDisplayName(expectedRole)));
            Assert.That(result.Score, Is.EqualTo(HandRoleRules.GetScore(expectedRole)));
        }

        [Test]
        public void FiveCardSequence_DoesNotUseJackQueenOrKing()
        {
            CardValue[] cards =
            {
                C(7, Flowers), C(8, Birds), C(9, Wind), C(10, Moon), C(11, Flowers)
            };

            Assert.That(CoreHandEvaluator.Evaluate(cards).Role, Is.EqualTo(HandRole.Miezu));
            Assert.That(CardPatternAnalyzer.FindSequence(new[] { 7, 8, 9, 10, 11 }, 5), Is.Empty);
        }

        [Test]
        public void SevenCardSequence_DoesNotUseJackQueenOrKing()
        {
            CardValue[] cards =
            {
                C(7, Flowers), C(8, Birds), C(9, Wind), C(10, Moon),
                C(11, Flowers), C(12, Birds), C(13, Wind)
            };

            Assert.That(CoreHandEvaluator.Evaluate(cards).Role, Is.EqualTo(HandRole.Miezu));
            Assert.That(CardPatternAnalyzer.FindSequence(new[] { 7, 8, 9, 10, 11, 12, 13 }, 7), Is.Empty);
        }

        [Test]
        public void SequenceContributors_IncludeEveryDuplicateOfASequenceNumber()
        {
            CardValue[] cards =
            {
                C(1, Flowers), C(2, Birds), C(3, Wind), C(3, Moon), C(4, Flowers), C(5, Birds)
            };

            HandEvaluationResult result = CoreHandEvaluator.Evaluate(cards);

            Assert.That(result.Role, Is.EqualTo(HandRole.Suzi));
            Assert.That(result.ContributingCardIndexes, Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void DetermineWinner_ReturnsMinusOneForEqualScores()
        {
            var hands = new[]
            {
                new HandEvaluationResult(HandRole.Suzi, null),
                new HandEvaluationResult(HandRole.Suzi, null)
            };

            Assert.That(CoreHandEvaluator.DetermineWinner(hands), Is.EqualTo(-1));
        }

        [Test]
        public void DetermineWinner_ReturnsIndexOfOnlyHighestScore()
        {
            var hands = new[]
            {
                new HandEvaluationResult(HandRole.Niso, null),
                new HandEvaluationResult(HandRole.Tenshu, null),
                new HandEvaluationResult(HandRole.Sanju, null)
            };

            Assert.That(CoreHandEvaluator.DetermineWinner(hands), Is.EqualTo(1));
        }

        [TestCase(HandRole.Miezu, 0, "不見", 0)]
        [TestCase(HandRole.Isso, 1, "一双", 5)]
        [TestCase(HandRole.Niso, 2, "二双", 10)]
        [TestCase(HandRole.Sanju, 3, "三珠", 20)]
        [TestCase(HandRole.Hikari, 4, "光", 30)]
        [TestCase(HandRole.Suzi, 5, "筋", 35)]
        [TestCase(HandRole.Yonju, 6, "四珠", 40)]
        [TestCase(HandRole.Tenshu, 7, "天守", 45)]
        [TestCase(HandRole.Nanahikari, 8, "七光", 60)]
        [TestCase(HandRole.Nanasuzi, 9, "七筋", 70)]
        [TestCase(HandRole.Tenshukaku, 10, "天守閣", 90)]
        public void LegacyCompatibleRoleContract_IsStable(
            HandRole role,
            int expectedValue,
            string expectedName,
            int expectedScore)
        {
            Assert.That((int)role, Is.EqualTo(expectedValue));
            Assert.That(HandRoleRules.GetDisplayName(role), Is.EqualTo(expectedName));
            Assert.That(HandRoleRules.GetScore(role), Is.EqualTo(expectedScore));
        }

        private static TestCaseData RoleCase(
            HandRole role,
            CardValue[] cards,
            params int[] contributingIndexes)
        {
            return new TestCaseData(role, cards, contributingIndexes).SetName(role.ToString());
        }

        private static CardValue C(int number, int suit)
        {
            return new CardValue(number, suit);
        }
    }
}
