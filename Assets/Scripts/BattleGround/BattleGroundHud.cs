using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleGroundHud : MonoBehaviour
{
    private GameObject confirmRoot;
    private Button surrenderButton;
    private Action surrender;
    private TMP_FontAsset font;

    public static BattleGroundHud GetOrCreate(UIManager uiManager, MonoBehaviour owner)
    {
        BattleGroundHud existing = FindObjectOfType<BattleGroundHud>(true);
        if (existing != null) return existing;
        Canvas canvas = uiManager != null && uiManager.deck != null
            ? uiManager.deck.GetComponentInParent<Canvas>()
            : uiManager != null ? uiManager.GetComponentInParent<Canvas>() : null;
        if (canvas == null && owner != null) canvas = owner.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        var root = new GameObject("BattleGroundHud", typeof(RectTransform), typeof(BattleGroundHud));
        root.transform.SetParent(canvas.transform, false);
        RuntimeUiFactory.Stretch(root.GetComponent<RectTransform>());
        return root.GetComponent<BattleGroundHud>();
    }

    public void Configure(bool active, Action onSurrender)
    {
        EnsureUi();
        surrender = onSurrender;
        gameObject.SetActive(active);
        confirmRoot.SetActive(false);
    }

    private void Update()
    {
        GameModeData mode = GameModeManager.GetGameModeData();
        if (gameObject.activeSelf && (mode == null || mode.Mode != GameModeData.GameMode.BattleGroundMode))
        {
            gameObject.SetActive(false);
        }
    }

    private void EnsureUi()
    {
        if (surrenderButton != null) return;
        font = FindObjectOfType<TextMeshProUGUI>(true)?.font ?? TMP_Settings.defaultFontAsset;
        surrenderButton = MakeButton(
            "SurrenderButton",
            transform,
            "降参",
            new Vector2(0.006f, 0.93f),
            new Vector2(0.055f, 0.975f));
        TextMeshProUGUI surrenderLabel = surrenderButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (surrenderLabel != null)
        {
            surrenderLabel.fontSize = 19f;
            surrenderLabel.enableAutoSizing = true;
            surrenderLabel.fontSizeMin = 13f;
            surrenderLabel.fontSizeMax = 19f;
        }
        surrenderButton.onClick.AddListener(() => confirmRoot.SetActive(true));

        confirmRoot = new GameObject("SurrenderConfirmation", typeof(RectTransform), typeof(Image));
        confirmRoot.transform.SetParent(transform, false);
        RuntimeUiFactory.Stretch(confirmRoot.GetComponent<RectTransform>());
        confirmRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform panel = RuntimeUiFactory.CreateRect("Panel", confirmRoot.transform);
        panel.anchorMin = new Vector2(0.3f, 0.34f); panel.anchorMax = new Vector2(0.7f, 0.66f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        RuntimeUiFactory.GetOrAdd<Image>(panel.gameObject).color = new Color(0.12f, 0.08f, 0.06f, 1f);
        TextMeshProUGUI question = RuntimeUiFactory.CreateText("Question", panel, font, 30, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        question.text = "バトルグラウンドを降参しますか？";
        question.rectTransform.anchorMin = new Vector2(0.05f, 0.5f); question.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);
        question.rectTransform.offsetMin = question.rectTransform.offsetMax = Vector2.zero;
        Button yes = MakeButton("Confirm", panel, "降参する", new Vector2(0.08f, 0.1f), new Vector2(0.47f, 0.42f));
        Button no = MakeButton("Cancel", panel, "戻る", new Vector2(0.53f, 0.1f), new Vector2(0.92f, 0.42f));
        yes.onClick.AddListener(() => { confirmRoot.SetActive(false); surrender?.Invoke(); });
        no.onClick.AddListener(() => confirmRoot.SetActive(false));
        confirmRoot.SetActive(false);
    }

    private Button MakeButton(string name, Transform parent, string label, Vector2 min, Vector2 max)
    {
        Button button = RuntimeUiFactory.CreateButton(name, parent);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        button.GetComponent<Image>().color = new Color(0.78f, 0.58f, 0.2f, 1f);
        TextMeshProUGUI text = RuntimeUiFactory.CreateText("Label", rect, font, 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
        RuntimeUiFactory.Stretch(text.rectTransform); text.text = label;
        return button;
    }
}
