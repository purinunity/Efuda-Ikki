using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HandEvaluator;

public static class SpecialCardResolver
{
    public enum SpecialCardId
    {
        Aiko,
        Seal,
        Bonus5,
        Curse,
        Bonus10,
        Bonus15,
        DoubleScore,
        Bet,
        Rain,
        Festival,
        Sunny,
        Swap
    }

    public sealed class ResolvedHand
    {
        public int PlayerId { get; }
        public HandInfo BaseHand { get; }
        public int BaseRoleIndex { get; }
        public int CurrentRoleIndex { get; private set; }
        public HandRank CurrentRank => HandRoleCatalog.GetAt(CurrentRoleIndex).Rank;
        public int BaseScore => HandRoleCatalog.GetAt(BaseRoleIndex).Score;
        public string BaseDisplayName => HandRoleCatalog.GetAt(BaseRoleIndex).DisplayName;
        public int Score { get; private set; }
        public string DisplayName => HandRoleCatalog.GetAt(CurrentRoleIndex).DisplayName;

        public ResolvedHand(int playerId, HandInfo baseHand)
        {
            PlayerId = playerId;
            BaseHand = baseHand;
            BaseRoleIndex = HandRoleCatalog.FindIndex(baseHand);
            CurrentRoleIndex = BaseRoleIndex;
            Score = BaseScore;
        }

        public void AddScore(int delta)
        {
            Score = Mathf.Max(0, Score + delta);
        }

        public void MultiplyScore(int factor)
        {
            Score = Mathf.Max(0, Score * Mathf.Max(0, factor));
        }

        public bool StepUpRank(int steps = 1)
        {
            int newIndex = HandRoleCatalog.ClampIndex(CurrentRoleIndex + Mathf.Max(0, steps));
            if (newIndex == CurrentRoleIndex)
            {
                return false;
            }

            int delta = HandRoleCatalog.GetAt(newIndex).Score - HandRoleCatalog.GetAt(CurrentRoleIndex).Score;
            CurrentRoleIndex = newIndex;
            AddScore(delta);
            return true;
        }

        public bool StepDownRank(int steps = 1)
        {
            int newIndex = HandRoleCatalog.ClampIndex(CurrentRoleIndex - Mathf.Max(0, steps));
            if (newIndex == CurrentRoleIndex)
            {
                return false;
            }

            int delta = HandRoleCatalog.GetAt(newIndex).Score - HandRoleCatalog.GetAt(CurrentRoleIndex).Score;
            CurrentRoleIndex = newIndex;
            AddScore(delta);
            return true;
        }

        public HandSnapshot CreateSnapshot()
        {
            return new HandSnapshot(CurrentRoleIndex, Score);
        }

        public void ApplySnapshot(HandSnapshot snapshot)
        {
            CurrentRoleIndex = HandRoleCatalog.ClampIndex(snapshot.RoleIndex);
            Score = Mathf.Max(0, snapshot.Score);
        }
    }

    public readonly struct HandSnapshot
    {
        public int RoleIndex { get; }
        public HandRank Rank { get; }
        public int Score { get; }

        public HandSnapshot(int roleIndex, int score)
        {
            RoleIndex = HandRoleCatalog.ClampIndex(roleIndex);
            Rank = HandRoleCatalog.GetAt(RoleIndex).Rank;
            Score = score;
        }
    }

    public sealed class ShowdownResult
    {
        public IReadOnlyList<ResolvedHand> Hands { get; }
        public int WinnerIndex { get; }
        public int Damage { get; }
        public IReadOnlyList<string> Logs { get; }
        public IReadOnlyList<EffectStep> EffectSteps { get; }
        public bool IsDraw => WinnerIndex < 0;

        public ShowdownResult(
            IReadOnlyList<ResolvedHand> hands,
            int winnerIndex,
            int damage,
            IReadOnlyList<string> logs,
            IReadOnlyList<EffectStep> effectSteps)
        {
            Hands = hands;
            WinnerIndex = winnerIndex;
            Damage = damage;
            Logs = logs;
            EffectSteps = effectSteps;
        }
    }

    public sealed class EffectStep
    {
        public int OwnerPlayerId { get; }
        public Card Card { get; }
        public string EffectName { get; }
        public string Message { get; }
        public bool WasSealed { get; }
        public string PlayerRoleName { get; }
        public string CpuRoleName { get; }
        public HandRank PlayerRoleRank { get; }
        public HandRank CpuRoleRank { get; }
        public int PlayerScore { get; }
        public int CpuScore { get; }

