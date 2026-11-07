using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class SessionBoundaryTests
{
    private readonly List<Object> created = new List<Object>();
    private UnityEngine.Random.State randomState;

    [SetUp]
    public void SetUp()
    {
        randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(1024);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = created.Count - 1; i >= 0; i--)
            if (created[i] != null) Object.DestroyImmediate(created[i]);
        created.Clear();
        UnityEngine.Random.state = randomState;
    }

    [Test]
    public void Snapshot_CopiesModeAndStreakWithoutReadingGlobalSelection()
    {
        GameModeData original = GameModeManager.GetGameModeData();
        try
        {
            var selected = new GameModeData(GameModeData.GameMode.BattleGroundMode);
            selected.IncrementWinStreak();
            GameModeManager.SetGameModeData(new GameModeData());
            GameUiSnapshot snapshot = new GameUiPresenter().CreateSnapshot(new GameState(), selected);
            selected.ResetWinStreak();
            selected.Mode = GameModeData.GameMode.IkkiMode;
            Assert.That(snapshot.Mode, Is.EqualTo(GameModeData.GameMode.BattleGroundMode));
            Assert.That(snapshot.WinStreak, Is.EqualTo(1));
        }
        finally { GameModeManager.SetGameModeData(original); }
    }

    [Test]
    public void Theme_TwoIndependentTargetsRestoreOnlyTheirOwnCardsAndImages()
    {
        var data = Track(ScriptableObject.CreateInstance<CardData>());
        data.BackImage = MakeSprite();
        var assetsA = Track(ScriptableObject.CreateInstance<BattleGroundVisualAssets>());
        var assetsB = Track(ScriptableObject.CreateInstance<BattleGroundVisualAssets>());
        assetsA.normalCardBack = MakeSprite();
        assetsA.battleBackground = MakeSprite();
        assetsA.roundCounterFrame = MakeSprite();
        assetsB.normalCardBack = MakeSprite();
        Card first = MakeCard(data), second = MakeCard(data), outside = MakeCard(data);
        outside.transform.localScale = new Vector3(0.43f, 0.56f, 1f);
        outside.IsFaceUp = true;
        Image background = MakeImage(), frame = MakeImage();
        background.sprite = data.BackImage;
        frame.sprite = data.BackImage;
        frame.preserveAspect = false;
        frame.raycastTarget = true;
        var themeA = new BattleGroundVisualTheme(assetsA, new[] { first, first }, background, frame, null);
        var themeB = new BattleGroundVisualTheme(assetsB, new[] { second }, null, null, null);
        themeA.Activate(1);
        themeB.Activate(2);
        themeA.Activate(3); // A second opponent must not replace the original-image cache.
        Assert.That(first.DisplayBackSprite, Is.SameAs(assetsA.normalCardBack));
        Assert.That(second.DisplayBackSprite, Is.SameAs(assetsB.normalCardBack));
        Assert.That(outside.DisplayBackSprite, Is.SameAs(data.BackImage));
        themeA.Deactivate();
        Assert.That(first.DisplayBackSprite, Is.SameAs(data.BackImage));
        Assert.That(second.DisplayBackSprite, Is.SameAs(assetsB.normalCardBack));
        Assert.That(themeB.IsActive, Is.True);
        Assert.That(background.sprite, Is.SameAs(data.BackImage));
        Assert.That(frame.sprite, Is.SameAs(data.BackImage));
        Assert.That(frame.preserveAspect, Is.False);
        Assert.That(frame.raycastTarget, Is.True);
        Assert.That(outside.IsFaceUp, Is.True);
        Assert.That(outside.transform.localScale, Is.EqualTo(new Vector3(0.43f, 0.56f, 1f)));
        themeB.Deactivate();
        themeA.Deactivate();
        Assert.That(second.DisplayBackSprite, Is.SameAs(data.BackImage));
    }

    [Test]
    public void Match_CompletesUsingRealRoundAndShowdownServices()
    {
        var state = new GameState();
        var views = new FakePresentation();
        MatchFlow flow = MakeMatch(state, views);
        var driver = new RoutineDriver(flow.Run(new GameModeData(), null));
        driver.Drain();
        Assert.That(flow.WasCancelled, Is.False);
        Assert.That(flow.WinnerIndex, Is.InRange(0, 1));
        Assert.That(flow.Results.Count, Is.GreaterThan(0));
        Assert.That(state.PlayerStates[1 - flow.WinnerIndex].LifePoints, Is.Zero);
        Assert.That(views.ShowdownCount, Is.EqualTo(flow.Results.Count));
    }

    [TestCase("Coin")]
    [TestCase("Reveal")]
    [TestCase("Showdown")]
    public void Match_CancelAtPresentationWaitDoesNotCommitOrAdvance(string wait)
    {
        var state = new GameState();
        var views = new FakePresentation();
        MatchFlow flow = MakeMatch(state, views);
        var driver = new RoutineDriver(flow.Run(new GameModeData(), null));
        driver.Until(() => views.LastWait == wait);
        int playerLife = state.PlayerStates[0].LifePoints;
        int cpuLife = state.PlayerStates[1].LifePoints;
        int round = state.RoundNumber;
        flow.Cancel();
        driver.Drain();
        Assert.That(flow.WasCancelled, Is.True);
        Assert.That(flow.Results, Is.Empty);
        Assert.That(state.PlayerStates[0].LifePoints, Is.EqualTo(playerLife));
        Assert.That(state.PlayerStates[1].LifePoints, Is.EqualTo(cpuLife));
        Assert.That(state.RoundNumber, Is.EqualTo(round));
        Assert.That(state.IsWaitingForHandReveal, Is.False);
    }

    [Test]
    public void Match_CancelBeforeFirstMoveNextDoesNotInitializeSavedInput()
    {
        var state = new GameState();
        state.InitializePlayerStates();
        state.PlayerStates[0].SetLifePoints(27);
        MatchFlow flow = MakeMatch(state, new FakePresentation());
        IEnumerator routine = flow.Run(new GameModeData(), null);
        flow.Cancel();
        Assert.That(routine.MoveNext(), Is.False);
        Assert.That(state.PlayerStates[0].LifePoints, Is.EqualTo(27));
    }

    [Test]
    public void Round_CancelDuringInitialLayoutDoesNotDealOrChangeTurn()
    {
        var state = new GameState();
        state.InitializePlayerStates();
        GameObject root = Track(new GameObject("Cancelled round pool"));
        root.SetActive(false);
        Cards pool = root.AddComponent<Cards>();
        var data = Track(ScriptableObject.CreateInstance<CardData>());
        pool.cardList.Add(MakeCard(data));
        bool cancelled = false;
        var round = new RoundFlowService(state, pool, null, null, () => cancelled);
        IEnumerator routine = round.InitializeRound();
        Assert.That(routine.MoveNext(), Is.True); // Initial layout wait, before dealing.
        cancelled = true;
        Assert.That(routine.MoveNext(), Is.False);
        Assert.That(state.PlayerStates[0].HandCards, Is.Empty);
        Assert.That(state.PlayerStates[1].HandCards, Is.Empty);
        Assert.That(state.deckCards.Count, Is.EqualTo(1));
        Assert.That(state.CurrentPlayerIndex, Is.Zero);
    }

    [TestCase("Coin")]
    [TestCase("Reveal")]
    [TestCase("Showdown")]
    public void Match_SurrenderAtWaitCompletesAsLossWithoutCommitting(string wait)
    {
        var state = new GameState();
        var views = new FakePresentation();
        MatchFlow flow = MakeMatch(state, views);
        var driver = new RoutineDriver(flow.Run(new GameModeData(), new BattleGroundRunState(null)));
        driver.Until(() => views.LastWait == wait);
        int life = state.PlayerStates[0].LifePoints;
        flow.Surrender();
        driver.Drain();
        Assert.That(flow.WinnerIndex, Is.EqualTo(1));
        Assert.That(flow.WasCancelled, Is.False);
        Assert.That(flow.Results, Is.Empty);
        Assert.That(state.PlayerStates[0].LifePoints, Is.EqualTo(life));
        Assert.That(state.IsWaitingForHandReveal, Is.False);
    }

    [Test]
    public void Session_CancelledResultCannotRewardSelectOpponentOrStartAgain()
    {
        var progress = new FakeProgress();
        var match = new CompletedMatch();
        var views = new FakePresentation();
        int randomCalls = 0;
        var flow = MakeSession(match, views, progress, (min, max) => { randomCalls++; return min; });
        Assert.That(flow.TryStart(new GameModeData(GameModeData.GameMode.BattleGroundMode), out IEnumerator routine), Is.True);
        Assert.That(flow.TryStart(new GameModeData(), out _), Is.False);
        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(routine.MoveNext(), Is.True); // Victory saved, result not acknowledged.
        Assert.That(progress.Saves, Is.EqualTo(1));
        int callsBeforeCancel = randomCalls;
        flow.Cancel();
        Assert.That(routine.MoveNext(), Is.False);
        Assert.That(views.RewardCount, Is.Zero);
        Assert.That(randomCalls, Is.EqualTo(callsBeforeCancel));
        Assert.That(progress.Saves, Is.EqualTo(1));
        Assert.That(flow.IsRunning, Is.False);
    }

    [Test]
    public void Session_DisposingOldGenerationDoesNotCancelRestart()
    {
        var flow = MakeSession(new CompletedMatch(), new FakePresentation(), new FakeProgress(), (min, max) => min);
        flow.TryStart(new GameModeData(), out IEnumerator old);
        old.MoveNext();
        flow.Cancel();
        Assert.That(flow.TryStart(new GameModeData(), out IEnumerator current), Is.True);
        (old as IDisposable).Dispose();
        Assert.That(flow.IsRunning, Is.True);
        Assert.That(current.MoveNext(), Is.True);
        flow.Cancel();
        Assert.That(current.MoveNext(), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Run_CancelledRewardCannotApplyHerbOrCard(bool card)
    {
        var views = new FakePresentation { HasRewardPanel = true, CardChoice = card };
        var mode = new GameModeData(GameModeData.GameMode.BattleGroundMode);
        GameObject root = Track(new GameObject("Reward pool"));
        root.SetActive(false);
        Cards pool = root.AddComponent<Cards>();
        var data = Track(ScriptableObject.CreateInstance<CardData>());
        data.name = "test-reward";
        pool.cardList.Add(MakeCard(data));
        var run = new BattleGroundFlow(pool, new FakeProgress(), views, (min, max) => min);
        run.Initialize(mode);
        var player = new PlayerState(0);
        player.SetLifePoints(23);
        run.CaptureVictory(mode, player);
        bool cancelled = false;
        IEnumerator reward = run.ShowReward(mode, () => cancelled);
        Assert.That(reward.MoveNext(), Is.True);
        Assert.That(((IEnumerator)reward.Current).MoveNext(), Is.True); // User has made a choice.
        Assert.That(views.RewardCount, Is.EqualTo(1));
        cancelled = true;
        Assert.That(reward.MoveNext(), Is.False);
        Assert.That(run.Run.PlayerLife, Is.EqualTo(23));
        Assert.That(run.Run.SpecialCards, Is.Empty);
    }

    private static GameSessionFlow MakeSession(IMatchFlow match, ISessionPresentation views,
        ISessionProgress progress, Func<int, int, int> random) => new GameSessionFlow(null,
        new GameState(), null, null, null, null, null, null, null, 0, null, null, null, null,
        randomRange: random, matchFlow: match, presentation: views, progress: progress);

    private MatchFlow MakeMatch(GameState state, ISessionPresentation views)
    {
        GameObject root = Track(new GameObject("Test pool"));
        root.SetActive(false);
        Cards pool = root.AddComponent<Cards>();
        for (int suit = 1; suit <= 4; suit++)
            for (int rank = 1; rank <= 13; rank++)
            {
                var data = Track(ScriptableObject.CreateInstance<CardData>());
                data.suit = (Suit)suit;
                data.number = (Number)rank;
                pool.cardList.Add(MakeCard(data));
            }
        return new MatchFlow(state, pool, null, null, null, null,
            new CpuSetupService(state, null, null, null, null, 0, null), views, null, (min, max) => min);
    }

    private Card MakeCard(CardData data)
    {
        var go = Track(new GameObject("Test Card", typeof(RectTransform), typeof(Image), typeof(Card)));
        Card card = go.GetComponent<Card>();
        card.Initialize();
        card.SetCardData(data);
        return card;
    }

    private Image MakeImage() => Track(new GameObject("Test image", typeof(RectTransform), typeof(Image))).GetComponent<Image>();
    private Sprite MakeSprite()
    {
        var texture = Track(new Texture2D(8, 8));
        return Track(Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * 0.5f));
    }
    private T Track<T>(T item) where T : Object { created.Add(item); return item; }

    private sealed class RoutineDriver
    {
        private readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
        public RoutineDriver(IEnumerator routine) { stack.Push(routine); }
        public void Drain() => Until(() => stack.Count == 0);
        public void Until(Func<bool> done)
        {
            int limit = 100000;
            while (!done() && stack.Count > 0 && --limit > 0)
            {
                IEnumerator current = stack.Peek();
                if (!current.MoveNext()) stack.Pop();
                else if (current.Current is IEnumerator nested) stack.Push(nested);
            }
            Assert.That(limit, Is.GreaterThan(0), "Coroutine failed to terminate.");
            Assert.That(done(), Is.True, "Coroutine ended before reaching the expected boundary.");
        }
    }

    private sealed class CompletedMatch : IMatchFlow
    {
        public int WinnerIndex { get; private set; }
        public bool WasCancelled { get; private set; }
        public IReadOnlyList<MatchRoundResult> Results { get; } = new MatchRoundResult[0];
        public IEnumerator Run(GameModeData mode, BattleGroundRunState run) { yield break; }
        public void Cancel() { WasCancelled = true; }
        public void Surrender() { WinnerIndex = 1; }
    }

    private sealed class FakeProgress : ISessionProgress
    {
        public int Saves;
        public bool IsBattleGroundUnlocked => true;
        public int BestBattleGroundStreak => Saves;
        public bool IsIkkiLevelUnlocked(int level) => true;
        public bool IsSpecialCardUnlocked(CardData card) => true;
        public int RecordIkkiVictory(int level) { Saves++; return level + 1; }
        public int RecordBattleGroundStreak(int streak) { Saves++; return streak; }
    }

    private sealed class FakePresentation : ISessionPresentation
    {
        public string LastWait;
        public int ShowdownCount, RewardCount;
        public bool CardChoice;
        public bool HandRevealConfirmed => true;
        public bool HasRewardPanel { get; set; }
        public IEnumerator CoinToss(int parentIndex) { LastWait = "Coin"; yield return null; }
        public IEnumerator HandReveal() { LastWait = "Reveal"; yield return null; }
        public IEnumerator Showdown(SpecialCardResolver.ShowdownResult result)
        { ShowdownCount++; LastWait = "Showdown"; yield return null; }
        public IEnumerator ShowResult(int level, IReadOnlyList<MatchRoundResult> results,
            bool playerWon, string buttonLabel, string summary) { yield return null; }
        public IEnumerator ChooseReward(BattleGroundRunState run, bool canChooseCard,
            Action<BattleGroundRewardPanel.Choice> selected)
        {
            RewardCount++;
            selected(CardChoice ? BattleGroundRewardPanel.Choice.SpecialCard : BattleGroundRewardPanel.Choice.Herb);
            yield return null;
        }
        public IEnumerator RevealReward(CardData card) { yield return null; }
        public void CancelHandReveal() { }
        public void Cancel() { }
    }
}
