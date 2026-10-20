using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

/// <summary>
/// 文字を焼き込んだ画像を、選択中の言語に合わせて差し替える。
/// Image に付ける。ボタンの場合は Highlighted にグレー版を設定すると、
/// マウスを乗せた・押したときの画像も言語に合わせて切り替わる。
/// </summary>
[RequireComponent(typeof(Image))]
public class LocalizeSprite : MonoBehaviour
{
    [SerializeField] LocalizedSprite normal = new LocalizedSprite();
    [Tooltip("ボタンの場合のみ設定(グレー版)。普通の画像なら空欄のまま")]
    [SerializeField] LocalizedSprite highlighted = new LocalizedSprite();
    [Tooltip("言語ごとに画像サイズが違う場合はオン")]
    [SerializeField] bool setNativeSize = false;

    Image image;
    Button button;

    void Awake()
    {
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (!normal.IsEmpty) normal.AssetChanged += OnNormalChanged;
        if (!highlighted.IsEmpty) highlighted.AssetChanged += OnHighlightedChanged;
    }

    void OnDisable()
    {
        if (!normal.IsEmpty) normal.AssetChanged -= OnNormalChanged;
        if (!highlighted.IsEmpty) highlighted.AssetChanged -= OnHighlightedChanged;
    }

    void OnNormalChanged(Sprite sprite)
    {
        if (sprite == null) return;
        image.sprite = sprite;
        if (setNativeSize) image.SetNativeSize();
    }

    void OnHighlightedChanged(Sprite sprite)
    {
        if (sprite == null || button == null) return;

        var state = button.spriteState;
        state.highlightedSprite = sprite;
        state.pressedSprite = sprite;
        state.selectedSprite = null; // クリック後に灰色のまま残らないようにする
        button.spriteState = state;

        // クリック直後(マウスが乗ったまま)でも表示をすぐ更新する
        if (image.overrideSprite != null)
            image.overrideSprite = sprite;
    }
}
