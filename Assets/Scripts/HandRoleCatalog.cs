using System;
using System.Collections.Generic;
using System.Linq;
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
    // 役の強さ順に並べる。点数調整はこの Score だけを変更する。
    private static readonly HandRoleDefinition[] RolesInRankOrder =
    {
        new HandRoleDefinition(HandRank.Miezu, "不見", 0),
        new HandRoleDefinition(HandRank.Isso, "一双", 5),
        new HandRoleDefinition(HandRank.Niso, "二双", 10),
        new HandRoleDefinition(HandRank.Sanju, "三珠", 20),
        new HandRoleDefinition(HandRank.Hikari, "光", 30),
        new HandRoleDefinition(HandRank.Suzi, "筋", 35),
        new HandRoleDefinition(HandRank.Yonju, "四珠", 40),
        new HandRoleDefinition(HandRank.Tenshu, "天守", 45),
        new HandRoleDefinition(HandRank.Nanahikari, "七光", 60),
        new HandRoleDefinition(HandRank.Nanasuzi, "七筋", 70),
        new HandRoleDefinition(HandRank.Tenshukaku, "天守閣", 90)
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
