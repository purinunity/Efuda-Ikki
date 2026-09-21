using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class BattleGroundRunState
{
    public const int MaxLife = 100;
    public const int MaxSpecialCards = 4;

    private readonly List<CardData> specialCards = new List<CardData>();

    public int PlayerLife { get; private set; } = MaxLife;
    public int WinStreak { get; private set; }
    public IReadOnlyList<CardData> SpecialCards => specialCards;

    public BattleGroundRunState(IEnumerable<CardData> initialCards)
    {
        if (initialCards == null) return;
        foreach (CardData card in initialCards)
        {
            AddUnique(card);
            if (specialCards.Count >= MaxSpecialCards) break;
        }
    }

    public void CaptureVictory(PlayerState player)
    {
        if (player != null)
        {
            PlayerLife = Mathf.Clamp(player.LifePoints, 0, MaxLife);
            var used = new HashSet<CardData>(
                player.UsedSpecialCards.Where(card => card != null).Select(card => card.CardData));
            specialCards.RemoveAll(card => card == null || used.Contains(card));
        }
        WinStreak++;
    }

    public int Heal(int amount)
    {
        PlayerLife = Mathf.Clamp(PlayerLife + Mathf.Max(0, amount), 0, MaxLife);
        return PlayerLife;
    }

    public bool CanAddSpecialCard => specialCards.Count < MaxSpecialCards;

    public bool AddSpecialCard(CardData card)
    {
        return CanAddSpecialCard && AddUnique(card);
    }

    private bool AddUnique(CardData card)
    {
        if (card == null || specialCards.Contains(card)) return false;
        specialCards.Add(card);
        return true;
    }
}
