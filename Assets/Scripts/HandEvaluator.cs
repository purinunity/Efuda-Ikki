using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ポーカーの役判定を行う静的クラス
public static class HandEvaluator
{
    // ポーカーの役を表す列挙型
    public enum HandRank
    {
        HighCard = 1,
        OnePair = 2,
        TwoPair = 3,
        ThreeOfAKind = 4,
        Straight = 5,
        Flush = 6,
        FullHouse = 7,
        FourOfAKind = 8,
        StraightFlush = 9,
        RoyalFlush = 10
    }

    // 役判定結果を格納するクラス
    public class HandInfo
    {
        public HandRank Rank { get; private set; }
        public string Name { get; private set; }
        public List<int> RankCards { get; private set; }

        public HandInfo(HandRank rank, string name, List<int> rankCards)
        {
            Rank = rank;
            Name = name;
            RankCards = rankCards;
        }
    }

    // 手札と共通札から役を判定するメソッド
    public static HandInfo EvaluateHandRank(List<Card> playerHand, List<Card> commonCards)
    {
    // 手札と共通札を結合
    List<Card> allCards = playerHand.Concat(commonCards).ToList();

        // カードが無い場合はハイカード扱い
        if (allCards == null || allCards.Count == 0)
        {
            return new HandInfo(HandRank.HighCard, "No Cards", new List<int>());
        }

    // 数字・スートごとの枚数を集計
    var numberCounts = allCards.GroupBy(c => c.CardData.number)
                   .ToDictionary(g => (int)g.Key, g => g.Count());

    var suitCounts = allCards.GroupBy(c => c.CardData.suit)
                 .ToDictionary(g => g.Key, g => g.Count());

    var numbers = allCards.Select(c => (int)c.CardData.number).OrderByDescending(n => n).ToList();

    // 各役の判定（ストレートフラッシュ→フォーカード→フルハウス...の順）
    var straightFlushInfo = GetStraightFlushInfo(allCards);
    if (straightFlushInfo != null) return straightFlushInfo;

    var fourKindInfo = GetFourOfAKindInfo(numberCounts);
    if (fourKindInfo != null) return fourKindInfo;

    var fullHouseInfo = GetFullHouseInfo(numberCounts);
    if (fullHouseInfo != null) return fullHouseInfo;

        var flushInfo = GetFlushInfo(suitCounts, numbers);
        if (flushInfo != null) return flushInfo;

        var straightInfo = GetStraightInfo(numbers);
        if (straightInfo != null) return straightInfo;

        var threeKindInfo = GetThreeOfAKindInfo(numberCounts);
        if (threeKindInfo != null) return threeKindInfo;

        var twoPairInfo = GetTwoPairInfo(numberCounts);
        if (twoPairInfo != null) return twoPairInfo;

        var onePairInfo = GetOnePairInfo(numberCounts);
        if (onePairInfo != null) return onePairInfo;

        return new HandInfo(HandRank.HighCard, "High Card", numbers);
    }

    // 以下に欠けていたヘルパーメソッドを追加します
    private static HandInfo GetStraightFlushInfo(List<Card> cards)
    {
        var flushSuits = cards.GroupBy(c => c.CardData.suit).Where(g => g.Count() >= 5).Select(g => g.Key).ToList();
        if (!flushSuits.Any()) return null;

        foreach (var suit in flushSuits)
        {
            var flushCards = cards.Where(c => c.CardData.suit == suit).OrderByDescending(c => c.CardData.number).ToList();
            if (flushCards.Count < 5) continue;

            var straightNumbers = GetStraightNumbers(flushCards.Select(c => (int)c.CardData.number).ToList());
            if (straightNumbers != null)
            {
                if (straightNumbers.SequenceEqual(new List<int> { 14, 13, 12, 11, 10 }))
                {
                    return new HandInfo(HandRank.RoyalFlush, "Royal Flush", straightNumbers);
                }
                return new HandInfo(HandRank.StraightFlush, "Straight Flush", straightNumbers);
            }
        }
        return null;
    }

    private static HandInfo GetFourOfAKindInfo(Dictionary<int, int> numberCounts)
    {
        var fourKind = numberCounts.FirstOrDefault(p => p.Value == 4);
        if (fourKind.Value == 4)
        {
            var kicker = numberCounts.Keys.Where(n => n != fourKind.Key).OrderByDescending(n => n).ToList();
            var rankCards = new List<int> { fourKind.Key };
            rankCards.AddRange(kicker);
            return new HandInfo(HandRank.FourOfAKind, "Four of a Kind", rankCards);
        }
        return null;
    }

