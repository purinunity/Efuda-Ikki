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
    [SerializeField] private TextMeshProUGUI H; // テキスト表示パネル
    [SerializeField] private TextMeshProUGUI L1; // テキスト表示パネル
    [SerializeField] private TextMeshProUGUI L2; // テキスト表示パネル

    public bool UIUpdateInProgress { get; private set; } = false;


    // 初期化処理
    private void Awake()
    {
    }

    // 終了時のイベント解除
    private void OnDestroy()
    {
    }

    // ゲーム状態変更時のUI更新処理
    public void UIUpdate(GameState state, float duration)
    {   
        UIUpdateInProgress = true;
        r.text = state.RoundNumber.ToString();
        L1.text = "体力 " + state.PlayerStates[0].LifePoints.ToString(); //☆修正by降幡
        L2.text = "体力 " + state.PlayerStates[1].LifePoints.ToString(); //☆修正by降幡
        var now = HandEvaluator.EvaluateHand(state.PlayerStates[0].HandCards, state.commonCards);
        H.text = now.Name;
        deck.SetCards(state.deckCards,duration); // デッキ表示
        common.SetCards(state.commonCards,duration); // 共通札表示
        trash.SetCards(state.trashCards,duration); // 捨て札表示
        foreach (var playerState in state.PlayerStates)
        {
            if (playerState.PlayerId == 0)
            {
                player1.SetCards(playerState.HandCards,duration); // プレイヤー1手札表示
            }
            else if (playerState.PlayerId == 1)
            {
                player2.SetCards(playerState.HandCards,duration); // プレイヤー2手札表示
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
            if (allComplete)
            {
                break;
            }
            yield return null;

        }
        UIUpdateInProgress = false;
    }
}
