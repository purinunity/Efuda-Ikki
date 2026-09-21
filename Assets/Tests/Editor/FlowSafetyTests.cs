using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class FlowSafetyTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int index = createdObjects.Count - 1; index >= 0; index--)
        {
            if (createdObjects[index] != null)
            {
                Object.DestroyImmediate(createdObjects[index]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void ShowdownCommit_AppliesDamageAndConsumesSpecialCardOnlyOnce()
    {
        GameState gameState = CreateGameState();
        AddPairHand(gameState.PlayerStates[0]);
        AddNoRoleHand(gameState.PlayerStates[1]);
        Card specialCard = CreateSpecialCard(SpecialCardResolver.SpecialCardId.Bonus5);
        gameState.PlayerStates[0].SetSpecialCardsForMatch(new[] { specialCard });
        specialCard.IsSelected = true;

        var service = new ShowdownFlowService(gameState, null);
        PreparedShowdown prepared = service.PrepareShowdown();
        ShowdownCommitResult first = service.Commit(prepared, 1);
        int lifeAfterFirstCommit = gameState.PlayerStates[1].LifePoints;
        ShowdownCommitResult second = service.Commit(prepared, 1);

        Assert.That(second, Is.SameAs(first));
        Assert.That(gameState.PlayerStates[1].LifePoints, Is.EqualTo(lifeAfterFirstCommit));
        Assert.That(lifeAfterFirstCommit, Is.EqualTo(90));
        Assert.That(gameState.PlayerStates[0].IsSpecialCardUsed(specialCard), Is.True);
        Assert.That(first.RoundResult.PlayerLifeBefore, Is.EqualTo(100));
        Assert.That(first.RoundResult.PlayerLifeAfter, Is.EqualTo(100));
        Assert.That(first.RoundResult.CpuLifeBefore, Is.EqualTo(100));
        Assert.That(first.RoundResult.CpuLifeAfter, Is.EqualTo(90));
    }

    [Test]
    public void ShowdownCommit_WhenLifeReachesZero_ReportsWinnerImmediately()
    {
        GameState gameState = CreateGameState();
        AddPairHand(gameState.PlayerStates[0]);
        AddNoRoleHand(gameState.PlayerStates[1]);
        gameState.PlayerStates[1].SetLifePoints(5);

        var service = new ShowdownFlowService(gameState, null);
        PreparedShowdown prepared = service.PrepareShowdown();
        ShowdownCommitResult result = service.Commit(prepared, 1);

        Assert.That(result.IsGameOver, Is.True);
        Assert.That(result.MatchWinnerIndex, Is.EqualTo(0));
        Assert.That(gameState.PlayerStates[1].LifePoints, Is.Zero);
        Assert.That(result.RoundResult.CpuLifeAfter, Is.Zero);
    }

    [Test]
    public void DisabledPlayerController_CancelsRoundBeforeShowdownCanContinue()
    {
        GameState gameState = CreateGameState();
        gameState.maxHandTrashTurn = 1;
        PlayerController playerController = CreateComponent<PlayerController>("Disabled Player");
        playerController.enabled = false;
        var service = new RoundFlowService(
            gameState,
            null,
            new Controller[] { playerController, null },
            null,
            () => false);

        RunToCompletion(service.RunExchangeRound());

        Assert.That(service.WasCancelled, Is.True);
        Assert.That(gameState.CurrentPlayerIndex, Is.Zero);
        Assert.That(gameState.PlayerStates[0].HandTrashTurnsUsed, Is.Zero);
    }

    [Test]
    public void NullControllerResult_IsHandledAsNoDiscardAndRoundCompletes()
    {
        GameState gameState = CreateGameState();
        gameState.maxHandTrashTurn = 1;
        for (int index = 0; index < 10; index++)
        {
            gameState.AddCardToDeck(CreateCard(
                (Number)(index % 10 + 1),
                (Suit)(index % 4 + 1),
                $"deck_{index}"));
        }

        NullResponseController player = CreateComponent<NullResponseController>("NullPlayer");
        NullResponseController cpu = CreateComponent<NullResponseController>("NullCpu");
        var service = new RoundFlowService(
            gameState,
            null,
            new Controller[] { player, cpu },
            null,
            () => false);

        LogAssert.Expect(
            LogType.Warning,
            "NullPlayer returned no action result. Continuing with no discarded cards.");
        LogAssert.Expect(
            LogType.Warning,
            "NullCpu returned no action result. Continuing with no discarded cards.");
        RunToCompletion(service.RunExchangeRound());

        Assert.That(service.WasCancelled, Is.False);
        Assert.That(gameState.CurrentPlayerIndex, Is.Zero);
        Assert.That(gameState.PlayerStates[0].HandTrashTurnsUsed, Is.EqualTo(1));
        Assert.That(gameState.PlayerStates[1].HandTrashTurnsUsed, Is.EqualTo(1));
        Assert.That(gameState.PlayerStates[0].HandCards.Count, Is.EqualTo(5));
        Assert.That(gameState.PlayerStates[1].HandCards.Count, Is.EqualTo(5));
    }

    [Test]
    public void SessionCancel_CompletesPendingPlayerInputAsCancelled()
    {
        GameState gameState = CreateGameState();
        PlayerController playerController = CreateComponent<PlayerController>("Pending Player");
        ControllerResponse callbackResponse = null;
        IEnumerator input = playerController.Act(gameState, response => callbackResponse = response);
        Assert.That(input.MoveNext(), Is.True);
        Assert.That(playerController.IsInputReceivable, Is.True);

        var flow = new GameSessionFlow(
            playerController,
            gameState,
            null,
            playerController,
            null,
            null,
            null,
            null,
            null,
            0,
            null,
            null,
            null,
            null);

        flow.Cancel();

        Assert.That(callbackResponse, Is.Not.Null);
        Assert.That(callbackResponse.actionCompleted, Is.False);
        Assert.That(playerController.IsInputReceivable, Is.False);
        (input as System.IDisposable)?.Dispose();
    }

    private GameState CreateGameState()
    {
        var gameState = new GameState();
        gameState.InitializePlayerStates();
        return gameState;
    }

    private void AddPairHand(PlayerState player)
    {
        player.AddCardToHand(CreateCard(Number.Two, Suit.Flowers, "pair_1"));
        player.AddCardToHand(CreateCard(Number.Two, Suit.Birds, "pair_2"));
        player.AddCardToHand(CreateCard(Number.Five, Suit.Wind, "pair_3"));
        player.AddCardToHand(CreateCard(Number.Eight, Suit.Moon, "pair_4"));
        player.AddCardToHand(CreateCard(Number.Ten, Suit.Flowers, "pair_5"));
    }

    private void AddNoRoleHand(PlayerState player)
    {
        player.AddCardToHand(CreateCard(Number.One, Suit.Flowers, "none_1"));
        player.AddCardToHand(CreateCard(Number.Three, Suit.Birds, "none_2"));
        player.AddCardToHand(CreateCard(Number.Five, Suit.Wind, "none_3"));
        player.AddCardToHand(CreateCard(Number.Eight, Suit.Moon, "none_4"));
        player.AddCardToHand(CreateCard(Number.Ten, Suit.Birds, "none_5"));
    }

    private Card CreateSpecialCard(SpecialCardResolver.SpecialCardId id)
    {
        Assert.That(SpecialCardCatalog.TryGet(id, out SpecialCardCatalog.Entry entry), Is.True);
        return CreateCard(Number.Joker, Suit.Joker, entry.LegacyAssetNames[0]);
    }

    private Card CreateCard(Number number, Suit suit, string assetName)
    {
        CardData data = ScriptableObject.CreateInstance<CardData>();
        data.name = assetName;
        data.number = number;
        data.suit = suit;
        createdObjects.Add(data);

        Card card = CreateComponent<Card>($"Card {assetName}");
        card.SetCardData(data);
        card.IsFaceUp = true;
        return card;
    }

    private T CreateComponent<T>(string name) where T : Component
    {
        var gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        return gameObject.AddComponent<T>();
    }

    private static void RunToCompletion(IEnumerator routine)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(routine);
        int iterations = 0;
        while (stack.Count > 0)
        {
            IEnumerator current = stack.Peek();
            if (!current.MoveNext())
            {
                (current as System.IDisposable)?.Dispose();
                stack.Pop();
                continue;
            }

            Assert.That(++iterations, Is.LessThan(200), "The flow did not complete.");
            if (current.Current is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }

            Assert.That(current.Current, Is.Null, "The synchronous test only supports null yields.");
        }
    }

    public sealed class NullResponseController : Controller
    {
        public override IEnumerator Act(
            GameState gameState,
            System.Action<ControllerResponse> callback)
        {
            callback?.Invoke(null);
            yield break;
        }
    }
}
