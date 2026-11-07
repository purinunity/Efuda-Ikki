using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 起動時の言語処理。最初に読み込まれるシーンに置く。
/// ・初回起動:言語選択パネルを表示し、選ばれた言語を保存して次へ進む
/// ・2回目以降:保存された言語を適用して、すぐ次へ進む
/// </summary>
public class FirstLaunchLanguage : MonoBehaviour
{
    [Serializable]
    public class Choice
    {
        public Button button;
        [Tooltip("en / ja / zh-Hans / ko など")]
        public string localeCode;
    }

    [SerializeField] GameObject selectPanel;
    [SerializeField] Choice[] choices;
    [Tooltip("言語の決定後に読み込むシーン名。空欄ならパネルを閉じるだけ")]
    [SerializeField] string nextScene = "Title";

    bool decided;

    void Awake()
    {
        selectPanel.SetActive(false);
        foreach (var c in choices)
        {
            string code = c.localeCode;
            c.button.onClick.AddListener(() => StartCoroutine(Choose(code)));
        }
    }

    IEnumerator Start()
    {
        if (LanguageManager.HasChosen)
        {
            yield return LanguageManager.ApplySaved();
            Proceed();
        }
        else
        {
            yield return LocalizationSettings.InitializationOperation;
            selectPanel.SetActive(true);
        }
    }

    IEnumerator Choose(string code)
    {
        if (decided) yield break; // 連打対策
        decided = true;
        yield return LanguageManager.SetLanguage(code);
        Proceed();
    }

    void Proceed()
    {
        selectPanel.SetActive(false);
        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
    }
}
