using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EfudaIkki.Core;

// CPUプレイヤーの行動を制御するクラス
public class CPUController : Controller
{
    private const int CpuPlayerId = 1;

    [Header("Default CPU Difficulty")]
    [Tooltip("Used as the fallback difficulty and copied per character when CpuCharacterSettings are applied.")]
    [SerializeField] private CpuDifficultySettings difficultySettings = new CpuDifficultySettings();
    private CpuDifficultySettings defaultDifficultySettings;
    private CpuLevelDefinition currentLevelDefinition = CpuLevelCatalog.GetLevel(CpuLevelCatalog.MinLevel);
    private IRandomSource randomSource;

    private IRandomSource RandomSource => randomSource ?? (randomSource = new UnityRandomSource());

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

    public void SetRandomSource(IRandomSource source)
    {
        randomSource = source;
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
        HandEvaluator.HandRank cpuRank = EvaluateCpuHandRank(gameState, cpuState);

        string[] candidateIds = usableCpuSpecialCards
            .Select(GetStableSpecialCardId)
            .ToArray();
        int selectedIndex = CpuSpecialCardPolicy.SelectCardIndex(
            candidateIds,
            (HandRole)(int)cpuRank,
            CreateFixedChoice(levelDefinition.FixedCard1, levelDefinition.FixedCard1Condition),
            CreateFixedChoice(levelDefinition.FixedCard2, levelDefinition.FixedCard2Condition),
            difficultySettings.SpecialCardDecisionStrength,
            levelDefinition.Level,
            RandomSource);

        return selectedIndex >= 0 && selectedIndex < usableCpuSpecialCards.Count
            ? usableCpuSpecialCards[selectedIndex]
            : null;
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

    private static CpuFixedSpecialChoice CreateFixedChoice(
        SpecialCardResolver.SpecialCardId cardId,
        CpuFixedCardCondition condition)
    {
        string stableId = SpecialCardCatalog.TryGet(cardId, out SpecialCardCatalog.Entry entry)
            ? entry.Id
            : cardId.ToString();
        return new CpuFixedSpecialChoice(stableId, (CpuHandCondition)(int)condition);
    }

    private static string GetStableSpecialCardId(Card card)
    {
        return card != null &&
               SpecialCardCatalog.TryGet(card.CardData, out SpecialCardCatalog.Entry entry)
            ? entry.Id
            : card?.CardData?.name ?? string.Empty;
    }

    // CPUの行動ロジック
    public override IEnumerator Act(GameState gameState, System.Action<ControllerResponse> callback)
    {
        EnsureDifficultySettings();
        yield return new WaitForSeconds(difficultySettings.ThinkingDelaySeconds); // 思考時間の演出

        PlayerState playerState = gameState?.GetPlayerState();
        List<Card> playerHand = playerState?.HandCards ?? new List<Card>();
        List<Card> commonCards = gameState?.commonCards ?? new List<Card>();

        CpuExchangeDecision decision = CpuExchangePolicy.Decide(
            ToCardValues(playerHand),
            ToCardValues(commonCards),
            gameState != null ? gameState.maxHandTrashCount : 0,
            difficultySettings.ExchangeDecisionStrength,
            RandomSource);
        Debug.Log($"CPUの役判定結果: {HandRoleRules.GetDisplayName(decision.EvaluatedRole)} " +
                  $"(ランク: {decision.EvaluatedRole}, ポイント: {HandRoleRules.GetScore(decision.EvaluatedRole)})");

        List<Card> trash = decision.DiscardIndexes
            .Where(index => index >= 0 && index < playerHand.Count && playerHand[index] != null)
            .Select(index => playerHand[index])
            .ToList();

        var response = new ControllerResponse
        {
            actionCompleted = true,
            cardsTrash = trash
        };
        callback?.Invoke(response);
    }

    private static List<CardValue> ToCardValues(IEnumerable<Card> cards)
    {
        return (cards ?? Enumerable.Empty<Card>())
            .Select(card => card != null && card.CardData != null
                ? new CardValue((int)card.CardData.number, (int)card.CardData.suit)
                : CardValue.Invalid)
            .ToList();
    }
}
