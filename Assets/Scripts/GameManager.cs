using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static HandEvaluator;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    private class CpuCharacterSettings
    {
        [SerializeField, Min(0)] private int characterIndex;
        [SerializeField, Range(0f, 1f)] private float exchangeDecisionStrength = 1f;
        [SerializeField, Range(0f, 1f)] private float specialCardDecisionStrength = 1f;
        [SerializeField] private bool useConfiguredSpecialCards = true;
        [SerializeField] private List<CardData> specialCardDatas = new List<CardData>();
        [SerializeField, Min(0)] private int randomSpecialCardCount = 4;

        public int CharacterIndex => characterIndex;
        public float ExchangeDecisionStrength => exchangeDecisionStrength;
        public float SpecialCardDecisionStrength => specialCardDecisionStrength;
        public bool UseConfiguredSpecialCards => useConfiguredSpecialCards;
        public List<CardData> SpecialCardDatas => specialCardDatas;
        public int RandomSpecialCardCount => randomSpecialCardCount;
    }

    public GameState gameState = new GameState();

    [SerializeField] private Cards allCards;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CPUController cpuController;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ShowdownCutInPopup showdownCutInPopup;
    [SerializeField] private TitleUIManager titleUIManager;
    [SerializeField] private CharacterManager characterManager;
    [SerializeField] private Cards specialCardsDeck1;
    [SerializeField] private Cards specialCardsDeck2;
    [SerializeField] private int cpuSpecialCardCount = 4;
    [SerializeField] private List<CpuCharacterSettings> cpuCharacterSettings = new List<CpuCharacterSettings>();

    private Controller[] controllers;
    private CpuCharacterSettings currentCpuCharacterSettings;
    private bool initialized = false;
    private bool gameOver = false;
    private bool isGameRunning = false;

    private void Start()
    {
        isGameRunning = false;
    }

    public void StartGameWithMode(GameModeData modeData)
    {
        if (modeData == null)
        {
            Debug.LogWarning("GameModeData is null. Starting with default mode data.");
            modeData = new GameModeData();
        }

        if (isGameRunning)
        {
            return;
        }

        isGameRunning = true;
        gameOver = false;
        gameState.InitializePlayerStates();
        controllers = new Controller[] { playerController, cpuController };

        int stageNumber = modeData.Mode == GameModeData.GameMode.KatinukiMode ? modeData.SelectedStage : 0;
        ApplyStageSettings(stageNumber);
        ApplySpecialCards(stageNumber);

        if (modeData.Mode == GameModeData.GameMode.KatinukiMode)
        {
            Debug.Log($"Katinuki mode started with stage {modeData.SelectedStage}.");
            StartCoroutine(GameFlow());
        }
        else if (modeData.Mode == GameModeData.GameMode.BattleGroundMode)
        {
            Debug.Log("BattleGround mode started.");
            StartCoroutine(GameFlow());
        }
    }

    private void ApplyStageSettings(int stageNumber)
    {
        currentCpuCharacterSettings = FindCpuCharacterSettings(stageNumber);

        if (characterManager != null)
        {
            characterManager.SetCPUImage(stageNumber);
        }

        ApplyCpuDecisionSettings(stageNumber);

        gameState.maxHandTrashTurn = 2;
        gameState.maxHandTrashCount = 5;

        Debug.Log($"Stage {stageNumber}: exchange limit fixed to 2 turns / 5 cards.");
    }

    private void ApplySpecialCards(int stageNumber)
    {
        GameModeData modeData = GameModeManager.GetGameModeData();

        if (gameState.PlayerStates == null || gameState.PlayerStates.Count == 0)
        {
            return;
        }

        if (modeData.SelectedSpecialCardDatas != null && modeData.SelectedSpecialCardDatas.Count > 0)
        {
            if (specialCardsDeck1 == null)
            {
                Debug.LogWarning("specialCardsDeck1 is not assigned.");
            }
            else
            {
                gameState.PlayerStates[0].SpecialCards = specialCardsDeck1.GetCards(modeData.SelectedSpecialCardDatas);
            }
        }
        else
        {
            Debug.Log("No player special cards selected.");
        }

        if (gameState.PlayerStates.Count > 1)
        {
            gameState.PlayerStates[1].SpecialCards = BuildCpuSpecialCards(stageNumber);
        }
    }

    private List<Card> BuildCpuSpecialCards(int stageNumber)
    {
        CpuCharacterSettings settings = currentCpuCharacterSettings;
        if (settings == null || settings.CharacterIndex != stageNumber)
        {
            settings = FindCpuCharacterSettings(stageNumber);
        }

        if (settings != null && settings.UseConfiguredSpecialCards)
        {
            return BuildConfiguredCpuSpecialCards(settings);
        }

        int count = settings != null ? settings.RandomSpecialCardCount : cpuSpecialCardCount;
        return BuildRandomCpuSpecialCards(count);
    }

    private List<Card> BuildConfiguredCpuSpecialCards(CpuCharacterSettings settings)
    {
        List<Card> selectedCards = new List<Card>();
        if (specialCardsDeck2 == null || specialCardsDeck2.cardList == null)
        {
            Debug.LogWarning("specialCardsDeck2 is not assigned.");
            return selectedCards;
        }

        foreach (Card card in specialCardsDeck2.cardList)
        {
            if (card != null)
            {
                card.IsSelected = false;
            }
        }

        if (settings.SpecialCardDatas != null)
        {
            foreach (CardData cardData in settings.SpecialCardDatas)
            {
                if (cardData == null)
                {
                    continue;
                }

                Card card = specialCardsDeck2.GetCard(cardData);
                if (card != null && !selectedCards.Contains(card))
                {
                    card.IsSelected = false;
                    selectedCards.Add(card);
                }
            }
        }

        Debug.Log($"CPU character {settings.CharacterIndex}: configured special cards selected: {selectedCards.Count}");
        return selectedCards;
    }

    private List<Card> BuildRandomCpuSpecialCards(int requestedCount)
    {
        List<Card> selectedCards = new List<Card>();
        if (specialCardsDeck2 == null || specialCardsDeck2.cardList == null)
        {
            Debug.LogWarning("specialCardsDeck2 is not assigned.");
            return selectedCards;
        }

        List<Card> availableCards = new List<Card>();
        foreach (Card card in specialCardsDeck2.cardList)
        {
            if (card != null)
            {
                card.IsSelected = false;
                availableCards.Add(card);
            }
        }

        int count = Mathf.Clamp(requestedCount, 0, availableCards.Count);
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(i, availableCards.Count);
            Card temp = availableCards[i];
            availableCards[i] = availableCards[randomIndex];
            availableCards[randomIndex] = temp;
            selectedCards.Add(availableCards[i]);
        }

        Debug.Log($"CPU special cards selected: {selectedCards.Count}");
        return selectedCards;
    }

    private void ApplyCpuDecisionSettings(int stageNumber)
    {
        if (cpuController == null)
        {
            return;
        }

        if (currentCpuCharacterSettings == null)
        {
            cpuController.ResetDecisionStrengths();
            Debug.Log($"CPU character {stageNumber}: using CPUController default strengths.");
            return;
        }

        cpuController.SetDecisionStrengths(
            currentCpuCharacterSettings.ExchangeDecisionStrength,
            currentCpuCharacterSettings.SpecialCardDecisionStrength);

        Debug.Log(
            $"CPU character {stageNumber}: strengths set to exchange {currentCpuCharacterSettings.ExchangeDecisionStrength}, special {currentCpuCharacterSettings.SpecialCardDecisionStrength}.");
    }

    private CpuCharacterSettings FindCpuCharacterSettings(int stageNumber)
    {
        if (cpuCharacterSettings == null)
        {
            return null;
        }

        foreach (CpuCharacterSettings settings in cpuCharacterSettings)
        {
            if (settings != null && settings.CharacterIndex == stageNumber)
            {
                return settings;
            }
        }

        return null;
    }

    private IEnumerator GameFlow()
    {
        while (!gameOver)
        {
            initialized = false;
            StartCoroutine(InitializeGame());
            yield return new WaitUntil(() => initialized);

            Debug.Log("Game flow started.");
            Debug.Log($"Round {gameState.RoundNumber} started.");

            yield return StartCoroutine(Round());
            yield return StartCoroutine(ShowDown());

            if (gameOver)
            {
                break;
            }

            gameState.NextRound();
        }
    }

    private IEnumerator InitializeGame()
    {
        foreach (var card in allCards.cardList)
        {
            gameState.AddCardToDeck(card);
        }

        gameState.CardReset();
        gameState.ShuffleDeck();
        yield return UIUpdateWithWaiting(3f);

        for (int i = 0; i < gameState.playerCount; i++)
        {
            gameState.AddCardToPlayerHand();
            yield return UIUpdateWithWaiting(3f);
            gameState.NextTurn();
        }

        gameState.AddCardToCommon();
        yield return UIUpdateWithWaiting(3f);

        gameState.OpenPlayerHands(0);
        gameState.OpenCommonCards();
        yield return UIUpdateWithWaiting(3f);

        initialized = true;
        Debug.Log("Game initialized.");
    }

    private IEnumerator Round()
    {
        for (int i = 0; i < gameState.maxHandTrashTurn; i++)
        {
            for (int j = 0; j < gameState.playerCount; j++)
            {
                if (gameOver)
                {
                    yield break;
                }

                Controller controller = controllers[gameState.CurrentPlayerIndex];
                bool waiting = true;
                ControllerResponse response = null;

                yield return StartCoroutine(controller.Act(gameState, r =>
                {
                    response = r;
                    waiting = false;
                }));

                while (waiting)
                {
                    yield return null;
                }

                gameState.TrashCards(response.cardsTrash);
                yield return UIUpdateWithWaiting(3f);

                gameState.AddCardToPlayerHand();
                if (gameState.CurrentPlayerIndex == 0)
                {
                    gameState.OpenPlayerHands(0);
                }

                yield return UIUpdateWithWaiting(3f);
                gameState.NextTurn();

                if (gameOver)
                {
                    yield break;
                }
            }
        }
    }

    private IEnumerator ShowDown()
    {
        Debug.Log("ShowDown started.");

        for (int i = 0; i < gameState.playerCount; i++)
        {
            gameState.OpenPlayerHands(i);
        }

        if (cpuController != null)
        {
            cpuController.SelectSpecialCard(gameState);
        }

        List<Card> specialCardsToConsume = CollectSelectedUsableSpecialCards();
        SpecialCardResolver.ShowdownResult showdownResult = SpecialCardResolver.Resolve(gameState);
        foreach (SpecialCardResolver.ResolvedHand hand in showdownResult.Hands)
        {
            Debug.Log(
                $"Player {hand.PlayerId} hand: {hand.BaseHand.Rank} -> {hand.DisplayName} ({hand.Score})");
        }

        foreach (string logLine in showdownResult.Logs)
        {
            Debug.Log(logLine);
        }

        yield return StartCoroutine(PlayShowdownCutIn(showdownResult));
        MarkSpecialCardsUsed(specialCardsToConsume);

        if (showdownResult.IsDraw)
        {
            Debug.Log("Round ended in a draw.");
            yield break;
        }

        int winner = showdownResult.WinnerIndex;
        int damage = showdownResult.Damage;

        Debug.Log($"Winner is Player {winner}. Damage: {damage}");

        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (i != winner)
            {
                gameState.PlayerStates[i].decreaseLifePoints(damage);
            }

            Debug.Log($"Player {i} life: {gameState.PlayerStates[i].LifePoints}");
        }

        if (CheckGameOver())
        {
            StartCoroutine(HandleGameEnd());
        }
    }

    private List<Card> CollectSelectedUsableSpecialCards()
    {
        List<Card> selectedCards = new List<Card>();
        if (gameState == null || gameState.PlayerStates == null)
        {
            return selectedCards;
        }

        foreach (PlayerState playerState in gameState.PlayerStates)
        {
            if (playerState?.SpecialCards == null)
            {
                continue;
            }

            foreach (Card card in playerState.SpecialCards)
            {
                if (card == null || !card.IsSelected || playerState.IsSpecialCardUsed(card))
                {
                    continue;
                }

                selectedCards.Add(card);
            }
        }

        return selectedCards;
    }

    private void MarkSpecialCardsUsed(List<Card> usedCards)
    {
        if (usedCards == null || usedCards.Count == 0 || gameState?.PlayerStates == null)
        {
            return;
        }

        foreach (PlayerState playerState in gameState.PlayerStates)
        {
            if (playerState?.SpecialCards == null)
            {
                continue;
            }

            foreach (Card card in usedCards)
            {
                if (card != null && playerState.SpecialCards.Contains(card))
                {
                    playerState.MarkSpecialCardUsed(card);
                }
            }
        }
    }

    private IEnumerator PlayShowdownCutIn(SpecialCardResolver.ShowdownResult showdownResult)
    {
        ShowdownCutInPopup popup = GetShowdownCutInPopup();
        if (popup == null)
        {
            yield break;
        }

        ShowdownCutInPopup.Data cutInData = BuildShowdownCutInData(showdownResult);
        yield return StartCoroutine(popup.Play(cutInData));
    }

    private ShowdownCutInPopup GetShowdownCutInPopup()
    {
        Transform popupParent = GetShowdownCutInParent();

        if (IsUsableShowdownCutInPopup(showdownCutInPopup))
        {
            showdownCutInPopup.SetPopupParent(popupParent);
            showdownCutInPopup.Initialize();
            return showdownCutInPopup;
        }

        if (showdownCutInPopup != null)
        {
            Debug.LogWarning("ShowdownCutInPopup must be attached to its own UI GameObject, not the GameManager GameObject. A popup object will be created under the game Canvas.");
            showdownCutInPopup = null;
        }

        foreach (ShowdownCutInPopup candidate in FindObjectsOfType<ShowdownCutInPopup>(true))
        {
            if (!IsUsableShowdownCutInPopup(candidate))
            {
                continue;
            }

            showdownCutInPopup = candidate;
            showdownCutInPopup.SetPopupParent(popupParent);
            showdownCutInPopup.Initialize();
            return showdownCutInPopup;
        }

        showdownCutInPopup = ShowdownCutInPopup.Create(popupParent);
        return showdownCutInPopup;
    }

    private bool IsUsableShowdownCutInPopup(ShowdownCutInPopup popup)
    {
        return popup != null && popup.gameObject != gameObject;
    }

    private Transform GetShowdownCutInParent()
    {
        Canvas gameCanvas = FindGameCanvas();
        if (gameCanvas != null)
        {
            return gameCanvas.transform;
        }

        return uiManager != null ? uiManager.transform : transform;
    }

    private Canvas FindGameCanvas()
    {
        if (uiManager == null)
        {
            return GetComponentInParent<Canvas>();
        }

        Canvas canvas =
            FindCanvas(uiManager.deck) ??
            FindCanvas(uiManager.common) ??
            FindCanvas(uiManager.player1) ??
            FindCanvas(uiManager.player2) ??
            FindCanvas(uiManager.player1Special) ??
            FindCanvas(uiManager.player2Special) ??
            FindCanvas(uiManager.trash) ??
            uiManager.GetComponentInParent<Canvas>();

        return canvas;
    }

    private static Canvas FindCanvas(Component component)
    {
        return component != null ? component.GetComponentInParent<Canvas>() : null;
    }

    private ShowdownCutInPopup.Data BuildShowdownCutInData(SpecialCardResolver.ShowdownResult showdownResult)
    {
        SpecialCardResolver.ResolvedHand playerHand = FindResolvedHand(showdownResult, 0);
        SpecialCardResolver.ResolvedHand cpuHand = FindResolvedHand(showdownResult, 1);

        return new ShowdownCutInPopup.Data(
            characterManager != null ? characterManager.GetPlayerSprite() : null,
            characterManager != null ? characterManager.GetCpuSprite() : null,
            GetBaseRoleName(playerHand),
            GetBaseRoleName(cpuHand),
            GetBaseScore(playerHand),
            GetBaseScore(cpuHand),
            GetFinalRoleName(playerHand),
            GetFinalRoleName(cpuHand),
            GetFinalScore(playerHand),
            GetFinalScore(cpuHand),
            BuildShowdownCardSprites(0),
            BuildShowdownCardSprites(1),
            BuildRoleHighlightFlags(0, GetBaseRoleName(playerHand)),
            BuildRoleHighlightFlags(1, GetBaseRoleName(cpuHand)),
            GetSelectedSpecialCardSprite(0),
            GetSelectedSpecialCardSprite(1),
            BuildEffectStepData(showdownResult),
            showdownResult.WinnerIndex,
            showdownResult.Damage);
    }

    private List<Sprite> BuildShowdownCardSprites(int playerId)
    {
        List<Sprite> sprites = new List<Sprite>();
        if (gameState == null || gameState.PlayerStates == null || playerId < 0 || playerId >= gameState.PlayerStates.Count)
        {
            return sprites;
        }

        PlayerState playerState = gameState.PlayerStates[playerId];
        if (playerState?.HandCards != null)
        {
            foreach (Card card in playerState.HandCards)
            {
                AddCardSprite(sprites, card);
            }
        }

        if (gameState.commonCards != null)
        {
            foreach (Card card in gameState.commonCards)
            {
                AddCardSprite(sprites, card);
            }
        }

        return sprites;
    }

    private Sprite GetSelectedSpecialCardSprite(int playerId)
    {
        if (gameState == null || gameState.PlayerStates == null || playerId < 0 || playerId >= gameState.PlayerStates.Count)
        {
            return null;
        }

        PlayerState playerState = gameState.PlayerStates[playerId];
        if (playerState?.SpecialCards == null)
        {
            return null;
        }

        foreach (Card card in playerState.SpecialCards)
        {
            if (card != null && card.IsSelected && !playerState.IsSpecialCardUsed(card))
            {
                return card.CardData != null ? card.CardData.Image : null;
            }
        }

        return null;
    }

    private static void AddCardSprite(List<Sprite> sprites, Card card)
    {
        if (sprites == null)
        {
            return;
        }

        sprites.Add(card != null && card.CardData != null ? card.CardData.Image : null);
    }

    private List<bool> BuildRoleHighlightFlags(int playerId, string roleName)
    {
        List<Card> cards = BuildShowdownCards(playerId);
        HashSet<Card> relatedCards = FindRoleRelatedCards(cards, roleName);
        List<bool> highlights = new List<bool>();

        foreach (Card card in cards)
        {
            highlights.Add(card != null && relatedCards.Contains(card));
        }

        return highlights;
    }

    private List<Card> BuildShowdownCards(int playerId)
    {
        List<Card> cards = new List<Card>();
        if (gameState == null || gameState.PlayerStates == null || playerId < 0 || playerId >= gameState.PlayerStates.Count)
        {
            return cards;
        }

        PlayerState playerState = gameState.PlayerStates[playerId];
        if (playerState?.HandCards != null)
        {
            cards.AddRange(playerState.HandCards);
        }

        if (gameState.commonCards != null)
        {
            cards.AddRange(gameState.commonCards);
        }

        return cards;
    }

    private HashSet<Card> FindRoleRelatedCards(List<Card> cards, string roleName)
    {
        HashSet<Card> relatedCards = new HashSet<Card>();
        if (cards == null || string.IsNullOrEmpty(roleName) || roleName == "不見")
        {
            return relatedCards;
        }

        Dictionary<Number, List<Card>> numberGroups = BuildNumberGroups(cards);
        Dictionary<Suit, List<Card>> suitGroups = BuildSuitGroups(cards);

        switch (roleName)
        {
            case "一双":
                AddNumberGroupByCount(relatedCards, numberGroups, 2, 1);
                break;
            case "二双":
                AddNumberGroupByCount(relatedCards, numberGroups, 2, 2);
                break;
            case "三珠":
                AddNumberGroupByCount(relatedCards, numberGroups, 3, 1);
                break;
            case "四珠":
                AddNumberGroupByCount(relatedCards, numberGroups, 4, 1);
                break;
            case "天守":
                AddTenshuGroups(relatedCards, suitGroups, 1);
                break;
            case "筋":
                AddSequenceCards(relatedCards, numberGroups, 5);
                break;
            case "光":
                AddSuitGroupByCount(relatedCards, suitGroups, 5, 1);
                break;
            case "七筋":
                AddSequenceCards(relatedCards, numberGroups, 7);
                break;
            case "七光":
                AddSuitGroupByCount(relatedCards, suitGroups, 7, 1);
                break;
            case "天守閣":
                AddTenshuGroups(relatedCards, suitGroups, 2);
                break;
        }

        return relatedCards;
    }

    private static Dictionary<Number, List<Card>> BuildNumberGroups(List<Card> cards)
    {
        Dictionary<Number, List<Card>> groups = new Dictionary<Number, List<Card>>();
        foreach (Card card in cards)
        {
            if (card?.CardData == null)
            {
                continue;
            }

            Number number = card.CardData.number;
            if (!groups.TryGetValue(number, out List<Card> group))
            {
                group = new List<Card>();
                groups[number] = group;
            }

            group.Add(card);
        }

        return groups;
    }

    private static Dictionary<Suit, List<Card>> BuildSuitGroups(List<Card> cards)
    {
        Dictionary<Suit, List<Card>> groups = new Dictionary<Suit, List<Card>>();
        foreach (Card card in cards)
        {
            if (card?.CardData == null)
            {
                continue;
            }

            Suit suit = card.CardData.suit;
            if (!groups.TryGetValue(suit, out List<Card> group))
            {
                group = new List<Card>();
                groups[suit] = group;
            }

            group.Add(card);
        }

        return groups;
    }

    private static void AddNumberGroupByCount(
        HashSet<Card> relatedCards,
        Dictionary<Number, List<Card>> numberGroups,
        int requiredCount,
        int requiredGroups)
    {
        int addedGroups = 0;
        foreach (KeyValuePair<Number, List<Card>> group in numberGroups)
        {
            if (group.Value.Count < requiredCount)
            {
                continue;
            }

            foreach (Card card in group.Value)
            {
                relatedCards.Add(card);
            }

            addedGroups++;
            if (addedGroups >= requiredGroups)
            {
                return;
            }
        }
    }

    private static void AddSuitGroupByCount(
        HashSet<Card> relatedCards,
        Dictionary<Suit, List<Card>> suitGroups,
        int requiredCount,
        int requiredGroups)
    {
        int addedGroups = 0;
        foreach (KeyValuePair<Suit, List<Card>> group in suitGroups)
        {
            if (group.Value.Count < requiredCount)
            {
                continue;
            }

            foreach (Card card in group.Value)
            {
                relatedCards.Add(card);
            }

            addedGroups++;
            if (addedGroups >= requiredGroups)
            {
                return;
            }
        }
    }

    private static void AddTenshuGroups(
        HashSet<Card> relatedCards,
        Dictionary<Suit, List<Card>> suitGroups,
        int requiredGroups)
    {
        int addedGroups = 0;
        foreach (KeyValuePair<Suit, List<Card>> group in suitGroups)
        {
            if (!HasNumber(group.Value, Number.Jack) ||
                !HasNumber(group.Value, Number.Queen) ||
                !HasNumber(group.Value, Number.King))
            {
                continue;
            }

            AddCardsWithNumber(relatedCards, group.Value, Number.Jack);
            AddCardsWithNumber(relatedCards, group.Value, Number.Queen);
            AddCardsWithNumber(relatedCards, group.Value, Number.King);

            addedGroups++;
            if (addedGroups >= requiredGroups)
            {
                return;
            }
        }
    }

    private static void AddSequenceCards(
        HashSet<Card> relatedCards,
        Dictionary<Number, List<Card>> numberGroups,
        int sequenceLength)
    {
        List<Number> sequence = FindSequence(numberGroups, sequenceLength);
        foreach (Number number in sequence)
        {
            if (!numberGroups.TryGetValue(number, out List<Card> cards))
            {
                continue;
            }

            foreach (Card card in cards)
            {
                relatedCards.Add(card);
            }
        }
    }

    private static List<Number> FindSequence(Dictionary<Number, List<Card>> numberGroups, int sequenceLength)
    {
        Number[] order =
        {
            Number.One, Number.Two, Number.Three, Number.Four, Number.Five,
            Number.Six, Number.Seven, Number.Eight, Number.Nine, Number.Ten,
            Number.Jack, Number.Queen, Number.King, Number.One
        };

        List<Number> current = new List<Number>();
        foreach (Number number in order)
        {
            if (numberGroups.ContainsKey(number))
            {
                current.Add(number);
                if (current.Count >= sequenceLength)
                {
                    return current.GetRange(current.Count - sequenceLength, sequenceLength);
                }
            }
            else
            {
                current.Clear();
            }
        }

        return new List<Number>();
    }

    private static bool HasNumber(List<Card> cards, Number number)
    {
        foreach (Card card in cards)
        {
            if (card?.CardData != null && card.CardData.number == number)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddCardsWithNumber(HashSet<Card> relatedCards, List<Card> cards, Number number)
    {
        foreach (Card card in cards)
        {
            if (card?.CardData != null && card.CardData.number == number)
            {
                relatedCards.Add(card);
            }
        }
    }

    private List<ShowdownCutInPopup.Data.EffectStepData> BuildEffectStepData(
        SpecialCardResolver.ShowdownResult showdownResult)
    {
        List<ShowdownCutInPopup.Data.EffectStepData> steps =
            new List<ShowdownCutInPopup.Data.EffectStepData>();

        if (showdownResult?.EffectSteps == null)
        {
            return steps;
        }

        foreach (SpecialCardResolver.EffectStep step in showdownResult.EffectSteps)
        {
            steps.Add(new ShowdownCutInPopup.Data.EffectStepData(
                step.OwnerPlayerId,
                step.Card != null && step.Card.CardData != null ? step.Card.CardData.Image : null,
                step.EffectName,
                step.Message,
                step.WasSealed,
                step.PlayerRoleName,
                step.CpuRoleName,
                step.PlayerScore,
                step.CpuScore));
        }

        return steps;
    }

    private SpecialCardResolver.ResolvedHand FindResolvedHand(SpecialCardResolver.ShowdownResult showdownResult, int playerId)
    {
        if (showdownResult == null || showdownResult.Hands == null)
        {
            return null;
        }

        foreach (SpecialCardResolver.ResolvedHand hand in showdownResult.Hands)
        {
            if (hand != null && hand.PlayerId == playerId)
            {
                return hand;
            }
        }

        return null;
    }

    private string GetBaseRoleName(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? hand.BaseDisplayName : "不見";
    }

    private string GetFinalRoleName(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? hand.DisplayName : "不見";
    }

    private int GetBaseScore(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? hand.BaseScore : 0;
    }

    private int GetFinalScore(SpecialCardResolver.ResolvedHand hand)
    {
        return hand != null ? hand.Score : 0;
    }

    private bool CheckGameOver()
    {
        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (gameState.PlayerStates[i].LifePoints <= 0)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator HandleGameEnd()
    {
        gameOver = true;

        int winnerIndex = -1;
        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (gameState.PlayerStates[i].LifePoints > 0)
            {
                winnerIndex = i;
                break;
            }
        }

        if (winnerIndex == -1)
        {
            Debug.Log("Game ended with no remaining players.");
        }
        else
        {
            Debug.Log($"Game over. Winner: Player {winnerIndex}.");
        }

        yield return UIUpdateWithWaiting(5f);

        isGameRunning = false;

        if (titleUIManager != null)
        {
            titleUIManager.ShowTitleScreen();
            GameModeManager.ResetGameModeData();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    private IEnumerator UIUpdateWithWaiting(float duration = 5f)
    {
        uiManager.UIUpdate(gameState, duration);

        while (uiManager.UIUpdateInProgress)
        {
            yield return null;
        }

        Debug.Log("UI update completed.");
    }
}
