using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerRemainTrashCount : MonoBehaviour
{
    private TextMeshProUGUI playerRemainTrashCount;

    // 初期化：TextMeshPro コンポーネント参照を取得する
    private void Awake()
    {
        if (playerRemainTrashCount == null)
        {
            playerRemainTrashCount = GetComponent<TextMeshProUGUI>();
        }
    }

    // 残り手札交換回数表示を更新する
    internal void UpdateRemainTrashCount(int remainTrashCount)
    {
        playerRemainTrashCount.text = "残" + remainTrashCount;
    }
}
