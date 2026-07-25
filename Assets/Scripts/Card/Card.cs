using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// カードオブジェクトの制御を行うクラス
// ボタンで選択可能・選択状態の管理・表示切替など

public class Card : MonoBehaviour // カードの表示・状態管理
{
    private readonly CardRuntimeState runtimeState = new CardRuntimeState();

    public CardRuntimeState RuntimeState => runtimeState;
    public CardData CardData => runtimeState.CardData; // カード情報
    private Image cardImage; // カード画像
    private RectTransform cardRect; // RectTransform参照
    public bool IsFaceUp // 表向きかどうか
    {
        get => runtimeState.IsFaceUp;
        set => runtimeState.IsFaceUp = value;
    }
    private bool LastFaceUp { get; set; } = false; // 最後に表向きだったかどうか
    public bool MoveComplete { get; private set; } = true; // 移動完了フラグ
    public bool IsSelectable // 選択可能フラグ
    {
        get => runtimeState.IsSelectable;
        set => runtimeState.IsSelectable = value;
    }
    private bool LastSelected { get; set; } = false; // 最後に選択されていたかどうか
    public bool IsSelected // 選択状態フラグ
    {
        get => runtimeState.IsSelected;
        set => runtimeState.IsSelected = value;
    }

    public Vector2 TargetPosition { get; set; } //位置を保存するプロパティ
    private Vector2 LastWorldPosition { get; set; } //最後に移動した位置を保存するプロパティ
    private float selectedYOffset = 20f; // 選択時のYオフセット

    private Coroutine movementCoroutine;
    private const float PositionTolerance = 0.1f;

    public float SelectedYOffset
    {
        get => selectedYOffset;
        set => selectedYOffset = value;
    }

    public bool UseSelectedYOffset { get; set; } = true;

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

