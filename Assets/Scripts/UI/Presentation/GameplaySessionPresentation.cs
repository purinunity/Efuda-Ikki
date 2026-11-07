using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameplaySessionPresentation : ISessionPresentation
{
    private readonly GameplayUiReferences references;
    private readonly CharacterManager characters;
    private readonly ShowdownPresentationService showdown;
    private readonly Action<ShowdownCutInPopup> popupChanged;
    private readonly Action<MatchResultPanel> resultChanged;
    private readonly MatchResultPanel initialResult;
    private HandRevealPanel handReveal;
    private CoinTossPanel coinToss;
    private MatchResultPanel resultPanel;
    private BattleGroundRewardPanel reward;

    public bool HandRevealConfirmed => handReveal == null || handReveal.WasConfirmed;
    public bool HasRewardPanel => references != null && references.BattleGroundRewardPanel != null;

    public GameplaySessionPresentation(MonoBehaviour owner, GameState state, UIManager ui,
        CharacterManager characters, Cards specials, ShowdownCutInPopup popup,
        MatchResultPanel result, GameplayUiReferences references,
        Action<ShowdownCutInPopup> popupChanged, Action<MatchResultPanel> resultChanged)
    {
        this.references = references;
        this.characters = characters;
        initialResult = result;
        this.popupChanged = popupChanged;
        this.resultChanged = resultChanged;
        resultPanel = references != null ? references.MatchResultPanel : result;
        coinToss = references != null ? references.CoinTossPanel : null;
        handReveal = references != null ? references.HandRevealPanel : null;
        reward = references != null ? references.BattleGroundRewardPanel : null;
        showdown = new ShowdownPresentationService(owner, state, ui, characters,
            references != null ? references.ShowdownCutInPopup : popup, specials);
    }

    public IEnumerator CoinToss(int parentIndex)
    {
        coinToss = references != null ? references.CoinTossPanel : null;
        if (coinToss != null) yield return coinToss.Show(parentIndex);
    }

    public IEnumerator HandReveal()
    {
        handReveal = references != null ? references.HandRevealPanel : null;
        if (handReveal != null) yield return handReveal.WaitForConfirmation();
    }

    public IEnumerator Showdown(SpecialCardResolver.ShowdownResult result)
    {
        yield return showdown.Play(result);
        popupChanged?.Invoke(showdown.CurrentPopup);
    }

    public IEnumerator ShowResult(int level, IReadOnlyList<MatchRoundResult> results,
        bool playerWon, string buttonLabel, string summary)
    {
        resultPanel = references != null ? references.MatchResultPanel : initialResult;
        resultChanged?.Invoke(resultPanel);
        if (resultPanel == null) yield break;
        resultPanel.SetCharacterSprites(characters != null ? characters.GetPlayerSprite() : null,
            characters != null ? characters.GetCpuSprite() : null);
        yield return resultPanel.Show(level, results, playerWon, buttonLabel, summary);
    }

    public IEnumerator ChooseReward(BattleGroundRunState run, bool canChooseCard,
        Action<BattleGroundRewardPanel.Choice> selected)
    {
        reward = references != null ? references.BattleGroundRewardPanel : null;
        if (reward != null) yield return reward.Show(run.PlayerLife, run.WinStreak, canChooseCard, selected);
    }

    public IEnumerator RevealReward(CardData card)
    {
        if (reward != null) yield return reward.RevealCard(card);
    }

    public void CancelHandReveal()
    {
        if (handReveal != null) handReveal.CancelDisplay();
    }

    public void Cancel()
    {
        if (resultPanel != null) resultPanel.CancelDisplay();
        if (reward != null) reward.CancelDisplay();
        if (coinToss != null) coinToss.CancelDisplay();
        CancelHandReveal();
        if (showdown.CurrentPopup != null) showdown.CurrentPopup.CancelDisplay();
    }
}
