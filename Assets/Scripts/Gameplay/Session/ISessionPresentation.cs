using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>Waits for authored views. Contains no match rules or progress writes.</summary>
public interface ISessionPresentation
{
    bool HandRevealConfirmed { get; }
    bool HasRewardPanel { get; }
    IEnumerator CoinToss(int parentIndex);
    IEnumerator HandReveal();
    IEnumerator Showdown(SpecialCardResolver.ShowdownResult result);
    IEnumerator ShowResult(int level, IReadOnlyList<MatchRoundResult> results,
        bool playerWon, string buttonLabel, string summary);
    IEnumerator ChooseReward(BattleGroundRunState run, bool canChooseCard,
        Action<BattleGroundRewardPanel.Choice> selected);
    IEnumerator RevealReward(CardData card);
    void CancelHandReveal();
    void Cancel();
}
