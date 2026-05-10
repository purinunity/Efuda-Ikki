using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;

// プレイヤーの現在の役を表示するクラス
public class PlayerRole2 : MonoBehaviour
{
    private TextMeshProUGUI roleText; // 役表示用テキストコンポーネント
    private Coroutine rouletteCoroutine;
    // 演出中かどうか（外部から待機に使える）
    public bool IsAnimating => rouletteCoroutine != null;

    Animator animator;

    // ルーレットで表示する候補役一覧（表示順などは任意）
    private static readonly string[] RoleCandidates = HandRoleCatalog.GetDisplayNames();

    private void Awake()
    {
        if (roleText == null)
        {
            roleText = GetComponent<TextMeshProUGUI>();
            animator = GetComponent<Animator>();
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
        rouletteCoroutine = StartCoroutine(RoleRoulette(name));
    }

    private void StopRouletteIfRunning()
    {
        if (rouletteCoroutine != null)
        {
            StopCoroutine(rouletteCoroutine);
            rouletteCoroutine = null;
        }
    }

    private IEnumerator RoleRoulette(string finalRole)
    {
        animator.SetTrigger("Role");

        roleText.text = finalRole;
        rouletteCoroutine = null;

        yield break; 
    }

    private void OnDestroy()
    {
        StopRouletteIfRunning();
    }
}
