using System;
using System.Collections.Generic;
using System.Linq;

namespace EfudaIkki.Core
{
    public enum CpuHandCondition
    {
        Always = 0,
        AtLeastIsso = 1,
        AtLeastNiso = 2,
        AtMostIsso = 3,
        AtMostNiso = 4,
        AtMostSanju = 5,
        NisoThroughSuzi = 6
    }

    public readonly struct CpuFixedSpecialChoice
    {
        public string CardId { get; }
        public CpuHandCondition Condition { get; }

        public CpuFixedSpecialChoice(string cardId, CpuHandCondition condition)
        {
            CardId = cardId ?? string.Empty;
            Condition = condition;
        }
    }

    public sealed class CpuExchangeDecision
    {
        private readonly int[] discardIndexes;

        public HandRole EvaluatedRole { get; }
        public IReadOnlyList<int> DiscardIndexes => discardIndexes;

        public CpuExchangeDecision(HandRole evaluatedRole, IEnumerable<int> discardIndexes)
        {
            EvaluatedRole = evaluatedRole;
            this.discardIndexes = discardIndexes == null
                ? Array.Empty<int>()
                : discardIndexes.Distinct().ToArray();
        }
    }

    /// <summary>
    /// Unity objects and frame timingから独立したCPU交換方針。
    /// </summary>
    public static class CpuExchangePolicy
    {
        public static CpuExchangeDecision Decide(
            IReadOnlyList<CardValue> hand,
            IReadOnlyList<CardValue> commonCards,
            int maxTrashCount,
            float decisionStrength,
            IRandomSource randomSource)
        {
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            IReadOnlyList<CardValue> safeHand = hand ?? Array.Empty<CardValue>();
            var allCards = new List<CardValue>(safeHand.Count + (commonCards?.Count ?? 0));
            allCards.AddRange(safeHand);
            if (commonCards != null)
            {
                allCards.AddRange(commonCards);
            }

            HandEvaluationResult evaluation = CoreHandEvaluator.Evaluate(allCards);
            IReadOnlyList<int> planned = BuildPlannedDiscardIndexes(safeHand, allCards, evaluation);
            IReadOnlyList<int> selected = ShouldFollowPlan(decisionStrength, randomSource)
                ? planned
                : BuildRandomDiscardIndexes(safeHand.Count, maxTrashCount, randomSource);

            return new CpuExchangeDecision(evaluation.Role, selected);
        }

        private static IReadOnlyList<int> BuildPlannedDiscardIndexes(
            IReadOnlyList<CardValue> hand,
            IReadOnlyList<CardValue> allCards,
            HandEvaluationResult evaluation)
        {
            switch (evaluation.Role)
            {
                case HandRole.Miezu:
                    return Enumerable.Range(0, hand.Count).ToArray();
                case HandRole.Nanasuzi:
                case HandRole.Nanahikari:
                case HandRole.Tenshukaku:
                    return Array.Empty<int>();
                case HandRole.Tenshu:
                    return Enumerable.Range(0, hand.Count)
                        .Where(index => !hand[index].HasValue || hand[index].Number <= 10)
                        .ToArray();
                case HandRole.Hikari:
                    return FindMinoritySuitDiscardIndexes(hand, allCards);
                default:
                    var keep = new HashSet<int>(evaluation.ContributingCardIndexes);
                    return Enumerable.Range(0, hand.Count)
                        .Where(index => !keep.Contains(index))
                        .ToArray();
            }
        }

        private static IReadOnlyList<int> FindMinoritySuitDiscardIndexes(
            IReadOnlyList<CardValue> hand,
            IReadOnlyList<CardValue> allCards)
        {
            var suitOrder = new List<int>();
            var counts = new Dictionary<int, int>();
            foreach (CardValue card in allCards)
            {
                if (!card.HasValue)
                {
                    continue;
                }

                if (!counts.ContainsKey(card.Suit))
                {
                    counts.Add(card.Suit, 0);
                    suitOrder.Add(card.Suit);
                }

                counts[card.Suit]++;
            }

            if (suitOrder.Count == 0)
            {
                return Array.Empty<int>();
            }

            int minoritySuit = suitOrder[0];
            int minorityCount = counts[minoritySuit];
            foreach (int suit in suitOrder)
            {
                if (counts[suit] < minorityCount)
                {
                    minoritySuit = suit;
                    minorityCount = counts[suit];
                }
            }

            return Enumerable.Range(0, hand.Count)
                .Where(index => hand[index].HasValue && hand[index].Suit == minoritySuit)
                .ToArray();
        }

