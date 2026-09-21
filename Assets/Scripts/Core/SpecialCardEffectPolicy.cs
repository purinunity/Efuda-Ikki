using System;
using System.Collections.Generic;
using System.Linq;

namespace EfudaIkki.Core
{
    public static class SpecialCardEffectPolicy
    {
        private sealed class Context
        {
            public List<SpecialResolvedHand> Hands { get; }
            public List<string> Logs { get; } = new List<string>();
            public List<SpecialEffectStepResult> Steps { get; } = new List<SpecialEffectStepResult>();
            public bool ForceDraw { get; set; }
            public bool RemainingEffectsSealed { get; set; }
            public int? FestivalSwingOverride { get; }
            public int? BetMultiplierOverride { get; }

            public Context(SpecialCardResolutionInput input)
            {
                Hands = input.BaseRoles
                    .Select((role, index) => new SpecialResolvedHand(index, role))
                    .ToList();
                FestivalSwingOverride = input.FestivalSwingOverride;
                BetMultiplierOverride = input.BetMultiplierOverride;
            }
        }

        public static SpecialCardResolutionResult Resolve(
            SpecialCardResolutionInput input,
            IRandomSource randomSource)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (randomSource == null)
            {
                throw new ArgumentNullException(nameof(randomSource));
            }

            var context = new Context(input);
            IEnumerable<SpecialCardEffectInput> orderedEffects = input.Effects
                .OrderBy(effect => effect.Priority)
                .ThenBy(effect => effect.OwnerPlayerId)
                .ThenBy(effect => effect.SourceIndex);

            foreach (SpecialCardEffectInput effect in orderedEffects)
            {
                if (effect.OwnerPlayerId < 0 || effect.OwnerPlayerId >= context.Hands.Count)
                {
                    continue;
                }

                if (context.RemainingEffectsSealed)
                {
                    string sealedMessage =
                        $"Player {effect.OwnerPlayerId}: {effect.DisplayName} was sealed.";
                    context.Logs.Add(sealedMessage);
                    context.Steps.Add(new SpecialEffectStepResult(
                        effect,
                        sealedMessage,
                        true,
                        context.Hands));
                    continue;
                }

                int logStart = context.Logs.Count;
                Apply(effect, context, randomSource);
                string message = logStart < context.Logs.Count
                    ? string.Join("\n", context.Logs.Skip(logStart))
                    : effect.DisplayName;
                context.Steps.Add(new SpecialEffectStepResult(
                    effect,
                    message,
                    false,
                    context.Hands));
            }

            int winnerIndex = context.ForceDraw
                ? -1
                : SpecialCardOutcomeCalculator.DetermineWinner(context.Hands);
            int damage = SpecialCardOutcomeCalculator.CalculateDamage(context.Hands, winnerIndex);
            return new SpecialCardResolutionResult(
                context.Hands,
                winnerIndex,
                damage,
                context.Logs,
                context.Steps);
        }

