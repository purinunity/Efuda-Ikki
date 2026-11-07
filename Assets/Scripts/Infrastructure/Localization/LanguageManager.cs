using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// 言語の切り替えと保存をまとめたクラス(シーンに置く必要はない)。
/// 言語コードは Locale アセットの Inspector に表示されるもの(en / ja / zh-Hans / ko など)を使う。
/// </summary>
public static class LanguageManager
{
    const string Key = "Settings.Language";

    /// <summary>一度でも言語を選んだことがあるか(初回起動の判定に使う)</summary>
    public static bool HasChosen => PlayerPrefs.HasKey(Key);

    /// <summary>保存されている言語コード</summary>
    public static string SavedCode => PlayerPrefs.GetString(Key, "");

    /// <summary>現在選択中の言語コード</summary>
    public static string CurrentCode =>
        LocalizationSettings.SelectedLocale != null
            ? LocalizationSettings.SelectedLocale.Identifier.Code
            : "";

    /// <summary>言語を切り替えて保存する(StartCoroutine で呼ぶ)</summary>
    public static IEnumerator SetLanguage(string code)
    {
        yield return LocalizationSettings.InitializationOperation;

        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale == null)
        {
            Debug.LogWarning($"言語コード '{code}' が Available Locales に見つかりません");
            yield break;
        }

        LocalizationSettings.SelectedLocale = locale;
        PlayerPrefs.SetString(Key, code);
        PlayerPrefs.Save();
    }

    /// <summary>保存されている言語を適用する(起動時に呼ぶ)</summary>
    public static IEnumerator ApplySaved()
    {
        if (!HasChosen) yield break;
        yield return SetLanguage(SavedCode);
    }
}
