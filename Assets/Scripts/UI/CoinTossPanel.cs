using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-authored coin toss overlay. Only the coin animation changes its transform.</summary>
public sealed class CoinTossPanel : MonoBehaviour
{
    private const float SpinDuration = 2.1f;
    private const float ResultDuration = 0.9f;

    [Header("Scene References")]
    [SerializeField] private Image overlayImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image coinImage;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Assets")]
    [SerializeField] private Sprite parentSprite;
    [SerializeField] private Sprite childSprite;
    [SerializeField] private Sprite backgroundSprite;

    private bool cancelled;
    private bool initialized;
    private Vector2 baseCoinPosition;
    private Quaternion baseCoinRotation;
    private Vector3 baseCoinScale;

    public IEnumerator Show(int parentPlayerIndex)
    {
        Initialize();
        cancelled = false;
        gameObject.SetActive(true);
        if (coinImage == null || resultText == null) yield break;
        resultText.text = string.Empty;

        float elapsed = 0f;
        while (!cancelled && elapsed < SpinDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / SpinDuration);
            float turns = Mathf.Lerp(0f, 7f, 1f - Mathf.Pow(1f - progress, 1.7f));
            float angle = turns * 360f;
            coinImage.sprite = Mathf.Cos(angle * Mathf.Deg2Rad) >= 0f ? parentSprite : childSprite;
            float roll = Mathf.Sin(progress * Mathf.PI) * 24f;
            coinImage.rectTransform.localRotation =
                baseCoinRotation * Quaternion.Euler(0f, angle, roll);
            float bounce = Mathf.Sin(progress * Mathf.PI) * 48f;
            coinImage.rectTransform.anchoredPosition =
                baseCoinPosition + new Vector2(0f, bounce);
            yield return null;
        }

        if (!cancelled)
        {
            coinImage.sprite = parentPlayerIndex == 0 ? parentSprite : childSprite;
            RestoreCoinTransform();
            resultText.text = parentPlayerIndex == 0 ? "あなたが親" : "CPUが親";
            float resultElapsed = 0f;
            while (!cancelled && resultElapsed < ResultDuration)
            {
                resultElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        gameObject.SetActive(false);
    }

    public void CancelDisplay()
    {
        cancelled = true;
        RestoreCoinTransform();
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    private void Initialize()
    {
        if (initialized) return;
        if (parentSprite == null) parentSprite = Resources.Load<Sprite>("CoinToss/parent_coin");
        if (childSprite == null) childSprite = Resources.Load<Sprite>("CoinToss/child_coin");
        if (backgroundSprite == null) backgroundSprite = Resources.Load<Sprite>("CoinToss/coin_background");
        if (overlayImage == null || frameImage == null || coinImage == null || resultText == null)
        {
            Debug.LogError("CoinTossPanel: scene references are incomplete.", this);
            return;
        }

        initialized = true;
        frameImage.sprite = backgroundSprite;
        coinImage.sprite = parentSprite;
        baseCoinPosition = coinImage.rectTransform.anchoredPosition;
        baseCoinRotation = coinImage.rectTransform.localRotation;
        baseCoinScale = coinImage.rectTransform.localScale;
    }

    private void RestoreCoinTransform()
    {
        if (coinImage == null) return;
        coinImage.rectTransform.anchoredPosition = baseCoinPosition;
        coinImage.rectTransform.localRotation = baseCoinRotation;
        coinImage.rectTransform.localScale = baseCoinScale;
    }

#if UNITY_EDITOR
    public void ApplyEditorPreview()
    {
        Initialize();
        if (frameImage != null) frameImage.sprite = backgroundSprite;
        if (coinImage != null) coinImage.sprite = parentSprite;
        if (resultText != null) resultText.text = "あなたが親";
        RestoreCoinTransform();
    }
#endif

    private void OnValidate()
    {
        if (!Application.isPlaying && !string.IsNullOrEmpty(gameObject.scene.path) &&
            (overlayImage == null || frameImage == null || coinImage == null || resultText == null))
        {
            Debug.LogWarning("CoinTossPanel: assign all scene-authored references.", this);
        }
    }
}
