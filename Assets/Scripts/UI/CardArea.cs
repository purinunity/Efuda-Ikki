using System.Collections.Generic;
using UnityEngine;

// カードを配置・表示するエリアを管理するクラス
public class CardArea : MonoBehaviour
{
    public List<Card> cardsInArea = new List<Card>(); // このエリア内のカードリスト
    public RectTransform areaRect; // カードエリアのRectTransform
    public bool isHorizontal = true;
    public bool isVertical = false;

    // エリアの初期化（カードリストクリア）
    public void Initialize()
    {
        cardsInArea.Clear(); // このエリア内のカードリストをクリア
    }
    // オブジェクト生成時の初期化
    public void Awake()
    {
        Initialize();
    }


    // エリア内のカードを全てクリア
    /// <summary>
    /// エリア内のカードを全て破棄してリストをクリアします。
    /// 注意: カードの GameObject を Destroy します。カードオブジェクトを残したい場合は別メソッドを追加してください。
    /// </summary>
    public void ClearCard()
    {
        for (int i = 0; i < cardsInArea.Count; i++)
        {
            var c = cardsInArea[i];
            if (c != null && c.gameObject != null)
            {
                Destroy(c.gameObject);
            }
        }
        cardsInArea.Clear();
    }

    // 複数のカードをセット
    public void SetCards(List<Card> cards, float totalDuration = 1.0f)
    {
        cardsInArea.Clear();
        foreach (var card in cards)
        {
            if (card != null && !cardsInArea.Contains(card))
            {
                cardsInArea.Add(card);
            }
        }
        CardsPositionUpdate(totalDuration);
    }

    // カードの位置を更新（横並び・縦並び対応）
    private void CardsPositionUpdate(float totalDuration = 1.0f)
    {
        if (areaRect == null || cardsInArea == null || cardsInArea.Count == 0)
        {
            return; // レイアウトするものがないか、areaRect が割り当てられていません
        }
        // レイアウトモード判定:
        // - isVertical && isHorizontal => 斜め
        // - isVertical && !isHorizontal => 縦
        // - !isVertical && isHorizontal => 横
        // - !isVertical && !isHorizontal => 全て重ねる
        bool both = isVertical && isHorizontal;
        bool onlyHorizontal = isHorizontal && !isVertical;
        bool onlyVertical = isVertical && !isHorizontal;

        // 準備: 共通のカード配列とサイズ配列を作る
        List<Card> validCards = new List<Card>();
        List<float> widths = new List<float>();
        List<float> heights = new List<float>();
        float areaWidth = areaRect.rect.width;
        float areaHeight = areaRect.rect.height;
        float totalCardsWidth = 0f;
        float totalCardsHeight = 0f;

        foreach (var c in cardsInArea)
        {
            if (c == null) continue;
            validCards.Add(c);
            RectTransform r = c.GetCardRect();
            float w = (r != null && r.rect.width > 0f) ? r.rect.width * r.localScale.x : 100f; // デフォルト幅
            float h = (r != null && r.rect.height > 0f) ? r.rect.height * r.localScale.y : 150f; // デフォルト高さ
            widths.Add(w);
            heights.Add(h);
            totalCardsWidth += w;
            totalCardsHeight += h;
        }

        int visibleCount = validCards.Count;
        if (visibleCount == 0) return;

        // ヘルパーで各軸の中心座標を計算
        List<float> centersX = ComputeCenters(areaWidth, widths);
        List<float> centersY = ComputeCenters(areaHeight, heights, vertical:true);

        if (both)
        {
            // 斜め配置: XとYの中心配列を組み合わせる（インデックス一致で配置）
            for (int idx = 0; idx < visibleCount; idx++)
            {
                var card = validCards[idx];
                float centerX = centersX.Count > idx ? centersX[idx] : 0f;
                float centerY = centersY.Count > idx ? centersY[idx] : 0f;
                card.gameObject.transform.SetParent(areaRect);
                card.gameObject.transform.SetAsLastSibling();
                card.TargetPosition = new Vector2(centerX, centerY);
                if (card.MoveComplete)
                {
                    card.WaitAndMove(totalDuration / visibleCount * idx, totalDuration / visibleCount);
                }
            }
            return;
        }

        if (onlyHorizontal)
        {
            // 横配置: centersX を使用し Y=0
            for (int idx = 0; idx < visibleCount; idx++)
            {
                var card = validCards[idx];
                float centerX = centersX.Count > idx ? centersX[idx] : 0f;
                card.gameObject.transform.SetParent(areaRect);
                card.gameObject.transform.SetAsLastSibling();
                card.TargetPosition = new Vector2(centerX, 0f);
                if (card.MoveComplete)
                {
                    card.WaitAndMove(totalDuration / visibleCount * idx, totalDuration / visibleCount);
                }
            }
            return;
        }
        if (onlyVertical)
        {
            // 縦配置: centersY を使用、X=0
            for (int idx = 0; idx < visibleCount; idx++)
            {
                var card = validCards[idx];
                float centerY = centersY.Count > idx ? centersY[idx] : 0f;
                card.gameObject.transform.SetParent(areaRect);
                card.gameObject.transform.SetAsLastSibling();
                card.TargetPosition = new Vector2(0f, centerY);
                if (card.MoveComplete)
                {
                    card.WaitAndMove(totalDuration / visibleCount * idx, totalDuration / visibleCount);
                }
            }
            return;
        }

        // 両方無効: 全て重ねて表示（中央）
        for (int idx = 0; idx < visibleCount; idx++)
        {
            var card = validCards[idx];
            card.gameObject.transform.SetParent(areaRect);
            card.gameObject.transform.SetAsLastSibling();
            card.TargetPosition = Vector2.zero;
            if (card.MoveComplete)
            {
                card.WaitAndMove(0f, totalDuration);
            }
        }
        return;
        }

