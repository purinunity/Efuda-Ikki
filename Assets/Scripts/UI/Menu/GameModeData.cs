using System.Collections.Generic;
using UnityEngine;

public class GameModeData
{
    public enum GameMode
    {
        KatinukiMode,      // 勝ち抜きモード
        BattleGroundMode   // バトルグラウンドモード
    }

    public GameMode Mode { get; set; }
    public int SelectedStage { get; set; } // 選択されたステージ番号
    public List<Card> SelectedSpecialCards { get; set; } // メニュー画面用の参照（表示用）

    // デフォルトコンストラクタ
    public GameModeData()
    {
        Mode = GameMode.KatinukiMode;
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
