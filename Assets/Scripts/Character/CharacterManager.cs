using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class CharacterManager : MonoBehaviour
{
    public GameObject Player;
    public GameObject CPU;

    SpriteRenderer PlayerSpriteRenderer;
    SpriteRenderer CPUSpriteRenderer;

    public Sprite PlayerSprite;
    public Sprite[] CPUSprite;
    public int CPUNuber;

    void Awake()
    {
        PlayerSpriteRenderer = Player.GetComponent<SpriteRenderer>();
        CPUSpriteRenderer = CPU.GetComponent<SpriteRenderer>();

        PlayerSpriteRenderer.sprite = PlayerSprite;
        CPUSpriteRenderer.sprite = CPUSprite[CPUNuber];
    }

    public void SetCPUImage(int cpuCharNum)
    {
        PlayerSpriteRenderer.sprite = PlayerSprite;
        CPUSpriteRenderer.sprite = CPUSprite[cpuCharNum];
    }
}
