using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// CPUプレイヤーの行動を制御するクラス
public class CPUController : Controller
{
    private const int CpuPlayerId = 1;

    [SerializeField, Range(0f, 1f)] private float exchangeDecisionStrength = 1f;
    [SerializeField, Range(0f, 1f)] private float specialCardDecisionStrength = 1f;
    private float defaultExchangeDecisionStrength;
    private float defaultSpecialCardDecisionStrength;

    private sealed class SpecialCardCandidate
    {
        public Card Card { get; }
        public int Utility { get; }

        public SpecialCardCandidate(Card card, int utility)
        {
            Card = card;
            Utility = utility;
        }
    }

    private void Awake()
    {
        defaultExchangeDecisionStrength = exchangeDecisionStrength;
        defaultSpecialCardDecisionStrength = specialCardDecisionStrength;
    }

    public void SetDecisionStrengths(float exchangeStrength, float specialCardStrength)
    {
        exchangeDecisionStrength = Mathf.Clamp01(exchangeStrength);
        specialCardDecisionStrength = Mathf.Clamp01(specialCardStrength);
    }

    public void ResetDecisionStrengths()
    {
        SetDecisionStrengths(defaultExchangeDecisionStrength, defaultSpecialCardDecisionStrength);
    }

    public void SelectSpecialCard(GameState gameState)
    {
        if (gameState == null || gameState.PlayerStates == null || gameState.PlayerStates.Count <= CpuPlayerId)
        {
            return;
        }

        PlayerState cpuState = gameState.PlayerStates[CpuPlayerId];
        List<Card> cpuSpecialCards = cpuState.SpecialCards;
        if (cpuSpecialCards == null || cpuSpecialCards.Count == 0)
        {
            return;
        }

        List<Card> usableCpuSpecialCards = cpuSpecialCards
            .Where(card => card != null && !cpuState.IsSpecialCardUsed(card))
            .ToList();
        if (usableCpuSpecialCards.Count == 0)
        {
            SelectOnly(cpuSpecialCards, null);
            return;
        }

        Dictionary<Card, bool> originalSelections = CaptureSpecialCardSelections(gameState);

        List<SpecialCardCandidate> evaluatedCandidates = new List<SpecialCardCandidate>();

        List<Card> candidates = new List<Card> { null };
        candidates.AddRange(usableCpuSpecialCards);

        foreach (Card candidate in candidates)
        {
            SelectOnly(cpuSpecialCards, candidate);
            SpecialCardResolver.ShowdownResult result = SpecialCardResolver.Resolve(
                gameState,
                festivalSwingOverride: 0,
                betMultiplierOverride: 1);
            int utility = EvaluateSpecialCardUtility(result, CpuPlayerId);

            evaluatedCandidates.Add(new SpecialCardCandidate(candidate, utility));
        }

        RestoreSpecialCardSelections(originalSelections);
        Card bestCard = ChooseSpecialCardCandidate(evaluatedCandidates);
        SelectOnly(cpuSpecialCards, bestCard);

        string selectedName = bestCard != null && bestCard.CardData != null ? bestCard.CardData.name : "No special card";
        Debug.Log($"CPU selected special card: {selectedName}");
    }

    private Card ChooseSpecialCardCandidate(List<SpecialCardCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        List<SpecialCardCandidate> rankedCandidates = candidates
            .OrderByDescending(candidate => candidate.Utility)
            .ToList();

        float strength = Mathf.Clamp01(specialCardDecisionStrength);
        if (rankedCandidates.Count == 1 || strength >= 0.999f)
        {
            return rankedCandidates[0].Card;
        }

        if (strength <= 0.001f)
        {
            return rankedCandidates[Random.Range(0, rankedCandidates.Count)].Card;
        }

        int candidatePoolSize = Mathf.Clamp(
            Mathf.CeilToInt(Mathf.Lerp(rankedCandidates.Count, 1, strength)),
            1,
            rankedCandidates.Count);

        return rankedCandidates[Random.Range(0, candidatePoolSize)].Card;
    }

    private Dictionary<Card, bool> CaptureSpecialCardSelections(GameState gameState)
    {
        Dictionary<Card, bool> selections = new Dictionary<Card, bool>();
        foreach (PlayerState playerState in gameState.PlayerStates)
        {
            if (playerState?.SpecialCards == null)
            {
                continue;
            }

            foreach (Card card in playerState.SpecialCards)
            {
                if (card != null && !selections.ContainsKey(card))
                {
                    selections.Add(card, card.IsSelected);
                }
            }
        }

        return selections;
    }

    private void RestoreSpecialCardSelections(Dictionary<Card, bool> selections)
    {
        foreach (KeyValuePair<Card, bool> selection in selections)
        {
            if (selection.Key != null)
            {
                selection.Key.IsSelected = selection.Value;
            }
        }
    }

    private void SelectOnly(List<Card> cards, Card selectedCard)
    {
        foreach (Card card in cards)
        {
            if (card == null)
            {
                continue;
            }

            card.IsSelected = card == selectedCard;
        }
    }

    private int EvaluateSpecialCardUtility(SpecialCardResolver.ShowdownResult result, int cpuPlayerId)
    {
        if (result == null)
        {
            return int.MinValue;
        }

        if (result.IsDraw)
        {
            return 0;
        }

        int damageWeight = Mathf.Max(0, result.Damage);
        if (result.WinnerIndex == cpuPlayerId)
        {
            return 10000 + damageWeight;
        }

        return -10000 - damageWeight;
    }
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
        trash = ApplyExchangeDecisionStrength(trash, playerHand, gameState.maxHandTrashCount);

        var response = new ControllerResponse
        {
            actionCompleted = true,
            cardsTrash = trash
        };
        callback?.Invoke(response);
    }

    private List<Card> ApplyExchangeDecisionStrength(List<Card> plannedTrash, List<Card> playerHand, int maxTrashCount)
    {
        float strength = Mathf.Clamp01(exchangeDecisionStrength);
        if (strength >= 0.999f || Random.value <= strength)
        {
            return plannedTrash;
        }

        return BuildRandomTrash(playerHand, maxTrashCount);
    }

    private List<Card> BuildRandomTrash(List<Card> playerHand, int maxTrashCount)
    {
        List<Card> randomTrash = new List<Card>();
        if (playerHand == null || playerHand.Count == 0)
        {
            return randomTrash;
        }

        List<Card> candidates = new List<Card>(playerHand);
        int trashCount = Random.Range(0, Mathf.Min(Mathf.Max(0, maxTrashCount), candidates.Count) + 1);

        for (int i = 0; i < trashCount; i++)
        {
            int randomIndex = Random.Range(i, candidates.Count);
            Card temp = candidates[i];
            candidates[i] = candidates[randomIndex];
            candidates[randomIndex] = temp;
            randomTrash.Add(candidates[i]);
        }

        return randomTrash;
    }
}
