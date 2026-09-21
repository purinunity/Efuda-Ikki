using System;
using System.Collections.Generic;

namespace EfudaIkki.Core
{
    public static class SpecialCardOutcomeCalculator
    {
        public static int DetermineWinner(IReadOnlyList<SpecialResolvedHand> hands)
        {
            if (hands == null || hands.Count == 0)
            {
                return -1;
            }

            int winnerIndex = -1;
            int highestScore = int.MinValue;
            for (int i = 0; i < hands.Count; i++)
            {
                int score = hands[i]?.Score ?? 0;
                if (score > highestScore)
                {
                    highestScore = score;
                    winnerIndex = i;
                }
                else if (score == highestScore)
                {
                    winnerIndex = -1;
                }
            }

            return winnerIndex;
        }

        public static int CalculateDamage(
            IReadOnlyList<SpecialResolvedHand> hands,
            int winnerIndex)
        {
            return hands == null || winnerIndex < 0 || winnerIndex >= hands.Count
                ? 0
                : Math.Max(0, hands[winnerIndex]?.Score ?? 0);
        }
    }
}
