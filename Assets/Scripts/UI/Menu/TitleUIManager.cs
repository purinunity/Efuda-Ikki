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
    }

    public void ShowSpecialCardSelect()
    {
        HideAllPanels();
        SetPanelActive(specialCardSelectPanel, true);

        var specialCardPanel = specialCardSelectPanel != null
            ? specialCardSelectPanel.GetComponent<SpecialCardSelectPanel>()
            : null;
        specialCardPanel?.RefreshSelectionFromModeData();
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
        ShowSpecialCardSelect();
    }

    public void SelectKachinukiMode()
    {
        if (!GameProgressStore.IsKachinukiUnlocked)
        {
            Debug.LogWarning("Kachinuki mode is locked until Ikki mode is cleared.");
            RefreshMainMenuState();
            return;
        }

        currentGameModeData = new GameModeData(GameModeData.GameMode.KachinukiMode);
        currentGameModeData.SetCurrentLevel(CpuLevelCatalog.MinLevel);
        currentGameModeData.ResetWinStreak();
        GameModeManager.SetGameModeData(currentGameModeData);
        ShowSpecialCardSelect();
    }

    public void SelectKatinukiMode()
    {
        SelectIkkiMode();
    }

    public void SelectBattleGroundMode()
    {
        SelectKachinukiMode();
    }

    public void SelectStage(int stageNumber)
    {
        currentGameModeData = new GameModeData(GameModeData.GameMode.IkkiMode);
        currentGameModeData.SetCurrentLevel(stageNumber + 1);
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
