using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 特殊札選択用のカードエリア
/// カードを中央に重ねて表示し、クリックされたカードを最下段（背後）に表示する
/// </summary>
public class SpecialCardArea : CardArea
{
    private float stackOverlapOffset = 10f; // 重ねるときの見える量（ピクセル）

    /// <summary>
    /// カードをオーバーラップさせて中央に配置するオーバーライド
    /// </summary>
    public override void SetCards(List<Card> cards, float totalDuration = 1.0f)
    {
        cardsInArea.Clear();
        foreach (var card in cards)
        {
            if (card != null && !cardsInArea.Contains(card))
            {
                cardsInArea.Add(card);
                // カード選択時に最下段に移動するハンドラを設定
                SetupCardClickHandler(card);
            }
        }
        CardsStackedPositionUpdate(totalDuration);
    }

    /// <summary>
    /// 速度指定版のカード配置（オーバーライド）
    /// </summary>
    public override void SetCardsBySpeed(List<Card> cards, float moveSpeed, float turnSpeed)
    {
        cardsInArea.Clear();
        foreach (var card in cards)
        {
            if (card != null && !cardsInArea.Contains(card))
            {
                cardsInArea.Add(card);
                SetupCardClickHandler(card);
            }
        }
        CardsStackedPositionUpdateBySpeed(moveSpeed, turnSpeed);
    }

    /// <summary>
    /// カード配置：重ねる位置を計算し、アニメーションなしで即座に適用
    /// </summary>
    private void CardsStackedPositionUpdate(float totalDuration = 1.0f)
    {
        if (areaRect == null || cardsInArea == null || cardsInArea.Count == 0)
        {
            return;
        }

        int visibleCount = cardsInArea.Count;
        for (int idx = 0; idx < visibleCount; idx++)
        {
            var card = cardsInArea[idx];
            if (card == null) continue;

            // カードを親に設定
            card.gameObject.transform.SetParent(areaRect);
            
            // Z順序（シブリングインデックス）を設定：最初のカードが最下段（インデックス0）
            card.gameObject.transform.SetSiblingIndex(idx);

            // 位置：中央に配置。z軸のオフセットも追加して視覚的に重なりを示す
            Vector2 stackPosition = Vector2.zero;
            // 微妙なオフセット（右下方向）を加えてスタック感を演出
            stackPosition.x = stackOverlapOffset * idx * 0.1f;
            stackPosition.y = -stackOverlapOffset * idx * 0.1f;
            card.TargetPosition = stackPosition;

            // アニメーション実行
            if (card.MoveComplete)
            {
                card.WaitAndMove(totalDuration / visibleCount * idx, totalDuration / visibleCount);
            }
        }
    }

    /// <summary>
    /// 速度指定版のカード重ね配置
    /// </summary>
    private void CardsStackedPositionUpdateBySpeed(float moveSpeed, float turnSpeed)
    {
        if (areaRect == null || cardsInArea == null || cardsInArea.Count == 0)
        {
            return;
        }

        int visibleCount = cardsInArea.Count;
        for (int idx = 0; idx < visibleCount; idx++)
        {
            var card = cardsInArea[idx];
            if (card == null) continue;

            card.gameObject.transform.SetParent(areaRect);
            card.gameObject.transform.SetSiblingIndex(idx);

            Vector2 stackPosition = Vector2.zero;
            stackPosition.x = stackOverlapOffset * idx * 0.1f;
            stackPosition.y = -stackOverlapOffset * idx * 0.1f;
            card.TargetPosition = stackPosition;

            if (card.MoveComplete)
            {
                card.WaitAndMoveBySpeed(0, moveSpeed, turnSpeed);
            }
        }
    }

    /// <summary>
    /// カード選択時：クリックされたカードを最下段（インデックス0）に移動
    /// </summary>
    private void SetupCardClickHandler(Card card)
    {
        Button cardButton = card.GetComponent<Button>();
        if (cardButton == null) return;

        // 既存のリスナーに追加する（既にSetupCardSelectionで登録されているリスナーに加えて）
        cardButton.onClick.AddListener(() => BringCardToBack(card));
    }

    /// <summary>
    /// 指定されたカードを最下段（背後）に移動させ、他のカードを前へシフト
    /// 大回りアニメーションで演出的に移動させる
    /// </summary>
    private void BringCardToBack(Card selectedCard)
    {
        if (selectedCard == null || !cardsInArea.Contains(selectedCard))
        {
            return;
        }

        // クリックされたカードをリストの最後に移動（表示順的には最下段）
        cardsInArea.Remove(selectedCard);
        cardsInArea.Add(selectedCard);

        // 全カードの Z 順序（シブリングインデックス）を更新
        for (int idx = 0; idx < cardsInArea.Count; idx++)
        {
            var card = cardsInArea[idx];
            if (card != null)
            {
                card.gameObject.transform.SetSiblingIndex(idx);
            }
        }

        // 大回りアニメーションで最下段に移動
        StartCoroutine(AnimateCardToBackWithArc(selectedCard));
    }

    /// <summary>
    /// カードを上方に大回りさせながら最下段位置へ移動させるアニメーション
    /// </summary>
    private System.Collections.IEnumerator AnimateCardToBackWithArc(Card card)
    {
        // 1. カードを上に移動（大回りの頂点）
        Vector2 currentPos = card.TargetPosition;
        Vector2 arcTopPos = new Vector2(currentPos.x, currentPos.y + 250f); // 上へ250px
        card.TargetPosition = arcTopPos;
        card.MoveAndTurnCard(0.25f);
        
        yield return new WaitUntil(() => card.MoveComplete);
        
        // 2. 最終位置（中央のスタック位置）へ移動
        Vector2 finalPos = Vector2.zero;
        // 最後に追加されたカードなので、インデックスは cardsInArea.Count - 1
        int finalIndex = cardsInArea.Count - 1;
        finalPos.x = stackOverlapOffset * finalIndex * 0.1f;
        finalPos.y = -stackOverlapOffset * finalIndex * 0.1f;
        card.TargetPosition = finalPos;
        card.MoveAndTurnCard(0.35f);
        
        yield return new WaitUntil(() => card.MoveComplete);
    }

    /// <summary>
    /// 重ねるときのオフセットを設定
    /// </summary>
    public void SetStackOverlapOffset(float offset)
    {
        stackOverlapOffset = offset;
    }
}
