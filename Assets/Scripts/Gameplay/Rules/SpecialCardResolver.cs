using System;
using System.Collections.Generic;
using System.Linq;
using EfudaIkki.Core;
using UnityEngine;
using static HandEvaluator;

public static class SpecialCardResolver
{
    public const string NoUseSpecialCardAssetName = "sp_no_use";
    public const int InitialUnlockedSpecialCardCount = 4;

    public enum SpecialCardId
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

        internal void ApplyResolvedState(int roleIndex, int score)
        {
            CurrentRoleIndex = HandRoleCatalog.ClampIndex(roleIndex);
            Score = Mathf.Max(0, score);
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

    private sealed class PendingEffect
    {
        public int OwnerPlayerId { get; }
        public Card Card { get; }
        public SpecialCardCatalog.Entry Definition { get; }

        public PendingEffect(int ownerPlayerId, Card card, SpecialCardCatalog.Entry definition)
        {
            OwnerPlayerId = ownerPlayerId;
            Card = card;
            Definition = definition;
        }
    }

    public static int SpecialCardCount => SpecialCardCatalog.UnlockableCardCount;

    public static bool TryGetSpecialCardId(CardData cardData, out SpecialCardId id)
    {
        id = default;
        if (cardData == null || string.IsNullOrWhiteSpace(cardData.name))
        {
            return false;
        }

        if (!SpecialCardCatalog.TryGet(cardData, out SpecialCardCatalog.Entry definition))
        {
            return false;
        }

        id = definition.CardId;
        return true;
    }

    public static bool IsSpecialCard(CardData cardData, SpecialCardId id)
    {
        return TryGetSpecialCardId(cardData, out SpecialCardId resolvedId) && resolvedId == id;
    }

    public static bool IsCpuOnlySpecialCard(CardData cardData)
    {
        return IsSpecialCard(cardData, SpecialCardId.Oni);
    }

    public static bool TryGetUnlockOrder(CardData cardData, out int order)
    {
        order = 0;
        if (!TryGetSpecialCardId(cardData, out SpecialCardId id))
        {
            return false;
        }

        if (!SpecialCardCatalog.TryGet(id, out SpecialCardCatalog.Entry definition) ||
            definition.UnlockOrder <= 0)
        {
            return false;
        }

        order = definition.UnlockOrder;
        return true;
    }

    public static int GetUnlockedCardCountAfterIkkiVictory(int defeatedLevel)
    {
        int unlockWins = Mathf.Clamp(defeatedLevel, 0, SpecialCardCount - InitialUnlockedSpecialCardCount);
        return InitialUnlockedSpecialCardCount + unlockWins;
    }

    public static bool IsNoUseSpecialCard(CardData cardData)
    {
        return cardData != null &&
               string.Equals(cardData.name, NoUseSpecialCardAssetName, StringComparison.Ordinal);
    }

    public static bool TryGetSpecialCardTooltip(CardData cardData, out string displayName, out string description)
    {
        displayName = string.Empty;
        description = string.Empty;

        if (cardData == null)
        {
            return false;
        }

        if (IsNoUseSpecialCard(cardData))
        {
            displayName = "使用しない";
            description = "このターンは特殊札を使用しない。";
            return true;
        }

        if (!SpecialCardCatalog.TryGet(cardData, out SpecialCardCatalog.Entry definition))
        {
            return false;
        }

        displayName = definition.DisplayName;
        description = definition.Description;
        return true;
    }

    public static ShowdownResult Resolve(
        GameState gameState,
        int? festivalSwingOverride = null,
        int? betMultiplierOverride = null)
    {
        return ResolveWithRandom(
            gameState,
            new UnityRandomSource(),
            festivalSwingOverride,
            betMultiplierOverride);
    }

    /// <summary>
    /// Resolves a showdown with an injectable random source while keeping the legacy
    /// <see cref="Resolve(GameState, int?, int?)"/> overload unambiguous for null arguments.
    /// </summary>
    public static ShowdownResult ResolveWithRandom(
        GameState gameState,
        IRandomSource randomSource,
        int? festivalSwingOverride = null,
        int? betMultiplierOverride = null)
    {
        if (gameState == null || gameState.PlayerStates == null)
        {
            return new ShowdownResult(
                new List<ResolvedHand>(),
                -1,
                0,
                new List<string>(),
                new List<EffectStep>());
        }

        randomSource = randomSource ?? new UnityRandomSource();
        List<ResolvedHand> hands = new List<ResolvedHand>();
        for (int i = 0; i < gameState.PlayerStates.Count; i++)
        {
            HandInfo baseHand = EvaluateHand(gameState.PlayerStates[i].HandCards, gameState.commonCards);
            hands.Add(new ResolvedHand(i, baseHand));
        }

        List<PendingEffect> pendingEffects = CollectEffects(gameState);
        var coreInput = new SpecialCardResolutionInput(
            hands.Select(hand => (HandRole)(int)hand.BaseHand.Rank),
            pendingEffects.Select((effect, index) => new SpecialCardEffectInput(
                index,
                effect.OwnerPlayerId,
                (SpecialEffectKind)(int)effect.Definition.CardId,
                effect.Definition.Priority,
                effect.Definition.DisplayName)),
            festivalSwingOverride,
            betMultiplierOverride);
        SpecialCardResolutionResult coreResult =
            SpecialCardEffectPolicy.Resolve(coreInput, randomSource);

        foreach (SpecialResolvedHand coreHand in coreResult.Hands)
        {
            if (coreHand.PlayerId >= 0 && coreHand.PlayerId < hands.Count)
            {
                hands[coreHand.PlayerId].ApplyResolvedState(
                    (int)coreHand.CurrentRole,
                    coreHand.Score);
            }
        }

        List<EffectStep> effectSteps = coreResult.EffectSteps
            .Select(step => CreateEffectStep(step, pendingEffects))
            .Where(step => step != null)
            .ToList();

        return new ShowdownResult(
            hands,
            coreResult.WinnerIndex,
            coreResult.Damage,
            coreResult.Logs,
            effectSteps);
    }

    private static EffectStep CreateEffectStep(
        SpecialEffectStepResult step,
        IReadOnlyList<PendingEffect> pendingEffects)
    {
        if (step == null || pendingEffects == null ||
            step.SourceIndex < 0 || step.SourceIndex >= pendingEffects.Count)
        {
            return null;
        }

        PendingEffect effect = pendingEffects[step.SourceIndex];
        SpecialResolvedHand playerHand = GetCoreHand(step.Hands, 0);
        SpecialResolvedHand cpuHand = GetCoreHand(step.Hands, 1);

        return new EffectStep(
            step.OwnerPlayerId,
            effect.Card,
            step.EffectName,
            step.Message,
            step.WasSealed,
            playerHand != null ? HandRoleRules.GetDisplayName(playerHand.CurrentRole) : string.Empty,
            cpuHand != null ? HandRoleRules.GetDisplayName(cpuHand.CurrentRole) : string.Empty,
            playerHand != null ? (HandRank)(int)playerHand.CurrentRole : HandRank.Miezu,
            cpuHand != null ? (HandRank)(int)cpuHand.CurrentRole : HandRank.Miezu,
            playerHand != null ? playerHand.Score : 0,
            cpuHand != null ? cpuHand.Score : 0);
    }

    private static SpecialResolvedHand GetCoreHand(
        IReadOnlyList<SpecialResolvedHand> hands,
        int playerId)
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

                if (IsNoUseSpecialCard(card.CardData))
                {
                    continue;
                }

                if (!SpecialCardCatalog.TryGet(card.CardData, out SpecialCardCatalog.Entry definition))
                {
                    Debug.LogWarning($"Unknown special card: {card.CardData.name}");
                    continue;
                }

                effects.Add(new PendingEffect(playerId, card, definition));
                break;
            }
        }

        return effects
            .OrderBy(effect => effect.Definition.Priority)
            .ThenBy(effect => effect.OwnerPlayerId)
            .ToList();
    }

}
