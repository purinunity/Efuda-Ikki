using System.Collections.Generic;
using UnityEngine;

public sealed class CpuSetupService
{
    private readonly GameState gameState;
    private readonly CPUController cpuController;
    private readonly CharacterManager characterManager;
    private readonly Cards playerSpecialCardsDeck;
    private readonly Cards cpuSpecialCardsDeck;
    private readonly List<CpuCharacterSettings> characterSettings;
    private CpuCharacterSettings currentCharacterSettings;
    private CpuLevelDefinition currentLevelDefinition;

    public CpuSetupService(
        GameState gameState,
        CPUController cpuController,
        CharacterManager characterManager,
        Cards playerSpecialCardsDeck,
        Cards cpuSpecialCardsDeck,
        int defaultCpuSpecialCardCount,
        List<CpuCharacterSettings> characterSettings)
    {
        this.gameState = gameState;
        this.cpuController = cpuController;
        this.characterManager = characterManager;
        this.playerSpecialCardsDeck = playerSpecialCardsDeck;
        this.cpuSpecialCardsDeck = cpuSpecialCardsDeck;
        this.characterSettings = characterSettings;
    }

    public void ApplyStageSettings(int stageNumber)
    {
        ApplyLevelSettings(stageNumber + 1);
    }

    public void ApplyLevelSettings(int level)
    {
        currentLevelDefinition = CpuLevelCatalog.GetLevel(level);
        int characterIndex = currentLevelDefinition.CharacterIndex;
        currentCharacterSettings = FindCharacterSettings(characterIndex);

        if (characterManager != null)
        {
            characterManager.SetCPUImage(characterIndex);
        }

        ApplyDifficulty(characterIndex);
        ApplyLifePoints(currentLevelDefinition);

        if (cpuController != null)
        {
            cpuController.ApplyLevelDefinition(currentLevelDefinition);
        }

        gameState.maxHandTrashTurn = 2;
        gameState.maxHandTrashCount = 5;

        Debug.Log(
            $"CPU level {currentLevelDefinition.Level}: HP {currentLevelDefinition.InitialLifePoints}, " +
            $"fixed cards {currentLevelDefinition.FixedCard1}/{currentLevelDefinition.FixedCard2}.");
    }

    public void ApplySpecialCards(GameModeData modeData, int stageNumber)
    {
        ApplySpecialCardsForLevel(modeData, stageNumber + 1);
    }

    public void ApplySpecialCardsForLevel(GameModeData modeData, int level)
    {
        if (gameState.PlayerStates == null || gameState.PlayerStates.Count == 0)
        {
            return;
        }

        ApplyPlayerSpecialCards(modeData);

        if (gameState.PlayerStates.Count > 1)
        {
            CpuLevelDefinition levelDefinition = currentLevelDefinition != null &&
                                                 currentLevelDefinition.Level == CpuLevelCatalog.ClampLevel(level)
                ? currentLevelDefinition
                : CpuLevelCatalog.GetLevel(level);

            gameState.PlayerStates[1].SetSpecialCardsForMatch(BuildCpuSpecialCards(levelDefinition));
        }
    }

    private void ApplyLifePoints(CpuLevelDefinition levelDefinition)
    {
        if (gameState?.PlayerStates == null)
        {
            return;
        }

        if (gameState.PlayerStates.Count > 0)
        {
            gameState.PlayerStates[0].ResetForMatch(PlayerState.DefaultLifePoints);
        }

        if (gameState.PlayerStates.Count > 1)
        {
            gameState.PlayerStates[1].ResetForMatch(levelDefinition.InitialLifePoints);
        }
    }

    private void ApplyPlayerSpecialCards(GameModeData modeData)
    {
        if (gameState?.PlayerStates == null || gameState.PlayerStates.Count == 0)
        {
            return;
        }

        PlayerState playerState = gameState.PlayerStates[0];
        playerState.SetSpecialCardsForMatch(null);

        if (modeData?.SelectedSpecialCardDatas != null && modeData.SelectedSpecialCardDatas.Count > 0)
        {
            if (playerSpecialCardsDeck == null)
            {
                Debug.LogWarning("specialCardsDeck1 is not assigned.");
                return;
            }

            playerState.SetSpecialCardsForMatch(
                playerSpecialCardsDeck.GetCards(modeData.SelectedSpecialCardDatas));
            return;
        }

        Debug.Log("No player special cards selected.");
    }

