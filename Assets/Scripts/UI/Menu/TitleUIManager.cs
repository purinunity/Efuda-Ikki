using UnityEngine;

public class TitleUIManager : MonoBehaviour
{
    public CanvasGroup titleScreenPanel;
    public CanvasGroup modeSelectPanel;
    public CanvasGroup stageSelectPanel;
    public CanvasGroup specialCardSelectPanel;
    public CanvasGroup settingsPanel;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private UiRootCoordinator rootCoordinator;
    [SerializeField] private MenuUiReferences menuUiReferences;

    private GameModeData currentGameModeData;

    private void Awake()
    {
        BindViewEvents();
    }

    private void Start()
    {
        ShowTitleScreen();
    }

    private void OnDestroy() => UnbindViewEvents();

    private void BindViewEvents()
    {
        UnbindViewEvents();
        if (menuUiReferences == null) return;
        if (menuUiReferences.Title != null) menuUiReferences.Title.ContinueRequested += ShowMainMenu;
        if (menuUiReferences.ModeSelect != null)
        {
            menuUiReferences.ModeSelect.IkkiModeRequested += SelectIkkiMode;
            menuUiReferences.ModeSelect.BattleGroundRequested += SelectBattleGroundMode;
            menuUiReferences.ModeSelect.SettingsRequested += ShowSettings;
        }
        if (menuUiReferences.StageSelect != null)
        {
            menuUiReferences.StageSelect.StageRequested += SelectStage;
            menuUiReferences.StageSelect.BackRequested += BackFromStageSelect;
        }
        if (menuUiReferences.SpecialCardSelect != null)
        {
            menuUiReferences.SpecialCardSelect.StartRequested += StartGame;
            menuUiReferences.SpecialCardSelect.BackRequested += BackFromSpecialCardSelect;
        }
        if (menuUiReferences.Settings != null) menuUiReferences.Settings.BackRequested += BackToMainMenu;
    }

    private void UnbindViewEvents()
    {
        if (menuUiReferences == null) return;
        if (menuUiReferences.Title != null) menuUiReferences.Title.ContinueRequested -= ShowMainMenu;
        if (menuUiReferences.ModeSelect != null)
        {
            menuUiReferences.ModeSelect.IkkiModeRequested -= SelectIkkiMode;
            menuUiReferences.ModeSelect.BattleGroundRequested -= SelectBattleGroundMode;
            menuUiReferences.ModeSelect.SettingsRequested -= ShowSettings;
        }
        if (menuUiReferences.StageSelect != null)
        {
            menuUiReferences.StageSelect.StageRequested -= SelectStage;
            menuUiReferences.StageSelect.BackRequested -= BackFromStageSelect;
        }
        if (menuUiReferences.SpecialCardSelect != null)
        {
            menuUiReferences.SpecialCardSelect.StartRequested -= StartGame;
            menuUiReferences.SpecialCardSelect.BackRequested -= BackFromSpecialCardSelect;
        }
        if (menuUiReferences.Settings != null) menuUiReferences.Settings.BackRequested -= BackToMainMenu;
    }

    private void Update()
    {
        if (!IsTitleScreenVisible()) return;

        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (control && shift && Input.GetKeyDown(KeyCode.U))
        {
            GameProgressStore.UnlockAllProgress();
            GameModeManager.ResetGameModeData();
            currentGameModeData = null;
            Debug.Log("Debug command accepted: all modes, stages, and special cards unlocked.");
        }
    }

    private bool IsTitleScreenVisible()
    {
        return titleScreenPanel != null &&
               titleScreenPanel.gameObject.activeInHierarchy &&
               titleScreenPanel.alpha > 0.99f &&
               titleScreenPanel.interactable;
    }

    private void SetPanelActive(CanvasGroup panel, bool isActive)
    {
        if (panel == null)
        {
            return;
        }

        // Scene authors select the screen they want to edit with the normal
        // Active checkbox on the screen wrapper (TitleScreen, ModeSelectScreen,
        // and so on). Keep the prefab instance itself active so disabling a
        // LayoutGroup never serializes driven child RectTransforms as overrides.
        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;
        panel.gameObject.SetActive(true);
        GetPanelVisibilityRoot(panel).SetActive(isActive);
    }

    private static GameObject GetPanelVisibilityRoot(CanvasGroup panel)
    {
        Transform parent = panel.transform.parent;
        return parent != null && parent.name.EndsWith("Screen")
            ? parent.gameObject
            : panel.gameObject;
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
        rootCoordinator?.ShowMenu();
        HideAllPanels();
        SetPanelActive(titleScreenPanel, true);
    }

    public void ShowMainMenu()
    {
        rootCoordinator?.ShowMenu();
        HideAllPanels();
        // Resolve unlock state and layout while the screen is still hidden.
        // This prevents the authored default state from being rendered for one
        // frame before the saved progression is applied.
        RefreshMainMenuState();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        SetPanelActive(modeSelectPanel, true);
    }

    public void ShowStageSelect()
    {
        rootCoordinator?.ShowMenu();
        HideAllPanels();
        SetPanelActive(stageSelectPanel, true);

        var characterSelectPanel = stageSelectPanel != null
            ? stageSelectPanel.GetComponent<StageSelectPanel>()
            : null;
        characterSelectPanel?.RefreshProgression();
    }

    public void ShowSpecialCardSelect()
    {
        rootCoordinator?.ShowMenu();
        HideAllPanels();
        SetPanelActive(specialCardSelectPanel, true);

        var specialCardPanel = specialCardSelectPanel != null
            ? specialCardSelectPanel.GetComponent<SpecialCardSelectPanel>()
            : null;
        specialCardPanel?.ResetSelectionForOpen();
    }

    public void ShowSettings()
    {
        rootCoordinator?.ShowMenu();
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
            rootCoordinator?.ShowGameplay();
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
