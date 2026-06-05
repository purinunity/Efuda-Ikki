using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// CPUプレイヤーの行動を制御するクラス
public class CPUController : Controller
{
    private const int CpuPlayerId = 1;

    [Header("Default CPU Difficulty")]
    [Tooltip("Used as the fallback difficulty and copied per character when CpuCharacterSettings are applied.")]
    [SerializeField] private CpuDifficultySettings difficultySettings = new CpuDifficultySettings();
    private CpuDifficultySettings defaultDifficultySettings;
    private CpuLevelDefinition currentLevelDefinition = CpuLevelCatalog.GetLevel(CpuLevelCatalog.MinLevel);

    private void Awake()
    {
        EnsureDifficultySettings();
        defaultDifficultySettings = difficultySettings.Clone();
    }

    public void ApplyDifficulty(CpuDifficultySettings settings)
    {
        EnsureDifficultySettings();
        difficultySettings.CopyFrom(settings);
    }

    public void ApplyLevelDefinition(CpuLevelDefinition levelDefinition)
    {
        currentLevelDefinition = levelDefinition ?? CpuLevelCatalog.GetLevel(CpuLevelCatalog.MinLevel);
    }

    public void SetDecisionStrengths(float exchangeStrength, float specialCardStrength)
    {
        EnsureDifficultySettings();
        difficultySettings.SetDecisionStrengths(exchangeStrength, specialCardStrength);
    }

    public void ResetDecisionStrengths()
    {
        ResetDifficulty();
    }

    public void ResetDifficulty()
    {
        EnsureDifficultySettings();
        if (defaultDifficultySettings == null)
        {
            defaultDifficultySettings = difficultySettings.Clone();
        }

        difficultySettings.CopyFrom(defaultDifficultySettings);
    }

    private void EnsureDifficultySettings()
    {
        if (difficultySettings == null)
        {
            difficultySettings = new CpuDifficultySettings();
        }
    }

    public void SelectSpecialCard(GameState gameState)
    {
        EnsureDifficultySettings();

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
            CardSelectionUtility.ClearSelections(cpuSpecialCards);
            return;
        }

        Card selectedCard = ChooseSpecialCardForCurrentLevel(gameState, cpuState, usableCpuSpecialCards);
        CardSelectionUtility.SelectOnly(cpuSpecialCards, selectedCard);

