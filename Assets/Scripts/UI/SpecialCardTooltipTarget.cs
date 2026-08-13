using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Card))]
public sealed class SpecialCardTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private static ShowdownCutInAssetSet tooltipAssetSet;
    private Card card;
    private bool isPointerOver;
    private bool tooltipEnabled = true;

    private void Awake()
    {
        card = GetComponent<Card>();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    public void SetTooltipEnabled(bool enabled)
    {
        tooltipEnabled = enabled;
        if (!tooltipEnabled)
        {
            HideTooltip();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!TryGetTooltipSprite(out Sprite tooltipSprite))
        {
            return;
        }

        isPointerOver = true;
        SpecialCardTooltip.Show(this, tooltipSprite, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isPointerOver)
        {
            return;
        }

        SpecialCardTooltip.SetPosition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPointerOver)
        {
            return;
        }

        isPointerOver = false;
        SpecialCardTooltip.Hide(this);
    }

    private bool TryGetTooltipSprite(out Sprite tooltipSprite)
    {
        tooltipSprite = null;

        if (!tooltipEnabled)
        {
            return false;
        }

        if (card == null)
        {
            card = GetComponent<Card>();
        }

        if (card == null ||
            !SpecialCardResolver.TryGetSpecialCardId(card.CardData, out SpecialCardResolver.SpecialCardId id))
        {
            return false;
        }

        if (tooltipAssetSet == null)
        {
            tooltipAssetSet = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        }

        tooltipSprite = tooltipAssetSet != null
            ? tooltipAssetSet.GetSpecialCardTooltipSprite(id)
            : null;
        return tooltipSprite != null;
    }

    private void HideTooltip()
    {
        if (!isPointerOver)
        {
            return;
        }

        isPointerOver = false;
        SpecialCardTooltip.Hide(this);
    }
}
