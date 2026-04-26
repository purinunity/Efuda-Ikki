using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowdownCutInPopup : MonoBehaviour
{
    private const float ReferenceWidth = 1024f;
    private const float ReferenceHeight = 576f;
    private const int ShowdownCardCount = 7;

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
        public IReadOnlyList<Sprite> PlayerCardSprites { get; }
        public IReadOnlyList<Sprite> CpuCardSprites { get; }
        public Sprite PlayerSpecialCardSprite { get; }
        public Sprite CpuSpecialCardSprite { get; }
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
            IReadOnlyList<Sprite> playerCardSprites,
            IReadOnlyList<Sprite> cpuCardSprites,
            Sprite playerSpecialCardSprite,
            Sprite cpuSpecialCardSprite,
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
            PlayerCardSprites = playerCardSprites;
            CpuCardSprites = cpuCardSprites;
            PlayerSpecialCardSprite = playerSpecialCardSprite;
            CpuSpecialCardSprite = cpuSpecialCardSprite;
            WinnerIndex = winnerIndex;
            Damage = damage;
        }
    }

    [SerializeField] private ShowdownCutInAssetSet assetSet;
    [SerializeField] private float baseDisplaySeconds = 1.8f;
    [SerializeField] private float specialCallSeconds = 0.9f;
    [SerializeField] private bool buildMissingUiAtRuntime = true;
    [SerializeField] private Vector4 fallbackCpuRoleSpriteRect = new Vector4(208f, 16f, 624f, 96f);
    [SerializeField] private Vector4 fallbackPlayerRoleSpriteRect = new Vector4(208f, 464f, 624f, 96f);
    [SerializeField] private Vector4 fallbackCpuCardStartRect = new Vector4(304f, 160f, 54f, 76f);
    [SerializeField] private Vector4 fallbackPlayerCardStartRect = new Vector4(240f, 340f, 54f, 76f);
    [SerializeField] private float fallbackCardGap = 8f;
    [SerializeField] private Vector4 fallbackCpuSpecialCardRect = new Vector4(856f, 184f, 54f, 76f);
    [SerializeField] private Vector4 fallbackPlayerSpecialCardRect = new Vector4(112f, 316f, 54f, 76f);

    [Header("Hierarchy References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform stage;
    [SerializeField] private Image screenFillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image playerCharacterBaseImage;
    [SerializeField] private Image cpuCharacterBaseImage;
    [SerializeField] private Image playerCharacterImage;
    [SerializeField] private Image cpuCharacterImage;
    [SerializeField] private Image playerRoleImage;
    [SerializeField] private Image cpuRoleImage;
    [SerializeField] private TextMeshProUGUI playerRoleFallbackText;
    [SerializeField] private TextMeshProUGUI cpuRoleFallbackText;
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private TextMeshProUGUI cpuScoreText;
    [SerializeField] private TextMeshProUGUI specialCallText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private Image[] playerCardImages = new Image[ShowdownCardCount];
    [SerializeField] private Image[] cpuCardImages = new Image[ShowdownCardCount];
    [SerializeField] private Image playerSpecialCardImage;
    [SerializeField] private Image cpuSpecialCardImage;
    [SerializeField] private Button closeButton;
    private bool closeRequested;
    private bool initialized;

    private void Awake()
    {
        CacheRootComponents();
        HideImmediately();
    }

    public static ShowdownCutInPopup Create(Transform parent, ShowdownCutInAssetSet assets = null)
    {
        GameObject popupObject = new GameObject(
            "ShowdownCutInPopup",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(ShowdownCutInPopup));

        ShowdownCutInPopup popup = popupObject.GetComponent<ShowdownCutInPopup>();
        popup.assetSet = assets;
        popup.SetPopupParent(parent);
        popup.Initialize();
        return popup;
    }

    public void SetPopupParent(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        if (transform.parent != parent)
        {
            transform.SetParent(parent, false);
        }

        ApplyLayerRecursively(gameObject, parent.gameObject.layer);

        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect != null)
        {
            Stretch(rootRect);
            rootRect.localScale = Vector3.one;
        }

        transform.SetAsLastSibling();
        ConfigurePopupCanvas();
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

        CacheRootComponents();
        ConfigurePopupCanvas();
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect != null)
        {
            Stretch(rootRect);
            rootRect.localScale = Vector3.one;
        }

        if (!HasRequiredUiReferences())
        {
            if (buildMissingUiAtRuntime)
            {
                BuildUi();
            }
            else
            {
                Debug.LogWarning("ShowdownCutInPopup: hierarchy references are incomplete.");
            }
        }

        ConfigureUiReferences();
        WireCloseButton();
        HideImmediately();
        initialized = true;
    }

