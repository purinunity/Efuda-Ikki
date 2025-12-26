using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerCount : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TextMeshProUGUI playerCount;

    void Update()
    {
        playerCount.text = "残 " + gameManager.remainingNumber;
    }
}
