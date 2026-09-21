using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ShowdownCutInPopup
{
    private const float ReferenceWidth = 1024f;
    private const float ReferenceHeight = 576f;
    private const int HandCardCount = 5;
    private const int CommonCardCount = 2;
    private const int ShowdownCardCount = 7;
    private static readonly Color LifeDeductionColor = new Color32(0xB4, 0x24, 0x34, 0xFF);
    private static readonly Vector4 CpuCharacterBaseRect = new Vector4(848f, 16f, 128f, 128f);
    private static readonly Vector4 CpuCharacterRect = new Vector4(852f, 20f, 120f, 120f);
    private static readonly Vector4 CpuLifeDeductionRect = new Vector4(824f, 128f, 176f, 56f);
    private static readonly Vector4 PlayerCharacterBaseRect = new Vector4(48f, 432f, 128f, 128f);
    private static readonly Vector4 PlayerCharacterRect = new Vector4(52f, 436f, 120f, 120f);
    private static readonly Vector4 PlayerLifeDeductionRect = new Vector4(24f, 392f, 176f, 56f);
    private static readonly Vector4 CpuHandFrameRect = new Vector4(336f, 16f, 480f, 128f);
    private static readonly Vector4 PlayerHandFrameRect = new Vector4(208f, 432f, 480f, 128f);
    private static readonly Vector4 CpuCommonFrameRect = new Vector4(112f, 160f, 192f, 128f);
    private static readonly Vector4 PlayerCommonFrameRect = new Vector4(720f, 288f, 192f, 128f);
    private static readonly Vector4 CpuSpecialCardSlotRect = new Vector4(208f, 16f, 96f, 128f);
    private static readonly Vector4 PlayerSpecialCardSlotRect = new Vector4(720f, 432f, 96f, 128f);
    private static readonly Vector2 ScoreTextSize = new Vector2(176f, 72f);
    private static readonly Vector4 SpecialCallBackdropRect = new Vector4(304f, 224f, 416f, 128f);
    private static readonly Vector4 SpecialActivationRect = new Vector4(152f, 113f, 720f, 350f);
    private static readonly Vector4 ResultBackdropRect = new Vector4(304f, 208f, 416f, 160f);
    private static readonly Vector4 CpuResultStampRect = new Vector4(64f, 32f, 96f, 96f);
    private static readonly Vector4 PlayerResultStampRect = new Vector4(864f, 448f, 96f, 96f);
    private static readonly Vector4 SpecialCallTextRect = new Vector4(320f, 240f, 384f, 96f);
    private static readonly Vector4 ResultTextRect = new Vector4(320f, 224f, 384f, 64f);
    private static readonly Vector4 DamageTextRect = new Vector4(320f, 296f, 384f, 48f);
    private static readonly Vector4 CloseButtonRect = new Vector4(976f, 16f, 40f, 40f);

    private static void Stretch(RectTransform rectTransform)
    {
        RuntimeUiFactory.Stretch(rectTransform);
    }

    private static void SetNormalizedRect(
        RectTransform rectTransform,
        float xMin,
        float yMin,
        float xMax,
        float yMax)
    {
        RuntimeUiFactory.SetNormalizedRect(rectTransform, xMin, yMin, xMax, yMax);
    }

    private static void SetNormalizedRect(
        Image image,
        float xMin,
        float yMin,
        float xMax,
        float yMax)
    {
        if (image != null)
        {
            SetNormalizedRect(image.rectTransform, xMin, yMin, xMax, yMax);
        }
    }

    private static void SetNormalizedRect(
        TextMeshProUGUI text,
        float xMin,
        float yMin,
        float xMax,
        float yMax)
    {
        if (text != null)
        {
            SetNormalizedRect(text.rectTransform, xMin, yMin, xMax, yMax);
        }
    }

    private static void SetReferencePixelRect(Image image, Vector4 rect)
    {
        if (image != null)
        {
            SetReferencePixelRect(image.rectTransform, rect);
        }
    }

    private static void SetReferencePixelRect(TextMeshProUGUI text, Vector4 rect)
    {
        if (text != null)
        {
            SetReferencePixelRect(text.rectTransform, rect);
        }
    }

    private static void SetReferencePixelRect(RectTransform rectTransform, Vector4 rect)
    {
        RuntimeUiFactory.SetReferencePixelRect(
            rectTransform,
            rect,
            ReferenceWidth,
            ReferenceHeight);
    }

    private Vector4 GetCpuRoleSpriteRect()
    {
        return assetSet != null ? assetSet.cpuRoleSpriteRect : fallbackCpuRoleSpriteRect;
    }

    private Vector4 GetPlayerRoleSpriteRect()
    {
        return assetSet != null ? assetSet.playerRoleSpriteRect : fallbackPlayerRoleSpriteRect;
    }
}
