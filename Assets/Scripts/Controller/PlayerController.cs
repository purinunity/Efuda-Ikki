using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// プレイヤーの入力を制御するクラス
public class PlayerController : Controller
{
    public bool IsInputReceived = false; // 入力が完了したかどうかのフラグ
    public List<Card> trash; // 捨てるカードリスト
    [SerializeField] public CardArea playerHands; // 捨てるカードリスト（外部から設定用）
    public bool IsInputReceivable { get; set; } = false; // 入力受付可能フラグ
    private int maxTrashCountThisTurn = int.MaxValue;

    // プレイヤーの入力待ち（UI表示や入力完了まで待機）
    // 現状はダミーで即座に応答
    public override IEnumerator Act(GameState gameState, System.Action<ControllerResponse> callback)
    {
        maxTrashCountThisTurn = Mathf.Max(0, gameState.maxHandTrashCount);
        IsInputReceivable = true; // 入力受付可能に設定
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
    }
}
