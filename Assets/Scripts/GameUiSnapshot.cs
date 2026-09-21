using System.Collections.Generic;

/// <summary>
/// Immutable-by-convention data required to render one game UI frame.
/// Collections are copied by the presenter so later GameState mutations do not
/// change an update that is already being presented.
/// </summary>
public sealed class GameUiSnapshot
{
    public int RoundNumber { get; }
    public int PlayerLifePoints { get; }
    public int CpuLifePoints { get; }
    public string PlayerRoleName { get; }
    public int RemainingTrashTurns { get; }
    public int MaxHandTrashCount { get; }
    public bool PlayerCanSelectSpecialCard { get; }
    public List<Card> DeckCards { get; }
    public List<Card> CommonCards { get; }
    public List<Card> TrashCards { get; }
    public List<Card> PlayerHandCards { get; }
    public List<Card> CpuHandCards { get; }
    public List<Card> PlayerSpecialCards { get; }
    public List<Card> CpuSpecialCards { get; }
    public List<Card> PlayerUsedSpecialCards { get; }
    public List<Card> CpuUsedSpecialCards { get; }

    public GameUiSnapshot(
        int roundNumber,
        int playerLifePoints,
        int cpuLifePoints,
        string playerRoleName,
        int remainingTrashTurns,
        int maxHandTrashCount,
        bool playerCanSelectSpecialCard,
        List<Card> deckCards,
        List<Card> commonCards,
        List<Card> trashCards,
        List<Card> playerHandCards,
        List<Card> cpuHandCards,
        List<Card> playerSpecialCards,
        List<Card> cpuSpecialCards,
        List<Card> playerUsedSpecialCards,
        List<Card> cpuUsedSpecialCards)
    {
        RoundNumber = roundNumber;
        PlayerLifePoints = playerLifePoints;
        CpuLifePoints = cpuLifePoints;
        PlayerRoleName = playerRoleName ?? string.Empty;
        RemainingTrashTurns = remainingTrashTurns;
        MaxHandTrashCount = maxHandTrashCount;
        PlayerCanSelectSpecialCard = playerCanSelectSpecialCard;
        DeckCards = deckCards ?? new List<Card>();
        CommonCards = commonCards ?? new List<Card>();
        TrashCards = trashCards ?? new List<Card>();
        PlayerHandCards = playerHandCards ?? new List<Card>();
        CpuHandCards = cpuHandCards ?? new List<Card>();
        PlayerSpecialCards = playerSpecialCards ?? new List<Card>();
        CpuSpecialCards = cpuSpecialCards ?? new List<Card>();
        PlayerUsedSpecialCards = playerUsedSpecialCards ?? new List<Card>();
        CpuUsedSpecialCards = cpuUsedSpecialCards ?? new List<Card>();
    }
}
