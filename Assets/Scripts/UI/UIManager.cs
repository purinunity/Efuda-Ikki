using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI全体の管理・演出調整を行うクラス
// デッキ・共通札・手札・捨て札などの表示を制御
public class UIManager : MonoBehaviour
{
    [SerializeField] public CardArea deck; // デッキ表示エリア
    [SerializeField] public CardArea common; // 共通札表示エリア
    [SerializeField] public CardArea player1; // プレイヤー1の手札表示エリア
    [SerializeField] public CardArea player2; // プレイヤー2の手札表示エリア
    [SerializeField] public CardArea trash; // 捨て札表示エリア
    [SerializeField] public Cards allCards; // 全カード管理
    // [SerializeField] private GameManager gameManager; // ゲーム管理
    [SerializeField] private TextMeshProUGUI r; // テキスト表示パネル
    [SerializeField] private PlayerRole H; // テキスト表示パネル 
    [SerializeField] private TextMeshProUGUI L1; // テキスト表示パネル
    [SerializeField] private TextMeshProUGUI L2; // テキスト表示パネル
    [SerializeField] private PlayerRemainTrashCount playerRemainTrashCount; // 残り手札交換回数表示
    public float cardMoveSpeed = 800f; // カード移動速度（単位: ピクセル/秒）
    public float cardTurnSpeed = 720f; // カード回転速度（単位: 度/秒）

    public bool UIUpdateInProgress { get; private set; } = false;


    // 初期化処理：必要ならコンポーネント参照や初期UI状態の設定を行う（現在は特に処理なし）
    private void Awake()
    {
    }

    // 破棄時の後処理：イベント解除やコルーチン停止等があればここで行う（現在は特に処理なし）
    private void OnDestroy()
    {
    }

    // ゲーム状態変更時のUI更新処理
    public void UIUpdate(GameState state, float duration)
    {   
        UIUpdateInProgress = true;
        r.text = state.RoundNumber.ToString(); // ラウンド数
        L1.text = state.PlayerStates[0].LifePoints.ToString(); //☆修正by降幡
        L2.text = state.PlayerStates[1].LifePoints.ToString(); //☆修正by降幡
        var now = HandEvaluator.EvaluateHand(state.PlayerStates[0].HandCards, state.commonCards);
        // 役の表示をルーレット演出付きで更新
        H.SetRole(now.Name, duration);
        playerRemainTrashCount.UpdateRemainTrashCount(state.maxHandTrashTurn - state.PlayerStates[0].HandTrashTurnsUsed); // 残り手札交換回数表示更新

        // deck.SetCards(state.deckCards,duration); // デッキ表示
        // common.SetCards(state.commonCards,duration); // 共通札表示
        // trash.SetCards(state.trashCards,duration); // 捨て札表示
        deck.SetCardsBySpeed(state.deckCards, cardMoveSpeed, cardTurnSpeed); // デッキ表示
        common.SetCardsBySpeed(state.commonCards, cardMoveSpeed, cardTurnSpeed); // 共通札表示
        trash.SetCardsBySpeed(state.trashCards, cardMoveSpeed, cardTurnSpeed); // 捨て札表示
        foreach (var playerState in state.PlayerStates)
        {
            if (playerState.PlayerId == 0 && playerState.HandCards.Count == 5)
            {
                // player1.SetCards(playerState.HandCards,duration); // プレイヤー1手札表示
                player1.SetCardsBySpeed(playerState.HandCards, cardMoveSpeed, cardTurnSpeed); // プレイヤー1手札表示
            }
            else if (playerState.PlayerId == 1 && playerState.HandCards.Count == 5)
            {
                // player2.SetCards(playerState.HandCards,duration); // プレイヤー2手札表示
                player2.SetCardsBySpeed(playerState.HandCards, cardMoveSpeed, cardTurnSpeed); // プレイヤー2手札表示
            }
        }

        foreach (var card in allCards.cardList)
        {
            card.IsSelectable = false; // 全カードの選択不可に設定
        }
        foreach (var card in player1.cardsInArea)
        {
            card.IsSelectable = true; // プレイヤー1の手札のみ選択可能に設定
        }
        StartCoroutine(CheckUIUpdateComplete()); // UI更新完了待機
    }

    IEnumerator CheckUIUpdateComplete()
    {
        yield return null; // 1フレーム待機
        while (true)
        {
            bool allComplete = true;
            foreach (var card in allCards.cardList)
            {
                if (!card.MoveComplete)
                {
                    allComplete = false;
                    break;
                }
            }
            // プレイヤー役のルーレット演出が終わっているかも確認
            if (H != null && H.IsAnimating) allComplete = false;
            if (allComplete)
            {
                break;
            }
            yield return null;

        }
        UIUpdateInProgress = false;
    }
}