        public EffectStep(
            int ownerPlayerId,
            Card card,
            string effectName,
            string message,
            bool wasSealed,
            string playerRoleName,
            string cpuRoleName,
            HandRank playerRoleRank,
            HandRank cpuRoleRank,
            int playerScore,
            int cpuScore)
        {
            OwnerPlayerId = ownerPlayerId;
            Card = card;
            EffectName = effectName;
            Message = message;
            WasSealed = wasSealed;
            PlayerRoleName = playerRoleName;
            CpuRoleName = cpuRoleName;
            PlayerRoleRank = playerRoleRank;
            CpuRoleRank = cpuRoleRank;
            PlayerScore = playerScore;
            CpuScore = cpuScore;
        }
    }

    private sealed class SpecialCardDefinition
    {
        public SpecialCardId Id { get; }
        public string DisplayName { get; }
        public int Priority { get; }
        public string[] AssetNames { get; }

        public SpecialCardDefinition(SpecialCardId id, string displayName, int priority, params string[] assetNames)
        {
            Id = id;
            DisplayName = displayName;
            Priority = priority;
            AssetNames = assetNames ?? Array.Empty<string>();
        }
    }

    private sealed class PendingEffect
    {
        public int OwnerPlayerId { get; }
        public Card Card { get; }
        public SpecialCardDefinition Definition { get; }

        public PendingEffect(int ownerPlayerId, Card card, SpecialCardDefinition definition)
        {
            OwnerPlayerId = ownerPlayerId;
            Card = card;
            Definition = definition;
        }
    }

    private sealed class ResolutionContext
    {
        public IReadOnlyList<ResolvedHand> Hands { get; }
        public List<string> Logs { get; } = new List<string>();
        public List<EffectStep> EffectSteps { get; } = new List<EffectStep>();
        public bool ForceDraw { get; set; }
        public bool AreRemainingEffectsSealed { get; private set; }
        public int DamageMultiplier { get; private set; } = 1;
        public int? FestivalSwingOverride { get; }
        public int? BetMultiplierOverride { get; }

        public ResolutionContext(List<ResolvedHand> hands, int? festivalSwingOverride, int? betMultiplierOverride)
        {
            Hands = hands;
            FestivalSwingOverride = festivalSwingOverride;
            BetMultiplierOverride = betMultiplierOverride;
        }

        public void SealRemainingEffects()
        {
            AreRemainingEffectsSealed = true;
        }

        public void DoubleDamage()
        {
            DamageMultiplier *= 2;
        }

        public IEnumerable<int> GetOpponentIds(int ownerPlayerId)
        {
            for (int i = 0; i < Hands.Count; i++)
            {
                if (i != ownerPlayerId)
                {
                    yield return i;
                }
            }
        }

        public int GetWinnerIndex()
        {
            if (ForceDraw)
            {
                return -1;
            }

            int winnerIndex = -1;
            int highestScore = int.MinValue;

            for (int i = 0; i < Hands.Count; i++)
            {
                int score = Hands[i].Score;
                if (score > highestScore)
                {
                    highestScore = score;
                    winnerIndex = i;
                    continue;
                }

                if (score == highestScore)
                {
                    winnerIndex = -1;
                }
            }

            return winnerIndex;
        }

        public int GetDamage()
        {
            int winnerIndex = GetWinnerIndex();
            if (winnerIndex < 0)
            {
                return 0;
            }

            return Mathf.Max(0, Hands[winnerIndex].Score * DamageMultiplier);
        }
    }

    // Priority is centralized here so effect order can be adjusted without
    // changing the showdown flow code.
    private static readonly SpecialCardDefinition[] Definitions =
    {
        new SpecialCardDefinition(SpecialCardId.Seal, "封札", 100, "sp 2", "sp_seal"),
        new SpecialCardDefinition(SpecialCardId.Rain, "雨札", 200, "sp 9", "sp_rain"),
        new SpecialCardDefinition(SpecialCardId.Sunny, "晴札", 300, "sp 11", "sp_sunny"),
        new SpecialCardDefinition(SpecialCardId.Swap, "換札", 400, "sp 12", "sp_swap"),
        new SpecialCardDefinition(SpecialCardId.Bonus5, "副札5", 500, "sp 3", "sp_bonus5"),
        new SpecialCardDefinition(SpecialCardId.Bonus10, "副札10", 600, "sp 5", "sp_bonus10"),
        new SpecialCardDefinition(SpecialCardId.Bonus15, "副札15", 700, "sp 6", "sp_bonus15"),
        new SpecialCardDefinition(SpecialCardId.Festival, "祭札", 800, "sp 10", "sp_festival"),
        new SpecialCardDefinition(SpecialCardId.Curse, "呪い札", 900, "sp 4", "sp_curse"),
        new SpecialCardDefinition(SpecialCardId.DoubleScore, "倍札", 1000, "sp 7", "sp_double_score"),
        new SpecialCardDefinition(SpecialCardId.Bet, "賭札", 1100, "sp 8", "sp_bet"),
        new SpecialCardDefinition(SpecialCardId.Aiko, "相子札", 1200, "sp 1", "sp_aiko")
    };

