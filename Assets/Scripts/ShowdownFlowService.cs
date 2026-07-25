using System.Collections.Generic;

public sealed class PreparedShowdown
{
    public SpecialCardResolver.ShowdownResult Result { get; }
    public IReadOnlyList<Card> SpecialCardsToConsume { get; }

    public PreparedShowdown(
        SpecialCardResolver.ShowdownResult result,
        IReadOnlyList<Card> specialCardsToConsume)
    {
        Result = result;
        SpecialCardsToConsume = specialCardsToConsume;
    }
}

public sealed class ShowdownFlowService
{
    private readonly GameState gameState;
    private readonly CPUController cpuController;

    public ShowdownFlowService(GameState gameState, CPUController cpuController)
    {
        this.gameState = gameState;
        this.cpuController = cpuController;
    }

    public PreparedShowdown PrepareShowdown()
    {
        OpenAllHands();

        if (cpuController != null)
        {
            cpuController.SelectSpecialCard(gameState);
        }

        List<Card> specialCardsToConsume = CollectSelectedUsableSpecialCards();
        SpecialCardResolver.ShowdownResult result = SpecialCardResolver.Resolve(gameState);
        return new PreparedShowdown(result, specialCardsToConsume);
    }

    public void MarkSpecialCardsUsed(IReadOnlyList<Card> usedCards)
    {
        if (usedCards == null || usedCards.Count == 0 || gameState?.PlayerStates == null)
        {
            return;
        }

        foreach (PlayerState playerState in gameState.PlayerStates)
        {
            if (playerState?.SpecialCards == null)
            {
                continue;
            }

            foreach (Card card in usedCards)
            {
                if (card != null && playerState.SpecialCards.Contains(card))
                {
                    playerState.MarkSpecialCardUsed(card);
                }
            }
        }
    }

    public void ApplyDamage(SpecialCardResolver.ShowdownResult showdownResult)
    {
        if (showdownResult == null || showdownResult.IsDraw || gameState?.PlayerStates == null)
        {
            return;
        }

        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (i != showdownResult.WinnerIndex)
            {
                gameState.PlayerStates[i].decreaseLifePoints(showdownResult.Damage);
            }
        }
    }

    public bool CheckGameOver()
    {
        if (gameState?.PlayerStates == null)
        {
            return false;
        }

        for (int i = 0; i < gameState.playerCount; i++)
        {
            if (gameState.PlayerStates[i].LifePoints <= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void OpenAllHands()
    {
        if (gameState == null)
        {
            return;
        }

        for (int i = 0; i < gameState.playerCount; i++)
        {
            gameState.OpenPlayerHands(i);
        }
    }

    private List<Card> CollectSelectedUsableSpecialCards()
    {
        List<Card> selectedCards = new List<Card>();
        if (gameState == null || gameState.PlayerStates == null)
        {
            return selectedCards;
        }

        foreach (PlayerState playerState in gameState.PlayerStates)
        {
            if (playerState?.SpecialCards == null)
            {
                continue;
            }

            selectedCards.AddRange(CardSelectionUtility.GetSelectedCards(
                playerState.SpecialCards,
                card => !playerState.IsSpecialCardUsed(card) &&
                        !SpecialCardResolver.IsNoUseSpecialCard(card.CardData)));
        }

        return selectedCards;
    }
}
