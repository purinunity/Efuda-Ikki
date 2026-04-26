using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowdownCutInPopup : MonoBehaviour
{
    public sealed class Data
    {
        public Sprite PlayerCharacterSprite { get; }
        public Sprite CpuCharacterSprite { get; }
        public string PlayerBaseRoleName { get; }
        public string CpuBaseRoleName { get; }
        public int PlayerBaseScore { get; }
        public int CpuBaseScore { get; }
        public string PlayerFinalRoleName { get; }
        public string CpuFinalRoleName { get; }
        public int PlayerFinalScore { get; }
        public int CpuFinalScore { get; }
        public int WinnerIndex { get; }
        public int Damage { get; }
        public bool IsDraw => WinnerIndex < 0;

        public Data(
            Sprite playerCharacterSprite,
            Sprite cpuCharacterSprite,
            string playerBaseRoleName,
            string cpuBaseRoleName,
            int playerBaseScore,
            int cpuBaseScore,
            string playerFinalRoleName,
            string cpuFinalRoleName,
            int playerFinalScore,
            int cpuFinalScore,
            int winnerIndex,
            int damage)
        {
            PlayerCharacterSprite = playerCharacterSprite;
            CpuCharacterSprite = cpuCharacterSprite;
            PlayerBaseRoleName = playerBaseRoleName;
            CpuBaseRoleName = cpuBaseRoleName;
            PlayerBaseScore = playerBaseScore;
            CpuBaseScore = cpuBaseScore;
            PlayerFinalRoleName = playerFinalRoleName;
            CpuFinalRoleName = cpuFinalRoleName;
            PlayerFinalScore = playerFinalScore;
            CpuFinalScore = cpuFinalScore;
            WinnerIndex = winnerIndex;
            Damage = damage;
        }
    }

    [SerializeField] private ShowdownCutInAssetSet assetSet;
    [SerializeField] private float baseDisplaySeconds = 1.8f;
    [SerializeField] private float specialCallSeconds = 0.9f;

    private Canvas popupCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform stage;
    private Image screenFillImage;
    private Image backgroundImage;
    private Image playerCharacterBaseImage;
    private Image cpuCharacterBaseImage;
    private Image playerCharacterImage;
    private Image cpuCharacterImage;
    private Image playerRoleImage;
    private Image cpuRoleImage;
    private TextMeshProUGUI playerRoleFallbackText;
    private TextMeshProUGUI cpuRoleFallbackText;
    private TextMeshProUGUI playerScoreText;
    private TextMeshProUGUI cpuScoreText;
    private TextMeshProUGUI specialCallText;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI damageText;
    private Button closeButton;
    private bool closeRequested;
    private bool initialized;

    public static ShowdownCutInPopup Create(Transform parent, ShowdownCutInAssetSet assets = null)
    {
        GameObject popupObject = new GameObject(
            "ShowdownCutInPopup",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(ShowdownCutInPopup));

        popupObject.transform.SetParent(null, false);
        ShowdownCutInPopup popup = popupObject.GetComponent<ShowdownCutInPopup>();
        popup.assetSet = assets;
        popup.Initialize();
        return popup;
    }

    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (assetSet == null)
        {
            assetSet = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        }

        canvasGroup = GetComponent<CanvasGroup>();
        ConfigurePopupCanvas();
        Stretch(GetComponent<RectTransform>());
        BuildUi();
        HideImmediately();
        initialized = true;
    }

    public IEnumerator Play(Data data)
    {
        Initialize();
        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        closeRequested = false;

        ShowBaseResult(data);
        yield return new WaitForSeconds(baseDisplaySeconds);

        ShowSpecialCall();
        yield return new WaitForSeconds(specialCallSeconds);

        ShowFinalResult(data);
        yield return new WaitUntil(() => closeRequested);

        HideImmediately();
    }

    private void ConfigurePopupCanvas()
    {
        popupCanvas = GetComponent<Canvas>();
        if (popupCanvas != null)
        {
            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 5000;
        }

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024f, 576f);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void BuildUi()
    {
        stage = CreateRect("Stage", transform);
        Stretch(stage);

        screenFillImage = CreateImage("ScreenFill", stage, null, false);
        screenFillImage.color = Color.black;
        screenFillImage.raycastTarget = true;
        Stretch(screenFillImage.rectTransform);

        backgroundImage = CreateImage("Background", stage, assetSet != null ? assetSet.cutInBackground : null, false);
        Stretch(backgroundImage.rectTransform);

        cpuCharacterBaseImage = CreateImage("CpuCharacterBase", stage, assetSet != null ? assetSet.characterBase : null, false);
        SetNormalizedRect(cpuCharacterBaseImage.rectTransform, 0.047f, 0.75f, 0.172f, 0.972f);

        playerCharacterBaseImage = CreateImage("PlayerCharacterBase", stage, assetSet != null ? assetSet.characterBase : null, false);
        SetNormalizedRect(playerCharacterBaseImage.rectTransform, 0.828f, 0.028f, 0.953f, 0.25f);

        cpuCharacterImage = CreateImage("CpuCharacter", stage, null, true);
        SetNormalizedRect(cpuCharacterImage.rectTransform, 0.052f, 0.755f, 0.167f, 0.967f);

        playerCharacterImage = CreateImage("PlayerCharacter", stage, null, true);
        SetNormalizedRect(playerCharacterImage.rectTransform, 0.833f, 0.033f, 0.948f, 0.245f);

        cpuRoleImage = CreateImage("CpuRole", stage, null, true);
        SetNormalizedRect(cpuRoleImage.rectTransform, 0.328f, 0.825f, 0.797f, 0.94f);

        playerRoleImage = CreateImage("PlayerRole", stage, null, true);
        SetNormalizedRect(playerRoleImage.rectTransform, 0.203f, 0.075f, 0.672f, 0.19f);

        cpuRoleFallbackText = CreateText("CpuRoleText", stage, 30, Color.black);
        SetNormalizedRect(cpuRoleFallbackText.rectTransform, 0.328f, 0.825f, 0.797f, 0.94f);

        playerRoleFallbackText = CreateText("PlayerRoleText", stage, 30, Color.black);
        SetNormalizedRect(playerRoleFallbackText.rectTransform, 0.203f, 0.075f, 0.672f, 0.19f);

        cpuScoreText = CreateText("CpuScore", stage, 48, Color.black);
        SetNormalizedRect(cpuScoreText.rectTransform, 0.203f, 0.75f, 0.297f, 0.972f);

        playerScoreText = CreateText("PlayerScore", stage, 48, Color.black);
        SetNormalizedRect(playerScoreText.rectTransform, 0.703f, 0.028f, 0.797f, 0.25f);

        specialCallText = CreateText("SpecialCall", stage, 64, Color.white);
        specialCallText.fontStyle = FontStyles.Bold;
        specialCallText.outlineColor = Color.black;
        specialCallText.outlineWidth = 0.25f;
        SetNormalizedRect(specialCallText.rectTransform, 0.2f, 0.41f, 0.8f, 0.58f);

        resultText = CreateText("Result", stage, 56, Color.black);
        resultText.fontStyle = FontStyles.Bold;
        SetNormalizedRect(resultText.rectTransform, 0.18f, 0.39f, 0.82f, 0.55f);

        damageText = CreateText("Damage", stage, 42, Color.black);
        SetNormalizedRect(damageText.rectTransform, 0.18f, 0.29f, 0.82f, 0.4f);

        closeButton = CreateButton("CloseButton", stage, "閉じる");
        SetNormalizedRect(closeButton.GetComponent<RectTransform>(), 0.82f, 0.82f, 0.95f, 0.92f);
        closeButton.onClick.AddListener(() => closeRequested = true);
        closeButton.gameObject.SetActive(false);
    }

    private void ShowBaseResult(Data data)
    {
        SetBackground(assetSet != null ? assetSet.cutInBackground : null, new Color(0.82f, 0.82f, 0.82f, 1f));
        SetCharacters(data);
        SetRole(playerRoleImage, playerRoleFallbackText, true, data.PlayerBaseRoleName);
        SetRole(cpuRoleImage, cpuRoleFallbackText, false, data.CpuBaseRoleName);
        playerScoreText.text = $"{data.PlayerBaseScore}点";
        cpuScoreText.text = $"{data.CpuBaseScore}点";
        specialCallText.gameObject.SetActive(false);
        resultText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
    }

    private void ShowSpecialCall()
    {
        specialCallText.text = "特殊札発動";
        specialCallText.gameObject.SetActive(true);
        resultText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
    }

    private void ShowFinalResult(Data data)
    {
        SetBackground(assetSet != null ? assetSet.cutInBackground : null, Color.black);
        SetRole(playerRoleImage, playerRoleFallbackText, true, data.PlayerFinalRoleName);
        SetRole(cpuRoleImage, cpuRoleFallbackText, false, data.CpuFinalRoleName);
        playerScoreText.text = $"{data.PlayerFinalScore}点";
        cpuScoreText.text = $"{data.CpuFinalScore}点";
        specialCallText.gameObject.SetActive(false);
        resultText.text = BuildWinnerText(data);
        damageText.text = data.IsDraw ? "ダメージ 0" : $"ダメージ {data.Damage}";
        resultText.gameObject.SetActive(true);
        damageText.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
        closeButton.Select();
    }

    private void SetBackground(Sprite sprite, Color fillColor)
    {
        if (screenFillImage != null)
        {
            screenFillImage.color = fillColor;
        }

        backgroundImage.sprite = sprite;
        backgroundImage.color = sprite != null ? Color.white : new Color(0f, 0f, 0f, 0.85f);
    }

    private void SetCharacters(Data data)
    {
        SetImage(playerCharacterImage, data.PlayerCharacterSprite);
        SetImage(cpuCharacterImage, data.CpuCharacterSprite);
        SetImage(playerCharacterBaseImage, assetSet != null ? assetSet.characterBase : null);
        SetImage(cpuCharacterBaseImage, assetSet != null ? assetSet.characterBase : null);
    }

    private void SetRole(Image image, TextMeshProUGUI fallbackText, bool isPlayer, string roleName)
    {
        Sprite roleSprite = assetSet != null ? assetSet.GetRoleSprite(isPlayer, roleName) : null;
        SetImage(image, roleSprite);
        fallbackText.text = roleName;
        fallbackText.gameObject.SetActive(roleSprite == null);
    }

    private void SetImage(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
        image.gameObject.SetActive(sprite != null);
    }

    private static string BuildWinnerText(Data data)
    {
        if (data.IsDraw)
        {
            return "引き分け";
        }

        return data.WinnerIndex == 0 ? "プレイヤー勝利" : "CPU勝利";
    }

    private void HideImmediately()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, bool preserveAspect)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : Color.clear;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, float maxFontSize, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (assetSet != null && assetSet.textFont != null)
        {
            text.font = assetSet.textFont;
        }

        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = maxFontSize;
        text.color = color;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        colors.highlightedColor = new Color(0.18f, 0.18f, 0.18f, 0.95f);
        colors.pressedColor = new Color(0.02f, 0.02f, 0.02f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", buttonObject.transform, 32, Color.white);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform);
        return button;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetNormalizedRect(RectTransform rectTransform, float xMin, float yMin, float xMax, float yMax)
    {
        rectTransform.anchorMin = new Vector2(xMin, yMin);
        rectTransform.anchorMax = new Vector2(xMax, yMax);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
