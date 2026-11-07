using UnityEngine;

// 複数のカード情報を保持する ScriptableObject
[CreateAssetMenu(fileName = "NewCards", menuName = "ScriptableObjects/CardsData")]
public class CardsData : ScriptableObject
{
    public CardData[] cards; // 複数のカードデータ配列
}