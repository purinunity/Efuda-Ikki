using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 設定値の保持・保存・適用を一手に担うクラス。
/// 最初に読み込まれるシーン(タイトル等)に1つだけ置く。シーンをまたいで生き続ける。
/// </summary>
[DefaultExecutionOrder(-100)] // 他のスクリプトより先に Awake を実行し、Instance を必ず用意しておく
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    /// <summary>音量バーのマス数(素材に合わせて9)</summary>
    public const int MaxVolumeLevel = 9;

    [Header("Audio")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] string bgmParam = "BGMVolume";   // AudioMixerでExposeしたパラメータ名
    [SerializeField] string seParam = "SEVolume";
    [SerializeField, Range(0, MaxVolumeLevel)] int defaultBgmLevel = 6;
    [SerializeField, Range(0, MaxVolumeLevel)] int defaultSeLevel = 6;

    [Header("Window")]
    [SerializeField] bool defaultFullscreen = true;
    [SerializeField] Vector2Int windowedSize = new Vector2Int(1280, 720);

    const string KeyBgm = "Settings.BgmLevel";
    const string KeySe = "Settings.SeLevel";
    const string KeyFullscreen = "Settings.Fullscreen";

    public int BgmLevel { get; private set; }
    public int SeLevel { get; private set; }
    public bool IsFullscreen { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BgmLevel = PlayerPrefs.GetInt(KeyBgm, defaultBgmLevel);
        SeLevel = PlayerPrefs.GetInt(KeySe, defaultSeLevel);
        IsFullscreen = PlayerPrefs.GetInt(KeyFullscreen, defaultFullscreen ? 1 : 0) == 1;
    }

    void Start()
    {
        // AudioMixer.SetFloat は Awake 中だと反映されないことがあるので Start で適用する
        ApplyVolume(bgmParam, BgmLevel);
        ApplyVolume(seParam, SeLevel);
        ApplyFullscreen(IsFullscreen);
    }

    // ---------- 音量 ----------

    public void SetBgmLevel(int level)
    {
        BgmLevel = Mathf.Clamp(level, 0, MaxVolumeLevel);
        ApplyVolume(bgmParam, BgmLevel);
        PlayerPrefs.SetInt(KeyBgm, BgmLevel);
        PlayerPrefs.Save();
    }

    public void SetSeLevel(int level)
    {
        SeLevel = Mathf.Clamp(level, 0, MaxVolumeLevel);
        ApplyVolume(seParam, SeLevel);
        PlayerPrefs.SetInt(KeySe, SeLevel);
        PlayerPrefs.Save();
    }

    void ApplyVolume(string param, int level)
    {
        // 段階(0〜9)をデシベルに変換。人の耳は対数的なので線形で入れると変化が偏る
        float db = level <= 0 ? -80f : Mathf.Log10((float)level / MaxVolumeLevel) * 20f;
        mixer.SetFloat(param, db);
    }

    // ---------- フルスクリーン ----------

    public void SetFullscreen(bool on)
    {
        IsFullscreen = on;
        ApplyFullscreen(on);
        PlayerPrefs.SetInt(KeyFullscreen, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplyFullscreen(bool on)
    {
        if (on)
        {
            var r = Screen.currentResolution; // モニタの解像度
            Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(windowedSize.x, windowedSize.y, FullScreenMode.Windowed);
        }
    }
}
