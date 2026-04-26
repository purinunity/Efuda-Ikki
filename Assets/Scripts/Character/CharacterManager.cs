using UnityEngine;
using UnityEngine.UI;

public class CharacterManager : MonoBehaviour
{
    public GameObject Player;
    public GameObject CPU;

    [SerializeField] private Image playerImage;
    [SerializeField] private Image cpuImage;

    public Sprite PlayerSprite;
    public Sprite[] CPUSprite;
    public int CPUNuber;

    void Awake()
    {
        ResolveImages();
        ApplyPlayerSprite();
        ApplyCpuSprite(CPUNuber);
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

        CPUNuber = Mathf.Clamp(cpuCharNum, 0, CPUSprite.Length - 1);
        cpuImage.sprite = CPUSprite[CPUNuber];
        cpuImage.preserveAspect = true;
    }

    public void SetCPUImage(int cpuCharNum)
    {
        ResolveImages();
        ApplyPlayerSprite();
        ApplyCpuSprite(cpuCharNum);
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
            int index = Mathf.Clamp(CPUNuber, 0, CPUSprite.Length - 1);
            return CPUSprite[index];
        }

        return null;
    }
}
