using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ポーカーの役判定を行う静的クラス
public static class HandEvaluator
{
    // ポーカーの役を表す列挙型
    public enum HandRank
    {
        Miezu = 0,
        Isso = 5,
        Niso = 10,
        Sanju = 20,
        Yonju = 40,
        Tenshu = 40,
        Suzi = 50,
        Hikari = 50,
        Nanasuzi = 80,
        Nanahikari = 80,
        Tenshukaku = 100
    }

    // 役判定結果を格納するクラス
    public class HandInfo
    {
        public HandRank Rank { get; private set; }
        public string Name { get; private set; }

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
        List<Card> allCards = playerHand.Concat(commonCards).ToList();

        // 数字・スートごとの枚数を集計
        var numberCounts = allCards.GroupBy(c => c.CardData.number)
                    .ToDictionary(g => (int)g.Key, g => g.Count());

        var suitCounts = allCards.GroupBy(c => c.CardData.suit)
                    .ToDictionary(g => g.Key, g => g.Count());

        var suitGroups = allCards.GroupBy(c => c.CardData.suit)
                    .ToDictionary(g => g.Key, g => g.Select(card => card.CardData.number).ToList());
        string rankName = "不見";
        HandRank rank = HandRank.Miezu;
        // 一双判定
        foreach (var count in numberCounts.Values)
        {
            if (count >= 2)
            {
                rankName = "一双";
                rank = HandRank.Isso;
                break;
            }
        }
        // 二双判定
        if (numberCounts.Values.Count(c => c >= 2) >= 2)
        {
            rankName = "二双";
            rank = HandRank.Niso;
        }
        // 三珠判定
        foreach (var count in numberCounts.Values)
        {
            if (count >= 3)
            {
                rankName = "三珠";
                rank = HandRank.Sanju;
                break;
            }
        }
        // 四珠判定
        foreach (var count in numberCounts.Values)
        {
            if (count >= 4)
            {
                rankName = "四珠";
                rank = HandRank.Yonju;
                break;
            }
        }
        // 天守判定
        if (suitGroups.Values.Any(numbers =>
            numbers.Contains(Number.Jack) &&
            numbers.Contains(Number.Queen) &&
            numbers.Contains(Number.King)))
        {
            rankName = "天守";
            rank = HandRank.Tenshu;
        }
        // 筋判定
        int suziCount = 0;
        List<Number> nums = new List<Number> {
            Number.One, Number.Two, Number.Three,
            Number.Four, Number.Five, Number.Six,
            Number.Seven, Number.Eight, Number.Nine,
            Number.Ten, Number.Jack, Number.Queen,
            Number.King, Number.One }; // エースは高値または低値として扱う
        foreach (var num in nums)
        {
            if (numberCounts.ContainsKey((int)num))
            {
                suziCount++;
            }
            else
            {
                suziCount = 0;
            }
            if (suziCount >= 5)
            {
                rankName = "筋";
                rank = HandRank.Suzi;
                break;
            }
        }
        // 光判定
        if (suitCounts.Values.Any(number => number >= 5))
        {
            rankName = "光";
            rank = HandRank.Hikari;
        }
        // 七筋判定
        suziCount = 0;
        foreach (var num in nums)
        {
            if (numberCounts.ContainsKey((int)num))
            {
                suziCount++;
            }
            else
            {
                suziCount = 0;
            }
            if (suziCount >= 7)
            {
                rankName = "七筋";
                rank = HandRank.Nanasuzi;
                break;
            }
        }
        // 七光判定
        if (suitCounts.Values.Any(number => number >= 7))
        {
            rankName = "七光";
            rank = HandRank.Nanahikari;
        }
        // 天守閣判定
        if (suitGroups.Values.Count(numbers =>
            numbers.Contains(Number.Jack) &&
            numbers.Contains(Number.Queen) &&
            numbers.Contains(Number.King)) >= 2)
        {
            rankName = "天守閣";
            rank = HandRank.Tenshukaku;
        }
        return new HandInfo(rank, rankName);
    }
    
    public static int DetermineWinner(List<HandInfo> handInfos)
    {
        List<int> winners = new List<int>();
        HandRank highestRank = HandRank.Miezu;

        for (int i = 0; i < handInfos.Count; i++)
        {
            if (handInfos[i].Rank > highestRank)
            {
                highestRank = handInfos[i].Rank;
                winners = new List<int> { i };
            }
            else if (handInfos[i].Rank == highestRank)
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