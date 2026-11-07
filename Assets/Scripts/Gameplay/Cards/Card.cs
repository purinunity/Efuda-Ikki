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
    [SerializeField] private CardData authoredCardData;

    public CardRuntimeState RuntimeState => runtimeState;
    public CardData CardData => authoredCardData; // Scene/Prefabに保存されるカード情報
    private ICardVisualTheme visualTheme;
    public Sprite DisplayFaceSprite => visualTheme != null ? visualTheme.ResolveFace(CardData) : CardData != null ? CardData.Image : null;
    public Sprite DisplayBackSprite => visualTheme != null ? visualTheme.ResolveBack(CardData) : CardData != null ? CardData.BackImage : null;

    internal void SetVisualTheme(ICardVisualTheme theme) => visualTheme = theme;
    internal bool ClearVisualTheme(ICardVisualTheme theme)
    {
        if (!ReferenceEquals(visualTheme, theme)) return false;
        visualTheme = null;
        return true;
    }
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

    private CardMotion motion;
    private CardMotion Motion => motion ?? (motion = new CardMotion(this, complete => MoveComplete = complete, SyncMotionState));
    private Button selectionButton;
    private bool selectionListenerRegistered;
    private const float PositionTolerance = 0.1f;

    public float SelectedYOffset
    {
        get => selectedYOffset;
        set => selectedYOffset = value;
    }

    public bool UseSelectedYOffset { get; set; } = true;
    public float DisplayScale { get; set; } = 0.25f;

    // 初期化処理（Image, RectTransform取得）
    public void Initialize(float selectedYOffset = 20f)
    {
        runtimeState.SetCardData(authoredCardData);
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
        RegisterSelectionListener();

        // TooltipTarget is authored on Card.prefab. Runtime initialization must
        // not add components, so the complete card structure is visible in Edit Mode.
    }
    // クリックイベントで選択フラグをトグル
    private void ToggleSelect()
    {
        var selectionLimiter = GetComponentInParent<LimitedSelectableCardArea>();
        if (selectionLimiter != null)
        {
            selectionLimiter.TryToggleSelection(this);
            return;
        }

        if (!MoveComplete)
        {
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

    private void OnEnable()
    {
        RegisterSelectionListener();
    }

    private void OnDisable()
    {
        UnregisterSelectionListener();
        StopMotion(true);
    }

    private void OnDestroy()
    {
        UnregisterSelectionListener();
        StopMotion(true);
    }

    // カード情報をセットし、画像を更新
    public void SetCardData(CardData cardData)
    {
        authoredCardData = cardData;
        runtimeState.SetCardData(cardData);
        if (cardImage != null && CardData != null)
        {
            cardImage.sprite = DisplayBackSprite;
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
            cardImage.sprite = IsFaceUp
                ? DisplayFaceSprite
                : DisplayBackSprite;
            cardImage.SetNativeSize();
        }

        // 表示スケールを固定しておく（Turn時と同じ基準にする）
        if (cardRect != null)
        {
            cardRect.localScale = Vector3.one * DisplayScale;
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

        if (HasAnchoredPositionChanged())
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
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
        if (cardRect == null)
        {
            cardRect = GetComponent<RectTransform>();
        }

        StopMotion(true);
        if (cardRect != null)
        {
            if (cardImage != null && CardData != null)
            {
                cardImage.sprite = IsFaceUp
                    ? DisplayFaceSprite
                    : DisplayBackSprite;
                cardImage.SetNativeSize();
                cardRect.localScale = Vector3.one * DisplayScale;
            }

            cardRect.anchoredPosition = GetFinalAnchoredPosition();
        }

        LastWorldPosition = GetTargetWorldPosition();
        LastFaceUp = IsFaceUp;
        LastSelected = IsSelected;
    }

    private void StartMotion(IEnumerator routine)
    {
        Motion.Start(routine);
    }

    private void StopMotion(bool markComplete)
    {
        Motion.Stop(markComplete);
    }

    private void RegisterSelectionListener()
    {
        if (selectionButton == null)
        {
            selectionButton = GetComponent<Button>();
            selectionListenerRegistered = false;
        }

        if (!isActiveAndEnabled || selectionButton == null || selectionListenerRegistered)
        {
            return;
        }

        selectionButton.onClick.AddListener(ToggleSelect);
        selectionListenerRegistered = true;
    }

    private void UnregisterSelectionListener()
    {
        if (selectionButton != null && selectionListenerRegistered)
        {
            selectionButton.onClick.RemoveListener(ToggleSelect);
        }

        selectionListenerRegistered = false;
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

    private bool HasAnchoredPositionChanged()
    {
        if (cardRect == null)
        {
            cardRect = GetComponent<RectTransform>();
        }

        return cardRect == null ||
               Vector2.Distance(cardRect.anchoredPosition, GetFinalAnchoredPosition()) > PositionTolerance;
    }

    private IEnumerator MoveAndTurnToPositionBySpeed(float moveSpeed, float turnSpeed)
    {
        var WorldPosition = GetTargetWorldPosition();
        bool worldPositionChanged = HasWorldPositionChanged(WorldPosition);
        bool anchoredPositionChanged = HasAnchoredPositionChanged();
        bool selectionChanged = IsSelected != LastSelected;
        bool faceChanged = IsFaceUp != LastFaceUp;
        bool needsMove = worldPositionChanged || anchoredPositionChanged || selectionChanged;

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
        bool anchoredPositionChanged = HasAnchoredPositionChanged();
        bool selectionChanged = IsSelected != LastSelected;
        bool faceChanged = IsFaceUp != LastFaceUp;
        bool needsMove = worldPositionChanged || anchoredPositionChanged || selectionChanged;
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
        return CardMotion.Move(rectTransform, GetFinalAnchoredPosition(), moveDuration);
    }

    private IEnumerator MoveAlongArcToTargetPosition(float moveDuration, float lift)
    {
        if (cardRect == null) cardRect = GetComponent<RectTransform>();
        return CardMotion.MoveAlongArc(cardRect, GetFinalAnchoredPosition(), moveDuration, lift);
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
            cardImage.sprite = DisplayFaceSprite;
            cardImage.SetNativeSize();
            rectTransform.localScale = Vector3.one * DisplayScale;
        }
        else
        {
            cardImage.sprite = DisplayBackSprite;
            cardImage.SetNativeSize();
            rectTransform.localScale = Vector3.one * DisplayScale;
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

}
