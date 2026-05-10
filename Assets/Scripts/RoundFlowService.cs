using System;
using System.Collections;
using UnityEngine;

public sealed class RoundFlowService
{
    private readonly GameState gameState;
    private readonly Cards allCards;
    private readonly Controller[] controllers;
    private readonly Func<float, IEnumerator> waitForUi;
    private readonly Func<bool> isGameOver;

    public RoundFlowService(
        GameState gameState,
        Cards allCards,
        Controller[] controllers,
        Func<float, IEnumerator> waitForUi,
        Func<bool> isGameOver)
    {
        this.gameState = gameState;
        this.allCards = allCards;
        this.controllers = controllers;
        this.waitForUi = waitForUi;
        this.isGameOver = isGameOver;
    }

    public IEnumerator InitializeRound()
    {
        foreach (Card card in allCards.cardList)
        {
            gameState.AddCardToDeck(card);
        }

        gameState.CardReset();
        gameState.ShuffleDeck();
        yield return WaitForUi(3f);

        for (int i = 0; i < gameState.playerCount; i++)
        {
            gameState.AddCardToPlayerHand();
            yield return WaitForUi(3f);
            gameState.NextTurn();
        }

        gameState.AddCardToCommon();
        yield return WaitForUi(3f);

        gameState.OpenPlayerHands(0);
        gameState.OpenCommonCards();
        yield return WaitForUi(3f);

        Debug.Log("Game initialized.");
    }

    public IEnumerator RunExchangeRound()
    {
        for (int i = 0; i < gameState.maxHandTrashTurn; i++)
        {
            for (int j = 0; j < gameState.playerCount; j++)
            {
                if (IsGameOver())
                {
                    yield break;
                }

                Controller controller = controllers[gameState.CurrentPlayerIndex];
                bool waiting = true;
                ControllerResponse response = null;

                yield return controller.Act(gameState, r =>
                {
                    response = r;
                    waiting = false;
                });

                while (waiting)
                {
                    yield return null;
                }

                gameState.TrashCards(response.cardsTrash);
                yield return WaitForUi(3f);

                gameState.AddCardToPlayerHand();
                if (gameState.CurrentPlayerIndex == 0)
                {
                    gameState.OpenPlayerHands(0);
                }

                yield return WaitForUi(3f);
                gameState.NextTurn();

                if (IsGameOver())
                {
                    yield break;
                }
            }
        }
    }

    private IEnumerator WaitForUi(float duration)
    {
        if (waitForUi != null)
        {
            yield return waitForUi(duration);
        }
    }

    private bool IsGameOver()
    {
        return isGameOver != null && isGameOver();
    }
}