#if UNITY_EDITOR
    [ContextMenu("Build Editable UI")]
    private void BuildEditableUi()
    {
        if (assetSet == null)
        {
            assetSet = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        }

        CacheRootComponents();
        ConfigurePopupCanvas();
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect != null)
        {
            Stretch(rootRect);
        }

        BuildUi();
        ConfigureUiReferences();
        WireCloseButton();
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

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

    private void CacheRootComponents()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

    }

    private bool HasRequiredUiReferences()
    {
        return stage != null &&
               screenFillImage != null &&
               backgroundImage != null &&
               playerCharacterBaseImage != null &&
               cpuCharacterBaseImage != null &&
               playerCharacterImage != null &&
               cpuCharacterImage != null &&
               playerRoleImage != null &&
               cpuRoleImage != null &&
               playerRoleFallbackText != null &&
               cpuRoleFallbackText != null &&
               playerScoreText != null &&
               cpuScoreText != null &&
               specialCallText != null &&
               resultText != null &&
               damageText != null &&
               HasImageSlots(playerCardImages, ShowdownCardCount) &&
               HasImageSlots(cpuCardImages, ShowdownCardCount) &&
               playerSpecialCardImage != null &&
               cpuSpecialCardImage != null &&
               closeButton != null;
    }

    private void ConfigureUiReferences()
    {
        if (screenFillImage != null)
        {
            screenFillImage.color = Color.black;
            screenFillImage.raycastTarget = true;
        }

        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = false;
            if (backgroundImage.sprite == null && assetSet != null)
            {
                backgroundImage.sprite = assetSet.cutInBackground;
            }
        }

        ConfigureImage(playerCharacterBaseImage, preserveAspect: false);
        ConfigureImage(cpuCharacterBaseImage, preserveAspect: false);
        ConfigureImage(playerCharacterImage, preserveAspect: true);
        ConfigureImage(cpuCharacterImage, preserveAspect: true);
        ConfigureImage(playerRoleImage, preserveAspect: true);
        ConfigureImage(cpuRoleImage, preserveAspect: true);
        ConfigureImageArray(playerCardImages, preserveAspect: true);
        ConfigureImageArray(cpuCardImages, preserveAspect: true);
        ConfigureImage(playerSpecialCardImage, preserveAspect: true);
        ConfigureImage(cpuSpecialCardImage, preserveAspect: true);

        ApplyTextDefaults(playerRoleFallbackText);
        ApplyTextDefaults(cpuRoleFallbackText);
        ApplyTextDefaults(playerScoreText);
        ApplyTextDefaults(cpuScoreText);
        ApplyTextDefaults(specialCallText);
        ApplyTextDefaults(resultText);
        ApplyTextDefaults(damageText);
    }

    private void ConfigureImage(Image image, bool preserveAspect)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
    }

    private void ConfigureImageArray(Image[] images, bool preserveAspect)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            ConfigureImage(image, preserveAspect);
        }
    }

    private static bool HasImageSlots(Image[] images, int requiredCount)
    {
        if (images == null || images.Length < requiredCount)
        {
            return false;
        }

        for (int i = 0; i < requiredCount; i++)
        {
            if (images[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyTextDefaults(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        if (assetSet != null && assetSet.textFont != null)
        {
            text.font = assetSet.textFont;
        }

        text.raycastTarget = false;
    }

    private void WireCloseButton()
    {
        if (closeButton == null)
        {
            return;
        }

        closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnCloseButtonClicked()
    {
        closeRequested = true;
    }

    private void ConfigurePopupCanvas()
    {
        Canvas parentCanvas = GetParentCanvas();
        Canvas localCanvas = GetComponent<Canvas>();
        CanvasScaler localScaler = GetComponent<CanvasScaler>();
        GraphicRaycaster localRaycaster = GetComponent<GraphicRaycaster>();

        if (parentCanvas == null)
        {
            Debug.LogWarning("ShowdownCutInPopup should be placed under the game Canvas.");
        }

        if (localCanvas != null)
        {
            localCanvas.enabled = false;
            localCanvas.overrideSorting = false;
        }

        if (localScaler != null)
        {
            localScaler.enabled = false;
        }

        if (localRaycaster != null)
        {
            localRaycaster.enabled = false;
        }
    }

    private Canvas GetParentCanvas()
    {
        Transform parent = transform.parent;
        return parent != null ? parent.GetComponentInParent<Canvas>() : null;
    }

    private void BuildUi()
    {
        if (stage == null)
        {
            stage = CreateRect("Stage", transform);
        }
        Stretch(stage);

        if (screenFillImage == null)
        {
            screenFillImage = CreateImage("ScreenFill", stage, null, false);
            Stretch(screenFillImage.rectTransform);
        }
        screenFillImage.color = Color.black;
        screenFillImage.raycastTarget = true;

        if (backgroundImage == null)
        {
            backgroundImage = CreateImage("Background", stage, assetSet != null ? assetSet.cutInBackground : null, false);
            Stretch(backgroundImage.rectTransform);
        }

        if (cpuCharacterBaseImage == null)
        {
            cpuCharacterBaseImage = CreateImage("CpuCharacterBase", stage, assetSet != null ? assetSet.characterBase : null, false);
            SetNormalizedRect(cpuCharacterBaseImage.rectTransform, 0.047f, 0.75f, 0.172f, 0.972f);
        }

        if (playerCharacterBaseImage == null)
        {
            playerCharacterBaseImage = CreateImage("PlayerCharacterBase", stage, assetSet != null ? assetSet.characterBase : null, false);
            SetNormalizedRect(playerCharacterBaseImage.rectTransform, 0.828f, 0.028f, 0.953f, 0.25f);
        }

        if (cpuCharacterImage == null)
        {
            cpuCharacterImage = CreateImage("CpuCharacter", stage, null, true);
            SetNormalizedRect(cpuCharacterImage.rectTransform, 0.052f, 0.755f, 0.167f, 0.967f);
        }

        if (playerCharacterImage == null)
        {
            playerCharacterImage = CreateImage("PlayerCharacter", stage, null, true);
            SetNormalizedRect(playerCharacterImage.rectTransform, 0.833f, 0.033f, 0.948f, 0.245f);
        }

        if (cpuRoleImage == null)
        {
            cpuRoleImage = CreateImage("CpuRole", stage, null, true);
            SetReferencePixelRect(cpuRoleImage.rectTransform, GetCpuRoleSpriteRect());
        }

        if (playerRoleImage == null)
        {
            playerRoleImage = CreateImage("PlayerRole", stage, null, true);
            SetReferencePixelRect(playerRoleImage.rectTransform, GetPlayerRoleSpriteRect());
        }

        if (cpuRoleFallbackText == null)
        {
            cpuRoleFallbackText = CreateText("CpuRoleText", stage, 30, Color.black);
            SetReferencePixelRect(cpuRoleFallbackText.rectTransform, GetCpuRoleSpriteRect());
        }

        if (playerRoleFallbackText == null)
        {
            playerRoleFallbackText = CreateText("PlayerRoleText", stage, 30, Color.black);
            SetReferencePixelRect(playerRoleFallbackText.rectTransform, GetPlayerRoleSpriteRect());
        }

        if (cpuScoreText == null)
        {
            cpuScoreText = CreateText("CpuScore", stage, 48, Color.black);
            SetNormalizedRect(cpuScoreText.rectTransform, 0.203f, 0.75f, 0.297f, 0.972f);
        }

        if (playerScoreText == null)
        {
            playerScoreText = CreateText("PlayerScore", stage, 48, Color.black);
            SetNormalizedRect(playerScoreText.rectTransform, 0.703f, 0.028f, 0.797f, 0.25f);
        }

        BuildCardImageSlots(ref cpuCardImages, "CpuShowdownCard", fallbackCpuCardStartRect);
        BuildCardImageSlots(ref playerCardImages, "PlayerShowdownCard", fallbackPlayerCardStartRect);

        if (cpuSpecialCardImage == null)
        {
            cpuSpecialCardImage = CreateImage("CpuSpecialCard", stage, null, true);
            SetReferencePixelRect(cpuSpecialCardImage.rectTransform, fallbackCpuSpecialCardRect);
        }

        if (playerSpecialCardImage == null)
        {
            playerSpecialCardImage = CreateImage("PlayerSpecialCard", stage, null, true);
            SetReferencePixelRect(playerSpecialCardImage.rectTransform, fallbackPlayerSpecialCardRect);
        }

        if (specialCallText == null)
        {
            specialCallText = CreateText("SpecialCall", stage, 64, Color.white);
            specialCallText.fontStyle = FontStyles.Bold;
            specialCallText.outlineColor = Color.black;
            specialCallText.outlineWidth = 0.25f;
            SetNormalizedRect(specialCallText.rectTransform, 0.2f, 0.41f, 0.8f, 0.58f);
        }

        if (resultText == null)
        {
            resultText = CreateText("Result", stage, 56, Color.black);
            resultText.fontStyle = FontStyles.Bold;
            SetNormalizedRect(resultText.rectTransform, 0.18f, 0.39f, 0.82f, 0.55f);
        }

        if (damageText == null)
        {
            damageText = CreateText("Damage", stage, 42, Color.black);
            SetNormalizedRect(damageText.rectTransform, 0.18f, 0.29f, 0.82f, 0.4f);
        }

        if (closeButton == null)
        {
            closeButton = CreateButton("CloseButton", stage, "閉じる");
            SetNormalizedRect(closeButton.GetComponent<RectTransform>(), 0.82f, 0.82f, 0.95f, 0.92f);
        }

        closeButton.gameObject.SetActive(false);
    }

    private void BuildCardImageSlots(ref Image[] images, string namePrefix, Vector4 startRect)
    {
        EnsureImageArraySize(ref images, ShowdownCardCount);
        for (int i = 0; i < ShowdownCardCount; i++)
        {
            if (images[i] == null)
            {
                images[i] = CreateImage($"{namePrefix}{i + 1}", stage, null, true);
                Vector4 rect = startRect;
                rect.x += i * (startRect.z + fallbackCardGap);
                SetReferencePixelRect(images[i].rectTransform, rect);
            }
        }
    }

    private static void EnsureImageArraySize(ref Image[] images, int requiredCount)
    {
        if (images != null && images.Length == requiredCount)
        {
            return;
        }

        Image[] resized = new Image[requiredCount];
        if (images != null)
        {
            int copyCount = Mathf.Min(images.Length, requiredCount);
            for (int i = 0; i < copyCount; i++)
            {
                resized[i] = images[i];
            }
        }

        images = resized;
    }

    private void ShowBaseResult(Data data)
    {
        SetBackground(assetSet != null ? assetSet.cutInBackground : null, new Color(0.82f, 0.82f, 0.82f, 1f));
        SetCharacters(data);
        SetCardImages(playerCardImages, data.PlayerCardSprites);
        SetCardImages(cpuCardImages, data.CpuCardSprites);
        SetImage(playerSpecialCardImage, data.PlayerSpecialCardSprite);
        SetImage(cpuSpecialCardImage, data.CpuSpecialCardSprite);
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
        if (fallbackText != null)
        {
            fallbackText.text = roleName;
            fallbackText.gameObject.SetActive(roleSprite == null);
        }
    }

    private void SetCardImages(Image[] images, IReadOnlyList<Sprite> sprites)
    {
        if (images == null)
        {
            return;
        }

        for (int i = 0; i < images.Length; i++)
        {
            Sprite sprite = sprites != null && i < sprites.Count ? sprites[i] : null;
            SetImage(images[i], sprite);
        }
    }

    private void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

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
        rectObject.layer = parent.gameObject.layer;
        return rectObject.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, bool preserveAspect)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        imageObject.layer = parent.gameObject.layer;
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
        textObject.layer = parent.gameObject.layer;
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
        buttonObject.layer = parent.gameObject.layer;

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

    private static void ApplyLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            ApplyLayerRecursively(child.gameObject, layer);
        }
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

    private static void SetReferencePixelRect(RectTransform rectTransform, Vector4 rect)
    {
        float x = rect.x;
        float yFromTop = rect.y;
        float width = Mathf.Max(0f, rect.z);
        float height = Mathf.Max(0f, rect.w);

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.anchoredPosition = new Vector2(
            x + width * 0.5f - ReferenceWidth * 0.5f,
            ReferenceHeight * 0.5f - yFromTop - height * 0.5f);
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
