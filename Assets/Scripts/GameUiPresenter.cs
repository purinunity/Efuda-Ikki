using System.Collections.Generic;

public sealed class GameUiPresenter
{
    public GameUiSnapshot CreateSnapshot(GameState state)
    {
        if (state == null)
        {
            return null;
        }

        PlayerState player = GetPlayer(state, 0);
        PlayerState cpu = GetPlayer(state, 1);
        List<Card> playerHand = Copy(player != null ? player.HandCards : null);
        List<Card> commonCards = Copy(state.commonCards);
        string roleName = player != null
            ? HandEvaluator.EvaluateHand(playerHand, commonCards).Name
            : string.Empty;

        int maxTrashTurns = System.Math.Max(0, state.maxHandTrashTurn);
        int usedTrashTurns = player != null ? player.HandTrashTurnsUsed : 0;

        return new GameUiSnapshot(
            state.RoundNumber,
            player != null ? player.LifePoints : 0,
            cpu != null ? cpu.LifePoints : 0,
            roleName,
            System.Math.Max(0, maxTrashTurns - usedTrashTurns),
            System.Math.Max(0, state.maxHandTrashCount),
            player != null && usedTrashTurns < maxTrashTurns,
            Copy(state.deckCards),
            commonCards,
            Copy(state.trashCards),
            playerHand,
            Copy(cpu != null ? cpu.HandCards : null),
            Copy(player != null ? player.SpecialCards : null),
            Copy(cpu != null ? cpu.SpecialCards : null),
            Copy(player != null ? player.UsedSpecialCards : null),
            Copy(cpu != null ? cpu.UsedSpecialCards : null));
    }

    private static PlayerState GetPlayer(GameState state, int index)
    {
        return state.PlayerStates != null && index >= 0 && index < state.PlayerStates.Count
            ? state.PlayerStates[index]
            : null;
    }

    private static List<Card> Copy(IEnumerable<Card> cards)
    {
        return cards != null ? new List<Card>(cards) : new List<Card>();
    }
}
