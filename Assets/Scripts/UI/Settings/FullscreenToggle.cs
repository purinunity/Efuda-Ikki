using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// フルスクリーンの ON / OFF ボタン。
/// 「ON」「OFF」が焼き込まれた画像を、状態に合わせて差し替える。
/// </summary>
public class FullscreenToggle : MonoBehaviour
{
    [SerializeField] Button button;

    [Header("ON の画像")]
    [SerializeField] Sprite onNormal;       // 通常時
    [SerializeField] Sprite onHighlighted;  // マウスを乗せた・押した・選択中(グレー版)

    [Header("OFF の画像")]
    [SerializeField] Sprite offNormal;
    [SerializeField] Sprite offHighlighted;

    Image image;

    void Awake()
    {
        image = button.image;
        button.onClick.AddListener(() =>
        {
            GameSettings.Instance.SetFullscreen(!GameSettings.Instance.IsFullscreen);
            Refresh();
        });
    }

    void OnEnable() => Refresh();

    void Refresh()
    {
        bool on = GameSettings.Instance.IsFullscreen;
        Sprite normal = on ? onNormal : offNormal;
        Sprite highlighted = on ? onHighlighted : offHighlighted;

        // 通常時の画像
        image.sprite = normal;

        // Sprite Swap で使う画像も ON / OFF に合わせて入れ替える
        var state = button.spriteState;
        state.highlightedSprite = highlighted;
        state.pressedSprite = highlighted;
        state.selectedSprite = null; // クリック後の「選択中」は通常の見た目にする(マウスを離したらグレーが消える)
        button.spriteState = state;

        // クリック直後はボタンがグレー表示のままなので、表示中の画像もすぐ更新する
        // (通常状態では overrideSprite は null になっている)
        if (image.overrideSprite != null)
            image.overrideSprite = highlighted;
    }
}