    private static HandInfo GetFullHouseInfo(Dictionary<int, int> numberCounts)
    {
        var threeKind = numberCounts.FirstOrDefault(p => p.Value == 3);
        if (threeKind.Value == 3)
        {
            var pair = numberCounts.FirstOrDefault(p => p.Value == 2);
            if (pair.Value == 2)
            {
                return new HandInfo(HandRank.FullHouse, "Full House", new List<int> { threeKind.Key, pair.Key });
            }
        }
        return null;
    }

    private static HandInfo GetFlushInfo(Dictionary<Suit, int> suitCounts, List<int> numbers)
    {
        var flushSuit = suitCounts.FirstOrDefault(p => p.Value >= 5);
        if (flushSuit.Value >= 5)
        {
            return new HandInfo(HandRank.Flush, "Flush", numbers.Take(5).ToList());
        }
        return null;
    }

    private static HandInfo GetStraightInfo(List<int> numbers)
    {
        var straightNumbers = GetStraightNumbers(numbers);
        if (straightNumbers != null)
        {
            return new HandInfo(HandRank.Straight, "Straight", straightNumbers);
        }
        return null;
    }

    private static HandInfo GetThreeOfAKindInfo(Dictionary<int, int> numberCounts)
    {
        var threeKind = numberCounts.FirstOrDefault(p => p.Value == 3);
        if (threeKind.Value == 3)
        {
            var kicker = numberCounts.Keys.Where(n => n != threeKind.Key).OrderByDescending(n => n).ToList();
            var rankCards = new List<int> { threeKind.Key };
            rankCards.AddRange(kicker);
            return new HandInfo(HandRank.ThreeOfAKind, "Three of a Kind", rankCards);
        }
        return null;
    }

    private static HandInfo GetTwoPairInfo(Dictionary<int, int> numberCounts)
    {
        var pairs = numberCounts.Where(p => p.Value == 2).Select(p => p.Key).OrderByDescending(k => k).ToList();
        if (pairs.Count >= 2)
        {
            var kicker = numberCounts.Keys.Where(n => !pairs.Contains(n)).OrderByDescending(n => n).ToList();
            var rankCards = pairs.Take(2).ToList();
            rankCards.AddRange(kicker);
            return new HandInfo(HandRank.TwoPair, "Two Pair", rankCards);
        }
        return null;
    }

    private static HandInfo GetOnePairInfo(Dictionary<int, int> numberCounts)
    {
        var pair = numberCounts.FirstOrDefault(p => p.Value == 2);
        if (pair.Value == 2)
        {
            var kicker = numberCounts.Keys.Where(n => n != pair.Key).OrderByDescending(n => n).ToList();
            var rankCards = new List<int> { pair.Key };
            rankCards.AddRange(kicker);
            return new HandInfo(HandRank.OnePair, "One Pair", rankCards);
        }
        return null;
    }

    private static List<int> GetStraightNumbers(List<int> numbers)
    {
        var uniqueNumbers = numbers.Distinct().OrderBy(n => n).ToList();
        if (uniqueNumbers.Count < 5) return null;

        for (int i = 0; i <= uniqueNumbers.Count - 5; i++)
        {
            if (uniqueNumbers[i + 4] - uniqueNumbers[i] == 4)
            {
                return uniqueNumbers.Skip(i).Take(5).OrderByDescending(n => n).ToList();
            }
        }

        // A-2-3-4-5 のストレート (Aを1として扱う)
        if (uniqueNumbers.Contains(14) && uniqueNumbers.Contains(2) && uniqueNumbers.Contains(3) && uniqueNumbers.Contains(4) && uniqueNumbers.Contains(5))
        {
            return new List<int> { 5, 4, 3, 2, 1 };
        }

        return null;
    }

    public static int CompareHands(HandInfo hand1, HandInfo hand2)
    {
        if (hand1 == null && hand2 == null) return 0;
        if (hand1 == null) return -1;
        if (hand2 == null) return 1;

        if (hand1.Rank > hand2.Rank) return 1;
        if (hand1.Rank < hand2.Rank) return -1;

        if (hand1.RankCards == null && hand2.RankCards == null) return 0;
        if (hand1.RankCards == null) return -1;
        if (hand2.RankCards == null) return 1;

        for (int i = 0; i < Mathf.Min(hand1.RankCards.Count, hand2.RankCards.Count); i++)
        {
            if (hand1.RankCards[i] > hand2.RankCards[i]) return 1;
            if (hand1.RankCards[i] < hand2.RankCards[i]) return -1;
        }

        return 0;
    }
}