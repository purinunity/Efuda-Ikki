using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// 文字を焼き込んだ画像を、選択中の言語に合わせて差し替える。
/// Image に付ける。ボタンの場合は Highlighted にグレー版を設定すると、
/// マウスを乗せた・押したときの画像も言語に合わせて切り替わる。
/// 途中で解放されるボタンは Locked にロック中の画像を設定し、
/// 他のスクリプトから SetLocked(true / false) を呼んで状態を切り替える。
/// </summary>
[RequireComponent(typeof(Image))]
public class LocalizeSprite : MonoBehaviour
{
    [SerializeField] LocalizedSprite normal = new LocalizedSprite();
    [Tooltip("ボタンの場合のみ設定(グレー版)。普通の画像なら空欄のまま")]
    [SerializeField] LocalizedSprite highlighted = new LocalizedSprite();
    [Tooltip("途中で解放されるボタンのみ設定(ロック中の画像)。不要なら空欄のまま")]
    [SerializeField] LocalizedSprite locked = new LocalizedSprite();
    [Tooltip("言語ごとに画像サイズが違う場合はオン")]
    [SerializeField] bool setNativeSize = false;

    Image image;
    Button button;

    // 読み込み済みの、現在の言語の画像
    Sprite normalSprite;
    Sprite highlightedSprite;
    Sprite lockedSprite;

    bool isLocked;

    public bool IsLocked => isLocked;

    void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (!normal.IsEmpty) normal.AssetChanged += OnNormalChanged;
        if (!highlighted.IsEmpty) highlighted.AssetChanged += OnHighlightedChanged;
        if (!locked.IsEmpty) locked.AssetChanged += OnLockedChanged;
    }

    void OnDisable()
    {
        if (!normal.IsEmpty) normal.AssetChanged -= OnNormalChanged;
        if (!highlighted.IsEmpty) highlighted.AssetChanged -= OnHighlightedChanged;
        if (!locked.IsEmpty) locked.AssetChanged -= OnLockedChanged;
    }

    /// <summary>ロック中かどうかを切り替える(解放状態を管理するスクリプトから呼ぶ)</summary>
    public void SetLocked(bool value)
    {
        isLocked = value;
        Apply();
    }

    void OnNormalChanged(Sprite sprite)
    {
        normalSprite = sprite;
        Apply();
    }

    void OnHighlightedChanged(Sprite sprite)
    {
        highlightedSprite = sprite;
        Apply();
    }

    void OnLockedChanged(Sprite sprite)
    {
        lockedSprite = sprite;
        Apply();
    }

    /// <summary>現在の言語とロック状態に合わせて、表示とボタンの設定を整える</summary>
    void Apply()
    {
        if (image == null) return; // Awake 前に呼ばれた場合は、画像の読み込み完了時に反映される

        bool showLocked = isLocked && lockedSprite != null;

        Sprite main = showLocked ? lockedSprite : normalSprite;
        if (main != null)
        {
            image.sprite = main;
            if (setNativeSize) image.SetNativeSize();
        }

        if (button == null) return;

        if (showLocked)
        {
            // ロック中の画像は、マウスを乗せても変わらないようにする
            button.transition = Selectable.Transition.None;
            image.overrideSprite = null;
            return;
        }

        if (highlightedSprite != null)
        {
            button.transition = Selectable.Transition.SpriteSwap;

            var state = button.spriteState;
            state.highlightedSprite = highlightedSprite;
            state.pressedSprite = highlightedSprite;
            state.selectedSprite = null; // クリック後に灰色のまま残らないようにする
            button.spriteState = state;

            // クリック直後(マウスが乗ったまま)でも表示をすぐ更新する
            if (image.overrideSprite != null)
                image.overrideSprite = highlightedSprite;
        }
    }
}