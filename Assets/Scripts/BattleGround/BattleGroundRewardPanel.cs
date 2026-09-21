using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleGroundRewardPanel : MonoBehaviour
{
    public enum Choice { None, Herb, SpecialCard }
    private Choice choice;
    private RectTransform panel;
    private TextMeshProUGUI title;
    private TextMeshProUGUI detail;
    private Button herbButton;
    private Button cardButton;
    private TMP_FontAsset font;

    public static BattleGroundRewardPanel GetOrCreate(UIManager uiManager, MonoBehaviour owner)
    {
        BattleGroundRewardPanel existing = FindObjectOfType<BattleGroundRewardPanel>(true);
        if (existing != null) return existing;
        Canvas canvas = uiManager != null && uiManager.deck != null
            ? uiManager.deck.GetComponentInParent<Canvas>()
            : uiManager != null ? uiManager.GetComponentInParent<Canvas>() : null;
        if (canvas == null && owner != null) canvas = owner.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        var root = new GameObject("BattleGroundRewardPanel", typeof(RectTransform), typeof(Image), typeof(BattleGroundRewardPanel));
        root.transform.SetParent(canvas.transform, false);
        return root.GetComponent<BattleGroundRewardPanel>();
    }

    public IEnumerator Show(int life, int streak, bool canChooseCard, Action<Choice> selected)
    {
        EnsureUi();
        choice = Choice.None;
        title.text = $"勝利報酬　{streak}連勝";
        detail.text = $"現在の持ち点：{life}\n次の対戦へ持ち込む報酬を選んでください";
        cardButton.interactable = canChooseCard;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        while (choice == Choice.None) yield return null;
        gameObject.SetActive(false);
        selected?.Invoke(choice);
    }

    public IEnumerator RevealCard(CardData card)
    {
        EnsureUi();
        choice = Choice.None;
        title.text = "特殊札を獲得";
        detail.text = card != null ? GetCardName(card) : "獲得できる特殊札がありません";
        herbButton.gameObject.SetActive(false);
        cardButton.gameObject.SetActive(true);
        cardButton.interactable = true;
        SetButtonLabel(cardButton, "次の対戦へ");
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => choice = Choice.SpecialCard);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        while (choice == Choice.None) yield return null;
        gameObject.SetActive(false);
        ConfigureChoiceButtons();
    }

    public void CancelDisplay()
    {
        choice = Choice.Herb;
        gameObject.SetActive(false);
    }

    private void EnsureUi()
    {
        if (panel != null) return;
        font = FindObjectOfType<TextMeshProUGUI>(true)?.font ?? TMP_Settings.defaultFontAsset;
        RuntimeUiFactory.Stretch(GetComponent<RectTransform>());
        GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
        panel = RuntimeUiFactory.CreateRect("Panel", transform);
        panel.anchorMin = new Vector2(0.25f, 0.25f);
        panel.anchorMax = new Vector2(0.75f, 0.75f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        RuntimeUiFactory.GetOrAdd<Image>(panel.gameObject).color = new Color(0.12f, 0.08f, 0.06f, 1f);
        title = MakeText("Title", 38, FontStyles.Bold, new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f));
        detail = MakeText("Detail", 27, FontStyles.Normal, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.74f));
        herbButton = MakeButton("HerbButton", "薬草（50点回復）", new Vector2(0.08f, 0.1f), new Vector2(0.47f, 0.34f));
        cardButton = MakeButton("CardButton", "特殊札を1枚獲得", new Vector2(0.53f, 0.1f), new Vector2(0.92f, 0.34f));
        ConfigureChoiceButtons();
        gameObject.SetActive(false);
    }

    private void ConfigureChoiceButtons()
    {
        herbButton.gameObject.SetActive(true);
        cardButton.gameObject.SetActive(true);
        SetButtonLabel(herbButton, "薬草（50点回復）");
        SetButtonLabel(cardButton, "特殊札を1枚獲得");
        herbButton.onClick.RemoveAllListeners();
        cardButton.onClick.RemoveAllListeners();
        herbButton.onClick.AddListener(() => choice = Choice.Herb);
        cardButton.onClick.AddListener(() => choice = Choice.SpecialCard);
    }

    private TextMeshProUGUI MakeText(string name, int size, FontStyles style, Vector2 min, Vector2 max)
    {
        TextMeshProUGUI text = RuntimeUiFactory.CreateText(name, panel, font, size, style, TextAlignmentOptions.Center, Color.white);
        text.rectTransform.anchorMin = min; text.rectTransform.anchorMax = max;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return text;
    }

    private Button MakeButton(string name, string label, Vector2 min, Vector2 max)
    {
        Button button = RuntimeUiFactory.CreateButton(name, panel);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        button.GetComponent<Image>().color = new Color(0.78f, 0.58f, 0.2f, 1f);
        TextMeshProUGUI text = RuntimeUiFactory.CreateText("Label", rect, font, 25, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
        RuntimeUiFactory.Stretch(text.rectTransform); text.text = label;
        return button;
    }

    private static void SetButtonLabel(Button button, string value)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.text = value;
    }

    private static string GetCardName(CardData card)
    {
        return SpecialCardCatalog.TryGet(card, out SpecialCardCatalog.Entry entry) ? entry.DisplayName : card.name;
    }
}
