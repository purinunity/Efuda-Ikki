using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    // チュートリアルが完了してるかどうか
    public bool tutorial_done;
    // 一騎モードのカウント
    public int main_count;
    //　バトルグラウンドモードのカウント（ハイスコア）
    public int battle_ground_count;
    // seの音量（０～１０）
    public int se_volume;
    // bgmの音量（０～１０）
    public int bgm_volume;

    // コンストラクタ：new SaveData() した時点でここに書いた値が初期値として入る
    // （セーブファイルがまだ存在しない＝初回起動時に使われる値）
    public SaveData()
    {
        tutorial_done = false;      // 最初はチュートリアル未完了
        main_count = 0;             // 一騎モードのカウントは0から
        battle_ground_count = 0;    // ハイスコアも0から
        se_volume = 5;             // SE音量は中間値(5)からスタート
        bgm_volume = 5;            // BGM音量も中間値(5)からスタート
    }
}

public class SaveLoadManager : MonoBehaviour
{
    // 保存先のパス
    private static readonly string SavePath =
        Path.Combine(Application.persistentDataPath, "savedata.json");
    // Saveメソッド
    public static void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }
    //　Loadメソッド
    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new SaveData(); // デフォルト値
        }

        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<SaveData>(json);
    }
    // セーブデータの存在を確認するメソッド
    public static bool Exists()
    {
        return File.Exists(SavePath);
    }
    // セーブデータ削除メソッド
    public static void Delete()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
    }  
}

// ==========================================
// 【基本的な使い方】
// ==========================================
//
// ① ゲーム起動時（例：GameManagerのStart()など）に1回だけロードする
//    セーブファイルが存在しない場合は、SaveDataのコンストラクタで
//    設定した初期値（tutorial_done=false, 音量=10 など）が自動的に入る
//
//    private SaveData saveData;
//
//    void Start()
//    {
//        saveData = SaveLoadManager.Load();
//    }
//
// ② ロードしたデータはメモリ上の変数（saveData）として持ち回り、
//    ゲーム中はこの変数を書き換えていく
//    ※この時点ではまだファイルには反映されていない
//
//    void OnTutorialCleared()
//    {
//        saveData.tutorial_done = true;
//    }
//
//    void OnMainModeClear()
//    {
//        saveData.main_count++;
//    }
//
//    void OnSeVolumeChanged(int newVolume)
//    {
//        saveData.se_volume = newVolume;
//    }
//
// ③ 変更をファイルに反映したいタイミングでSave()を呼ぶ
//    （例：設定変更時、ゲームクリア時、タイトルに戻る時など）
//
//    SaveLoadManager.Save(saveData);
//
// ④ セーブデータの有無を確認したい場合（例：タイトル画面で
//    「つづきから」ボタンを表示するかどうかの判定など）
//
//    if (SaveLoadManager.Exists())
//    {
//        // 「つづきから」ボタンを表示
//    }
//
// ⑤ セーブデータを削除したい場合（デバッグ時や、
//    「データ削除」機能を実装する場合など）
//
//    SaveLoadManager.Delete();
//
// ==========================================