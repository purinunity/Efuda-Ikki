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
            $"special logic {currentLevelDefinition.UsageMode}.");
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

            gameState.PlayerStates[1].SpecialCards = BuildCpuSpecialCards(levelDefinition);
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
        if (modeData?.SelectedSpecialCardDatas != null && modeData.SelectedSpecialCardDatas.Count > 0)
        {
            if (playerSpecialCardsDeck == null)
            {
                Debug.LogWarning("specialCardsDeck1 is not assigned.");
                return;
            }

            gameState.PlayerStates[0].SpecialCards =
                playerSpecialCardsDeck.GetCards(modeData.SelectedSpecialCardDatas);
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

        foreach (SpecialCardResolver.SpecialCardId cardId in levelDefinition.SpecialCardIds)
        {
            Card card = FindCpuSpecialCard(cardId);
            if (card != null && !selectedCards.Contains(card))
            {
                selectedCards.Add(card);
            }
            else if (card == null)
            {
                Debug.LogWarning($"CPU level {levelDefinition.Level}: special card {cardId} was not found.");
            }
        }

        Debug.Log($"CPU level {levelDefinition.Level}: configured special cards selected: {selectedCards.Count}");
        return selectedCards;
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
