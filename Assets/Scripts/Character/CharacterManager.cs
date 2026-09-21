using System;
using EfudaIkki.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class CharacterManager : MonoBehaviour
{
    public GameObject Player;
    public GameObject CPU;

    [SerializeField] private Image playerImage;
    [SerializeField] private Image cpuImage;

    public Sprite PlayerSprite;
    public Sprite[] CPUSprite;
    [FormerlySerializedAs("CPUNuber")]
    [SerializeField] private int cpuNumber;

    public int CpuNumber
    {
        get => cpuNumber;
        set => cpuNumber = value;
    }

    [Obsolete("Use CpuNumber instead.")]
    public int CPUNuber
    {
        get => CpuNumber;
        set => CpuNumber = value;
    }

    void Awake()
    {
        ResolveImages();
        ApplyPlayerSprite();
        ApplyCpuSprite(CpuNumber);
    }

    private void ResolveImages()
    {
        if (playerImage == null && Player != null)
        {
            playerImage = Player.GetComponent<Image>();
            if (playerImage == null)
            {
                playerImage = Player.GetComponentInChildren<Image>(true);
            }
        }

        if (cpuImage == null && CPU != null)
        {
            cpuImage = CPU.GetComponent<Image>();
            if (cpuImage == null)
            {
                cpuImage = CPU.GetComponentInChildren<Image>(true);
            }
        }
    }

    private void ApplyPlayerSprite()
    {
        if (playerImage != null)
        {
            playerImage.sprite = PlayerSprite;
            playerImage.preserveAspect = true;
        }
    }

    private void ApplyCpuSprite(int cpuCharNum)
    {
        if (cpuImage == null || CPUSprite == null || CPUSprite.Length == 0)
        {
            return;
        }

        CpuNumber = Mathf.Clamp(cpuCharNum, 0, CPUSprite.Length - 1);
        cpuImage.sprite = CPUSprite[CpuNumber];
        cpuImage.preserveAspect = true;
    }

    public void SetCPUImage(int cpuCharNum)
    {
        ResolveImages();
        ApplyPlayerSprite();
        ApplyCpuSprite(cpuCharNum);
    }

    public void ApplyBattleGroundSprites(Sprite playerSprite, Sprite cpuSprite)
    {
        ResolveImages();
        if (playerImage != null && playerSprite != null)
        {
            playerImage.sprite = playerSprite;
            playerImage.preserveAspect = true;
        }
        if (cpuImage != null && cpuSprite != null)
        {
            cpuImage.sprite = cpuSprite;
            cpuImage.preserveAspect = true;
        }
    }

    public void RestoreModeSprites()
    {
        ApplyPlayerSprite();
        ApplyCpuSprite(CpuNumber);
    }

    public bool SetCPUCharacter(string characterId)
    {
        if (!StageCharacterCatalog.TryGet(characterId, out StageCharacterCatalog.Entry character))
        {
            Debug.LogWarning($"Unknown CPU character ID: {characterId}");
            return false;
        }

        SetCPUImage(character.Level - 1);
        return true;
    }

    public string GetCpuCharacterId()
    {
        StageCharacterCatalog.Entry character = StageCharacterCatalog.GetByLevel(CpuNumber + 1);
        return character?.Id;
    }

    public Sprite GetPlayerSprite()
    {
        ResolveImages();
        if (playerImage != null && playerImage.sprite != null)
        {
            return playerImage.sprite;
        }

        return PlayerSprite;
    }

    public Sprite GetCpuSprite()
    {
        ResolveImages();
        if (cpuImage != null && cpuImage.sprite != null)
        {
            return cpuImage.sprite;
        }

        if (CPUSprite != null && CPUSprite.Length > 0)
        {
            int index = Mathf.Clamp(CpuNumber, 0, CPUSprite.Length - 1);
            return CPUSprite[index];
        }

        return null;
    }
}
