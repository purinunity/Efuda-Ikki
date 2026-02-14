using UnityEngine;
using UnityEngine.UI;

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
        var modeData = GameModeManager.GetGameModeData();
        modeData?.ClearSpecialCards();
        currentGameModeData?.ClearSpecialCards();

        if (currentGameModeData != null && currentGameModeData.Mode == GameModeData.GameMode.KatinukiMode)
        {
            ShowStageSelect();
        }
        else
        {
            ShowMainMenu();
        }
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

    public void SelectKatinukiMode()
    {
        currentGameModeData = new GameModeData(GameModeData.GameMode.KatinukiMode);
        GameModeManager.SetGameModeData(currentGameModeData);
        ShowStageSelect();
    }

    public void SelectBattleGroundMode()
    {
        currentGameModeData = new GameModeData(GameModeData.GameMode.BattleGroundMode);
        GameModeManager.SetGameModeData(currentGameModeData);
        ShowSpecialCardSelect();
    }

    public void SelectStage(int stageNumber)
    {
        if (currentGameModeData != null && currentGameModeData.Mode == GameModeData.GameMode.KatinukiMode)
        {
            currentGameModeData.SelectedStage = stageNumber;
            GameModeManager.SetGameModeData(currentGameModeData);
            ShowSpecialCardSelect();
        }
    }
}
