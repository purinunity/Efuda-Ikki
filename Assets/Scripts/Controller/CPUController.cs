using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// CPUプレイヤーの行動を制御するクラス
public class CPUController : Controller
{
    // CPUの行動ロジック
    public override IEnumerator Act(GameState gameState, System.Action<ControllerResponse> callback)
    {
        yield return new WaitForSeconds(1.0f); // 思考時間の演出

        PlayerState playerState = gameState.GetPlayerState(); // 現在のプレイヤー状態取得
        List<Card> playerHand = playerState.HandCards; // 手札
        List<Card> commonCards = gameState.commonCards; // 共通札

        foreach (var card in playerHand)
        {
            card.IsFaceUp = true; // 役判定のためCPUの手札を表向きに設定
        }
        var handInfo = HandEvaluator.EvaluateHand(playerHand, commonCards); // 役判定
        Debug.Log($"CPUの役判定結果: {handInfo.Name} (ランク: {handInfo.Rank}, ポイント: {(int)handInfo.Rank})");
        foreach (var card in playerHand)
        {
            card.IsFaceUp = false; // CPUの手札を裏向きに戻す
        }

        List<Card> trash = new List<Card>(); // 捨てるカードリスト// 手札と共通札を結合
        List<Card> allCards = playerHand.Concat(commonCards).ToList();

        // 役ごとに捨てるカードを決定
        switch (handInfo.Name)
        {
            case "不見":
                // 不見の場合、全てのカードを捨てる
                trash.AddRange(playerHand);
                break;
            case "一双":
                // 一双の場合、ペアでないカードを全て捨てる
                var pairNumber = allCards.GroupBy(c => c.CardData.number)
                                    .Where(g => g.Count() >= 2)
                                    .Select(g => g.Key)
                                    .FirstOrDefault();
                foreach (var card in playerHand)
                {
                    if (card.CardData.number != pairNumber)
                    {
                        trash.Add(card);
                    }
                }
                break;
            case "二双":
                // 二双の場合、ペアでないカードを全て捨てる
                var pairNumbers = allCards.GroupBy(c => c.CardData.number)
                                    .Where(g => g.Count() >= 2)
                                    .Select(g => g.Key)
                                    .ToList();
                foreach (var card in playerHand)
                {
                    if (!pairNumbers.Contains(card.CardData.number))
                    {
                        trash.Add(card);
                    }
                }
                break;
            case "三珠":
                // 三珠の場合、トリプルでないカードを捨てる
                var tripleNumber = allCards.GroupBy(c => c.CardData.number)
                                    .Where(g => g.Count() >= 3)
                                    .Select(g => g.Key)
                                    .FirstOrDefault();
                foreach (var card in playerHand)
                {
                    if (card.CardData.number != tripleNumber)
                    {
                        trash.Add(card);
                    }
                }
                break;
            case "四珠":
                // 四珠の場合、トリプルでないカードを捨てる
                var quadNumber = allCards.GroupBy(c => c.CardData.number)
                                    .Where(g => g.Count() >= 4)
                                    .Select(g => g.Key)
                                    .FirstOrDefault();
                foreach (var card in playerHand)
                {
                    if (card.CardData.number != quadNumber)
                    {
                        trash.Add(card);
                    }
                }
                break;
            case "天守":
                // 天守の場合、１０以下のカードを捨てる 
                foreach (var card in playerHand)
                {
                    if ((int)card.CardData.number <= 10)
                    {
                        trash.Add(card);
                    }
                }
                break;
            case "筋":
                // 筋の場合、連続する5枚を除くすべてのカードを捨てる
                var sortedNumbers = allCards.Select(c => (int)c.CardData.number).Distinct().OrderBy(n => n).ToList();
                List<int> bestSequence = new List<int>();
                for (int i = 0; i < sortedNumbers.Count; i++)
                {
                    List<int> currentSequence = new List<int> { sortedNumbers[i] };
                    for (int j = i + 1; j < sortedNumbers.Count; j++)
                    {
                        if (sortedNumbers[j] == currentSequence.Last() + 1)
                        {
                            currentSequence.Add(sortedNumbers[j]);
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (currentSequence.Count > bestSequence.Count)
                    {
                        bestSequence = currentSequence;
                    }
                }
                foreach (var card in playerHand)
                {
                    if (!bestSequence.Contains((int)card.CardData.number))
                    {
                        trash.Add(card);
                    }
                }
                break;
            case "光":
                // 光の場合、スートが少数派であるカードを全て捨てる
                var suitCounts = allCards.GroupBy(c => c.CardData.suit)
                                    .ToDictionary(g => g.Key, g => g.Count());
                var minoritySuit = suitCounts.OrderBy(kv => kv.Value).First().Key;
                foreach (var card in playerHand)
                {
                    if (card.CardData.suit == minoritySuit)
                    {
                        trash.Add(card);
                    }
                }
                break;
            case "七筋":
                // 七筋の場合、捨てるカードはなし
                break;
            case "七光":
                // 七光の場合、捨てるカードはなし
                break;
            case "天守閣":
                // 天守閣の場合、捨てるカードはなし
                break;
            default:
                // 役がなければ全てのカードを捨てる
                trash.AddRange(playerHand);
                break;
        }

        // 行動結果をコールバック
        var response = new ControllerResponse
        {
            actionCompleted = true,
            cardsTrash = trash
        };
        callback?.Invoke(response);
    }
}