    private List<Card> BuildCpuSpecialCards(CpuLevelDefinition levelDefinition)
    {
        List<Card> selectedCards = new List<Card>();
        if (levelDefinition == null)
        {
            return selectedCards;
        }

        if (cpuSpecialCardsDeck == null || cpuSpecialCardsDeck.cardList == null)
        {
            Debug.LogWarning("specialCardsDeck2 is not assigned.");
            return selectedCards;
        }

        ClearCpuSpecialCardSelections();

        AddCpuSpecialCard(selectedCards, levelDefinition.FixedCard1, levelDefinition.Level);
        AddCpuSpecialCard(selectedCards, levelDefinition.FixedCard2, levelDefinition.Level);

        List<Card> freeCardCandidates = BuildFreeCardCandidates(levelDefinition);
        for (int i = 0; i < 2 && freeCardCandidates.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, freeCardCandidates.Count);
            selectedCards.Add(freeCardCandidates[randomIndex]);
            freeCardCandidates.RemoveAt(randomIndex);
        }

        Debug.Log($"CPU level {levelDefinition.Level}: selected {selectedCards.Count} special cards.");
        return selectedCards;
    }

    private void AddCpuSpecialCard(
        List<Card> selectedCards,
        SpecialCardResolver.SpecialCardId cardId,
        int level)
    {
        Card card = FindCpuSpecialCard(cardId);
        if (card != null && !selectedCards.Contains(card))
        {
            selectedCards.Add(card);
            return;
        }

        Debug.LogWarning($"CPU level {level}: fixed special card {cardId} was not found.");
    }

    private List<Card> BuildFreeCardCandidates(CpuLevelDefinition levelDefinition)
    {
        List<Card> candidates = new List<Card>();
        foreach (Card card in cpuSpecialCardsDeck.cardList)
        {
            if (card == null ||
                card.CardData == null ||
                !GameProgressStore.IsSpecialCardUnlocked(card.CardData) ||
                !SpecialCardResolver.TryGetSpecialCardId(
                    card.CardData,
                    out SpecialCardResolver.SpecialCardId cardId) ||
                cardId == levelDefinition.FixedCard1 ||
                cardId == levelDefinition.FixedCard2 ||
                cardId == SpecialCardResolver.SpecialCardId.Oni)
            {
                continue;
            }

            candidates.Add(card);
        }

        if (candidates.Count < 2)
        {
            Debug.LogWarning(
                $"CPU level {levelDefinition.Level}: only {candidates.Count} unlocked free special cards are available.");
        }

        return candidates;
    }

    private Card FindCpuSpecialCard(SpecialCardResolver.SpecialCardId cardId)
    {
        foreach (Card card in cpuSpecialCardsDeck.cardList)
        {
            if (card != null && SpecialCardResolver.IsSpecialCard(card.CardData, cardId))
            {
                return card;
            }
        }

        return null;
    }

    private void ApplyDifficulty(int characterIndex)
    {
        if (cpuController == null)
        {
            return;
        }

        if (currentCharacterSettings == null)
        {
            cpuController.ResetDifficulty();
            Debug.Log($"CPU character {characterIndex}: using CPUController default difficulty settings.");
            return;
        }

        cpuController.ApplyDifficulty(currentCharacterSettings.DifficultySettings);
        Debug.Log($"CPU character {characterIndex}: difficulty settings applied.");
    }

    private CpuCharacterSettings FindCharacterSettings(int characterIndex)
    {
        if (characterSettings == null)
        {
            return null;
        }

        foreach (CpuCharacterSettings settings in characterSettings)
        {
            if (settings != null && settings.CharacterIndex == characterIndex)
            {
                return settings;
            }
        }

        return null;
    }

    private void ClearCpuSpecialCardSelections()
    {
        CardSelectionUtility.ClearSelections(cpuSpecialCardsDeck.cardList);
    }
}
