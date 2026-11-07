using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class CpuCharacterSettings
{
    [Header("Character")]
    [Tooltip("Matches the selected stage number / CPU character index.")]
    [SerializeField, Min(0)] private int characterIndex;

    [Header("Difficulty")]
    [Tooltip("Decision and pacing settings for this CPU character.")]
    [SerializeField] private CpuDifficultySettings difficultySettings = new CpuDifficultySettings();

    [Header("Special Cards")]
    [Tooltip("When enabled, this character uses the configured special card list instead of random cards.")]
    [SerializeField] private bool useConfiguredSpecialCards = true;
    [Tooltip("Special cards held by this CPU character when configured cards are enabled.")]
    [SerializeField] private List<CardData> specialCardDatas = new List<CardData>();
    [Tooltip("Fallback random special-card count when configured cards are disabled or missing.")]
    [SerializeField, Min(0)] private int randomSpecialCardCount = 4;

    public int CharacterIndex => characterIndex;
    public CpuDifficultySettings DifficultySettings => difficultySettings ?? new CpuDifficultySettings();
    public bool UseConfiguredSpecialCards => useConfiguredSpecialCards;
    public List<CardData> SpecialCardDatas => specialCardDatas;
    public int RandomSpecialCardCount => randomSpecialCardCount;
}
