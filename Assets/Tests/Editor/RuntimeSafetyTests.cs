using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RuntimeSafetyTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int index = createdObjects.Count - 1; index >= 0; index--)
        {
            if (createdObjects[index] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[index]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void NullControllerResults_AreHandledAsNoDiscard()
    {
        var state = new GameState
        {
            maxHandTrashTurn = 1,
            maxHandTrashCount = 5
        };
        state.InitializePlayerStates();
        FillBothHands(state);

        NullResponseTestController player = CreateController("Null Player");
        NullResponseTestController cpu = CreateController("Null CPU");
        var service = new RoundFlowService(
            state,
            null,
            new Controller[] { player, cpu },
            _ => EmptyRoutine(),
            () => false);

        RunToCompletion(service.RunExchangeRound());

        Assert.That(state.PlayerStates[0].HandCards.Count, Is.EqualTo(5));
        Assert.That(state.PlayerStates[1].HandCards.Count, Is.EqualTo(5));
        Assert.That(state.PlayerStates[0].HandTrashTurnsUsed, Is.EqualTo(1));
        Assert.That(state.PlayerStates[1].HandTrashTurnsUsed, Is.EqualTo(1));
        Assert.That(state.CurrentPlayerIndex, Is.EqualTo(0));
    }

    [Test]
    public void ShowdownCommit_AppliesDamageAndSpecialConsumptionOnce()
    {
        var state = new GameState();
        state.InitializePlayerStates();
        Card specialCard = CreateCard("Selected Special");
        state.PlayerStates[0].SetSpecialCardsForMatch(new[] { specialCard });
        specialCard.IsSelected = true;

        var showdown = new SpecialCardResolver.ShowdownResult(
            new List<SpecialCardResolver.ResolvedHand>(),
            0,
            20,
            new List<string>(),
            new List<SpecialCardResolver.EffectStep>());
        var prepared = new PreparedShowdown(
            showdown,
            new[] { specialCard },
            state.PlayerStates[0].LifePoints,
            state.PlayerStates[1].LifePoints);
        var service = new ShowdownFlowService(state, null);

        ShowdownCommitResult first = service.Commit(prepared, 1);
        ShowdownCommitResult second = service.Commit(prepared, 1);

        Assert.That(second, Is.SameAs(first));
        Assert.That(state.PlayerStates[0].LifePoints, Is.EqualTo(100));
        Assert.That(state.PlayerStates[1].LifePoints, Is.EqualTo(80));
        Assert.That(state.PlayerStates[0].IsSpecialCardUsed(specialCard), Is.True);
        Assert.That(first.RoundResult.CpuLifeBefore, Is.EqualTo(100));
        Assert.That(first.RoundResult.CpuLifeAfter, Is.EqualTo(80));
    }

    [Test]
    public void PresenterSnapshot_IsUnaffectedByLaterStateCollectionChanges()
    {
        var state = new GameState();
        state.InitializePlayerStates();
        Card card = CreateCard("Snapshot Card");
        state.AddCardToDeck(card);

        GameUiSnapshot snapshot = new GameUiPresenter().CreateSnapshot(state);
        state.deckCards.Clear();
        state.PlayerStates[0].AddCardToHand(card);

        Assert.That(snapshot.DeckCards, Is.EqualTo(new[] { card }));
        Assert.That(snapshot.PlayerHandCards, Is.Empty);
    }

    private NullResponseTestController CreateController(string name)
    {
        var gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        return gameObject.AddComponent<NullResponseTestController>();
    }

    private Card CreateCard(string name)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(Card));
        createdObjects.Add(gameObject);
        return gameObject.GetComponent<Card>();
    }

    private void FillBothHands(GameState state)
    {
        for (int playerId = 0; playerId < 2; playerId++)
        {
            for (int index = 0; index < 5; index++)
            {
                state.PlayerStates[playerId].AddCardToHand(
                    CreateCard($"Player {playerId} Card {index}"));
            }
        }
    }

    private static IEnumerator EmptyRoutine()
    {
        yield break;
    }

    private static void RunToCompletion(IEnumerator routine)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(routine);
        int iterationCount = 0;
        while (stack.Count > 0)
        {
            Assert.That(++iterationCount, Is.LessThan(1000), "Coroutine did not complete.");
            IEnumerator current = stack.Peek();
            if (!current.MoveNext())
            {
                (current as IDisposable)?.Dispose();
                stack.Pop();
                continue;
            }

            if (current.Current is IEnumerator nested)
            {
                stack.Push(nested);
            }
        }
    }
}

public sealed class NullResponseTestController : Controller
{
    public override IEnumerator Act(GameState gameState, Action<ControllerResponse> callback)
    {
        callback?.Invoke(null);
        yield break;
    }
}