        string selectedName = selectedCard != null && selectedCard.CardData != null ? selectedCard.CardData.name : "No special card";
        Debug.Log($"CPU selected special card: {selectedName}");
    }

    private Card ChooseSpecialCardForCurrentLevel(
        GameState gameState,
        PlayerState cpuState,
        List<Card> usableCpuSpecialCards)
    {
        CpuLevelDefinition levelDefinition = currentLevelDefinition ?? CpuLevelCatalog.GetLevel(CpuLevelCatalog.MinLevel);
        switch (levelDefinition.UsageMode)
        {
            case CpuSpecialCardUsageMode.Random:
                return ChooseRandomSpecialCard(usableCpuSpecialCards);
            case CpuSpecialCardUsageMode.Judgment:
                return ChooseJudgmentSpecialCard(gameState, cpuState, usableCpuSpecialCards, levelDefinition);
            case CpuSpecialCardUsageMode.Strategy:
                return ChooseStrategySpecialCard(gameState, cpuState, usableCpuSpecialCards, levelDefinition);
            default:
                return null;
        }
    }

    private Card ChooseRandomSpecialCard(List<Card> usableCpuSpecialCards)
    {
        if (usableCpuSpecialCards == null || usableCpuSpecialCards.Count == 0 || Random.value >= 0.5f)
        {
            return null;
        }

        return usableCpuSpecialCards[Random.Range(0, usableCpuSpecialCards.Count)];
    }

    private Card ChooseJudgmentSpecialCard(
        GameState gameState,
        PlayerState cpuState,
        List<Card> usableCpuSpecialCards,
        CpuLevelDefinition levelDefinition)
    {
        PlayerState playerState = GetPlayerState(gameState);
        HandEvaluator.HandRank cpuRank = EvaluateCpuHandRank(gameState, cpuState);

        if (IsRoleAtOrBelow(cpuRank, HandEvaluator.HandRank.Niso))
        {
            Card card = FindUsableSlotCard(levelDefinition, CpuSpecialCardSlot.Buff, usableCpuSpecialCards);
            if (card != null) return card;
        }

        if (cpuState != null && cpuState.LifePoints <= 30)
        {
            Card card = FindUsableSlotCard(levelDefinition, CpuSpecialCardSlot.Defense, usableCpuSpecialCards);
            if (card != null) return card;
        }

        if (playerState != null && playerState.LifePoints >= 50)
        {
            Card card = FindUsableSlotCard(levelDefinition, CpuSpecialCardSlot.Attack, usableCpuSpecialCards);
            if (card != null) return card;
        }

        if (Random.value < 0.5f)
        {
            return FindUsableSlotCard(levelDefinition, CpuSpecialCardSlot.Flex, usableCpuSpecialCards);
        }

        return null;
    }

    private Card ChooseStrategySpecialCard(
        GameState gameState,
        PlayerState cpuState,
        List<Card> usableCpuSpecialCards,
        CpuLevelDefinition levelDefinition)
    {
        PlayerState playerState = GetPlayerState(gameState);
        HandEvaluator.HandRank cpuRank = EvaluateCpuHandRank(gameState, cpuState);

        if (IsRoleAtOrAbove(cpuRank, HandEvaluator.HandRank.Sanju))
        {
            Card card = FindUsableSlotCard(levelDefinition, CpuSpecialCardSlot.Buff, usableCpuSpecialCards);
            if (card != null) return card;
        }

        if (cpuState != null && cpuState.LifePoints <= 30)
        {
            Card card = FindUsableSlotCard(levelDefinition, CpuSpecialCardSlot.Defense, usableCpuSpecialCards);
            if (card != null) return card;
        }

        if (playerState != null && cpuState != null && playerState.LifePoints >= cpuState.LifePoints + 20)
        {
            Card card = FindUsableSlotCard(levelDefinition, CpuSpecialCardSlot.Attack, usableCpuSpecialCards);
            if (card != null) return card;
        }

        if (Random.value < 0.5f)
        {
            return FindUsableSlotCard(levelDefinition, CpuSpecialCardSlot.Flex, usableCpuSpecialCards);
        }

        return null;
    }

    private PlayerState GetPlayerState(GameState gameState)
    {
        if (gameState == null || gameState.PlayerStates == null || gameState.PlayerStates.Count == 0)
        {
            return null;
        }

        return gameState.PlayerStates[0];
    }

    private HandEvaluator.HandRank EvaluateCpuHandRank(GameState gameState, PlayerState cpuState)
    {
        if (cpuState == null)
        {
            return HandEvaluator.HandRank.Miezu;
        }

        HandEvaluator.HandInfo handInfo = HandEvaluator.EvaluateHand(cpuState.HandCards, gameState?.commonCards);
        return handInfo != null ? handInfo.Rank : HandEvaluator.HandRank.Miezu;
    }

    private Card FindUsableSlotCard(
        CpuLevelDefinition levelDefinition,
        CpuSpecialCardSlot slot,
        List<Card> usableCpuSpecialCards)
    {
        if (levelDefinition == null || usableCpuSpecialCards == null)
        {
            return null;
        }

        SpecialCardResolver.SpecialCardId cardId = levelDefinition.GetCardId(slot);
        foreach (Card card in usableCpuSpecialCards)
        {
            if (card != null && SpecialCardResolver.IsSpecialCard(card.CardData, cardId))
            {
                return card;
            }
        }

        return null;
    }

    private bool IsRoleAtOrBelow(HandEvaluator.HandRank rank, HandEvaluator.HandRank threshold)
    {
        return HandRoleCatalog.GetIndex(rank) <= HandRoleCatalog.GetIndex(threshold);
    }

    private bool IsRoleAtOrAbove(HandEvaluator.HandRank rank, HandEvaluator.HandRank threshold)
    {
        return HandRoleCatalog.GetIndex(rank) >= HandRoleCatalog.GetIndex(threshold);
    }

    // CPUの行動ロジック
    public override IEnumerator Act(GameState gameState, System.Action<ControllerResponse> callback)
    {
        EnsureDifficultySettings();
        yield return new WaitForSeconds(difficultySettings.ThinkingDelaySeconds); // 思考時間の演出

        PlayerState playerState = gameState.GetPlayerState(); // 現在のプレイヤー状態取得
        List<Card> playerHand = playerState.HandCards; // 手札
        List<Card> commonCards = gameState.commonCards; // 共通札

        foreach (var card in playerHand)
        {
            card.IsFaceUp = true; // 役判定のためCPUの手札を表向きに設定
        }
        var handInfo = HandEvaluator.EvaluateHand(playerHand, commonCards); // 役判定
        Debug.Log($"CPUの役判定結果: {handInfo.Name} (ランク: {handInfo.Rank}, ポイント: {handInfo.Score})");
        foreach (var card in playerHand)
        {
            card.IsFaceUp = false; // CPUの手札を裏向きに戻す
        }

        List<Card> trash = new List<Card>(); // 捨てるカードリスト// 手札と共通札を結合
        List<Card> allCards = playerHand.Concat(commonCards ?? Enumerable.Empty<Card>()).ToList();
        Dictionary<Number, List<Card>> numberGroups = CardPatternUtility.BuildNumberGroups(allCards);
        Dictionary<Suit, List<Card>> suitGroups = CardPatternUtility.BuildSuitGroups(allCards);

        // 役ごとに捨てるカードを決定
        switch (handInfo.Rank)
        {
            case HandEvaluator.HandRank.Miezu:
                // 不見の場合、全てのカードを捨てる
                trash.AddRange(playerHand);
                break;
            case HandEvaluator.HandRank.Isso:
                // 一双の場合、ペアでないカードを全て捨てる
                AddCardsNotMatchingNumbers(
                    trash,
                    playerHand,
                    CardPatternUtility.GetNumbersWithGroupCount(numberGroups, 2, 1));
                break;
            case HandEvaluator.HandRank.Niso:
                // 二双の場合、ペアでないカードを全て捨てる
                AddCardsNotMatchingNumbers(
                    trash,
                    playerHand,
                    CardPatternUtility.GetNumbersWithGroupCount(numberGroups, 2, 2));
                break;
            case HandEvaluator.HandRank.Sanju:
                // 三珠の場合、トリプルでないカードを捨てる
                AddCardsNotMatchingNumbers(
                    trash,
                    playerHand,
                    CardPatternUtility.GetNumbersWithGroupCount(numberGroups, 3, 1));
                break;
            case HandEvaluator.HandRank.Yonju:
                // 四珠の場合、フォーカードでないカードを捨てる
                AddCardsNotMatchingNumbers(
                    trash,
                    playerHand,
                    CardPatternUtility.GetNumbersWithGroupCount(numberGroups, 4, 1));
                break;
            case HandEvaluator.HandRank.Tenshu:
                // 天守の場合、１０以下のカードを捨てる 
                foreach (var card in playerHand)
                {
                    if ((int)card.CardData.number <= 10)
                    {
                        trash.Add(card);
                    }
                }
                break;
            case HandEvaluator.HandRank.Suzi:
                // 筋の場合、連続する5枚を除くすべてのカードを捨てる
                AddCardsNotMatchingNumbers(
                    trash,
                    playerHand,
                    CardPatternUtility.FindSequence(numberGroups, 5));
                break;
            case HandEvaluator.HandRank.Hikari:
                // 光の場合、スートが少数派であるカードを全て捨てる
                if (CardPatternUtility.TryFindMinoritySuit(suitGroups, out Suit minoritySuit))
                {
                    foreach (var card in playerHand)
                    {
                        if (card.CardData.suit == minoritySuit)
                        {
                            trash.Add(card);
                        }
                    }
                }
                break;
            case HandEvaluator.HandRank.Nanasuzi:
                // 七筋の場合、捨てるカードはなし
                break;
            case HandEvaluator.HandRank.Nanahikari:
                // 七光の場合、捨てるカードはなし
                break;
            case HandEvaluator.HandRank.Tenshukaku:
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

    private static void AddCardsNotMatchingNumbers(List<Card> trash, List<Card> playerHand, List<Number> keepNumbers)
    {
        if (trash == null || playerHand == null)
        {
            return;
        }

        if (keepNumbers == null || keepNumbers.Count == 0)
        {
            trash.AddRange(playerHand);
            return;
        }

        HashSet<Number> keepNumberSet = new HashSet<Number>(keepNumbers);
        foreach (Card card in playerHand)
        {
            if (card?.CardData == null || !keepNumberSet.Contains(card.CardData.number))
            {
                trash.Add(card);
            }
        }
    }

    private List<Card> ApplyExchangeDecisionStrength(List<Card> plannedTrash, List<Card> playerHand, int maxTrashCount)
    {
        float strength = difficultySettings.ExchangeDecisionStrength;
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
