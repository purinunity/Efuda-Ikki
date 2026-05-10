using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;

// プレイヤーの現在の役を表示するクラス
public class PlayerRole : MonoBehaviour
{
    private TextMeshProUGUI roleText; // 役表示用テキストコンポーネント
    private Coroutine rouletteCoroutine;
    // 演出中かどうか（外部から待機に使える）
    public bool IsAnimating => rouletteCoroutine != null;

    // ルーレットで表示する候補役一覧（表示順などは任意）
    private static readonly string[] RoleCandidates = HandRoleCatalog.GetDisplayNames();

    // 初期化処理：必要なコンポーネント参照を取得する
    private void Awake()
    {
        if (roleText == null)
        {
            roleText = GetComponent<TextMeshProUGUI>();
        }
    }

    // SetRole: 名前を即時表示するか、duration>0でルーレット演出を行って最終的に正しい役を表示する
    internal void SetRole(string name, float duration = 0f)
    {
        if (roleText == null)
        {
            roleText = GetComponent<TextMeshProUGUI>();
        }

        // 名前の変化がない場合は演出しない
        string finalName = name ?? string.Empty;
        string currentName = roleText.text ?? string.Empty;
        if (currentName == finalName)
        {
            // 演出中であれば停止して安定表示にする
            StopRouletteIfRunning();
            roleText.text = finalName;
            return;
        }

        // 空文字が渡された場合は演出を行わず即時表示
        if (string.IsNullOrEmpty(finalName))
        {
            StopRouletteIfRunning();
            roleText.text = finalName;
            return;
        }

        // 即時表示
        if (duration <= 0f)
        {
            StopRouletteIfRunning();
            roleText.text = finalName;
            return;
        }

        // 既に演出中なら停止して再開
        StopRouletteIfRunning();
        rouletteCoroutine = StartCoroutine(RoleRoulette(name, duration));
    }

    // ルーレット演出が実行中であれば停止させ、状態をクリアする
    private void StopRouletteIfRunning()
    {
        if (rouletteCoroutine != null)
        {
            StopCoroutine(rouletteCoroutine);
            rouletteCoroutine = null;
        }
    }

    // ルーレット演出コルーチン：候補を順に（ランダムに）表示し、指定時間の経過後に最終的な役を表示する
    private IEnumerator RoleRoulette(string finalRole, float duration)
    {
        float elapsed = 0f;
        // ルーレットは徐々に高速化して最後に停止
        while (elapsed < duration)
        {
            // 表示候補からランダムに選ぶ（最後は正解を出すため敢えて含めても良い）
            string candidate = RoleCandidates[UnityEngine.Random.Range(0, RoleCandidates.Length)];
            roleText.text = candidate;

            // 高速化する間隔（開始はやや遅め、終了に向けて短くする）
            float t = Mathf.Clamp01(elapsed / duration);
            float interval = Mathf.Lerp(0.15f, 0.03f, t);

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        roleText.text = finalRole;
        rouletteCoroutine = null;
    }

    // オブジェクト破棄時の後処理：演出が残っていれば停止する
    private void OnDestroy()
    {
        StopRouletteIfRunning();
    }
}
