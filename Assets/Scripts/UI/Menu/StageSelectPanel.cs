using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectPanel : MonoBehaviour
{
    public Button[] stageButtons = new Button[9];
    public TextMeshProUGUI stageInfoText;
    public Button backButton;
    public TitleUIManager titleUIManager;

    private void Start()
    {
        // 9つのステージボタンをセットアップ
        for (int i = 0; i < 9; i++)
        {
            int stageNumber = i; // クロージャ用
            if (stageButtons[i] != null)
            {
                stageButtons[i].onClick.AddListener(() => SelectStage(stageNumber));
            }
        }

        // 戻るボタン
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => titleUIManager.BackFromStageSelect());
        }
    }

    private void SelectStage(int stageNumber)
    {
        titleUIManager.SelectStage(stageNumber);
    }
}
