using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

/// <summary>
/// 言語切り替えボタン(要 Localization パッケージ)。
/// 押すたびに 日本語 → English → … と順番に切り替わる。
/// 選択した言語は Localization の「Player Pref Locale Selector」が自動で保存・復元する。
/// </summary>
public class LanguageSelector : MonoBehaviour
{
    [SerializeField] Button button;  // ONボタンと同じ枠素材を流用
    [SerializeField] TMP_Text label;

    bool busy;

    void Awake() => button.onClick.AddListener(() => StartCoroutine(Next()));

    void OnEnable() => StartCoroutine(RefreshWhenReady());

    IEnumerator RefreshWhenReady()
    {
        yield return LocalizationSettings.InitializationOperation;
        Refresh();
    }

    IEnumerator Next()
    {
        if (busy) yield break;
        busy = true;

        yield return LocalizationSettings.InitializationOperation;
        var locales = LocalizationSettings.AvailableLocales.Locales;
        int i = locales.IndexOf(LocalizationSettings.SelectedLocale);
        LocalizationSettings.SelectedLocale = locales[(i + 1) % locales.Count];

        Refresh();
        busy = false;
    }

    void Refresh()
    {
        var locale = LocalizationSettings.SelectedLocale;
        if (locale == null) return;
        // 「日本語」「English」のように、その言語自身の表記で表示する
        label.text = locale.Identifier.CultureInfo?.NativeName ?? locale.LocaleName;
    }
}