    private static readonly Dictionary<string, SpecialCardDefinition> DefinitionByAssetName = BuildDefinitionMap();

    public static ShowdownResult Resolve(
        GameState gameState,
        int? festivalSwingOverride = null,
        int? betMultiplierOverride = null)
    {
        List<ResolvedHand> hands = new List<ResolvedHand>();
        for (int i = 0; i < gameState.PlayerStates.Count; i++)
        {
            HandInfo baseHand = EvaluateHand(gameState.PlayerStates[i].HandCards, gameState.commonCards);
            hands.Add(new ResolvedHand(i, baseHand));
        }

        ResolutionContext context = new ResolutionContext(hands, festivalSwingOverride, betMultiplierOverride);
        List<PendingEffect> pendingEffects = CollectEffects(gameState);

        foreach (PendingEffect effect in pendingEffects)
        {
            if (context.AreRemainingEffectsSealed)
            {
                string sealedMessage = $"Player {effect.OwnerPlayerId}: {effect.Definition.DisplayName} was sealed.";
                context.Logs.Add(sealedMessage);
                context.EffectSteps.Add(CreateEffectStep(effect, context, sealedMessage, wasSealed: true));
                continue;
            }

            int logStartIndex = context.Logs.Count;
            ApplyEffect(effect, context);
            string message = BuildEffectStepMessage(context.Logs, logStartIndex, effect.Definition.DisplayName);
            context.EffectSteps.Add(CreateEffectStep(effect, context, message, wasSealed: false));
        }

        return new ShowdownResult(
            hands,
            context.GetWinnerIndex(),
            context.GetDamage(),
            context.Logs,
            context.EffectSteps);
    }

    private static void ApplyEffect(PendingEffect effect, ResolutionContext context)
    {
        ResolvedHand ownerHand = context.Hands[effect.OwnerPlayerId];

        switch (effect.Definition.Id)
        {
            case SpecialCardId.Aiko:
            {
                context.ForceDraw = true;
                context.Logs.Add($"Player {effect.OwnerPlayerId}: Aiko forced the round to a draw.");
                break;
            }
            case SpecialCardId.Seal:
            {
                context.SealRemainingEffects();
                context.Logs.Add($"Player {effect.OwnerPlayerId}: Seal disabled every later special card.");
                break;
            }
            case SpecialCardId.Bonus5:
            {
                ownerHand.AddScore(5);
                context.Logs.Add($"Player {effect.OwnerPlayerId}: Bonus+5 applied.");
                break;
            }
            case SpecialCardId.Curse:
            {
                foreach (int opponentId in context.GetOpponentIds(effect.OwnerPlayerId))
                {
                    context.Hands[opponentId].AddScore(-10);
                }
                context.Logs.Add($"Player {effect.OwnerPlayerId}: Curse reduced the opponent by 10.");
                break;
            }
            case SpecialCardId.Bonus10:
            {
                ownerHand.AddScore(10);
                context.Logs.Add($"Player {effect.OwnerPlayerId}: Bonus+10 applied.");
                break;
            }
            case SpecialCardId.Bonus15:
            {
                ownerHand.AddScore(15);
                context.Logs.Add($"Player {effect.OwnerPlayerId}: Bonus+15 applied.");
                break;
            }
            case SpecialCardId.DoubleScore:
            {
                ownerHand.MultiplyScore(2);
                context.Logs.Add($"Player {effect.OwnerPlayerId}: DoubleScore doubled the hand score.");
                break;
            }
            case SpecialCardId.Bet:
            {
                int multiplier = context.BetMultiplierOverride ?? (UnityEngine.Random.Range(0, 2) == 0 ? 0 : 2);
                ownerHand.MultiplyScore(multiplier);
                context.Logs.Add($"Player {effect.OwnerPlayerId}: Bet multiplied the hand score by {multiplier}.");
                break;
            }
            case SpecialCardId.Rain:
            {
                foreach (int opponentId in context.GetOpponentIds(effect.OwnerPlayerId))
                {
                    bool steppedDown = context.Hands[opponentId].StepDownRank(1);
                    if (steppedDown)
                    {
                        context.Logs.Add($"Player {effect.OwnerPlayerId}: Rain lowered Player {opponentId}'s hand by one rank.");
                    }
                    else
                    {
                        context.Logs.Add($"Player {effect.OwnerPlayerId}: Rain had no lower rank for Player {opponentId}.");
                    }
                }
                break;
            }
            case SpecialCardId.Festival:
            {
                int swing = context.FestivalSwingOverride ?? (UnityEngine.Random.Range(0, 2) == 0 ? -20 : 20);
                ownerHand.AddScore(swing);
                string direction = swing >= 0 ? "+20" : "-20";
                context.Logs.Add($"Player {effect.OwnerPlayerId}: Festival changed the hand score by {direction}.");
                break;
            }
            case SpecialCardId.Sunny:
            {
                bool steppedUp = ownerHand.StepUpRank(1);
                if (steppedUp)
                {
                    context.Logs.Add($"Player {effect.OwnerPlayerId}: Sunny raised the hand by one rank.");
                }
                else
                {
                    context.Logs.Add($"Player {effect.OwnerPlayerId}: Sunny had no higher rank to raise.");
                }
                break;
            }
            case SpecialCardId.Swap:
            {
                int opponentId = context.GetOpponentIds(effect.OwnerPlayerId).FirstOrDefault();
                if (opponentId != effect.OwnerPlayerId)
                {
                    ResolvedHand opponentHand = context.Hands[opponentId];
                    HandSnapshot ownerSnapshot = ownerHand.CreateSnapshot();
                    HandSnapshot opponentSnapshot = opponentHand.CreateSnapshot();
                    ownerHand.ApplySnapshot(opponentSnapshot);
                    opponentHand.ApplySnapshot(ownerSnapshot);
                    context.Logs.Add($"Player {effect.OwnerPlayerId}: Swap exchanged both hand results.");
                }
                break;
            }
        }
    }

