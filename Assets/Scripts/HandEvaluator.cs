using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ポーカーの役判定を行う静的クラス
public static class HandEvaluator
{
    // ポーカーの役を表す列挙型
    public enum HandRank
    {
        Miezu,
        Isso,
        Niso,
        Sanju,
        Hikari,
        Suzi,
        Yonju,
        Tenshu,
        Nanahikari,
        Nanasuzi,
        Tenshukaku
    }

    // 役判定結果を格納するクラス
    public class HandInfo
    {
        public HandRank Rank { get; private set; }
        public string Name { get; private set; }
        public int Score => HandRoleCatalog.GetScore(Rank);

        public HandInfo(HandRank rank, string name)
        {
            Rank = rank;
            Name = name;
        }
    }

    // 手札と共通札から役を判定するメソッド
    public static HandInfo EvaluateHand(List<Card> playerHand, List<Card> commonCards)
    {
        // 手札が5枚でない場合は役表示を行わない（空文字を返す）
        if (playerHand == null || playerHand.Count != 5)
        {
            return new HandInfo(HandRank.Miezu, "");
        }

        // 表向きになっていないカードが含まれている場合は判定不能とする（空文字を返す）
        if (playerHand.Any(card => !card.IsFaceUp))
        {
            return new HandInfo(HandRank.Miezu, "");
        }
        if (commonCards != null && commonCards.Any(card => !card.IsFaceUp))
        {
            return new HandInfo(HandRank.Miezu, "");
        }

        // 手札と共通札を結合
        List<Card> allCards = playerHand.Concat(commonCards ?? Enumerable.Empty<Card>()).ToList();

        // 数字・スートごとの枚数を集計
        Dictionary<Number, List<Card>> numberGroups = CardPatternUtility.BuildNumberGroups(allCards);
        Dictionary<Suit, List<Card>> suitGroups = CardPatternUtility.BuildSuitGroups(allCards);
        string rankName = HandRoleCatalog.GetDisplayName(HandRank.Miezu);
        HandRank rank = HandRank.Miezu;
        // 一双判定
        foreach (List<Card> group in numberGroups.Values)
        {
            if (group.Count >= 2)
            {
                SetBestHand(ref rank, ref rankName, HandRank.Isso);
                break;
            }
        }
        // 二双判定
        if (numberGroups.Values.Count(group => group.Count >= 2) >= 2)
        {
            SetBestHand(ref rank, ref rankName, HandRank.Niso);
        }
        // 三珠判定
        foreach (List<Card> group in numberGroups.Values)
        {
            if (group.Count >= 3)
            {
                SetBestHand(ref rank, ref rankName, HandRank.Sanju);
                break;
            }
        }
        // 四珠判定
        foreach (List<Card> group in numberGroups.Values)
        {
            if (group.Count >= 4)
            {
                SetBestHand(ref rank, ref rankName, HandRank.Yonju);
                break;
            }
        }
        // 天守判定
        if (suitGroups.Values.Any(cards =>
            CardPatternUtility.HasNumber(cards, Number.Jack) &&
            CardPatternUtility.HasNumber(cards, Number.Queen) &&
            CardPatternUtility.HasNumber(cards, Number.King)))
        {
            SetBestHand(ref rank, ref rankName, HandRank.Tenshu);
        }
        // 筋判定
        if (CardPatternUtility.FindSequence(numberGroups, 5).Count >= 5)
        {
            SetBestHand(ref rank, ref rankName, HandRank.Suzi);
        }
        // 光判定
        if (suitGroups.Values.Any(cards => cards.Count >= 5))
        {
            SetBestHand(ref rank, ref rankName, HandRank.Hikari);
        }
        // 七筋判定
        if (CardPatternUtility.FindSequence(numberGroups, 7).Count >= 7)
        {
            SetBestHand(ref rank, ref rankName, HandRank.Nanasuzi);
        }
        // 七光判定
        if (suitGroups.Values.Any(cards => cards.Count >= 7))
        {
            SetBestHand(ref rank, ref rankName, HandRank.Nanahikari);
        }
        // 天守閣判定
        if (suitGroups.Values.Count(cards =>
            CardPatternUtility.HasNumber(cards, Number.Jack) &&
            CardPatternUtility.HasNumber(cards, Number.Queen) &&
            CardPatternUtility.HasNumber(cards, Number.King)) >= 2)
        {
            SetBestHand(ref rank, ref rankName, HandRank.Tenshukaku);
        }
        return new HandInfo(rank, rankName);
    }

    private static void SetBestHand(ref HandRank rank, ref string rankName, HandRank candidateRank)
    {
        if (HandRoleCatalog.IsHigherRole(candidateRank, rank))
        {
            rank = candidateRank;
            rankName = HandRoleCatalog.GetDisplayName(candidateRank);
        }
    }
    
    public static int DetermineWinner(List<HandInfo> handInfos)
    {
        List<int> winners = new List<int>();
        int highestScore = HandRoleCatalog.GetScore(HandRank.Miezu);

        for (int i = 0; i < handInfos.Count; i++)
        {
            int score = handInfos[i] != null ? handInfos[i].Score : 0;
            if (score > highestScore)
            {
                highestScore = score;
                winners = new List<int> { i };
            }
            else if (score == highestScore)
            {
                winners.Add(i);
            }
        }

        if (winners.Count > 1)
        {
            return -1; // 引き分け
        }
        if (winners.Count == 0)
        {
            return -1; // 引き分け
        }

        return winners[0]; // 勝者のインデックスを返す
    }
}
