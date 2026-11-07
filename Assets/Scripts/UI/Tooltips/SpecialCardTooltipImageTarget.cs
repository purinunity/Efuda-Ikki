using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Tooltip input for authored images that display card data without owning a Card.</summary>
public sealed class SpecialCardTooltipImageTarget : MonoBehaviour,
    IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
{
    [SerializeField] private ShowdownCutInAssetSet visualAssets;
    private CardData cardData;
    private bool isPointerOver;

    public void SetCardData(CardData data)
    {
        Hide();
        cardData = data;
    }

    public void Clear() => SetCardData(null);

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (cardData == null || visualAssets == null || SpecialCardResolver.IsNoUseSpecialCard(cardData)) return;
        Sprite sprite = visualAssets.GetSpecialCardTooltipSprite(cardData);
        if (sprite == null) return;
        isPointerOver = true;
        SpecialCardTooltip.Show(this, sprite, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (isPointerOver) SpecialCardTooltip.SetPosition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData) => Hide();
    private void OnDisable() => Hide();

    private void Hide()
    {
        if (!isPointerOver) return;
        isPointerOver = false;
        SpecialCardTooltip.Hide(this);
    }
}
