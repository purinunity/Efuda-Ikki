using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Efuda Ikki/Battle Ground Visual Assets")]
public sealed class BattleGroundVisualAssets : ScriptableObject
{
    [Serializable]
    public sealed class SpecialCardSprite
    {
        public SpecialCardResolver.SpecialCardId id;
        public Sprite sprite;
    }

    public Sprite battleBackground;
    public Sprite roundCounterFrame;
    public Sprite normalCardBack;
    public Sprite specialCardBack;
    public Sprite noSpecialCard;
    public Sprite playerCharacter;
    public Sprite[] cpuCharacters = new Sprite[9];
    public SpecialCardSprite[] specialCards = Array.Empty<SpecialCardSprite>();

    public Sprite GetSpecialCard(SpecialCardResolver.SpecialCardId id)
    {
        if (specialCards == null) return null;
        foreach (SpecialCardSprite binding in specialCards)
        {
            if (binding != null && binding.id == id) return binding.sprite;
        }
        return null;
    }
}
