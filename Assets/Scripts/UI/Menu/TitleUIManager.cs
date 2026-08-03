using UnityEngine;

public class TitleUIManager : MonoBehaviour
{
    public CanvasGroup titleScreenPanel;
    public CanvasGroup modeSelectPanel;
    public CanvasGroup stageSelectPanel;
    public CanvasGroup specialCardSelectPanel;
    public CanvasGroup settingsPanel;

    [SerializeField] private GameManager gameManager;

    private GameModeData currentGameModeData;

    private void Start()
    {
        ShowTitleScreen();
    }

    private void SetPanelActive(CanvasGroup panel, bool isActive)
    {
        if (panel == null)
        {
            return;
        }

        panel.alpha = isActive ? 1f : 0f;
        panel.interactable = isActive;
        panel.blocksRaycasts = isActive;
    }

    private void HideAllPanels()
    {
        SetPanelActive(titleScreenPanel, false);
        SetPanelActive(modeSelectPanel, false);
        SetPanelActive(stageSelectPanel, false);
        SetPanelActive(specialCardSelectPanel, false);
        SetPanelActive(settingsPanel, false);
    }

    public void ShowTitleScreen()
    {
        HideAllPanels();
        SetPanelActive(titleScreenPanel, true);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        SetPanelActive(modeSelectPanel, true);
        RefreshMainMenuState();
    }

    public void ShowStageSelect()
    {
        HideAllPanels();
        SetPanelActive(stageSelectPanel, true);

        var characterSelectPanel = stageSelectPanel != null
            ? stageSelectPanel.GetComponent<StageSelectPanel>()
            : null;
        characterSelectPanel?.RefreshProgression();
    }

    public void ShowSpecialCardSelect()
    {
        HideAllPanels();
        SetPanelActive(specialCardSelectPanel, true);

        var specialCardPanel = specialCardSelectPanel != null
            ? specialCardSelectPanel.GetComponent<SpecialCardSelectPanel>()
            : null;
        specialCardPanel?.ResetSelectionForOpen();
    }

    public void ShowSettings()
    {
        HideAllPanels();
        SetPanelActive(settingsPanel, true);
    }

    public void BackToMainMenu()
    {
        ShowMainMenu();
    }

    public void BackFromStageSelect()
    {
        ShowMainMenu();
    }

    public void BackFromSpecialCardSelect()
    {
        GameModeData modeData =
            currentGameModeData ?? GameModeManager.GetGameModeData();
        if (modeData != null &&
            modeData.Mode == GameModeData.GameMode.IkkiMode)
        {
            ShowStageSelect();
            return;
        }

        ShowMainMenu();
    }

    public void SetSpecialCard(CardData cardData)
    {
        currentGameModeData?.AddSpecialCard(cardData);
    }

    public void RemoveSpecialCard(CardData cardData)
    {
        currentGameModeData?.RemoveSpecialCard(cardData);
    }

    public int GetSelectedSpecialCardCount()
    {
        return currentGameModeData?.SelectedSpecialCardDatas.Count ?? 0;
    }

    public void StartGame()
    {
        if (currentGameModeData == null)
        {
            Debug.LogError("GameModeData is not set.");
            return;
        }

        if (gameManager != null)
        {
            GameModeManager.SetGameModeData(currentGameModeData);
            gameManager.StartGameWithMode(currentGameModeData);
            HideAllPanels();
        }
    }

    public void SelectIkkiMode()
    {
        currentGameModeData = new GameModeData(GameModeData.GameMode.IkkiMode);
        currentGameModeData.SetCurrentLevel(CpuLevelCatalog.MinLevel);
        GameModeManager.SetGameModeData(currentGameModeData);
        ShowStageSelect();
    }

    public void SelectBattleGroundMode()
    {
        if (!GameProgressStore.IsBattleGroundUnlocked)
        {
            Debug.LogWarning("Battle Ground mode is locked until the final Ikki boss is defeated.");
            RefreshMainMenuState();
            return;
        }

        currentGameModeData = new GameModeData(GameModeData.GameMode.BattleGroundMode);
        currentGameModeData.SetCurrentLevel(CpuLevelCatalog.MinLevel);
        currentGameModeData.ResetWinStreak();
        GameModeManager.SetGameModeData(currentGameModeData);
        ShowSpecialCardSelect();
    }

    public void SelectKachinukiMode()
    {
        SelectBattleGroundMode();
    }

    public void SelectKatinukiMode()
    {
        SelectIkkiMode();
    }

    public void SelectStage(int stageNumber)
    {
        int level = stageNumber + 1;
        if (!GameProgressStore.IsIkkiLevelUnlocked(level))
        {
            Debug.LogWarning($"CPU level {level} is not unlocked.");
            return;
        }

        currentGameModeData = new GameModeData(GameModeData.GameMode.IkkiMode);
        currentGameModeData.SetCurrentLevel(level);
        GameModeManager.SetGameModeData(currentGameModeData);
        ShowSpecialCardSelect();
    }

    private void RefreshMainMenuState()
    {
        var mainMenuPanel = modeSelectPanel != null
            ? modeSelectPanel.GetComponent<MainMenuPanel>()
            : null;
        mainMenuPanel?.RefreshModeAvailability();
    }
}
