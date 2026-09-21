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

    /// <summary>
    /// True when an in-progress player action was cancelled rather than completed.
    /// The session flow uses this to avoid treating a disabled input component as
    /// permission to continue into showdown.
    /// </summary>
    public bool WasCancelled { get; private set; }

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
        if (gameState == null || allCards == null || allCards.cardList == null)
        {
            Debug.LogError("RoundFlowService cannot initialize because its game state or card deck is missing.");
            yield break;
        }

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
        if (gameState == null)
        {
            Debug.LogError("RoundFlowService cannot run because its game state is missing.");
            yield break;
        }

        for (int i = 0; i < gameState.maxHandTrashTurn; i++)
        {
            for (int j = 0; j < gameState.playerCount; j++)
            {
                if (IsGameOver())
                {
                    yield break;
                }

                Controller controller = GetCurrentController();
                bool callbackInvoked = false;
                ControllerResponse response = null;

                if (controller is PlayerController disabledPlayerController &&
                    !disabledPlayerController.isActiveAndEnabled)
                {
                    WasCancelled = true;
                    disabledPlayerController.CancelPendingInput();
                    yield break;
                }

                if (controller != null && controller.isActiveAndEnabled)
                {
                    IEnumerator action = null;
                    try
                    {
                        action = controller.Act(gameState, r =>
                        {
                            if (callbackInvoked)
                            {
                                Debug.LogWarning($"{controller.name} completed its action callback more than once.", controller);
                                return;
                            }

                            response = r;
                            callbackInvoked = true;
                        });
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, controller);
                    }

                    if (action != null)
                    {
                        while (!callbackInvoked)
                        {
                            if (IsGameOver())
                            {
                                DisposeControllerAction(action);
                                CancelControllerInput(controller);
                                yield break;
                            }

                            if (!controller.isActiveAndEnabled)
                            {
                                WasCancelled = controller is PlayerController;
                                CancelControllerInput(controller);
                                break;
                            }

                            bool hasNext;
                            object current = null;
                            try
                            {
                                hasNext = action.MoveNext();
                                if (hasNext)
                                {
                                    current = action.Current;
                                }
                            }
                            catch (Exception exception)
                            {
                                Debug.LogException(exception, controller);
                                hasNext = false;
                            }

                            if (!hasNext)
                            {
                                break;
                            }

                            yield return current;
                        }

                        DisposeControllerAction(action);
                    }
                }

                if (IsGameOver())
                {
                    CancelControllerInput(controller);
                    yield break;
                }

                if (response != null && !response.actionCompleted)
                {
                    WasCancelled = true;
                    CancelControllerInput(controller);
                    yield break;
                }

                if (!callbackInvoked || response == null)
                {
                    string controllerName = controller != null ? controller.name : "<missing controller>";
                    Debug.LogWarning($"{controllerName} returned no action result. Continuing with no discarded cards.");
                    response = CreateEmptyResponse();
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

    private Controller GetCurrentController()
    {
        int index = gameState.CurrentPlayerIndex;
        if (controllers == null || index < 0 || index >= controllers.Length)
        {
            return null;
        }

        return controllers[index];
    }

    private static void CancelControllerInput(Controller controller)
    {
        if (controller is PlayerController playerController)
        {
            playerController.CancelPendingInput();
        }
    }

    private static void DisposeControllerAction(IEnumerator action)
    {
        if (action is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static ControllerResponse CreateEmptyResponse()
    {
        return new ControllerResponse
        {
            actionCompleted = true,
            cardsTrash = new System.Collections.Generic.List<Card>()
        };
    }
}
