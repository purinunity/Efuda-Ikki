using UnityEngine;

// カード情報を保持する ScriptableObject
[CreateAssetMenu(fileName = "NewCard", menuName = "ScriptableObjects/CardData")]
public class CardData : ScriptableObject
{
    public Sprite Image; // 表面画像
    public Sprite BackImage; // 裏面画像
    public Number number; // カードの数字
    public Suit suit; // カードのスート
}

// カードの数字を表す列挙型
public enum Number
{
    Joker = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13
}

// カードのスート（絵柄）を表す列挙型
public enum Suit
{
    Joker,
    Flowers,
    Birds,
    Wind,
    Moon
}