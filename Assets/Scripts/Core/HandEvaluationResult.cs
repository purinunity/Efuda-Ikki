using System;
using System.Collections.Generic;

namespace EfudaIkki.Core
{
    public sealed class HandEvaluationResult
    {
        private readonly int[] contributingCardIndexes;

        public HandRole Role { get; }
        public string DisplayName => HandRoleRules.GetDisplayName(Role);
        public int Score => HandRoleRules.GetScore(Role);
        public IReadOnlyList<int> ContributingCardIndexes => contributingCardIndexes;

        public HandEvaluationResult(HandRole role, IEnumerable<int> contributingCardIndexes)
        {
            Role = role;
            this.contributingCardIndexes = contributingCardIndexes == null
                ? Array.Empty<int>()
                : new List<int>(contributingCardIndexes).ToArray();
        }
    }
}
