using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 「− [■■□□□□□□□] ＋」の音量バー1本分。BGM用・SE用に1つずつ付ける。
/// </summary>
public class VolumeBar : MonoBehaviour
{
    public enum Target { BGM, SE }

    [SerializeField] Target target;
    [SerializeField] Button minusButton;
    [SerializeField] Button plusButton;
    [Tooltip("灰色バーの各マスの上に重ねた白い四角。左から順に9個")]
    [SerializeField] Image[] segments;
    [Tooltip("SEバーで鳴らす確認音(任意)。OutputをSEグループにしておく")]
    [SerializeField] AudioSource previewSound;

    int level;

    void Awake()
    {
        minusButton.onClick.AddListener(() => Change(-1));
        plusButton.onClick.AddListener(() => Change(+1));
    }

    void OnEnable()
    {
        // メニューを開くたびに現在値を反映
        var s = GameSettings.Instance;
        level = target == Target.BGM ? s.BgmLevel : s.SeLevel;
        Refresh();
    }

    void Change(int delta)
    {
        int next = Mathf.Clamp(level + delta, 0, GameSettings.MaxVolumeLevel);
        if (next == level) return;
        level = next;

        if (target == Target.BGM) GameSettings.Instance.SetBgmLevel(level);
        else GameSettings.Instance.SetSeLevel(level);

        Refresh();
        if (previewSound != null) previewSound.Play();
    }

    void Refresh()
    {
        // 左から level 個だけ白マスを表示
        for (int i = 0; i < segments.Length; i++)
            segments[i].enabled = i < level;
    }
}
