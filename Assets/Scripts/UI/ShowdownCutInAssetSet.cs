using TMPro;
using UnityEngine;
using static HandEvaluator;

[CreateAssetMenu(fileName = "ShowdownCutInAssets", menuName = "Efuda Ikki/Showdown Cut In Assets")]
public class ShowdownCutInAssetSet : ScriptableObject
{
    public Sprite cutInBackground;
    public Sprite characterBase;
    public Sprite roleFrame;
    public Sprite winResult;
    public Sprite loseResult;
    public Sprite drawResult;
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

    public Sprite GetRoleSprite(bool isPlayer, HandRank roleRank)
    {
        switch (roleRank)
        {
            case HandRank.Isso:
                return isPlayer ? playerIsso : cpuIsso;
            case HandRank.Niso:
                return isPlayer ? playerNiso : cpuNiso;
            case HandRank.Sanju:
                return isPlayer ? playerSanju : cpuSanju;
            case HandRank.Yonju:
                return isPlayer ? playerYonju : cpuYonju;
            case HandRank.Hikari:
                return isPlayer ? playerHikari : cpuHikari;
            case HandRank.Suzi:
                return isPlayer ? playerSuzi : cpuSuzi;
            case HandRank.Tenshu:
                return isPlayer ? playerTenshu : cpuTenshu;
            case HandRank.Nanahikari:
                return isPlayer ? playerNanahikari : cpuNanahikari;
            case HandRank.Nanasuzi:
                return isPlayer ? playerNanasuzi : cpuNanasuzi;
            case HandRank.Tenshukaku:
                return isPlayer ? playerTenshukaku : cpuTenshukaku;
            case HandRank.Miezu:
            default:
                return isPlayer ? playerMiezu : cpuMiezu;
        }
    }
}