    private static EffectStep CreateEffectStep(
        PendingEffect effect,
        ResolutionContext context,
        string message,
        bool wasSealed)
    {
        ResolvedHand playerHand = GetHand(context.Hands, 0);
        ResolvedHand cpuHand = GetHand(context.Hands, 1);

        return new EffectStep(
            effect.OwnerPlayerId,
            effect.Card,
            effect.Definition.DisplayName,
            message,
            wasSealed,
            playerHand != null ? playerHand.DisplayName : string.Empty,
            cpuHand != null ? cpuHand.DisplayName : string.Empty,
            playerHand != null ? playerHand.CurrentRank : HandRank.Miezu,
            cpuHand != null ? cpuHand.CurrentRank : HandRank.Miezu,
            playerHand != null ? playerHand.Score : 0,
            cpuHand != null ? cpuHand.Score : 0);
    }

    private static string BuildEffectStepMessage(List<string> logs, int startIndex, string fallback)
    {
        if (logs == null || startIndex < 0 || startIndex >= logs.Count)
        {
            return fallback;
        }

        return string.Join("\n", logs.Skip(startIndex));
    }

    private static ResolvedHand GetHand(IReadOnlyList<ResolvedHand> hands, int playerId)
    {
        if (hands == null || playerId < 0 || playerId >= hands.Count)
        {
            return null;
        }

        return hands[playerId];
    }

    private static List<PendingEffect> CollectEffects(GameState gameState)
    {
        List<PendingEffect> effects = new List<PendingEffect>();

        for (int playerId = 0; playerId < gameState.PlayerStates.Count; playerId++)
        {
            PlayerState playerState = gameState.PlayerStates[playerId];
            if (playerState == null)
            {
                continue;
            }

            List<Card> specialCards = playerState.SpecialCards;
            if (specialCards == null)
            {
                continue;
            }

            foreach (Card card in CardSelectionUtility.GetSelectedCards(
                specialCards,
                card => !playerState.IsSpecialCardUsed(card)))
            {
                if (card?.CardData == null)
                {
                    continue;
                }

                if (!DefinitionByAssetName.TryGetValue(card.CardData.name, out SpecialCardDefinition definition))
                {
                    Debug.LogWarning($"Unknown special card: {card.CardData.name}");
                    continue;
                }

                effects.Add(new PendingEffect(playerId, card, definition));
            }
        }

        return effects
            .OrderBy(effect => effect.Definition.Priority)
            .ThenBy(effect => effect.OwnerPlayerId)
            .ToList();
    }

    private static Dictionary<string, SpecialCardDefinition> BuildDefinitionMap()
    {
        Dictionary<string, SpecialCardDefinition> map = new Dictionary<string, SpecialCardDefinition>(StringComparer.Ordinal);

        foreach (SpecialCardDefinition definition in Definitions)
        {
            foreach (string assetName in definition.AssetNames)
            {
                if (string.IsNullOrWhiteSpace(assetName))
                {
                    continue;
                }

                map[assetName] = definition;
            }
        }

        return map;
    }
}
