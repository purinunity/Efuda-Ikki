using System.Collections.Generic;
using UnityEngine;

public class GameModeData
{
    public enum GameMode
    {
        IkkiMode = 0,
        BattleGroundMode = 1,
        KatinukiMode = IkkiMode,
        KachinukiMode = BattleGroundMode
    }

    public GameMode Mode { get; set; }
    public int SelectedStage { get; set; }
    public int CurrentLevel { get; private set; }
    public int CurrentWinStreak { get; private set; }
    public List<CardData> SelectedSpecialCardDatas { get; private set; }

    public GameModeData()
    {
        Mode = GameMode.IkkiMode;
        SelectedStage = 0;
        CurrentLevel = 1;
        CurrentWinStreak = 0;
        SelectedSpecialCardDatas = new List<CardData>();
    }

    public GameModeData(GameMode gameMode)
    {
        Mode = gameMode;
        SelectedStage = 0;
        CurrentLevel = 1;
        CurrentWinStreak = 0;
        SelectedSpecialCardDatas = new List<CardData>();
    }

    public void SetCurrentLevel(int level)
    {
        CurrentLevel = Mathf.Clamp(level, 1, CpuLevelCatalog.MaxLevel);
        SelectedStage = CurrentLevel - 1;
    }

    public void AdvanceLevel(bool wrap)
    {
        int nextLevel = CurrentLevel + 1;
        if (nextLevel > CpuLevelCatalog.MaxLevel)
        {
            nextLevel = wrap ? 1 : CpuLevelCatalog.MaxLevel;
        }

        SetCurrentLevel(nextLevel);
    }

    public void IncrementWinStreak()
    {
        CurrentWinStreak++;
    }

    public void ResetWinStreak()
    {
        CurrentWinStreak = 0;
    }

    public bool AddSpecialCard(CardData cardData, int maxCount = 4)
    {
        if (cardData == null) return false;
        if (SelectedSpecialCardDatas.Contains(cardData)) return false;
        if (SelectedSpecialCardDatas.Count >= maxCount) return false;

        SelectedSpecialCardDatas.Add(cardData);
        return true;
    }

    public bool RemoveSpecialCard(CardData cardData)
    {
        if (cardData == null) return false;
        return SelectedSpecialCardDatas.Remove(cardData);
    }

    public void ClearSpecialCards()
    {
        SelectedSpecialCardDatas.Clear();
    }
}
