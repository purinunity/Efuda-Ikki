using System.Collections.Generic;

public sealed class PreparedShowdown
{
    public SpecialCardResolver.ShowdownResult Result { get; }
    public IReadOnlyList<Card> SpecialCardsToConsume { get; }
    public int PlayerLifeBefore { get; }
    public int CpuLifeBefore { get; }
    public bool IsCommitted => CommitResult != null;
    public ShowdownCommitResult CommitResult { get; private set; }

    public PreparedShowdown(
        SpecialCardResolver.ShowdownResult result,
        IReadOnlyList<Card> specialCardsToConsume,
        int playerLifeBefore,
        int cpuLifeBefore)
    {
        Result = result;
        SpecialCardsToConsume = specialCardsToConsume;
        PlayerLifeBefore = playerLifeBefore;
        CpuLifeBefore = cpuLifeBefore;
    }

    internal void MarkCommitted(ShowdownCommitResult commitResult)
    {
        CommitResult = commitResult;
    }
}

public sealed class ShowdownCommitResult
{
    public MatchRoundResult RoundResult { get; }
    public bool IsGameOver { get; }
    public int MatchWinnerIndex { get; }

    public ShowdownCommitResult(
        MatchRoundResult roundResult,
        bool isGameOver,
        int matchWinnerIndex)
    {
        RoundResult = roundResult;
        IsGameOver = isGameOver;
        MatchWinnerIndex = matchWinnerIndex;
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
        return new PreparedShowdown(
            result,
            specialCardsToConsume,
            GetLifePoints(0),
            GetLifePoints(1));
    }

    /// <summary>
    /// Applies every state mutation produced by a showdown exactly once.
    /// Repeated calls return the first result without consuming cards or dealing damage again.
    /// </summary>
    public ShowdownCommitResult Commit(PreparedShowdown preparedShowdown, int roundNumber)
    {
        if (preparedShowdown == null)
        {
            return new ShowdownCommitResult(null, CheckGameOver(), DetermineMatchWinnerIndex());
        }

        if (preparedShowdown.IsCommitted)
        {
            return preparedShowdown.CommitResult;
        }

        MarkSpecialCardsUsed(preparedShowdown.SpecialCardsToConsume);

        SpecialCardResolver.ShowdownResult showdownResult = preparedShowdown.Result;
        if (showdownResult != null && !showdownResult.IsDraw)
        {
            ApplyDamage(showdownResult);
        }

        MatchRoundResult roundResult = showdownResult == null
            ? null
            : new MatchRoundResult(
                roundNumber,
                showdownResult.WinnerIndex,
                showdownResult.Damage,
                preparedShowdown.PlayerLifeBefore,
                GetLifePoints(0),
                preparedShowdown.CpuLifeBefore,
                GetLifePoints(1));

        bool isGameOver = CheckGameOver();
        ShowdownCommitResult commitResult = new ShowdownCommitResult(
            roundResult,
            isGameOver,
            isGameOver ? DetermineMatchWinnerIndex() : -1);
        preparedShowdown.MarkCommitted(commitResult);
        return commitResult;
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
                gameState.PlayerStates[i].DecreaseLifePoints(showdownResult.Damage);
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

    public int DetermineMatchWinnerIndex()
    {
        if (gameState?.PlayerStates == null)
        {
            return -1;
        }

        for (int i = 0; i < gameState.PlayerStates.Count; i++)
        {
            PlayerState playerState = gameState.PlayerStates[i];
            if (playerState != null && playerState.LifePoints > 0)
            {
                return i;
            }
        }

        return -1;
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

            foreach (Card card in CardSelectionUtility.GetSelectedCards(
                playerState.SpecialCards,
                card => !playerState.IsSpecialCardUsed(card) &&
                        !SpecialCardResolver.IsNoUseSpecialCard(card.CardData)))
            {
                selectedCards.Add(card);
                break;
            }
        }

        return selectedCards;
    }

    private int GetLifePoints(int playerIndex)
    {
        if (gameState?.PlayerStates == null ||
            playerIndex < 0 ||
            playerIndex >= gameState.PlayerStates.Count ||
            gameState.PlayerStates[playerIndex] == null)
        {
            return 0;
        }

        return gameState.PlayerStates[playerIndex].LifePoints;
    }
}
