using System.Collections.Generic;
using UnityEngine;
using static HandEvaluator;

public partial class ShowdownCutInPopup
{
    /// <summary>
    /// Immutable presentation data consumed by the cut-in view and animation sequence.
    /// Kept nested to preserve the existing public API used by scene-facing code.
    /// </summary>
    public sealed class Data
    {
        public sealed class EffectStepData
        {
            public int OwnerPlayerId { get; }
            public Sprite SpecialCardSprite { get; }
            public string EffectName { get; }
            public string Message { get; }
            public bool WasSealed { get; }
            public string PlayerRoleName { get; }
            public string CpuRoleName { get; }
            public HandRank PlayerRoleRank { get; }
            public HandRank CpuRoleRank { get; }
            public int PlayerScore { get; }
            public int CpuScore { get; }

            public EffectStepData(
                int ownerPlayerId,
                Sprite specialCardSprite,
                string effectName,
                string message,
                bool wasSealed,
                string playerRoleName,
                string cpuRoleName,
                HandRank playerRoleRank,
                HandRank cpuRoleRank,
                int playerScore,
                int cpuScore)
            {
                OwnerPlayerId = ownerPlayerId;
                SpecialCardSprite = specialCardSprite;
                EffectName = effectName;
                Message = message;
                WasSealed = wasSealed;
                PlayerRoleName = playerRoleName;
                CpuRoleName = cpuRoleName;
                PlayerRoleRank = playerRoleRank;
                CpuRoleRank = cpuRoleRank;
                PlayerScore = playerScore;
                CpuScore = cpuScore;
            }
        }

        public Sprite PlayerCharacterSprite { get; }
        public Sprite CpuCharacterSprite { get; }
        public string PlayerBaseRoleName { get; }
        public string CpuBaseRoleName { get; }
        public HandRank PlayerBaseRoleRank { get; }
        public HandRank CpuBaseRoleRank { get; }
        public int PlayerBaseScore { get; }
        public int CpuBaseScore { get; }
        public string PlayerFinalRoleName { get; }
        public string CpuFinalRoleName { get; }
        public HandRank PlayerFinalRoleRank { get; }
        public HandRank CpuFinalRoleRank { get; }
        public int PlayerFinalScore { get; }
        public int CpuFinalScore { get; }
        public IReadOnlyList<Sprite> PlayerCardSprites { get; }
        public IReadOnlyList<Sprite> CpuCardSprites { get; }
        public IReadOnlyList<bool> PlayerCardHighlights { get; }
        public IReadOnlyList<bool> CpuCardHighlights { get; }
        public Sprite PlayerSpecialCardSprite { get; }
        public Sprite CpuSpecialCardSprite { get; }
        public IReadOnlyList<EffectStepData> EffectSteps { get; }
        public int WinnerIndex { get; }
        public int Damage { get; }
        public int PlayerLifeBefore { get; }
        public int CpuLifeBefore { get; }
        public int PlayerLifeAfter { get; }
        public int CpuLifeAfter { get; }
        public bool IsMatchDecided => PlayerLifeAfter <= 0 || CpuLifeAfter <= 0;
        public bool IsDraw => WinnerIndex < 0;

        public Data(
            Sprite playerCharacterSprite,
            Sprite cpuCharacterSprite,
            string playerBaseRoleName,
            string cpuBaseRoleName,
            HandRank playerBaseRoleRank,
            HandRank cpuBaseRoleRank,
            int playerBaseScore,
            int cpuBaseScore,
            string playerFinalRoleName,
            string cpuFinalRoleName,
            HandRank playerFinalRoleRank,
            HandRank cpuFinalRoleRank,
            int playerFinalScore,
            int cpuFinalScore,
            IReadOnlyList<Sprite> playerCardSprites,
            IReadOnlyList<Sprite> cpuCardSprites,
            IReadOnlyList<bool> playerCardHighlights,
            IReadOnlyList<bool> cpuCardHighlights,
            Sprite playerSpecialCardSprite,
            Sprite cpuSpecialCardSprite,
            IReadOnlyList<EffectStepData> effectSteps,
            int winnerIndex,
            int damage,
            int playerLifeBefore,
            int cpuLifeBefore,
            int playerLifeAfter,
            int cpuLifeAfter)
        {
            PlayerCharacterSprite = playerCharacterSprite;
            CpuCharacterSprite = cpuCharacterSprite;
            PlayerBaseRoleName = playerBaseRoleName;
            CpuBaseRoleName = cpuBaseRoleName;
            PlayerBaseRoleRank = playerBaseRoleRank;
            CpuBaseRoleRank = cpuBaseRoleRank;
            PlayerBaseScore = playerBaseScore;
            CpuBaseScore = cpuBaseScore;
            PlayerFinalRoleName = playerFinalRoleName;
            CpuFinalRoleName = cpuFinalRoleName;
            PlayerFinalRoleRank = playerFinalRoleRank;
            CpuFinalRoleRank = cpuFinalRoleRank;
            PlayerFinalScore = playerFinalScore;
            CpuFinalScore = cpuFinalScore;
            PlayerCardSprites = playerCardSprites;
            CpuCardSprites = cpuCardSprites;
            PlayerCardHighlights = playerCardHighlights;
            CpuCardHighlights = cpuCardHighlights;
            PlayerSpecialCardSprite = playerSpecialCardSprite;
            CpuSpecialCardSprite = cpuSpecialCardSprite;
            EffectSteps = effectSteps;
            WinnerIndex = winnerIndex;
            Damage = damage;
            PlayerLifeBefore = playerLifeBefore;
            CpuLifeBefore = cpuLifeBefore;
            PlayerLifeAfter = playerLifeAfter;
            CpuLifeAfter = cpuLifeAfter;
        }
    }
}
