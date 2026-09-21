using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleGroundRewardService
{
    public const int HerbHealAmount = 50;
    private readonly Func<int, int, int> randomRange;

    public BattleGroundRewardService(Func<int, int, int> randomRange = null)
    {
        this.randomRange = randomRange ?? UnityEngine.Random.Range;
    }

    public int ApplyHerb(BattleGroundRunState run)
    {
        return run != null ? run.Heal(HerbHealAmount) : 0;
    }

    public List<CardData> BuildCandidates(
        BattleGroundRunState run,
        IEnumerable<CardData> unlockedCards)
    {
        var result = new List<CardData>();
        if (run == null || !run.CanAddSpecialCard || unlockedCards == null) return result;

        var owned = new HashSet<CardData>(run.SpecialCards);
        foreach (CardData card in unlockedCards)
        {
            if (card != null && !owned.Contains(card) && !result.Contains(card)) result.Add(card);
        }
        return result;
    }

    public CardData GrantRandomSpecialCard(
        BattleGroundRunState run,
        IEnumerable<CardData> unlockedCards)
    {
        List<CardData> candidates = BuildCandidates(run, unlockedCards);
        if (candidates.Count == 0) return null;
        CardData reward = candidates[randomRange(0, candidates.Count)];
        return run.AddSpecialCard(reward) ? reward : null;
    }
}
