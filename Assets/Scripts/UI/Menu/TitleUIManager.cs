using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleUIManager : MonoBehaviour
{
    // パネルの参照
    public CanvasGroup titleScreenPanel;
    public CanvasGroup mainMenuPanel;
    public CanvasGroup modeSelectPanel; // モード選択パネル
    public CanvasGroup stageSelectPanel;
    public CanvasGroup deckSelectPanel;
    public CanvasGroup specialCardSelectPanel;
    public CanvasGroup settingsPanel;

    // その他の参照
    [SerializeField] private GameManager gameManager;

    private GameModeData currentGameModeData;
    private int selectedStage = -1;

    private void Start()
    {
        // 初期状態：タイトル画面のみ表示
        ShowTitleScreen();
    }

    // パネルの表示/非表示を制御するヘルパーメソッド
    private void SetPanelActive(CanvasGroup panel, bool isActive)
    {
        panel.alpha = isActive ? 1f : 0f;
        panel.interactable = isActive;
        panel.blocksRaycasts = isActive;
    }

    // 全パネルを非表示にする
    private void HideAllPanels()
    {
        SetPanelActive(titleScreenPanel, false);
        SetPanelActive(modeSelectPanel, false);
        SetPanelActive(stageSelectPanel, false);
        SetPanelActive(deckSelectPanel, false);
        SetPanelActive(specialCardSelectPanel, false);
        SetPanelActive(settingsPanel, false);
    }

    // タイトル画面を表示
    public void ShowTitleScreen()
    {
        HideAllPanels();
        SetPanelActive(titleScreenPanel, true);
    }

    // メインメニューを表示
    public void ShowMainMenu()
    {
        HideAllPanels();
        SetPanelActive(modeSelectPanel, true);
    }

    // ステージ選択を表示
    public void ShowStageSelect()
    {
        HideAllPanels();
        SetPanelActive(stageSelectPanel, true);
    }

    // 特殊札選択を表示
    public void ShowSpecialCardSelect()
    {
        HideAllPanels();
        SetPanelActive(specialCardSelectPanel, true);
    }

    // 設定画面を表示
    public void ShowSettings()
    {
        HideAllPanels();
        SetPanelActive(settingsPanel, true);
    }

    // メインメニューに戻る
    public void BackToMainMenu()
    {
        ShowMainMenu();
    }

    // ステージ選択からメインメニューに戻る
    public void BackFromStageSelect()
    {
        ShowMainMenu();
    }

    // 特殊札選択から前の画面に戻る
    public void BackFromSpecialCardSelect()
    {
        // 特殊札選択パネルから戻る際、選択済みの特殊札を初期化する
        var modeData = GameModeManager.GetGameModeData();
        if (modeData != null)
        {
            modeData.SelectedSpecialCards?.Clear();
        }
        if (currentGameModeData != null)
        {
            currentGameModeData.SelectedSpecialCards?.Clear();
        }

        // 現在のモードに応じて戻り先を判定
        if (currentGameModeData != null && currentGameModeData.Mode == GameModeData.GameMode.KatinukiMode)
        {
            // 勝ち抜きモード：ステージ選択に戻る
            ShowStageSelect();
        }
        else
        {
            // バトルグラウンドモード：メインメニューに戻る
            ShowMainMenu();
        }
    }

    // 特殊札を選択
    public void SetSpecialCard(Card card)
    {
        if (currentGameModeData.SelectedSpecialCards.Count < 4)
        {
            currentGameModeData.SelectedSpecialCards.Add(card);
        }
    }

    // 特殊札選択を解除
    public void RemoveSpecialCard(Card card)
    {
        currentGameModeData.SelectedSpecialCards.Remove(card);
    }

    // 選択された特殊札の数を取得
    public int GetSelectedSpecialCardCount()
    {
        return currentGameModeData.SelectedSpecialCards.Count;
    }

    // ゲームを開始
    public void StartGame()
    {
        if (currentGameModeData == null)
        {
            Debug.LogError("GameModeDataが設定されていません");
            return;
        }

        // GameManagerにモード情報を渡してゲーム開始
        if (gameManager != null)
        {
            // 最新のデータを共有してから開始
            GameModeManager.SetGameModeData(currentGameModeData);
            gameManager.StartGameWithMode(currentGameModeData);
            HideAllPanels();
        }
    }

    // 勝ち抜きモードを選択
    public void SelectKatinukiMode()
    {
        currentGameModeData = new GameModeData(GameModeData.GameMode.KatinukiMode);
        GameModeManager.SetGameModeData(currentGameModeData);
        ShowStageSelect();
    }

    // バトルグラウンドモードを選択
    public void SelectBattleGroundMode()
    {
        currentGameModeData = new GameModeData(GameModeData.GameMode.BattleGroundMode);
        GameModeManager.SetGameModeData(currentGameModeData);
        ShowSpecialCardSelect();
    }

    // ステージを選択
    public void SelectStage(int stageNumber)
    {
        if (currentGameModeData.Mode == GameModeData.GameMode.KatinukiMode)
        {
            currentGameModeData.SelectedStage = stageNumber;
            GameModeManager.SetGameModeData(currentGameModeData);
            ShowSpecialCardSelect();
        }
    }
}
