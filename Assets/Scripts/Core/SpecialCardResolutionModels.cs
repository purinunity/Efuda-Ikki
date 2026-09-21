using System;
using System.Collections.Generic;
using System.Linq;

namespace EfudaIkki.Core
{
    // Values intentionally mirror the existing SpecialCardResolver.SpecialCardId enum.
    public enum SpecialEffectKind
    {
        Aiko = 0,
        Seal = 1,
        Bonus5 = 2,
        Curse = 3,
        Bonus10 = 4,
        Bonus15 = 5,
        DoubleScore = 6,
        Bet = 7,
        Rain = 8,
        Festival = 9,
        Sunny = 10,
        Swap = 11,
        Oni = 12
    }

    public sealed class SpecialCardEffectInput
    {
        public int SourceIndex { get; }
        public int OwnerPlayerId { get; }
        public SpecialEffectKind Kind { get; }
        public int Priority { get; }
        public string DisplayName { get; }

        public SpecialCardEffectInput(
            int sourceIndex,
            int ownerPlayerId,
            SpecialEffectKind kind,
            int priority,
            string displayName)
        {
            SourceIndex = sourceIndex;
            OwnerPlayerId = ownerPlayerId;
            Kind = kind;
            Priority = priority;
            DisplayName = displayName ?? string.Empty;
        }
    }

    public sealed class SpecialCardResolutionInput
    {
        private readonly HandRole[] baseRoles;
        private readonly SpecialCardEffectInput[] effects;

        public IReadOnlyList<HandRole> BaseRoles => baseRoles;
        public IReadOnlyList<SpecialCardEffectInput> Effects => effects;
        public int? FestivalSwingOverride { get; }
        public int? BetMultiplierOverride { get; }

        public SpecialCardResolutionInput(
            IEnumerable<HandRole> baseRoles,
            IEnumerable<SpecialCardEffectInput> effects,
            int? festivalSwingOverride = null,
            int? betMultiplierOverride = null)
        {
            this.baseRoles = baseRoles?.ToArray() ?? Array.Empty<HandRole>();
            this.effects = effects?.Where(effect => effect != null).ToArray()
                ?? Array.Empty<SpecialCardEffectInput>();
            FestivalSwingOverride = festivalSwingOverride;
            BetMultiplierOverride = betMultiplierOverride;
        }
    }

    public sealed class SpecialResolvedHand
    {
        public int PlayerId { get; }
        public HandRole BaseRole { get; }
        public HandRole CurrentRole { get; internal set; }
        public int BaseScore => HandRoleRules.GetScore(BaseRole);
        public int Score { get; internal set; }

        internal SpecialResolvedHand(int playerId, HandRole baseRole)
        {
            PlayerId = playerId;
            BaseRole = baseRole;
            CurrentRole = baseRole;
            Score = HandRoleRules.GetScore(baseRole);
        }

        internal SpecialResolvedHand Clone()
        {
            return new SpecialResolvedHand(PlayerId, BaseRole)
            {
                CurrentRole = CurrentRole,
                Score = Score
            };
        }
    }

    public sealed class SpecialEffectStepResult
    {
        private readonly SpecialResolvedHand[] hands;

        public int SourceIndex { get; }
        public int OwnerPlayerId { get; }
        public SpecialEffectKind Kind { get; }
        public string EffectName { get; }
        public string Message { get; }
        public bool WasSealed { get; }
        public IReadOnlyList<SpecialResolvedHand> Hands => hands;

        internal SpecialEffectStepResult(
            SpecialCardEffectInput effect,
            string message,
            bool wasSealed,
            IEnumerable<SpecialResolvedHand> hands)
        {
            SourceIndex = effect.SourceIndex;
            OwnerPlayerId = effect.OwnerPlayerId;
            Kind = effect.Kind;
            EffectName = effect.DisplayName;
            Message = message ?? string.Empty;
            WasSealed = wasSealed;
            this.hands = hands.Select(hand => hand.Clone()).ToArray();
        }
    }

    public sealed class SpecialCardResolutionResult
    {
        private readonly SpecialResolvedHand[] hands;
        private readonly string[] logs;
        private readonly SpecialEffectStepResult[] effectSteps;

        public IReadOnlyList<SpecialResolvedHand> Hands => hands;
        public int WinnerIndex { get; }
        public int Damage { get; }
        public bool IsDraw => WinnerIndex < 0;
        public IReadOnlyList<string> Logs => logs;
        public IReadOnlyList<SpecialEffectStepResult> EffectSteps => effectSteps;

        internal SpecialCardResolutionResult(
            IEnumerable<SpecialResolvedHand> hands,
            int winnerIndex,
            int damage,
            IEnumerable<string> logs,
            IEnumerable<SpecialEffectStepResult> effectSteps)
        {
            this.hands = hands.Select(hand => hand.Clone()).ToArray();
            WinnerIndex = winnerIndex;
            Damage = damage;
            this.logs = logs?.ToArray() ?? Array.Empty<string>();
            this.effectSteps = effectSteps?.ToArray() ?? Array.Empty<SpecialEffectStepResult>();
        }
    }
}