    // 幅 or 高さと各カードサイズから、対応する中心座標リストを返す
    // vertical==true の場合、Y軸（上が正）の中心配列を返す
    private List<float> ComputeCenters(float areaSize, List<float> sizes, bool vertical = false)
    {
        List<float> centers = new List<float>();
        int n = sizes.Count;
        if (n == 0) return centers;

        float total = 0f;
        for (int i = 0; i < n; i++) total += sizes[i];

        // 単一要素は中央に配置
        if (n == 1)
        {
            centers.Add(0f);
            return centers;
        }

        if (total <= areaSize)
        {
            // ノーマル: 余白を均等に割り当てる
            float space = (areaSize - total) / (n + 1);
            float cursor = -areaSize / 2f + space;
            for (int i = 0; i < n; i++)
            {
                float half = sizes[i] / 2f;
                float center = cursor + half;
                float min = -areaSize / 2f + half;
                float max = areaSize / 2f - half;
                if (min > max) center = 0f;
                else center = Mathf.Clamp(center, min, max);
                if (vertical)
                {
                    // Y軸は上が正、cursor は上から始める
                    centers.Add(center);
                    cursor = center - half - space;
                }
                else
                {
                    centers.Add(center);
                    cursor = center + half + space;
                }
            }
        }
        else
        {
            // オーバーフロー: 隣接ペアごとに等しい重なり量を作る
            float overlap = (total - areaSize) / (n - 1);
            float center = vertical ? areaSize / 2f - sizes[0] / 2f : -areaSize / 2f + sizes[0] / 2f;
            for (int i = 0; i < n; i++)
            {
                float half = sizes[i] / 2f;
                float min = -areaSize / 2f + half;
                float max = areaSize / 2f - half;
                float centerClamped = (min > max) ? 0f : Mathf.Clamp(center, min, max);
                centers.Add(centerClamped);
                if (i + 1 < n)
                {
                    if (vertical)
                    {
                        center = center - (half + sizes[i + 1] / 2f) + overlap;
                    }
                    else
                    {
                        center = center + (half + sizes[i + 1] / 2f) - overlap;
                    }
                }
            }
        }

        return centers;
    }

    public Card DrawCard()
    {
        if (cardsInArea.Count > 0)
        {
            // int randomIndex = Random.Range(0, cardsInArea.Count);
            Card drawnCard = cardsInArea[0];
            cardsInArea.RemoveAt(0);
            Debug.Log($"Drawn Card: {drawnCard.GetCardNumber()} of {drawnCard.GetCardSuit()}");
            return drawnCard;
        }
        else
        {
            Debug.Log("No cards left to draw.");
            return null; // or throw an exception if preferred
        }
    }

    public Card DrawCard(Card card)
    {
        if (card == null)
        {
            Debug.LogWarning("DrawCard called with null card.");
            return null;
        }

        if (cardsInArea.Contains(card))
        {
            cardsInArea.Remove(card);
            Debug.Log($"Card drawn: {card.GetCardNumber()} of {card.GetCardSuit()}");
            return card;
        }

        Debug.Log("Card not found in the list.");
        return null; // or throw an exception if preferred
    }
    
    public List<Card> GetSelectedCardData()
    {
        List<Card> selectedCards = new List<Card>();
        foreach (var card in cardsInArea)
        {
            if (card == null) continue;
            if (card.IsSelected)
            {
                selectedCards.Add(card);
                card.IsSelected = false; // 取得後に選択状態をリセット
            }
        }
        return selectedCards;
    }
}
