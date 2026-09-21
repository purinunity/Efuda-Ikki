using System.Collections.Generic;
using System.Linq;

namespace EfudaIkki.Core
{
    public static class CoreHandEvaluator
    {
        private sealed class IndexedCard
        {
            public int Index { get; }
            public CardValue Value { get; }

            public IndexedCard(int index, CardValue value)
            {
                Index = index;
                Value = value;
            }
        }

        public static HandEvaluationResult Evaluate(IReadOnlyList<CardValue> cards)
        {
            var indexedCards = Index(cards);
            HandRole bestRole = HandRole.Miezu;
            IReadOnlyList<int> bestIndexes = new int[0];

            Consider(ref bestRole, ref bestIndexes, HandRole.Isso, FindNumberGroups(indexedCards, 2, 1));
            Consider(ref bestRole, ref bestIndexes, HandRole.Niso, FindNumberGroups(indexedCards, 2, 2));
            Consider(ref bestRole, ref bestIndexes, HandRole.Sanju, FindNumberGroups(indexedCards, 3, 1));
            Consider(ref bestRole, ref bestIndexes, HandRole.Yonju, FindNumberGroups(indexedCards, 4, 1));
            Consider(ref bestRole, ref bestIndexes, HandRole.Tenshu, FindCastleGroups(indexedCards, 1));
            Consider(ref bestRole, ref bestIndexes, HandRole.Suzi, FindSequence(indexedCards, 5));
            Consider(ref bestRole, ref bestIndexes, HandRole.Hikari, FindSuitGroups(indexedCards, 5, 1));
            Consider(ref bestRole, ref bestIndexes, HandRole.Nanasuzi, FindSequence(indexedCards, 7));
            Consider(ref bestRole, ref bestIndexes, HandRole.Nanahikari, FindSuitGroups(indexedCards, 7, 1));
            Consider(ref bestRole, ref bestIndexes, HandRole.Tenshukaku, FindCastleGroups(indexedCards, 2));

            return new HandEvaluationResult(bestRole, bestIndexes);
        }

        public static IReadOnlyList<int> FindContributingCardIndexes(
            IReadOnlyList<CardValue> cards,
            HandRole role)
        {
            var indexedCards = Index(cards);
            switch (role)
            {
                case HandRole.Isso: return FindNumberGroups(indexedCards, 2, 1) ?? new int[0];
                case HandRole.Niso: return FindNumberGroups(indexedCards, 2, 2) ?? new int[0];
                case HandRole.Sanju: return FindNumberGroups(indexedCards, 3, 1) ?? new int[0];
                case HandRole.Yonju: return FindNumberGroups(indexedCards, 4, 1) ?? new int[0];
                case HandRole.Tenshu: return FindCastleGroups(indexedCards, 1) ?? new int[0];
                case HandRole.Suzi: return FindSequence(indexedCards, 5) ?? new int[0];
                case HandRole.Hikari: return FindSuitGroups(indexedCards, 5, 1) ?? new int[0];
                case HandRole.Nanasuzi: return FindSequence(indexedCards, 7) ?? new int[0];
                case HandRole.Nanahikari: return FindSuitGroups(indexedCards, 7, 1) ?? new int[0];
                case HandRole.Tenshukaku: return FindCastleGroups(indexedCards, 2) ?? new int[0];
                default: return new int[0];
            }
        }

        public static int DetermineWinner(IReadOnlyList<HandEvaluationResult> hands)
        {
            if (hands == null || hands.Count == 0)
            {
                return -1;
            }

            int winner = -1;
            int highestScore = 0;
            bool tied = false;
            for (int i = 0; i < hands.Count; i++)
            {
                int score = hands[i]?.Score ?? 0;
                if (score > highestScore)
                {
                    highestScore = score;
                    winner = i;
                    tied = false;
                }
                else if (score == highestScore)
                {
                    if (winner < 0)
                    {
                        winner = i;
                    }
                    else
                    {
                        tied = true;
                    }
                }
            }

            return tied ? -1 : winner;
        }

