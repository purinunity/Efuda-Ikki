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
        PlayerSpriteRenderer = Player != null ? Player.GetComponent<SpriteRenderer>() : null;
        CPUSpriteRenderer = CPU != null ? CPU.GetComponent<SpriteRenderer>() : null;

        if (PlayerSpriteRenderer != null)
        {
            PlayerSpriteRenderer.sprite = PlayerSprite;
        }

        if (CPUSpriteRenderer != null && CPUSprite != null && CPUSprite.Length > 0)
        {
            CPUNuber = Mathf.Clamp(CPUNuber, 0, CPUSprite.Length - 1);
            CPUSpriteRenderer.sprite = CPUSprite[CPUNuber];
        }
    }

    public void SetCPUImage(int cpuCharNum)
    {
        if (PlayerSpriteRenderer != null)
        {
            PlayerSpriteRenderer.sprite = PlayerSprite;
        }

        if (CPUSpriteRenderer == null || CPUSprite == null || CPUSprite.Length == 0)
        {
            return;
        }

        CPUNuber = Mathf.Clamp(cpuCharNum, 0, CPUSprite.Length - 1);
        CPUSpriteRenderer.sprite = CPUSprite[CPUNuber];
    }

    public Sprite GetPlayerSprite()
    {
        if (PlayerSpriteRenderer != null && PlayerSpriteRenderer.sprite != null)
        {
            return PlayerSpriteRenderer.sprite;
        }

        return PlayerSprite;
    }

    public Sprite GetCpuSprite()
    {
        if (CPUSpriteRenderer != null && CPUSpriteRenderer.sprite != null)
        {
            return CPUSpriteRenderer.sprite;
        }

        if (CPUSprite != null && CPUSprite.Length > 0)
        {
            int index = Mathf.Clamp(CPUNuber, 0, CPUSprite.Length - 1);
            return CPUSprite[index];
        }

        return null;
    }
}
