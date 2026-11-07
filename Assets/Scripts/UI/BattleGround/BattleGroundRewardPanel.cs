using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleGroundRewardPanel : MonoBehaviour
{
    public enum Choice { None, Herb, SpecialCard }

    [Header("Scene References")]
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI detail;
    [SerializeField] private Button herbButton;
    [SerializeField] private Button cardButton;
    [SerializeField] private TextMeshProUGUI herbButtonLabel;
    [SerializeField] private TextMeshProUGUI cardButtonLabel;

    private Choice choice;

    private void Awake()
    {
        ValidateSceneReferences();
        ConfigureChoiceButtons();
        gameObject.SetActive(false);
    }

    private void OnDestroy() => UnwireButtons();

    public IEnumerator Show(int life, int streak, bool canChooseCard, Action<Choice> selected)
    {
        choice = Choice.None;
        if (title != null) title.text = $"勝利報酬　{streak}人抜き";
        if (detail != null) detail.text = $"現在の持ち点：{life}\n次の対戦へ持ち込む報酬を選んでください";
        if (cardButton != null) cardButton.interactable = canChooseCard;
        gameObject.SetActive(true);
        while (choice == Choice.None) yield return null;
        gameObject.SetActive(false);
        selected?.Invoke(choice);
    }

    public IEnumerator RevealCard(CardData card)
    {
        choice = Choice.None;
        if (title != null) title.text = "特殊札を獲得";
        if (detail != null)
        {
            detail.text = card != null
                ? GetCardName(card)
                : "獲得できる特殊札がありません";
        }
        if (herbButton != null) herbButton.gameObject.SetActive(false);
        if (cardButton != null)
        {
            cardButton.gameObject.SetActive(true);
            cardButton.interactable = true;
        }
        if (cardButtonLabel != null) cardButtonLabel.text = "次の対戦へ";
        UnwireButtons();
        cardButton?.onClick.AddListener(ChooseSpecialCard);
        gameObject.SetActive(true);
        while (choice == Choice.None) yield return null;
        gameObject.SetActive(false);
        ConfigureChoiceButtons();
    }

    public void CancelDisplay()
    {
        choice = Choice.Herb;
        gameObject.SetActive(false);
    }

    private void ConfigureChoiceButtons()
    {
        if (herbButton != null) herbButton.gameObject.SetActive(true);
        if (cardButton != null) cardButton.gameObject.SetActive(true);
        if (herbButtonLabel != null) herbButtonLabel.text = "薬草（50点回復）";
        if (cardButtonLabel != null) cardButtonLabel.text = "特殊札を1枚獲得";
        UnwireButtons();
        herbButton?.onClick.AddListener(ChooseHerb);
        cardButton?.onClick.AddListener(ChooseSpecialCard);
    }

    private void UnwireButtons()
    {
        herbButton?.onClick.RemoveListener(ChooseHerb);
        cardButton?.onClick.RemoveListener(ChooseSpecialCard);
    }

    private void ChooseHerb() => choice = Choice.Herb;
    private void ChooseSpecialCard() => choice = Choice.SpecialCard;

    private void ValidateSceneReferences()
    {
        if (title == null || detail == null || herbButton == null || cardButton == null ||
            herbButtonLabel == null || cardButtonLabel == null)
        {
            Debug.LogError("BattleGroundRewardPanel: scene references are incomplete.", this);
        }
    }

    private static string GetCardName(CardData card)
    {
        return SpecialCardCatalog.TryGet(card, out SpecialCardCatalog.Entry entry)
            ? entry.DisplayName
            : card.name;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && !string.IsNullOrEmpty(gameObject.scene.path))
        {
            ValidateSceneReferences();
        }
    }
}
