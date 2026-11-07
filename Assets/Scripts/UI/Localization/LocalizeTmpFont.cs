using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// 選択中の言語に合わせて、TextMeshPro のフォントを自動で差し替える。
/// 文字を表示する TextMeshPro オブジェクトに付け、Font 欄で Asset Table のエントリを選ぶ。
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LocalizeTmpFont : MonoBehaviour
{
    [Serializable]
    public class LocalizedTmpFont : LocalizedAsset<TMP_FontAsset> { }

    [SerializeField] LocalizedTmpFont font = new LocalizedTmpFont();

    TMP_Text text;

    void Awake() => text = GetComponent<TMP_Text>();

    void OnEnable() => font.AssetChanged += OnFontChanged;

    void OnDisable() => font.AssetChanged -= OnFontChanged;

    void OnFontChanged(TMP_FontAsset value)
    {
        if (value != null) text.font = value;
    }
}
