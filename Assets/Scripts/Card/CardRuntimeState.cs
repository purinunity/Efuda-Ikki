/// <summary>
/// Runtime gameplay state for a card instance.
/// Keep this separate from the Card MonoBehaviour so game logic can depend on
/// card state without owning UI movement or animation details.
/// </summary>
public class CardRuntimeState
{
    public CardData CardData { get; private set; }
    public bool IsFaceUp { get; set; }
    public bool IsSelectable { get; set; }
    public bool IsSelected { get; set; }

    public Number Number => CardData != null ? CardData.number : default;
    public Suit Suit => CardData != null ? CardData.suit : default;

    public CardRuntimeState(CardData cardData = null)
    {
        CardData = cardData;
    }

    public void SetCardData(CardData cardData)
    {
        CardData = cardData;
    }

    public void ResetVisibilityAndSelection()
    {
        IsFaceUp = false;
        IsSelectable = false;
        IsSelected = false;
    }
}
