using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
 
/// <summary>
/// オプション画面の言語ボタン。クリックするたびに order の順で言語が切り替わる。
/// ボタンの画像(English / 日本語 …)の切り替えは、同じボタンに付けた LocalizeSprite が行う。
/// </summary>
public class LanguageSelector : MonoBehaviour
{
    [SerializeField] Button button;
    [Tooltip("切り替わる順番(Locale の言語コード)")]
    [SerializeField] string[] order = { "en", "ja", "zh-Hans", "ko" };
 
    bool busy;
 
    void Awake()
    {
        button.onClick.AddListener(() =>
        {
            if (!busy) StartCoroutine(Next());
        });
    }
 
    IEnumerator Next()
    {
        busy = true;
        yield return LocalizationSettings.InitializationOperation;
 
        int i = Array.IndexOf(order, LanguageManager.CurrentCode); // 見つからなければ -1 → 先頭へ
        string next = order[(i + 1) % order.Length];
        yield return LanguageManager.SetLanguage(next);
 
        busy = false;
    }
}