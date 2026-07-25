using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Card))]
public sealed class SpecialCardTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private Card card;
    private bool isPointerOver;

    private void Awake()
    {
        card = GetComponent<Card>();
    }

    private void OnDisable()
    {
        if (isPointerOver)
        {
            isPointerOver = false;
            SpecialCardTooltip.Hide(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!TryGetTooltipText(out string title, out string body))
        {
            return;
        }

        isPointerOver = true;
        SpecialCardTooltip.Show(this, title, body, eventData.position);
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

    private bool TryGetTooltipText(out string title, out string body)
    {
        title = string.Empty;
        body = string.Empty;

        if (card == null)
        {
            card = GetComponent<Card>();
        }

        return card != null &&
               SpecialCardResolver.TryGetSpecialCardTooltip(card.CardData, out title, out body);
    }
}