        if (GetComponent<SpecialCardTooltipTarget>() == null)
        {
            gameObject.AddComponent<SpecialCardTooltipTarget>();
        }
    }
    // クリックイベントで選択フラグをトグル
    private void ToggleSelect()
    {
        if (!MoveComplete)
        {
            return;
        }

        var selectionLimiter = GetComponentInParent<LimitedSelectableCardArea>();
        if (selectionLimiter != null)
        {
            selectionLimiter.TryToggleSelection(this);
            return;
        }

        if (!IsSelectable) return;
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
        runtimeState.SetCardData(cardData);
        if (cardImage != null && CardData != null)
        {
            cardImage.sprite = CardData.BackImage;
        }
    }

    // 即時に表裏を設定して表示を更新する（特殊札選択などで使用）
    public void ForceSetFaceUp(bool faceUp)
    {
        if (cardImage == null) cardImage = GetComponent<Image>();
        if (cardRect == null) cardRect = GetComponent<RectTransform>();

        IsFaceUp = faceUp;
        if (CardData != null && cardImage != null)
        {
            cardImage.sprite = IsFaceUp ? CardData.Image : CardData.BackImage;
            cardImage.SetNativeSize();
        }

        // 表示スケールを固定しておく（Turn時と同じ基準にする）
        if (cardRect != null)
        {
            cardRect.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        }

        // 内部状態を整える
        StopMotion(true);
        LastFaceUp = IsFaceUp;
        LastSelected = IsSelected;
    }

    // カードの数字を取得
    public Number GetCardNumber()
    {
        return runtimeState.Number;
    }
    // カードのスートを取得
    public Suit GetCardSuit()
    {
        return runtimeState.Suit;
    }

    // カードを移動・回転させる（アニメーション用）
    public void MoveAndTurnCard(float moveDuration = 1f)
    {
        StartMotion(MoveAndTurnToPosition(moveDuration));
    }

    public void MoveAlongArcToTarget(float moveDuration = 0.3f, float lift = 32f)
    {
        StartMotion(MoveAlongArcToTargetPosition(moveDuration, lift));
    }

    public void WaitAndMove(float waittime, float moveDuration = 1f)
    {
        StartMotion(WaitAndMoveToPosition(waittime, moveDuration));
    }
    
    private IEnumerator WaitAndMoveToPosition(float waittime, float moveDuration)
    {
        yield return new WaitForSeconds(waittime); // 少し待ってから動かす
        yield return MoveAndTurnToPosition(moveDuration);
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
        StartMotion(MoveAndTurnToPositionBySpeed(moveSpeed, turnSpeed));
    }

    public void WaitAndMoveBySpeed(float waittime, float moveSpeed = 100f, float turnSpeed = 100f)
    {
        StartMotion(WaitAndMoveToPositionBySpeed(waittime, moveSpeed, turnSpeed));
    }

    private IEnumerator WaitAndMoveToPositionBySpeed(float waittime, float moveSpeed, float turnSpeed = 100f)
    {
        yield return new WaitForSeconds(waittime);
        yield return MoveAndTurnToPositionBySpeed(moveSpeed, turnSpeed);
    }

    public bool NeedsAnimationForCurrentTarget()
    {
        if (cardRect == null)
        {
            cardRect = GetComponent<RectTransform>();
        }

        if (cardRect == null)
        {
            return true;
        }

        if (Vector2.Distance(cardRect.anchoredPosition, GetFinalAnchoredPosition()) > PositionTolerance)
        {
            return true;
        }

        if (IsFaceUp != LastFaceUp || IsSelected != LastSelected)
        {
            return true;
        }

        return HasWorldPositionChanged(GetTargetWorldPosition());
    }

    public void SnapToTargetPosition()
    {
        if (cardRect == null)
        {
            cardRect = GetComponent<RectTransform>();
        }

        StopMotion(true);
        if (cardRect != null)
        {
            cardRect.anchoredPosition = GetFinalAnchoredPosition();
        }

        LastWorldPosition = GetTargetWorldPosition();
        LastFaceUp = IsFaceUp;
        LastSelected = IsSelected;
    }

    private void StartMotion(IEnumerator motion)
    {
        StopMotion(false);
        MoveComplete = false;
        movementCoroutine = StartCoroutine(RunMotion(motion));
    }

    private IEnumerator RunMotion(IEnumerator motion)
    {
        yield return motion;
        SyncMotionState();
        MoveComplete = true;
        movementCoroutine = null;
    }

    private void StopMotion(bool markComplete)
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        MoveComplete = markComplete;
    }

    private Vector2 GetFinalAnchoredPosition()
    {
        return IsSelected && UseSelectedYOffset
            ? new Vector2(TargetPosition.x, TargetPosition.y + selectedYOffset)
            : TargetPosition;
    }

    private Vector2 GetTargetWorldPosition()
    {
        return transform.parent != null ? (Vector2)transform.parent.TransformPoint(TargetPosition) : TargetPosition;
    }

    private void SyncMotionState()
    {
        LastWorldPosition = GetTargetWorldPosition();
        LastFaceUp = IsFaceUp;
        LastSelected = IsSelected;
    }

    private bool HasWorldPositionChanged(Vector2 worldPosition)
    {
        return Vector2.Distance(worldPosition, LastWorldPosition) > PositionTolerance;
    }

    private IEnumerator MoveAndTurnToPositionBySpeed(float moveSpeed, float turnSpeed)
    {
        var WorldPosition = GetTargetWorldPosition();
        bool worldPositionChanged = HasWorldPositionChanged(WorldPosition);
        bool selectionChanged = IsSelected != LastSelected;
        bool faceChanged = IsFaceUp != LastFaceUp;
        bool needsMove = worldPositionChanged || selectionChanged;

        if (needsMove)
        {
            // Moveカードを動かす処理（速度指定）
            yield return MoveToPositionBySpeed(cardRect, moveSpeed);
            LastWorldPosition = GetTargetWorldPosition();
            LastSelected = IsSelected;
        }

        if (faceChanged)
        {
            // Turnカードを裏表替える処理（速度指定）
            yield return TurnToPositionBySpeed(cardRect, turnSpeed);
            LastFaceUp = IsFaceUp;
        }

        if (!needsMove && !faceChanged)
        {
            yield return new WaitForSeconds(0f);
        }
        MoveComplete = true;
    }

    private IEnumerator MoveToPositionBySpeed(RectTransform rectTransform, float moveSpeed)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 finalTarget = GetFinalAnchoredPosition();
        float distance = Vector2.Distance(startPos, finalTarget);
        if (distance <= PositionTolerance)
        {
            rectTransform.anchoredPosition = finalTarget;
            yield break;
        }
        // moveSpeed <= 0 の場合は挙動を変えず 1 秒のデフォルト時間にフォールバックする
        float moveDuration = moveSpeed > 0f ? distance / moveSpeed : 1f;
        yield return MoveToPosition(rectTransform, moveDuration);
    }

    private IEnumerator TurnToPositionBySpeed(RectTransform rectTransform, float turnSpeed)
    {
        if (turnSpeed > 0f)
        {
            float originalWidth = rectTransform.sizeDelta.x;
            // 幅を縮める（または戻す）時間は width / speed で計算されるため、合計の回転時間はその 2 倍とする
            float halfDuration = originalWidth / turnSpeed;
            float turnDuration = halfDuration * 2f;
            yield return TurnToPosition(rectTransform, turnDuration);
        }
        else
        {
            // フォールバックとしてデフォルトで 1 秒の回転時間を使用する
            yield return TurnToPosition(rectTransform, 1f);
        }
    }

    private IEnumerator MoveAndTurnToPosition( float moveDuration)
    {
        // カード移動のデバッグ用ログ（必要なら有効化）
        var WorldPosition = GetTargetWorldPosition();
        bool worldPositionChanged = HasWorldPositionChanged(WorldPosition);
        bool selectionChanged = IsSelected != LastSelected;
        bool faceChanged = IsFaceUp != LastFaceUp;
        bool needsMove = worldPositionChanged || selectionChanged;
        float stepDuration = needsMove && faceChanged ? moveDuration / 2f : moveDuration;

        if (needsMove)
        {
            // Moveカードを動かす処理
            yield return MoveToPosition(cardRect, stepDuration);
            LastWorldPosition = GetTargetWorldPosition();
            LastSelected = IsSelected;
        }

        if (faceChanged)
        {
            // Turnカードを裏表替える処理
            yield return TurnToPosition(cardRect, stepDuration);
            LastFaceUp = IsFaceUp;
        }

        if (!needsMove && !faceChanged)
        {
            yield return new WaitForSeconds(0f);
        }
        MoveComplete = true;
    }

    private IEnumerator MoveToPosition(RectTransform rectTransform, float moveDuration)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 finalTarget = GetFinalAnchoredPosition();
        if (moveDuration <= 0f || Vector2.Distance(startPos, finalTarget) <= PositionTolerance)
        {
            rectTransform.anchoredPosition = finalTarget;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, finalTarget, EaseOutCubic(t));
            
            yield return null;
        }

        rectTransform.anchoredPosition = finalTarget;
    }

    private IEnumerator MoveAlongArcToTargetPosition(float moveDuration, float lift)
    {
        if (cardRect == null)
        {
            cardRect = GetComponent<RectTransform>();
        }

        if (cardRect == null)
        {
            yield break;
        }

        Vector2 startPos = cardRect.anchoredPosition;
        Vector2 finalTarget = GetFinalAnchoredPosition();
        float distance = Vector2.Distance(startPos, finalTarget);
        if (moveDuration <= 0f || distance <= PositionTolerance)
        {
            cardRect.anchoredPosition = finalTarget;
            yield break;
        }

        float requestedLift = Mathf.Max(0f, lift);
        float effectiveLift = requestedLift <= 0f ? 0f : Mathf.Min(requestedLift, distance * 0.8f + 8f);
        Vector2 controlPos = (startPos + finalTarget) * 0.5f + Vector2.up * effectiveLift;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseInOutCubic(Mathf.Clamp01(elapsed / moveDuration));
            Vector2 startToControl = Vector2.Lerp(startPos, controlPos, t);
            Vector2 controlToEnd = Vector2.Lerp(controlPos, finalTarget, t);
            cardRect.anchoredPosition = Vector2.Lerp(startToControl, controlToEnd, t);
            yield return null;
        }

        cardRect.anchoredPosition = finalTarget;
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

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}
