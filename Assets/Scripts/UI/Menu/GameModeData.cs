using System.Collections.Generic;
using UnityEngine;

public class GameModeData
{
    public enum GameMode
    {
        KatinukiMode,
        BattleGroundMode
    }

    public GameMode Mode { get; set; }
    public int SelectedStage { get; set; }
    public List<CardData> SelectedSpecialCardDatas { get; private set; }

    public GameModeData()
    {
        Mode = GameMode.KatinukiMode;
        SelectedStage = 0;
        SelectedSpecialCardDatas = new List<CardData>();
    }

    public GameModeData(GameMode gameMode)
    {
        Mode = gameMode;
        SelectedStage = 0;
        SelectedSpecialCardDatas = new List<CardData>();
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
