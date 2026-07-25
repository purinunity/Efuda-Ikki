using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// プレイヤーの入力を制御するクラス
public class PlayerController : Controller
{
    public bool IsInputReceived = false; // 入力が完了したかどうかのフラグ
    public List<Card> trash; // 捨てるカードリスト
    [SerializeField] public CardArea playerHands; // 捨てるカードリスト（外部から設定用）
    [SerializeField] private Button decisionButton;
    [SerializeField] private Image decisionButtonImage;
    [SerializeField] private Color decisionButtonNormalColor = Color.white;
    [SerializeField] private Color decisionButtonPressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    public bool IsInputReceivable { get; set; } = false; // 入力受付可能フラグ
    private int maxTrashCountThisTurn = int.MaxValue;
    private bool isFinalTrashTurnThisAct = false;

    private void Awake()
    {
        ResolveDecisionButton();
        SetDecisionButtonPressed(false, true);
    }

    // プレイヤーの入力待ち（UI表示や入力完了まで待機）
    // 現状はダミーで即座に応答
    public override IEnumerator Act(GameState gameState, System.Action<ControllerResponse> callback)
    {
        maxTrashCountThisTurn = Mathf.Max(0, gameState.maxHandTrashCount);
        isFinalTrashTurnThisAct = IsFinalTrashTurn(gameState);
        IsInputReceivable = true; // 入力受付可能に設定
        SetDecisionButtonPressed(false, true);
        while (!IsInputReceived)
        {
            yield return null; // 入力完了まで待機
        }
        IsInputReceived = false; // フラグをリセット
        IsInputReceivable = false; // 入力受付不可に設定

        var response = new ControllerResponse
        {
            actionCompleted = true,
            cardsTrash = trash
        };
        callback?.Invoke(response);
    }
    
    // プレイヤーの入力を受け取るメソッド
    public void ReceiveInput()
    {
        if (!IsInputReceivable) return; // 入力受付可能でなければ無視
        trash = playerHands.GetSelectedCardData(); // 選択されたカードを取得
        if (trash != null && trash.Count > maxTrashCountThisTurn)
        {
            trash = trash.GetRange(0, maxTrashCountThisTurn);
        }
        IsInputReceived = true;
        SetDecisionButtonPressed(true, false);

        if (isFinalTrashTurnThisAct)
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.SetPlayerSpecialCardInputEnabled(false);
            }
        }
    }

    private bool IsFinalTrashTurn(GameState gameState)
    {
        if (gameState == null || gameState.PlayerStates == null || gameState.PlayerStates.Count == 0)
        {
            return false;
        }

        PlayerState playerState = gameState.PlayerStates[0];
        int maxTurns = Mathf.Max(0, gameState.maxHandTrashTurn);
        return playerState != null && playerState.HandTrashTurnsUsed >= maxTurns - 1;
    }

    private void ResolveDecisionButton()
    {
        if (decisionButton == null)
        {
            GameObject buttonObject = GameObject.Find("D_button");
            if (buttonObject != null)
            {
                decisionButton = buttonObject.GetComponent<Button>();
            }
        }

        if (decisionButtonImage == null && decisionButton != null)
        {
            decisionButtonImage = decisionButton.targetGraphic as Image;
            if (decisionButtonImage == null)
            {
                decisionButtonImage = decisionButton.GetComponent<Image>();
            }
        }
    }

    private void SetDecisionButtonPressed(bool pressed, bool interactable)
    {
        ResolveDecisionButton();

        if (decisionButtonImage != null)
        {
            decisionButtonImage.color = pressed ? decisionButtonPressedColor : decisionButtonNormalColor;
        }

        if (decisionButton != null)
        {
            decisionButton.interactable = interactable;
        }
    }
}