        private static bool ShouldFollowPlan(float strength, IRandomSource randomSource)
        {
            float clamped = Math.Max(0f, Math.Min(1f, strength));
            return clamped >= 0.999f || randomSource.Value01() <= clamped;
        }

        private static IReadOnlyList<int> BuildRandomDiscardIndexes(
            int handCount,
            int maxTrashCount,
            IRandomSource randomSource)
        {
            int maximum = Math.Min(Math.Max(0, maxTrashCount), Math.Max(0, handCount));
            int trashCount = randomSource.Range(0, maximum + 1);
            int[] candidates = Enumerable.Range(0, Math.Max(0, handCount)).ToArray();
            for (int i = 0; i < trashCount; i++)
            {
                int randomIndex = randomSource.Range(i, candidates.Length);
                int temporary = candidates[i];
                candidates[i] = candidates[randomIndex];
                candidates[randomIndex] = temporary;
            }

            return candidates.Take(trashCount).ToArray();
        }
    }

    /// <summary>
    /// Stable card IDsだけを扱う、Unity非依存の特殊札選択方針。
    /// </summary>
    public static class CpuSpecialCardPolicy
    {
        public static int SelectCardIndex(
            IReadOnlyList<string> candidateCardIds,
            HandRole currentRole,
            CpuFixedSpecialChoice firstChoice,
            CpuFixedSpecialChoice secondChoice,
            float decisionStrength,
            int cpuLevel,
            IRandomSource randomSource)
        {
            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            if (candidateCardIds == null || candidateCardIds.Count == 0)
            {
                return -1;
            }

            float strength = Math.Max(0f, Math.Min(1f, decisionStrength));
            bool followsFixedChoice = strength >= 0.999f || randomSource.Value01() <= strength;
            if (followsFixedChoice)
            {
                int fixedIndex = FindMatchingFixedChoice(candidateCardIds, currentRole, firstChoice);
                if (fixedIndex >= 0)
                {
                    return fixedIndex;
                }

                fixedIndex = FindMatchingFixedChoice(candidateCardIds, currentRole, secondChoice);
                if (fixedIndex >= 0)
                {
                    return fixedIndex;
                }
            }

            if (randomSource.Value01() >= GetFreeCardUseProbability(cpuLevel))
            {
                return -1;
            }

            var freeIndexes = new List<int>();
            for (int i = 0; i < candidateCardIds.Count; i++)
            {
                string id = candidateCardIds[i] ?? string.Empty;
                if (!string.Equals(id, firstChoice.CardId, StringComparison.Ordinal) &&
                    !string.Equals(id, secondChoice.CardId, StringComparison.Ordinal))
                {
                    freeIndexes.Add(i);
                }
            }

            return freeIndexes.Count == 0
                ? -1
                : freeIndexes[randomSource.Range(0, freeIndexes.Count)];
        }

        public static float GetFreeCardUseProbability(int cpuLevel)
        {
            if (cpuLevel >= 9)
            {
                return 0.3f;
            }

            return cpuLevel >= 5 ? 0.4f : 0.5f;
        }

        public static bool MatchesCondition(HandRole role, CpuHandCondition condition)
        {
            int rankIndex = (int)role;
            switch (condition)
            {
                case CpuHandCondition.Always:
                    return true;
                case CpuHandCondition.AtLeastIsso:
                    return rankIndex >= (int)HandRole.Isso;
                case CpuHandCondition.AtLeastNiso:
                    return rankIndex >= (int)HandRole.Niso;
                case CpuHandCondition.AtMostIsso:
                    return rankIndex <= (int)HandRole.Isso;
                case CpuHandCondition.AtMostNiso:
                    return rankIndex <= (int)HandRole.Niso;
                case CpuHandCondition.AtMostSanju:
                    return rankIndex <= (int)HandRole.Sanju;
                case CpuHandCondition.NisoThroughSuzi:
                    return rankIndex >= (int)HandRole.Niso && rankIndex <= (int)HandRole.Suzi;
                default:
                    return false;
            }
        }

        private static int FindMatchingFixedChoice(
            IReadOnlyList<string> candidateCardIds,
            HandRole role,
            CpuFixedSpecialChoice choice)
        {
            if (!MatchesCondition(role, choice.Condition))
            {
                return -1;
            }

            for (int i = 0; i < candidateCardIds.Count; i++)
            {
                if (string.Equals(candidateCardIds[i], choice.CardId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
