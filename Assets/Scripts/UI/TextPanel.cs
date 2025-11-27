using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// UIのテキスト表示パネルを管理するクラス
// 機能:
// - パネルのフェードイン/フェードアウト
// - メッセージの切り替え (Next)
// - TextAreaコンポーネントを使用してテキスト表示を制御
public class TextPanel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Panel GameObject which should contain or be paired with a CanvasGroup")]
    [SerializeField] private GameObject textPanel; // panel画像
    [SerializeField] private TextArea textArea; // テキストエリアコンポーネント

    // Animation parameters
    [Header("Behavior")]
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private bool startHidden = true;

    // Runtime
    private CanvasGroup panelCanvasGroup;
    private List<string> messages = new List<string>();
    private int currentIndex = 0;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        // Ensure CanvasGroup on panel for easy fading
        if (textPanel == null)
        {
            Debug.LogWarning("TextPanel: panel reference is not set.");
        }
        else
        {
            panelCanvasGroup = textPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = textPanel.AddComponent<CanvasGroup>();
            }
        }

        if (textArea == null)
        {
            Debug.LogWarning("TextPanel: TextArea component reference is not set.");
        }

        if (startHidden)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }
            if (textArea != null) textArea.Clear();
        }
    }

    // Public API
    public void SetMessages(IEnumerable<string> msgs)
    {
        messages = new List<string>(msgs ?? new string[0]);
        currentIndex = 0;
    }

    public void StartDisplay()
    {
        if (messages == null || messages.Count == 0)
        {
            Debug.Log("TextPanel: No messages to display.");
            return;
        }

        ShowPanel();
        StartTypingCurrent();
    }

    public void ShowPanel(bool instant = false)
    {
        if (panelCanvasGroup == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(panelCanvasGroup, panelCanvasGroup.alpha, 1f, instant ? 0f : fadeDuration, true));
    }

    public void HidePanel(bool instant = false)
    {
        if (panelCanvasGroup == null) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(panelCanvasGroup, panelCanvasGroup.alpha, 0f, instant ? 0f : fadeDuration, false));
    }

    public void Next()
    {
        if (textArea.IsTyping)
        {
            // If currently typing, finish immediately
            FinishTypingInstant();
            return;
        }

        // advance to next message
        currentIndex++;
        if (currentIndex >= messages.Count)
        {
            // reached end -> hide
            HidePanel();
        }
        else
        {
            StartTypingCurrent();
        }
    }

    public void SkipAll()
    {
        // Immediately display last message and stop typing, then hide
        if (messages == null || messages.Count == 0)
        {
            HidePanel(true);
            return;
        }

        textArea.StopTyping();
        currentIndex = messages.Count - 1;
        textArea.SetText(messages[currentIndex], true);
        HidePanel();
    }

    public bool IsTyping => textArea != null && textArea.IsTyping;
    public bool IsShowing => panelCanvasGroup != null && panelCanvasGroup.alpha > 0f;

    // Internal helpers
    void StartTypingCurrent()
    {
        if (textArea == null || messages == null || messages.Count == 0) return;
        string msg = messages[Mathf.Clamp(currentIndex, 0, messages.Count - 1)];
        textArea.SetText(msg);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration, bool makeInteractableWhenShown)
    {
        float elapsed = 0f;
        if (duration <= 0f)
        {
            cg.alpha = to;
            cg.interactable = to > 0.5f && makeInteractableWhenShown;
            cg.blocksRaycasts = to > 0.5f && makeInteractableWhenShown;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cg.alpha = to;
        cg.interactable = to > 0.5f && makeInteractableWhenShown;
        cg.blocksRaycasts = to > 0.5f && makeInteractableWhenShown;
    }

    void FinishTypingInstant()
    {
        if (messages == null || messages.Count == 0) return;
        string full = messages[Mathf.Clamp(currentIndex, 0, messages.Count - 1)];
        textArea.SetText(full, true);
    }
}

