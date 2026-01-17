using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// プレイヤーやCPUの行動を定義する抽象クラス
public abstract class Controller : MonoBehaviour
{
    // 行動を実装する抽象メソッド
    public abstract IEnumerator Act(GameState gameState, System.Action<ControllerResponse> callback);
}

// コントローラーの行動結果を格納するクラス
public class ControllerResponse
{
    public bool actionCompleted; // 行動が完了したか
    public List<Card> cardsTrash; // 捨てるカード情報
    // 必要に応じて追加情報
}
