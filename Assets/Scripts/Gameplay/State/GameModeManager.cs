using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    private static GameModeData gameModeData;

    private void Awake()
    {
        // シングルトンパターン：シーン間で保持
        if (gameModeData == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    // ゲームモードデータを設定
    public static void SetGameModeData(GameModeData data)
    {
        gameModeData = data;
    }

    // ゲームモードデータを取得
    public static GameModeData GetGameModeData()
    {
        if (gameModeData == null)
        {
            gameModeData = new GameModeData();
        }
        return gameModeData;
    }

    // ゲームモードデータをリセット
    public static void ResetGameModeData()
    {
        gameModeData = null;
    }
}