        private static void Apply(
            SpecialCardEffectInput effect,
            Context context,
            IRandomSource randomSource)
        {
            SpecialResolvedHand owner = context.Hands[effect.OwnerPlayerId];
            switch (effect.Kind)
            {
                case SpecialEffectKind.Aiko:
                    context.ForceDraw = true;
                    context.Logs.Add(
                        $"Player {effect.OwnerPlayerId}: Aiko forced the round to a draw.");
                    break;
                case SpecialEffectKind.Seal:
                    context.RemainingEffectsSealed = true;
                    context.Logs.Add(
                        $"Player {effect.OwnerPlayerId}: Seal disabled every later special card.");
                    break;
                case SpecialEffectKind.Bonus5:
                    AddScore(owner, 5);
                    context.Logs.Add($"Player {effect.OwnerPlayerId}: Bonus+5 applied.");
                    break;
                case SpecialEffectKind.Curse:
                    foreach (SpecialResolvedHand opponent in Opponents(context.Hands, effect.OwnerPlayerId))
                    {
                        AddScore(opponent, -10);
                    }
                    context.Logs.Add(
                        $"Player {effect.OwnerPlayerId}: Curse reduced the opponent by 10.");
                    break;
                case SpecialEffectKind.Bonus10:
                    AddScore(owner, 10);
                    context.Logs.Add($"Player {effect.OwnerPlayerId}: Bonus+10 applied.");
                    break;
                case SpecialEffectKind.Bonus15:
                    AddScore(owner, 15);
                    context.Logs.Add($"Player {effect.OwnerPlayerId}: Bonus+15 applied.");
                    break;
                case SpecialEffectKind.Oni:
                    AddScore(owner, 30);
                    context.Logs.Add($"Player {effect.OwnerPlayerId}: Oni+30 applied.");
                    break;
                case SpecialEffectKind.DoubleScore:
                    owner.Score = Math.Max(0, owner.Score * 2);
                    context.Logs.Add(
                        $"Player {effect.OwnerPlayerId}: DoubleScore doubled the hand score.");
                    break;
                case SpecialEffectKind.Bet:
                    int multiplier = context.BetMultiplierOverride ??
                        (randomSource.Range(0, 2) == 0 ? 0 : 2);
                    owner.Score = Math.Max(0, owner.Score * Math.Max(0, multiplier));
                    context.Logs.Add(
                        $"Player {effect.OwnerPlayerId}: Bet multiplied the hand score by {multiplier}.");
                    break;
                case SpecialEffectKind.Rain:
                    foreach (SpecialResolvedHand opponent in Opponents(context.Hands, effect.OwnerPlayerId))
                    {
                        bool changed = StepRank(opponent, -1);
                        context.Logs.Add(changed
                            ? $"Player {effect.OwnerPlayerId}: Rain lowered Player {opponent.PlayerId}'s hand by one rank."
                            : $"Player {effect.OwnerPlayerId}: Rain had no lower rank for Player {opponent.PlayerId}.");
                    }
                    break;
                case SpecialEffectKind.Festival:
                    int swing = context.FestivalSwingOverride ??
                        (randomSource.Range(0, 2) == 0 ? -20 : 20);
                    AddScore(owner, swing);
                    context.Logs.Add(
                        $"Player {effect.OwnerPlayerId}: Festival changed the hand score by " +
                        (swing >= 0 ? "+20." : "-20."));
                    break;
                case SpecialEffectKind.Sunny:
                    bool steppedUp = StepRank(owner, 1);
                    context.Logs.Add(steppedUp
                        ? $"Player {effect.OwnerPlayerId}: Sunny raised the hand by one rank."
                        : $"Player {effect.OwnerPlayerId}: Sunny had no higher rank to raise.");
                    break;
                case SpecialEffectKind.Swap:
                    SpecialResolvedHand swapTarget = Opponents(context.Hands, effect.OwnerPlayerId).FirstOrDefault();
                    if (swapTarget != null)
                    {
                        HandRole role = owner.CurrentRole;
                        int score = owner.Score;
                        owner.CurrentRole = swapTarget.CurrentRole;
                        owner.Score = swapTarget.Score;
                        swapTarget.CurrentRole = role;
                        swapTarget.Score = score;
                        context.Logs.Add(
                            $"Player {effect.OwnerPlayerId}: Swap exchanged both hand results.");
                    }
                    break;
            }
        }

        private static IEnumerable<SpecialResolvedHand> Opponents(
            IEnumerable<SpecialResolvedHand> hands,
            int ownerPlayerId)
        {
            return hands.Where(hand => hand.PlayerId != ownerPlayerId);
        }

        private static void AddScore(SpecialResolvedHand hand, int delta)
        {
            hand.Score = Math.Max(0, hand.Score + delta);
        }

        private static bool StepRank(SpecialResolvedHand hand, int delta)
        {
            int current = (int)hand.CurrentRole;
            int next = Math.Max((int)HandRole.Miezu, Math.Min((int)HandRole.Tenshukaku, current + delta));
            if (current == next)
            {
                return false;
            }

            int scoreDelta = HandRoleRules.GetScore((HandRole)next) -
                             HandRoleRules.GetScore(hand.CurrentRole);
            hand.CurrentRole = (HandRole)next;
            AddScore(hand, scoreDelta);
            return true;
        }
    }
}
