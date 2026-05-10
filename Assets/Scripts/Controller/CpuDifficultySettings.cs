using UnityEngine;

[System.Serializable]
public sealed class CpuDifficultySettings
{
    [Header("Decision Accuracy")]
    [Tooltip("1 means the CPU follows its planned exchange decision. 0 means fully random exchange.")]
    [SerializeField, Range(0f, 1f)] private float exchangeDecisionStrength = 1f;
    [Tooltip("1 means the CPU chooses the best evaluated special card. 0 means random special-card choice.")]
    [SerializeField, Range(0f, 1f)] private float specialCardDecisionStrength = 1f;

    [Header("Special Card Evaluation")]
    [Tooltip("Utility added when a special-card choice makes the CPU win.")]
    [SerializeField, Min(0)] private int specialCardWinUtility = 10000;
    [Tooltip("Utility used when a special-card choice makes the round a draw.")]
    [SerializeField] private int specialCardDrawUtility = 0;
    [Tooltip("How strongly dealt or received damage affects special-card choice.")]
    [SerializeField, Min(0)] private int specialCardDamageUtilityWeight = 1;

    [Header("Pacing")]
    [Tooltip("Delay before the CPU acts.")]
    [SerializeField, Min(0f)] private float thinkingDelaySeconds = 1f;

    public float ExchangeDecisionStrength => Mathf.Clamp01(exchangeDecisionStrength);
    public float SpecialCardDecisionStrength => Mathf.Clamp01(specialCardDecisionStrength);
    public int SpecialCardWinUtility => Mathf.Max(0, specialCardWinUtility);
    public int SpecialCardDrawUtility => specialCardDrawUtility;
    public int SpecialCardDamageUtilityWeight => Mathf.Max(0, specialCardDamageUtilityWeight);
    public float ThinkingDelaySeconds => Mathf.Max(0f, thinkingDelaySeconds);

    public CpuDifficultySettings()
    {
    }

    public CpuDifficultySettings(CpuDifficultySettings source)
    {
        CopyFrom(source);
    }

    public CpuDifficultySettings Clone()
    {
        return new CpuDifficultySettings(this);
    }

    public void CopyFrom(CpuDifficultySettings source)
    {
        if (source == null)
        {
            return;
        }

        exchangeDecisionStrength = source.ExchangeDecisionStrength;
        specialCardDecisionStrength = source.SpecialCardDecisionStrength;
        specialCardWinUtility = source.SpecialCardWinUtility;
        specialCardDrawUtility = source.SpecialCardDrawUtility;
        specialCardDamageUtilityWeight = source.SpecialCardDamageUtilityWeight;
        thinkingDelaySeconds = source.ThinkingDelaySeconds;
    }

    public void SetDecisionStrengths(float exchangeStrength, float specialCardStrength)
    {
        exchangeDecisionStrength = Mathf.Clamp01(exchangeStrength);
        specialCardDecisionStrength = Mathf.Clamp01(specialCardStrength);
    }
}
