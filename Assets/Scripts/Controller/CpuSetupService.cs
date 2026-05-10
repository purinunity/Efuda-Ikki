using System.Collections.Generic;
using UnityEngine;

public sealed class CpuSetupService
{
    private readonly GameState gameState;
    private readonly CPUController cpuController;
    private readonly CharacterManager characterManager;
    private readonly Cards playerSpecialCardsDeck;
    private readonly Cards cpuSpecialCardsDeck;
    private readonly int defaultCpuSpecialCardCount;
    private readonly List<CpuCharacterSettings> characterSettings;
    private CpuCharacterSettings currentCharacterSettings;

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
        this.defaultCpuSpecialCardCount = defaultCpuSpecialCardCount;
        this.characterSettings = characterSettings;
    }

    public void ApplyStageSettings(int stageNumber)
    {
        currentCharacterSettings = FindCharacterSettings(stageNumber);

        if (characterManager != null)
        {
            characterManager.SetCPUImage(stageNumber);
        }

        ApplyDifficulty(stageNumber);

        gameState.maxHandTrashTurn = 2;
        gameState.maxHandTrashCount = 5;

        Debug.Log($"Stage {stageNumber}: exchange limit fixed to 2 turns / 5 cards.");
    }

    public void ApplySpecialCards(GameModeData modeData, int stageNumber)
    {
        if (gameState.PlayerStates == null || gameState.PlayerStates.Count == 0)
        {
            return;
        }

        ApplyPlayerSpecialCards(modeData);

        if (gameState.PlayerStates.Count > 1)
        {
            gameState.PlayerStates[1].SpecialCards = BuildCpuSpecialCards(stageNumber);
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

    private List<Card> BuildCpuSpecialCards(int stageNumber)
    {
        CpuCharacterSettings settings = currentCharacterSettings;
        if (settings == null || settings.CharacterIndex != stageNumber)
        {
            settings = FindCharacterSettings(stageNumber);
        }

        if (settings != null && settings.UseConfiguredSpecialCards)
        {
            return BuildConfiguredCpuSpecialCards(settings);
        }

        int count = settings != null ? settings.RandomSpecialCardCount : defaultCpuSpecialCardCount;
        return BuildRandomCpuSpecialCards(count);
    }

    private List<Card> BuildConfiguredCpuSpecialCards(CpuCharacterSettings settings)
    {
        List<Card> selectedCards = new List<Card>();
        if (cpuSpecialCardsDeck == null || cpuSpecialCardsDeck.cardList == null)
        {
            Debug.LogWarning("specialCardsDeck2 is not assigned.");
            return selectedCards;
        }

        ClearCpuSpecialCardSelections();

        if (settings.SpecialCardDatas != null)
        {
            foreach (CardData cardData in settings.SpecialCardDatas)
            {
                if (cardData == null)
                {
                    continue;
                }

                Card card = cpuSpecialCardsDeck.GetCard(cardData);
                if (card != null && !selectedCards.Contains(card))
                {
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
        if (cpuSpecialCardsDeck == null || cpuSpecialCardsDeck.cardList == null)
        {
            Debug.LogWarning("specialCardsDeck2 is not assigned.");
            return selectedCards;
        }

        List<Card> availableCards = new List<Card>();
        foreach (Card card in cpuSpecialCardsDeck.cardList)
        {
            if (card != null)
            {
                availableCards.Add(card);
            }
        }
        CardSelectionUtility.ClearSelections(availableCards);

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

    private void ApplyDifficulty(int stageNumber)
    {
        if (cpuController == null)
        {
            return;
        }

        if (currentCharacterSettings == null)
        {
            cpuController.ResetDifficulty();
            Debug.Log($"CPU character {stageNumber}: using CPUController default difficulty settings.");
            return;
        }

        cpuController.ApplyDifficulty(currentCharacterSettings.DifficultySettings);
        Debug.Log($"CPU character {stageNumber}: difficulty settings applied.");
    }

    private CpuCharacterSettings FindCharacterSettings(int stageNumber)
    {
        if (characterSettings == null)
        {
            return null;
        }

        foreach (CpuCharacterSettings settings in characterSettings)
        {
            if (settings != null && settings.CharacterIndex == stageNumber)
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
