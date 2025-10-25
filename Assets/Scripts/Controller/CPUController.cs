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

        var handInfo = HandEvaluator.EvaluateHandRank(playerHand, commonCards); // 役判定

        List<Card> trash = new List<Card>(); // 捨てるカードリスト

        // 役ごとに捨てるカードを決定
        switch (handInfo.Rank)
        {
            case HandEvaluator.HandRank.RoyalFlush:
            case HandEvaluator.HandRank.StraightFlush:
            case HandEvaluator.HandRank.Flush:
                // フラッシュ系の役の場合、手札からその役を構成するスートのカード以外を捨てる
                var allCards = playerHand.Concat(commonCards).ToList();
                var flushSuits = allCards.GroupBy(c => c.CardData.suit).Where(g => g.Count() >= 5).Select(g => g.Key).ToList();
                if (flushSuits.Any())
                {
                    var flushSuit = flushSuits.FirstOrDefault();
                    trash = playerHand.Where(c => c.CardData.suit != flushSuit).ToList();
                }
                break;

            case HandEvaluator.HandRank.FourOfAKind:
            case HandEvaluator.HandRank.FullHouse:
            case HandEvaluator.HandRank.Straight:
            case HandEvaluator.HandRank.ThreeOfAKind:
            case HandEvaluator.HandRank.TwoPair:
            case HandEvaluator.HandRank.OnePair:
                // 役を構成するカード以外を捨てる
                var numbersToKeep = handInfo.RankCards.ToList();
                trash = playerHand.Where(c => !numbersToKeep.Contains((int)c.CardData.number)).ToList();
                break;

            case HandEvaluator.HandRank.HighCard:
            default:
                // 役がなければ最もランクの低いカードを捨てる
                var lowCard = playerHand.OrderBy(c => (int)c.CardData.number).FirstOrDefault();
                if (lowCard != null)
                {
                    trash.Add(lowCard);
                }
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