using System.Collections;
using TMPro;
using UnityEngine;

// テキストの表示とタイプライター演出を管理するクラス
public class TextArea : MonoBehaviour
{
    [SerializeField] private TextMeshPro textComponent; // テキストコンポーネント
    [SerializeField] private float typingSpeed = 0.03f; // 1文字あたりの表示時間

    private Coroutine typingCoroutine;
    private bool isTyping = false;

    void Awake()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshPro>();
            if (textComponent == null)
            {
                Debug.LogWarning("TextArea: TextMeshPro component is required but not found.");
            }
        }
    }

    public void Clear()
    {
        if (textComponent != null)
        {
            textComponent.text = string.Empty;
        }
    }

    public void SetText(string text, bool instant = false)
    {
        if (instant)
        {
            StopTyping();
            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }
        else
        {
            StartTyping(text);
        }
    }

    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
    }

    private void StartTyping(string text)
    {
        StopTyping();
        if (textComponent != null)
        {
            typingCoroutine = StartCoroutine(TypeText(text));
        }
    }

    private IEnumerator TypeText(string fullText)
    {
        isTyping = true;
        textComponent.text = string.Empty;

        if (string.IsNullOrEmpty(fullText))
        {
            isTyping = false;
            yield break;
        }

        for (int i = 0; i < fullText.Length; i++)
        {
            textComponent.text += fullText[i];
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    public bool IsTyping => isTyping;
}
