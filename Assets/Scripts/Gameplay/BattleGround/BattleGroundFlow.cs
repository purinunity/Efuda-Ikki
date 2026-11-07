using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>Owns one run's carry-over, reward and opponent selection.</summary>
public sealed class BattleGroundFlow
{
    private readonly BattleGroundRewardService rewards;
    private readonly BattleGroundOpponentSelector opponents;
    private readonly Cards specialCardPool;
    private readonly ISessionProgress progress;
    private readonly ISessionPresentation presentation;
    public BattleGroundRunState Run { get; private set; }

    public BattleGroundFlow(Cards specialCardPool, ISessionProgress progress,
        ISessionPresentation presentation, Func<int, int, int> randomRange)
    {
        this.specialCardPool = specialCardPool;
        this.progress = progress;
        this.presentation = presentation;
        rewards = new BattleGroundRewardService(randomRange);
        opponents = new BattleGroundOpponentSelector(randomRange);
    }

    public void Initialize(GameModeData mode)
    {
        Run = new BattleGroundRunState(mode.SelectedSpecialCardDatas);
        mode.SetCurrentLevel(opponents.Select(mode.CurrentLevel, false));
        mode.ResetWinStreak();
    }

    public void CaptureVictory(GameModeData mode, PlayerState player)
    {
        mode.IncrementWinStreak();
        Run.CaptureVictory(player);
        mode.ReplaceSpecialCards(Run.SpecialCards);
    }

    public void SelectNextOpponent(GameModeData mode)
    {
        mode.SetCurrentLevel(opponents.Select(mode.CurrentLevel, true));
    }

    public IEnumerator ShowReward(GameModeData mode, Func<bool> isCancelled)
    {
        if (isCancelled()) yield break;
        List<CardData> unlocked = GetUnlockedCards();
        bool canChooseCard = rewards.BuildCandidates(Run, unlocked).Count > 0;
        if (!presentation.HasRewardPanel)
        {
            rewards.ApplyHerb(Run);
            yield break;
        }

        BattleGroundRewardPanel.Choice choice = BattleGroundRewardPanel.Choice.None;
        yield return presentation.ChooseReward(Run, canChooseCard, selected => choice = selected);
        if (isCancelled()) yield break;
        if (choice == BattleGroundRewardPanel.Choice.SpecialCard && canChooseCard)
        {
            CardData card = rewards.GrantRandomSpecialCard(Run, unlocked);
            yield return presentation.RevealReward(card);
            if (isCancelled()) yield break;
        }
        else rewards.ApplyHerb(Run);
        mode.ReplaceSpecialCards(Run.SpecialCards);
    }

    private List<CardData> GetUnlockedCards()
    {
        var result = new List<CardData>();
        if (specialCardPool == null || specialCardPool.cardList == null) return result;
        foreach (Card card in specialCardPool.cardList)
        {
            CardData data = card != null ? card.CardData : null;
            if (data == null || SpecialCardResolver.IsNoUseSpecialCard(data)) continue;
            if (progress.IsSpecialCardUnlocked(data) && !result.Contains(data)) result.Add(data);
        }
        return result;
    }
}
