using System.Collections.Generic;

namespace EfudaIkki.Core
{
    public static class CardPatternAnalyzer
    {
        private static readonly int[] SequenceOrder = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        public static IReadOnlyList<int> FindSequence(IEnumerable<int> numbers, int sequenceLength)
        {
            var present = new HashSet<int>(numbers ?? new int[0]);
            var current = new List<int>();
            if (sequenceLength <= 0)
            {
                return current;
            }

            foreach (int number in SequenceOrder)
            {
                if (present.Contains(number))
                {
                    current.Add(number);
                    if (current.Count >= sequenceLength)
                    {
                        return current.GetRange(current.Count - sequenceLength, sequenceLength);
                    }
                }
                else
                {
                    current.Clear();
                }
            }

            return new int[0];
        }
    }
}
