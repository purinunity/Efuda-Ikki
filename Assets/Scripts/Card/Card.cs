using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// カードオブジェクトの制御を行うクラス
// ボタンで選択可能・選択状態の管理・表示切替など

public class Card : MonoBehaviour // カードの表示・状態管理
{
    public CardData CardData { get; private set; } // カード情報
    private Image cardImage; // カード画像
    private RectTransform cardRect; // RectTransform参照
    public bool IsFaceUp = false; // 表向きかどうか
    private bool LastFaceUp { get; set; } = false; // 最後に表向きだったかどうか
    public bool MoveComplete { get; private set; } = true; // 移動完了フラグ
    public bool IsSelectable = false; // 選択可能フラグ
    private bool LastSelected { get; set; } = false; // 最後に選択されていたかどうか
    public bool IsSelected = false; // 選択状態フラグ

    public Vector2 TargetPosition { get; set; } //位置を保存するプロパティ
    private Vector2 LastWorldPosition { get; set; } //最後に移動した位置を保存するプロパティ

    // 初期化処理（Image, RectTransform取得）
    public void Initialize()
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
        if (cardRect == null)
        {
            cardRect = GetComponent<RectTransform>();
        }
        // Buttonコンポーネント取得とクリックイベント登録
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(ToggleSelect);
        }
    }
    // クリックイベントで選択フラグをトグル
    private void ToggleSelect()
    {
        if (!IsSelectable) return; // 選択可能でなければ無視
        IsSelected = !IsSelected;
        MoveAndTurnCard(0.3f);
    }

    // カードのRectTransform取得
    public RectTransform GetCardRect()
    {
        return cardRect;
    }

    // オブジェクト生成時の初期化
    public void Awake()
    {
        Initialize();
    }

    // カード情報をセットし、画像を更新
    public void SetCardData(CardData cardData)
    {
        CardData = cardData;
        if (cardImage != null && CardData != null)
        {
            cardImage.sprite = CardData.BackImage;
        }
    }

    // カードの数字を取得
    public Number GetCardNumber()
    {
        return CardData.number;
    }
    // カードのスートを取得
    public Suit GetCardSuit()
    {
        return CardData.suit;
    }

    // カードを移動・回転させる（アニメーション用）
    public void MoveAndTurnCard(float moveDuration = 1f)
    {
        MoveComplete = false;
        StartCoroutine(MoveAndTurnToPosition(moveDuration));
    }

    public void WaitAndMove(float waittime, float moveDuration = 1f)
    {
        MoveComplete = false;
        StartCoroutine(WaitAndMoveToPosition(waittime,moveDuration));
    }
    
    private IEnumerator WaitAndMoveToPosition(float waittime, float moveDuration)
    {
        yield return new WaitForSeconds(waittime); // 少し待ってから動かす
        yield return StartCoroutine(MoveAndTurnToPosition(moveDuration));
    }

    private IEnumerator MoveAndTurnToPosition( float moveDuration)
    {
        // Debug.Log($"Moving card {CardData.number} of {CardData.suit} to {TargetPosition}, FaceUp: {IsFaceUp}, Selected: {IsSelected}");
        var WorldPosition = this.transform.parent != null ? (Vector2)this.transform.parent.TransformPoint(TargetPosition) : TargetPosition;
        if (IsFaceUp != LastFaceUp && (WorldPosition != LastWorldPosition || IsSelected != LastSelected))
        {
            LastFaceUp = IsFaceUp;
            LastWorldPosition = WorldPosition;
            LastSelected = IsSelected;
            // Moveカードを動かす処理
            yield return MoveToPosition(cardRect, moveDuration / 2);
            // Turnカードを裏表替える処理
            yield return TurnToPosition(cardRect, moveDuration / 2);
        }
        else if (IsSelected != LastSelected)
        {
            LastSelected = IsSelected;
            // Moveカードを動かす処理
            yield return MoveToPosition(cardRect, moveDuration);
        }
        else if (IsFaceUp != LastFaceUp)
        {
            LastFaceUp = IsFaceUp;
            // Turnカードを裏表替える処理
            yield return TurnToPosition(cardRect, moveDuration);
        }
        else if (WorldPosition != LastWorldPosition)
        {
            LastWorldPosition = WorldPosition;
            // Moveカードを動かす処理
            yield return MoveToPosition(cardRect, moveDuration);
        }
        else
        {
            yield return new WaitForSeconds(0f);
        }
        MoveComplete = true;
    }

    // public void MoveCard(Vector2 targetPosition, float moveDuration = 0.5f)
    // {
    //     if (targetPosition == null)
    //     {
    //         Debug.LogError("Target position is null!");
    //         return;
    //     }
    //     StartCoroutine(MoveToPosition(cardRect, targetPosition, moveDuration));
    // }

    private IEnumerator MoveToPosition(RectTransform rectTransform, float moveDuration)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            if (IsSelected)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, new Vector2(TargetPosition.x, TargetPosition.y + 20), EaseOutCubic(t));
            }
            else
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, TargetPosition, EaseOutCubic(t));
            }
            
            yield return null;
        }

        if (IsSelected)
        {
            rectTransform.anchoredPosition = new Vector2(TargetPosition.x, TargetPosition.y + 20);
        }
        else
        {
            rectTransform.anchoredPosition = TargetPosition;
        }
    }

    private IEnumerator TurnToPosition(RectTransform rectTransform, float turnDuration)
    {
        float originalWidth = rectTransform.sizeDelta.x;
        float originalHeight = rectTransform.sizeDelta.y;
        float halfDuration = turnDuration / 2f;
        float elapsed = 0f;

        // 幅を徐々に0にする
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float newWidth = Mathf.Lerp(originalWidth, 0f, t);
            rectTransform.sizeDelta = new Vector2(newWidth, originalHeight);
            yield return null;
        }
        rectTransform.sizeDelta = new Vector2(0f, originalHeight);

        // 画像を切り替え
        if (IsFaceUp)
        {
            cardImage.sprite = CardData.Image;
            cardImage.SetNativeSize();
            rectTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // サイズを元に戻す
        }
        else
        {
            cardImage.sprite = CardData.BackImage;
            cardImage.SetNativeSize();
            rectTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // サイズを元に戻す
        }
        float targetWidth = rectTransform.sizeDelta.x;
        float targetHeight = rectTransform.sizeDelta.y;

        // 幅を元に戻す
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float newWidth = Mathf.Lerp(0f, targetWidth, t);
            rectTransform.sizeDelta = new Vector2(newWidth, targetHeight);
            yield return null;
        }
        rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}