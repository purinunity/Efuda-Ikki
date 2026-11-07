using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 複数のカードオブジェクトを管理するクラス。
// カードはScene上に事前配置し、Play開始時には生成しない。
[DisallowMultipleComponent]
public class Cards : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Scene上に事前配置されたカード。実行時には生成しません。")]
    private List<Card> sceneCards = new List<Card>();

    public List<Card> cardList => sceneCards;
    public CardsData CardsData;
    [SerializeField] private Card cardPrefab; // EditorでSceneカードを配置するためのPrefab

    [SerializeField]
    [Tooltip("選択時のYオフセット")]
    private float selectedYOffset = 20f;

    public void Initialize()
    {
        if (sceneCards == null)
        {
            sceneCards = new List<Card>();
        }

        foreach (Card card in sceneCards)
        {
            if (card != null)
            {
                card.Initialize(selectedYOffset);
            }
        }

        Debug.Log("Cards initialized.");
    }

    public void Awake()
    {
        if (!TryGetAuthoringError(out string error))
        {
            Initialize();
            Debug.Log($"Scene-authored cards initialized. Count: {sceneCards.Count}");
            return;
        }

        Debug.LogError($"Cards: {error}", this);
        enabled = false;
    }

    /// <summary>
    /// Editor、Build、Playの全経路で使用するSceneカード構成の検証。
    /// nullは正常、文字列が返った場合は実行・ビルドを継続できない。
    /// </summary>
    public bool TryGetAuthoringError(out string error)
    {
        if (CardsData == null || CardsData.cards == null)
        {
            error = "CardsData is not assigned.";
            return true;
        }

        if (sceneCards == null || sceneCards.Count != CardsData.cards.Length)
        {
            int actualCount = sceneCards != null ? sceneCards.Count : 0;
            error =
                $"Scene card count is {actualCount}, but CardsData requires " +
                $"{CardsData.cards.Length}. Use 'Rebuild Scene Cards' in Edit Mode.";
            return true;
        }

        for (int index = 0; index < sceneCards.Count; index++)
        {
            Card card = sceneCards[index];
            CardData expected = CardsData.cards[index];
            if (expected == null)
            {
                error = $"CardsData entry {index + 1} is not assigned.";
                return true;
            }

            if (card == null || card.CardData != expected)
            {
                error =
                    $"Scene card {index + 1} does not match CardsData. " +
                    "Rebuild the Scene cards from the Cards inspector.";
                return true;
            }

            if (card.GetComponent<Image>() == null ||
                card.GetComponent<Button>() == null ||
                card.GetComponent<SpecialCardTooltipTarget>() == null)
            {
                error =
                    $"Scene card {index + 1} is missing a required UI component. " +
                    "Restore Card.prefab and rebuild the Scene cards.";
                return true;
            }
        }

        error = null;
        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (TryGetAuthoringError(out string error))
        {
            Debug.LogWarning(
                $"Cards: {name}: {error}",
                this);
        }
    }
#endif

    public Card GetCard(CardData cardData)
    {
        foreach (Card card in sceneCards)
        {
            if (card != null && card.CardData == cardData)
            {
                return card;
            }
        }

        Debug.LogWarning($"Card with data {cardData?.name ?? "null"} not found.");
        return null;
    }

    public List<Card> GetCards(List<CardData> cardDataList)
    {
        var cards = new List<Card>();
        if (cardDataList == null)
        {
            return cards;
        }

        foreach (CardData cardData in cardDataList)
        {
            Card card = GetCard(cardData);
            if (card != null)
            {
                cards.Add(card);
            }
        }

        return cards;
    }
}
