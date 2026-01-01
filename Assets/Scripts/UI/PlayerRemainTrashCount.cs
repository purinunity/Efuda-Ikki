using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerRemainTrashCount : MonoBehaviour
{
    private TextMeshProUGUI playerRemainTrashCount;

    private void Awake()
    {
        if (playerRemainTrashCount == null)
        {
            playerRemainTrashCount = GetComponent<TextMeshProUGUI>();
        }
    }

    internal void UpdateRemainTrashCount(int remainTrashCount)
    {
        playerRemainTrashCount.text = "残 " + remainTrashCount;
    }
}
