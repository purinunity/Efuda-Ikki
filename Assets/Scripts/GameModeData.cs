using System.Collections.Generic;
using UnityEngine;

public class GameModeData
{
    public enum GameMode
    {
        KinouAriMode,      // 勝ち抜きモード
        BattleGroundMode   // バトルグラウンドモード
    }

    public GameMode Mode { get; set; }
    public int SelectedStage { get; set; } // 0-8（ステージ1-9）
    public List<Card> SelectedSpecialCards { get; set; }

    // デフォルトコンストラクタ
    public GameModeData()
    {
        Mode = GameMode.KinouAriMode;
        SelectedStage = 0;
        SelectedSpecialCards = new List<Card>();
    }

    // ゲームモード指定コンストラクタ
    public GameModeData(GameMode gameMode)
    {
        Mode = gameMode;
        SelectedStage = 0;
        SelectedSpecialCards = new List<Card>();
    }
}