        private static List<IndexedCard> Index(IReadOnlyList<CardValue> cards)
        {
            var result = new List<IndexedCard>();
            if (cards == null)
            {
                return result;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].HasValue)
                {
                    result.Add(new IndexedCard(i, cards[i]));
                }
            }

            return result;
        }

        private static void Consider(
            ref HandRole bestRole,
            ref IReadOnlyList<int> bestIndexes,
            HandRole candidate,
            IReadOnlyList<int> candidateIndexes)
        {
            if (candidateIndexes == null || !HandRoleRules.IsHigher(candidate, bestRole))
            {
                return;
            }

            bestRole = candidate;
            bestIndexes = candidateIndexes;
        }

        private static IReadOnlyList<int> FindNumberGroups(
            List<IndexedCard> cards,
            int minimumCount,
            int requiredGroups)
        {
            var groups = GroupByFirstOccurrence(cards, card => card.Value.Number);
            return FlattenFirstMatchingGroups(groups, group => group.Count >= minimumCount, requiredGroups);
        }

        private static IReadOnlyList<int> FindSuitGroups(
            List<IndexedCard> cards,
            int minimumCount,
            int requiredGroups)
        {
            var groups = GroupByFirstOccurrence(cards, card => card.Value.Suit);
            return FlattenFirstMatchingGroups(groups, group => group.Count >= minimumCount, requiredGroups);
        }

        private static IReadOnlyList<int> FindCastleGroups(List<IndexedCard> cards, int requiredGroups)
        {
            var groups = GroupByFirstOccurrence(cards, card => card.Value.Suit);
            return FlattenFirstMatchingGroups(
                groups,
                group => group.Any(card => card.Value.Number == 11) &&
                         group.Any(card => card.Value.Number == 12) &&
                         group.Any(card => card.Value.Number == 13),
                requiredGroups,
                group => group.Where(card => card.Value.Number >= 11 && card.Value.Number <= 13));
        }

        private static IReadOnlyList<int> FindSequence(List<IndexedCard> cards, int length)
        {
            IReadOnlyList<int> sequence = CardPatternAnalyzer.FindSequence(
                cards.Select(card => card.Value.Number),
                length);
            if (sequence.Count < length)
            {
                return null;
            }

            var numbers = new HashSet<int>(sequence);
            return cards.Where(card => numbers.Contains(card.Value.Number))
                .Select(card => card.Index)
                .ToArray();
        }

        private static List<List<IndexedCard>> GroupByFirstOccurrence(
            IEnumerable<IndexedCard> cards,
            System.Func<IndexedCard, int> keySelector)
        {
            var keys = new List<int>();
            var byKey = new Dictionary<int, List<IndexedCard>>();
            foreach (IndexedCard card in cards)
            {
                int key = keySelector(card);
                if (!byKey.TryGetValue(key, out List<IndexedCard> group))
                {
                    group = new List<IndexedCard>();
                    byKey.Add(key, group);
                    keys.Add(key);
                }

                group.Add(card);
            }

            return keys.Select(key => byKey[key]).ToList();
        }

        private static IReadOnlyList<int> FlattenFirstMatchingGroups(
            IEnumerable<List<IndexedCard>> groups,
            System.Func<List<IndexedCard>, bool> predicate,
            int requiredGroups,
            System.Func<List<IndexedCard>, IEnumerable<IndexedCard>> selector = null)
        {
            var indexes = new List<int>();
            int matchedGroups = 0;
            foreach (List<IndexedCard> group in groups)
            {
                if (!predicate(group))
                {
                    continue;
                }

                indexes.AddRange((selector == null ? group : selector(group)).Select(card => card.Index));
                matchedGroups++;
                if (matchedGroups >= requiredGroups)
                {
                    return indexes;
                }
            }

            return null;
        }
    }
}
