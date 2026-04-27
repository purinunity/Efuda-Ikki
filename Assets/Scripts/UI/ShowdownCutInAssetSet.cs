using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "ShowdownCutInAssets", menuName = "Efuda Ikki/Showdown Cut In Assets")]
public class ShowdownCutInAssetSet : ScriptableObject
{
    public Sprite cutInBackground;
    public Sprite characterBase;
    public Sprite roleFrame;
    public TMP_FontAsset textFont;
    public Vector4 cpuRoleSpriteRect = new Vector4(208f, 16f, 624f, 96f);
    public Vector4 playerRoleSpriteRect = new Vector4(208f, 464f, 624f, 96f);

    public Sprite playerMiezu;
    public Sprite playerIsso;
    public Sprite playerNiso;
    public Sprite playerSanju;
    public Sprite playerYonju;
    public Sprite playerTenshu;
    public Sprite playerSuzi;
    public Sprite playerHikari;
    public Sprite playerNanasuzi;
    public Sprite playerNanahikari;
    public Sprite playerTenshukaku;

    public Sprite cpuMiezu;
    public Sprite cpuIsso;
    public Sprite cpuNiso;
    public Sprite cpuSanju;
    public Sprite cpuYonju;
    public Sprite cpuTenshu;
    public Sprite cpuSuzi;
    public Sprite cpuHikari;
    public Sprite cpuNanasuzi;
    public Sprite cpuNanahikari;
    public Sprite cpuTenshukaku;

    public Sprite GetRoleSprite(bool isPlayer, string roleName)
    {
        switch (roleName)
        {
            case "一双":
                return isPlayer ? playerIsso : cpuIsso;
            case "二双":
                return isPlayer ? playerNiso : cpuNiso;
            case "三珠":
                return isPlayer ? playerSanju : cpuSanju;
            case "四珠":
                return isPlayer ? playerYonju : cpuYonju;
            case "天守":
                return isPlayer ? playerTenshu : cpuTenshu;
            case "筋":
                return isPlayer ? playerSuzi : cpuSuzi;
            case "光":
                return isPlayer ? playerHikari : cpuHikari;
            case "七筋":
                return isPlayer ? playerNanasuzi : cpuNanasuzi;
            case "七光":
                return isPlayer ? playerNanahikari : cpuNanahikari;
            case "天守閣":
                return isPlayer ? playerTenshukaku : cpuTenshukaku;
            case "不見":
            default:
                return isPlayer ? playerMiezu : cpuMiezu;
        }
    }
}
