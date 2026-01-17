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
    private float selectedYOffset = 20f; // 選択時のYオフセット

    public float SelectedYOffset
    {
        get => selectedYOffset;
        set => selectedYOffset = value;
    }

    // 初期化処理（Image, RectTransform取得）
    public void Initialize(float selectedYOffset = 20f)
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
        if (cardRect == null)
        {
            cardRect = GetComponent<RectTransform>();
        }
        this.selectedYOffset = selectedYOffset;
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

    // --- 速度指定版 API ---
    /// <summary>
    /// カードを「速度（単位: 単位/秒）」で移動・回転させるメソッド群（時間指定ではなく速度指定）
    /// moveSpeed: 移動速度（アンカー位置の単位/秒）
    /// turnSpeed: 回転(幅変化)の速度（Rect 単位/秒）
    /// どちらかの速度が <= 0 の場合は、従来の時間指定ベースの動作（デフォルトの所要時間）にフォールバックします。
    /// </summary>
    public void MoveAndTurnCardBySpeed(float moveSpeed = 100f, float turnSpeed = 100f)
    {
        MoveComplete = false;
        StartCoroutine(MoveAndTurnToPositionBySpeed(moveSpeed, turnSpeed));
    }

    public void WaitAndMoveBySpeed(float waittime, float moveSpeed = 100f, float turnSpeed = 100f)
    {
        MoveComplete = false;
        StartCoroutine(WaitAndMoveToPositionBySpeed(waittime, moveSpeed, turnSpeed));
    }

    private IEnumerator WaitAndMoveToPositionBySpeed(float waittime, float moveSpeed, float turnSpeed = 100f)
    {
        yield return new WaitForSeconds(waittime);
        yield return StartCoroutine(MoveAndTurnToPositionBySpeed(moveSpeed, turnSpeed));
    }

    private IEnumerator MoveAndTurnToPositionBySpeed(float moveSpeed, float turnSpeed)
    {
        var WorldPosition = this.transform.parent != null ? (Vector2)this.transform.parent.TransformPoint(TargetPosition) : TargetPosition;
        if (IsFaceUp != LastFaceUp && (WorldPosition != LastWorldPosition || IsSelected != LastSelected))
        {
            LastFaceUp = IsFaceUp;
            LastWorldPosition = WorldPosition;
            LastSelected = IsSelected;
            // Moveカードを動かす処理（速度指定）
            yield return MoveToPositionBySpeed(cardRect, moveSpeed);
            // Turnカードを裏表替える処理（速度指定）
            yield return TurnToPositionBySpeed(cardRect, turnSpeed);
        }
        else if (IsSelected != LastSelected)
        {
            LastSelected = IsSelected;
            // Moveカードを動かす処理（速度指定）
            yield return MoveToPositionBySpeed(cardRect, moveSpeed);
        }
        else if (IsFaceUp != LastFaceUp)
        {
            LastFaceUp = IsFaceUp;
            // Turnカードを裏表替える処理（速度指定）
            yield return TurnToPositionBySpeed(cardRect, turnSpeed);
        }
        else if (WorldPosition != LastWorldPosition)
        {
            LastWorldPosition = WorldPosition;
            // Moveカードを動かす処理（速度指定）
            yield return MoveToPositionBySpeed(cardRect, moveSpeed);
        }
        else
        {
            yield return new WaitForSeconds(0f);
        }
        MoveComplete = true;
    }

    private IEnumerator MoveToPositionBySpeed(RectTransform rectTransform, float moveSpeed)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 finalTarget = IsSelected ? new Vector2(TargetPosition.x, TargetPosition.y + selectedYOffset) : TargetPosition;
        float distance = Vector2.Distance(startPos, finalTarget);
        // moveSpeed <= 0 の場合は挙動を変えず 1 秒のデフォルト時間にフォールバックする
        float moveDuration = (moveSpeed > 0f && distance > 0f) ? distance / moveSpeed : 1f;
        yield return StartCoroutine(MoveToPosition(rectTransform, moveDuration));
    }

    private IEnumerator TurnToPositionBySpeed(RectTransform rectTransform, float turnSpeed)
    {
        if (turnSpeed > 0f)
        {
            float originalWidth = rectTransform.sizeDelta.x;
            // 幅を縮める（または戻す）時間は width / speed で計算されるため、合計の回転時間はその 2 倍とする
            float halfDuration = originalWidth / turnSpeed;
            float turnDuration = halfDuration * 2f;
            yield return StartCoroutine(TurnToPosition(rectTransform, turnDuration));
        }
        else
        {
            // フォールバックとしてデフォルトで 1 秒の回転時間を使用する
            yield return StartCoroutine(TurnToPosition(rectTransform, 1f));
        }
    }

    private IEnumerator MoveAndTurnToPosition( float moveDuration)
    {
        // カード移動のデバッグ用ログ（必要なら有効化）
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
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, new Vector2(TargetPosition.x, TargetPosition.y + selectedYOffset), EaseOutCubic(t));
            }
            else
            {
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, TargetPosition, EaseOutCubic(t));
            }
            
            yield return null;
        }

        if (IsSelected)
        {
            rectTransform.anchoredPosition = new Vector2(TargetPosition.x, TargetPosition.y + selectedYOffset);
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
            rectTransform.localScale = new Vector3(0.25f, 0.25f, 0.25f); // サイズを元に戻す　☆修正by降幡
        }
        else
        {
            cardImage.sprite = CardData.BackImage;
            cardImage.SetNativeSize();
            rectTransform.localScale = new Vector3(0.25f, 0.25f, 0.25f); // サイズを元に戻す　☆修正by降幡
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