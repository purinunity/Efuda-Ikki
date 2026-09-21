using System;
using System.Collections.Generic;
using System.Linq;
using EfudaIkki.Core;
using static HandEvaluator;

public sealed class HandRoleDefinition
{
    public HandRank Rank { get; }
    public string DisplayName { get; }
    public int Score { get; }

    public HandRoleDefinition(HandRank rank, string displayName, int score)
    {
        Rank = rank;
        DisplayName = displayName;
        Score = score;
    }
}

public static class HandRoleCatalog
{
    // Public ordering is retained for serialized/UI compatibility; values come from Core.
    private static readonly HandRoleDefinition[] RolesInRankOrder =
    {
        Create(HandRank.Miezu),
        Create(HandRank.Isso),
        Create(HandRank.Niso),
        Create(HandRank.Sanju),
        Create(HandRank.Hikari),
        Create(HandRank.Suzi),
        Create(HandRank.Yonju),
        Create(HandRank.Tenshu),
        Create(HandRank.Nanahikari),
        Create(HandRank.Nanasuzi),
        Create(HandRank.Tenshukaku)
    };

    private static readonly Dictionary<HandRank, int> IndexByRank = BuildIndexByRank();
    private static readonly Dictionary<string, int> IndexByDisplayName = BuildIndexByDisplayName();

    public static IReadOnlyList<HandRoleDefinition> Roles => RolesInRankOrder;
    public static int Count => RolesInRankOrder.Length;

    public static HandRoleDefinition GetAt(int index)
    {
        return RolesInRankOrder[ClampIndex(index)];
    }

    public static HandRoleDefinition Get(HandRank rank)
    {
        return GetAt(GetIndex(rank));
    }

    public static int GetIndex(HandRank rank)
    {
        return IndexByRank.TryGetValue(rank, out int index) ? index : 0;
    }

    public static int FindIndex(HandInfo handInfo)
    {
        if (handInfo == null)
        {
            return 0;
        }

        if (!string.IsNullOrEmpty(handInfo.Name) &&
            IndexByDisplayName.TryGetValue(handInfo.Name, out int namedIndex))
        {
            return namedIndex;
        }

        return GetIndex(handInfo.Rank);
    }

    public static int ClampIndex(int index)
    {
        return Math.Max(0, Math.Min(index, RolesInRankOrder.Length - 1));
    }

    public static int GetScore(HandRank rank)
    {
        return Get(rank).Score;
    }

    public static string GetDisplayName(HandRank rank)
    {
        return Get(rank).DisplayName;
    }

    public static bool IsHigherRole(HandRank candidateRank, HandRank currentRank)
    {
        int candidateScore = GetScore(candidateRank);
        int currentScore = GetScore(currentRank);
        if (candidateScore != currentScore)
        {
            return candidateScore > currentScore;
        }

        return GetIndex(candidateRank) > GetIndex(currentRank);
    }

    public static string[] GetDisplayNames()
    {
        return RolesInRankOrder.Select(role => role.DisplayName).ToArray();
    }

    private static Dictionary<HandRank, int> BuildIndexByRank()
    {
        Dictionary<HandRank, int> map = new Dictionary<HandRank, int>();
        for (int i = 0; i < RolesInRankOrder.Length; i++)
        {
            map[RolesInRankOrder[i].Rank] = i;
        }

        return map;
    }

    private static HandRoleDefinition Create(HandRank rank)
    {
        HandRole coreRole = (HandRole)(int)rank;
        return new HandRoleDefinition(
            rank,
            HandRoleRules.GetDisplayName(coreRole),
            HandRoleRules.GetScore(coreRole));
    }

    private static Dictionary<string, int> BuildIndexByDisplayName()
    {
        Dictionary<string, int> map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < RolesInRankOrder.Length; i++)
        {
            map[RolesInRankOrder[i].DisplayName] = i;
        }

        return map;
    }
}
