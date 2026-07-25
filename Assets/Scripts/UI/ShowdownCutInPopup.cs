using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HandEvaluator;

public class ShowdownCutInPopup : MonoBehaviour
{
    private const float ReferenceWidth = 1024f;
    private const float ReferenceHeight = 576f;
    private const int HandCardCount = 5;
    private const int CommonCardCount = 2;
    private const int ShowdownCardCount = 7;
    private static readonly Vector4 CpuCharacterBaseRect = new Vector4(848f, 16f, 128f, 128f);
    private static readonly Vector4 CpuCharacterRect = new Vector4(864f, 32f, 96f, 96f);
    private static readonly Vector4 PlayerCharacterBaseRect = new Vector4(48f, 432f, 128f, 128f);
    private static readonly Vector4 PlayerCharacterRect = new Vector4(64f, 448f, 96f, 96f);
    private static readonly Vector4 CpuHandFrameRect = new Vector4(336f, 16f, 480f, 128f);
    private static readonly Vector4 PlayerHandFrameRect = new Vector4(208f, 432f, 480f, 128f);
    private static readonly Vector4 CpuCommonFrameRect = new Vector4(112f, 160f, 192f, 128f);
    private static readonly Vector4 PlayerCommonFrameRect = new Vector4(720f, 288f, 192f, 128f);
    private static readonly Vector4 CpuSpecialCardSlotRect = new Vector4(208f, 16f, 96f, 128f);
    private static readonly Vector4 PlayerSpecialCardSlotRect = new Vector4(720f, 432f, 96f, 128f);
    private static readonly Vector2 ScoreTextSize = new Vector2(176f, 72f);
    private static readonly Vector4 SpecialCallBackdropRect = new Vector4(304f, 224f, 416f, 128f);
    private static readonly Vector4 ResultBackdropRect = new Vector4(304f, 208f, 416f, 160f);
    private static readonly Vector4 CpuResultStampRect = new Vector4(64f, 32f, 96f, 96f);
    private static readonly Vector4 PlayerResultStampRect = new Vector4(864f, 448f, 96f, 96f);
    private static readonly Vector4 SpecialCallTextRect = new Vector4(320f, 240f, 384f, 96f);
    private static readonly Vector4 ResultTextRect = new Vector4(320f, 224f, 384f, 64f);
    private static readonly Vector4 DamageTextRect = new Vector4(320f, 296f, 384f, 48f);

    public sealed class Data
    {
        public sealed class EffectStepData
        {
            public int OwnerPlayerId { get; }
            public Sprite SpecialCardSprite { get; }
            public string EffectName { get; }
            public string Message { get; }
            public bool WasSealed { get; }
            public string PlayerRoleName { get; }
            public string CpuRoleName { get; }
            public HandRank PlayerRoleRank { get; }
            public HandRank CpuRoleRank { get; }
            public int PlayerScore { get; }
            public int CpuScore { get; }

            public EffectStepData(
                int ownerPlayerId,
                Sprite specialCardSprite,
                string effectName,
                string message,
                bool wasSealed,
                string playerRoleName,
                string cpuRoleName,
                HandRank playerRoleRank,
                HandRank cpuRoleRank,
                int playerScore,
                int cpuScore)
            {
                OwnerPlayerId = ownerPlayerId;
                SpecialCardSprite = specialCardSprite;
                EffectName = effectName;
                Message = message;
                WasSealed = wasSealed;
                PlayerRoleName = playerRoleName;
                CpuRoleName = cpuRoleName;
                PlayerRoleRank = playerRoleRank;
                CpuRoleRank = cpuRoleRank;
                PlayerScore = playerScore;
                CpuScore = cpuScore;
            }
        }

        public Sprite PlayerCharacterSprite { get; }
        public Sprite CpuCharacterSprite { get; }
        public string PlayerBaseRoleName { get; }
        public string CpuBaseRoleName { get; }
        public HandRank PlayerBaseRoleRank { get; }
        public HandRank CpuBaseRoleRank { get; }
        public int PlayerBaseScore { get; }
        public int CpuBaseScore { get; }
        public string PlayerFinalRoleName { get; }
        public string CpuFinalRoleName { get; }
        public HandRank PlayerFinalRoleRank { get; }
        public HandRank CpuFinalRoleRank { get; }
        public int PlayerFinalScore { get; }
        public int CpuFinalScore { get; }
        public IReadOnlyList<Sprite> PlayerCardSprites { get; }
        public IReadOnlyList<Sprite> CpuCardSprites { get; }
        public IReadOnlyList<bool> PlayerCardHighlights { get; }
        public IReadOnlyList<bool> CpuCardHighlights { get; }
        public Sprite PlayerSpecialCardSprite { get; }
        public Sprite CpuSpecialCardSprite { get; }
        public IReadOnlyList<EffectStepData> EffectSteps { get; }
        public int WinnerIndex { get; }
        public int Damage { get; }
        public int PlayerLifeBefore { get; }
        public int CpuLifeBefore { get; }
        public int PlayerLifeAfter { get; }
        public int CpuLifeAfter { get; }
        public bool IsMatchDecided => PlayerLifeAfter <= 0 || CpuLifeAfter <= 0;
        public bool IsDraw => WinnerIndex < 0;

        public Data(
            Sprite playerCharacterSprite,
            Sprite cpuCharacterSprite,
            string playerBaseRoleName,
            string cpuBaseRoleName,
            HandRank playerBaseRoleRank,
            HandRank cpuBaseRoleRank,
            int playerBaseScore,
            int cpuBaseScore,
            string playerFinalRoleName,
            string cpuFinalRoleName,
            HandRank playerFinalRoleRank,
            HandRank cpuFinalRoleRank,
            int playerFinalScore,
            int cpuFinalScore,
            IReadOnlyList<Sprite> playerCardSprites,
            IReadOnlyList<Sprite> cpuCardSprites,
            IReadOnlyList<bool> playerCardHighlights,
            IReadOnlyList<bool> cpuCardHighlights,
            Sprite playerSpecialCardSprite,
            Sprite cpuSpecialCardSprite,
            IReadOnlyList<EffectStepData> effectSteps,
            int winnerIndex,
            int damage,
            int playerLifeBefore,
            int cpuLifeBefore,
            int playerLifeAfter,
            int cpuLifeAfter)
        {
            PlayerCharacterSprite = playerCharacterSprite;
            CpuCharacterSprite = cpuCharacterSprite;
            PlayerBaseRoleName = playerBaseRoleName;
            CpuBaseRoleName = cpuBaseRoleName;
            PlayerBaseRoleRank = playerBaseRoleRank;
            CpuBaseRoleRank = cpuBaseRoleRank;
            PlayerBaseScore = playerBaseScore;
            CpuBaseScore = cpuBaseScore;
            PlayerFinalRoleName = playerFinalRoleName;
            CpuFinalRoleName = cpuFinalRoleName;
            PlayerFinalRoleRank = playerFinalRoleRank;
            CpuFinalRoleRank = cpuFinalRoleRank;
            PlayerFinalScore = playerFinalScore;
            CpuFinalScore = cpuFinalScore;
            PlayerCardSprites = playerCardSprites;
            CpuCardSprites = cpuCardSprites;
            PlayerCardHighlights = playerCardHighlights;
            CpuCardHighlights = cpuCardHighlights;
            PlayerSpecialCardSprite = playerSpecialCardSprite;
            CpuSpecialCardSprite = cpuSpecialCardSprite;
            EffectSteps = effectSteps;
            WinnerIndex = winnerIndex;
            Damage = damage;
            PlayerLifeBefore = playerLifeBefore;
            CpuLifeBefore = cpuLifeBefore;
            PlayerLifeAfter = playerLifeAfter;
            CpuLifeAfter = cpuLifeAfter;
        }
    }

    [SerializeField] private ShowdownCutInAssetSet assetSet;
    [SerializeField] private bool buildMissingUiAtRuntime = true;
    [SerializeField] private Color roleCardDimColor = new Color(1f, 1f, 1f, 0.34f);
    [SerializeField] private Color activeSpecialCardTint = new Color(1f, 0.86f, 0.18f, 1f);
    [SerializeField] private Color inactiveSpecialCardTint = new Color(1f, 1f, 1f, 0.42f);
    [SerializeField] private Vector4 fallbackCpuRoleSpriteRect = new Vector4(304f, 176f, 416f, 82f);
    [SerializeField] private Vector4 fallbackPlayerRoleSpriteRect = new Vector4(304f, 318f, 416f, 82f);
    [SerializeField] private float scoreStepInterval = 0.02f;
    [SerializeField] private float roleFrameInDuration = 0.28f;
    [SerializeField] private float matchDecisionInDuration = 0.42f;

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
    [SerializeField] private TextMeshProUGUI playerScoreText;
    [SerializeField] private TextMeshProUGUI cpuScoreText;
    [SerializeField] private Image specialCallBackdropImage;
    [SerializeField] private Image resultBackdropImage;
    [SerializeField] private Image resultStampImage;
    [SerializeField] private Image cpuResultStampImage;
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
    private int currentPlayerScore;
    private int currentCpuScore;

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
        yield return AnimateRoleFrameIn();
        yield return WaitForAdvanceInput();

        if (HasEffectSteps(data))
        {
            ShowSpecialCall();
            yield return WaitForAdvanceInput();

            foreach (Data.EffectStepData step in data.EffectSteps)
            {
                yield return ShowEffectStep(step);
                yield return WaitForAdvanceInput();
            }
        }

        yield return ShowFinalResult(data);
        yield return new WaitUntil(() => closeRequested);

        HideImmediately();
    }

    private static bool HasEffectSteps(Data data)
    {
        return data != null && data.EffectSteps != null && data.EffectSteps.Count > 0;
    }

    private IEnumerator WaitForAdvanceInput()
    {
        yield return null;

        while (Input.GetMouseButton(0) || Input.touchCount > 0)
        {
            yield return null;
        }

        while (!IsAdvanceInputDown())
        {
            yield return null;
        }
    }

    private static bool IsAdvanceInputDown()
    {
        if (Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return true;
        }

        if (Input.touchCount <= 0)
        {
            return false;
        }

        Touch touch = Input.GetTouch(0);
        return touch.phase == TouchPhase.Began;
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
               playerScoreText != null &&
               cpuScoreText != null &&
               specialCallBackdropImage != null &&
               resultBackdropImage != null &&
               resultStampImage != null &&
               cpuResultStampImage != null &&
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
        ApplyRuntimeLayout();

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
        ConfigureImage(resultStampImage, preserveAspect: true);
        ConfigureImage(cpuResultStampImage, preserveAspect: true);
        ConfigureTextBackdrop(specialCallBackdropImage);
        ConfigureTextBackdrop(resultBackdropImage);
        PlaceBackdropBehindText(specialCallBackdropImage, specialCallText);
        PlaceBackdropBehindText(resultBackdropImage, resultText);
        PlaceImageBehindText(resultStampImage, resultText);
        DisableLegacyRoleFallbackTexts();

        ApplyTextDefaults(playerScoreText);
        ApplyTextDefaults(cpuScoreText);
        ApplyTextDefaults(specialCallText);
        ApplyTextDefaults(resultText);
        ApplyTextDefaults(damageText);
        ConfigureScoreText(cpuScoreText);
        ConfigureScoreText(playerScoreText);
        ApplyReadableOverlayText(specialCallText, new Color(1f, 0.9f, 0.35f, 1f));
        ApplyReadableOverlayText(resultText, Color.white);
        ApplyReadableOverlayText(damageText, new Color(1f, 0.92f, 0.72f, 1f));
        ApplyStageSiblingOrder();
    }

    private void ApplyRuntimeLayout()
    {
        if (stage == null)
        {
            return;
        }

        Stretch(stage);
        EnsureDirectStageChild(screenFillImage);
        EnsureDirectStageChild(backgroundImage);
        EnsureDirectStageChild(cpuCharacterBaseImage);
        EnsureDirectStageChild(playerCharacterBaseImage);
        EnsureDirectStageChild(cpuCharacterImage);
        EnsureDirectStageChild(playerCharacterImage);
        EnsureDirectStageChild(cpuRoleImage);
        EnsureDirectStageChild(playerRoleImage);
        EnsureDirectStageChild(cpuScoreText);
        EnsureDirectStageChild(playerScoreText);
        EnsureDirectStageChild(cpuCardImages);
        EnsureDirectStageChild(playerCardImages);
        EnsureDirectStageChild(cpuSpecialCardImage);
        EnsureDirectStageChild(playerSpecialCardImage);
        EnsureDirectStageChild(specialCallBackdropImage);
        EnsureDirectStageChild(resultBackdropImage);
        EnsureDirectStageChild(specialCallText);
        EnsureDirectStageChild(resultText);
        EnsureDirectStageChild(damageText);
        EnsureDirectStageChild(resultStampImage);
        EnsureDirectStageChild(cpuResultStampImage);
        EnsureDirectStageChild(closeButton);

        if (screenFillImage != null)
        {
            Stretch(screenFillImage.rectTransform);
        }

        if (backgroundImage != null)
        {
            Stretch(backgroundImage.rectTransform);
        }

        SetReferencePixelRect(cpuCharacterBaseImage, CpuCharacterBaseRect);
        SetReferencePixelRect(playerCharacterBaseImage, PlayerCharacterBaseRect);
        SetReferencePixelRect(cpuCharacterImage, CpuCharacterRect);
        SetReferencePixelRect(playerCharacterImage, PlayerCharacterRect);

        if (cpuRoleImage != null)
        {
            SetReferencePixelRect(cpuRoleImage.rectTransform, GetCpuRoleSpriteRect());
        }

        if (playerRoleImage != null)
        {
            SetReferencePixelRect(playerRoleImage.rectTransform, GetPlayerRoleSpriteRect());
        }

        ApplyShowdownCardLayout(cpuCardImages, CpuHandFrameRect, CpuCommonFrameRect);
        ApplyShowdownCardLayout(playerCardImages, PlayerHandFrameRect, PlayerCommonFrameRect);

        if (cpuSpecialCardImage != null)
        {
            SetReferencePixelRect(cpuSpecialCardImage.rectTransform, CpuSpecialCardSlotRect);
        }

        if (playerSpecialCardImage != null)
        {
            SetReferencePixelRect(playerSpecialCardImage.rectTransform, PlayerSpecialCardSlotRect);
        }

        PlaceScoreAtRoleSwordTip(cpuScoreText, cpuRoleImage, false);
        PlaceScoreAtRoleSwordTip(playerScoreText, playerRoleImage, true);

        SetReferencePixelRect(specialCallBackdropImage, SpecialCallBackdropRect);
        SetReferencePixelRect(resultBackdropImage, ResultBackdropRect);
        if (resultStampImage != null)
        {
            SetReferencePixelRect(resultStampImage.rectTransform, PlayerResultStampRect);
        }

        if (cpuResultStampImage != null)
        {
            SetReferencePixelRect(cpuResultStampImage.rectTransform, CpuResultStampRect);
        }

        SetReferencePixelRect(specialCallText, SpecialCallTextRect);
        SetReferencePixelRect(resultText, ResultTextRect);
        SetReferencePixelRect(damageText, DamageTextRect);

        if (closeButton != null)
        {
            SetNormalizedRect(closeButton.GetComponent<RectTransform>(), 0.82f, 0.82f, 0.95f, 0.92f);
        }
    }

    private void ApplyShowdownCardLayout(Image[] images, Vector4 handFrameRect, Vector4 commonFrameRect)
    {
        if (images == null)
        {
            return;
        }

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null)
            {
                continue;
            }

            SetReferencePixelRect(images[i].rectTransform, GetShowdownCardSlotRect(handFrameRect, commonFrameRect, i));
        }
    }

    private static Vector4 GetShowdownCardSlotRect(Vector4 handFrameRect, Vector4 commonFrameRect, int index)
    {
        if (index < HandCardCount)
        {
            const float handCardWidth = 72f;
            const float handCardHeight = 96f;
            float step = HandCardCount > 1
                ? (handFrameRect.z - handCardWidth) / (HandCardCount - 1)
                : 0f;
            float x = handFrameRect.x + index * step;
            float y = handFrameRect.y + (handFrameRect.w - handCardHeight) * 0.5f;
            return new Vector4(x, y, handCardWidth, handCardHeight);
        }

        const float commonCardWidth = 72f;
        const float commonCardHeight = 96f;
        const float commonCardGap = 16f;
        int commonIndex = index - HandCardCount;
        float totalWidth = CommonCardCount * commonCardWidth + (CommonCardCount - 1) * commonCardGap;
        float startX = commonFrameRect.x + (commonFrameRect.z - totalWidth) * 0.5f;
        return new Vector4(
            startX + commonIndex * (commonCardWidth + commonCardGap),
            commonFrameRect.y + (commonFrameRect.w - commonCardHeight) * 0.5f,
            commonCardWidth,
            commonCardHeight);
    }

    private void EnsureDirectStageChild(Component component)
    {
        if (component == null || stage == null || component.transform.parent == stage)
        {
            return;
        }

        component.transform.SetParent(stage, false);
    }

    private void EnsureDirectStageChild(Image[] images)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            EnsureDirectStageChild(image);
        }
    }

    private void PlaceScoreAtRoleSwordTip(TextMeshProUGUI scoreText, Image roleImage, bool isPlayer)
    {
        if (scoreText == null || roleImage == null)
        {
            return;
        }

        RectTransform scoreRect = scoreText.rectTransform;
        RectTransform roleRect = roleImage.rectTransform;
        if (scoreRect == null || roleRect == null)
        {
            return;
        }

        if (scoreRect.parent != roleRect)
        {
            scoreRect.SetParent(roleRect, false);
        }

        Vector2 anchor = isPlayer ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        scoreRect.anchorMin = anchor;
        scoreRect.anchorMax = anchor;
        scoreRect.pivot = isPlayer ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        scoreRect.sizeDelta = ScoreTextSize;
        scoreRect.anchoredPosition = isPlayer ? new Vector2(-44f, 0f) : new Vector2(44f, 0f);
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

    private void ConfigureTextBackdrop(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.color = new Color(0f, 0f, 0f, 0.68f);
        image.preserveAspect = false;
        image.raycastTarget = false;
    }

    private static void PlaceBackdropBehindText(Image backdrop, TextMeshProUGUI text)
    {
        if (backdrop == null || text == null || backdrop.transform.parent != text.transform.parent)
        {
            return;
        }

        backdrop.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
    }

    private static void PlaceImageBehindText(Image image, TextMeshProUGUI text)
    {
        if (image == null || text == null || image.transform.parent != text.transform.parent)
        {
            return;
        }

        image.transform.SetSiblingIndex(text.transform.GetSiblingIndex());
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

    private void ApplyReadableOverlayText(TextMeshProUGUI text, Color color)
    {
        if (text == null)
        {
            return;
        }

        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.outlineColor = Color.black;
        text.outlineWidth = 0.28f;
    }

    private static void ConfigureScoreText(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 30f;
        text.fontSizeMax = 68f;
        text.color = Color.black;
        text.fontStyle = FontStyles.Bold;
        text.outlineWidth = 0f;
    }

    private void ApplyStageSiblingOrder()
    {
        SetAsLastSibling(screenFillImage);
        SetAsLastSibling(backgroundImage);
        SetAsLastSibling(cpuCharacterBaseImage);
        SetAsLastSibling(playerCharacterBaseImage);
        SetAsLastSibling(cpuCharacterImage);
        SetAsLastSibling(playerCharacterImage);
        SetAsLastSibling(cpuRoleImage);
        SetAsLastSibling(playerRoleImage);
        SetAsLastSibling(cpuCardImages);
        SetAsLastSibling(playerCardImages);
        SetAsLastSibling(cpuSpecialCardImage);
        SetAsLastSibling(playerSpecialCardImage);
        SetAsLastSibling(cpuScoreText);
        SetAsLastSibling(playerScoreText);
        SetAsLastSibling(specialCallBackdropImage);
        SetAsLastSibling(resultBackdropImage);
        SetAsLastSibling(cpuResultStampImage);
        SetAsLastSibling(resultStampImage);
        SetAsLastSibling(specialCallText);
        SetAsLastSibling(resultText);
        SetAsLastSibling(damageText);
        SetAsLastSibling(closeButton);
    }

    private static void SetAsLastSibling(Component component)
    {
        if (component != null)
        {
            component.transform.SetAsLastSibling();
        }
    }

    private static void SetAsLastSibling(Image[] images)
    {
        if (images == null)
        {
            return;
        }

        foreach (Image image in images)
        {
            SetAsLastSibling(image);
        }
    }

    private void DisableLegacyRoleFallbackTexts()
    {
        if (stage == null)
        {
            return;
        }

        DisableStageChild("PlayerRoleText");
        DisableStageChild("CpuRoleText");
    }

    private void DisableStageChild(string childName)
    {
        Transform child = stage.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
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
            SetReferencePixelRect(cpuCharacterBaseImage.rectTransform, CpuCharacterBaseRect);
        }

        if (playerCharacterBaseImage == null)
        {
            playerCharacterBaseImage = CreateImage("PlayerCharacterBase", stage, assetSet != null ? assetSet.characterBase : null, false);
            SetReferencePixelRect(playerCharacterBaseImage.rectTransform, PlayerCharacterBaseRect);
        }

        if (cpuCharacterImage == null)
        {
            cpuCharacterImage = CreateImage("CpuCharacter", stage, null, true);
            SetReferencePixelRect(cpuCharacterImage.rectTransform, CpuCharacterRect);
        }

        if (playerCharacterImage == null)
        {
            playerCharacterImage = CreateImage("PlayerCharacter", stage, null, true);
            SetReferencePixelRect(playerCharacterImage.rectTransform, PlayerCharacterRect);
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

        DisableLegacyRoleFallbackTexts();

        if (cpuScoreText == null)
        {
            cpuScoreText = CreateText("CpuScore", stage, 48, Color.black);
            PlaceScoreAtRoleSwordTip(cpuScoreText, cpuRoleImage, false);
        }

        if (playerScoreText == null)
        {
            playerScoreText = CreateText("PlayerScore", stage, 48, Color.black);
            PlaceScoreAtRoleSwordTip(playerScoreText, playerRoleImage, true);
        }

        BuildCardImageSlots(ref cpuCardImages, "CpuShowdownCard", CpuHandFrameRect, CpuCommonFrameRect);
        BuildCardImageSlots(ref playerCardImages, "PlayerShowdownCard", PlayerHandFrameRect, PlayerCommonFrameRect);

        if (cpuSpecialCardImage == null)
        {
            cpuSpecialCardImage = CreateImage("CpuSpecialCard", stage, null, true);
            SetReferencePixelRect(cpuSpecialCardImage.rectTransform, CpuSpecialCardSlotRect);
        }

        if (playerSpecialCardImage == null)
        {
            playerSpecialCardImage = CreateImage("PlayerSpecialCard", stage, null, true);
            SetReferencePixelRect(playerSpecialCardImage.rectTransform, PlayerSpecialCardSlotRect);
        }

        if (specialCallBackdropImage == null)
        {
            specialCallBackdropImage = CreateImage("SpecialCallBackdrop", stage, null, false);
            SetReferencePixelRect(specialCallBackdropImage.rectTransform, SpecialCallBackdropRect);
        }

        if (resultBackdropImage == null)
        {
            resultBackdropImage = CreateImage("ResultBackdrop", stage, null, false);
            SetReferencePixelRect(resultBackdropImage.rectTransform, ResultBackdropRect);
        }

        if (resultStampImage == null)
        {
            resultStampImage = CreateImage("PlayerResultStamp", stage, null, true);
            SetReferencePixelRect(resultStampImage.rectTransform, PlayerResultStampRect);
        }

        if (cpuResultStampImage == null)
        {
            cpuResultStampImage = CreateImage("CpuResultStamp", stage, null, true);
            SetReferencePixelRect(cpuResultStampImage.rectTransform, CpuResultStampRect);
        }

        if (specialCallText == null)
        {
            specialCallText = CreateText("SpecialCall", stage, 64, Color.white);
            specialCallText.fontStyle = FontStyles.Bold;
            specialCallText.outlineColor = Color.black;
            specialCallText.outlineWidth = 0.25f;
            SetReferencePixelRect(specialCallText.rectTransform, SpecialCallTextRect);
        }

        if (resultText == null)
        {
            resultText = CreateText("Result", stage, 56, Color.black);
            resultText.fontStyle = FontStyles.Bold;
            SetReferencePixelRect(resultText.rectTransform, ResultTextRect);
        }

        if (damageText == null)
        {
            damageText = CreateText("Damage", stage, 42, Color.black);
            SetReferencePixelRect(damageText.rectTransform, DamageTextRect);
        }

        if (closeButton == null)
        {
            closeButton = CreateButton("CloseButton", stage, "閉じる");
            SetNormalizedRect(closeButton.GetComponent<RectTransform>(), 0.82f, 0.82f, 0.95f, 0.92f);
        }

        closeButton.gameObject.SetActive(false);
    }

    private void BuildCardImageSlots(
        ref Image[] images,
        string namePrefix,
        Vector4 handFrameRect,
        Vector4 commonFrameRect)
    {
        EnsureImageArraySize(ref images, ShowdownCardCount);
        for (int i = 0; i < ShowdownCardCount; i++)
        {
            if (images[i] == null)
            {
                images[i] = CreateImage($"{namePrefix}{i + 1}", stage, null, true);
                SetReferencePixelRect(
                    images[i].rectTransform,
                    GetShowdownCardSlotRect(handFrameRect, commonFrameRect, i));
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
        SetCardImages(playerCardImages, data.PlayerCardSprites, data.PlayerCardHighlights);
        SetCardImages(cpuCardImages, data.CpuCardSprites, data.CpuCardHighlights);
        SetImage(playerSpecialCardImage, data.PlayerSpecialCardSprite);
        SetImage(cpuSpecialCardImage, data.CpuSpecialCardSprite);
        ResetSpecialCardHighlights();
        SetRole(playerRoleImage, null, true, data.PlayerBaseRoleName, data.PlayerBaseRoleRank);
        SetRole(cpuRoleImage, null, false, data.CpuBaseRoleName, data.CpuBaseRoleRank);
        SetScoresImmediately(data.PlayerBaseScore, data.CpuBaseScore);
        SetActive(specialCallBackdropImage, false);
        SetActive(resultBackdropImage, false);
        SetActive(resultStampImage, false);
        SetActive(cpuResultStampImage, false);
        specialCallText.gameObject.SetActive(false);
        resultText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
    }

    private void ShowSpecialCall()
    {
        specialCallText.text = "特殊札発動";
        SetActive(specialCallBackdropImage, true);
        SetActive(resultBackdropImage, false);
        SetActive(resultStampImage, false);
        SetActive(cpuResultStampImage, false);
        ResetSpecialCardHighlights();
        specialCallText.gameObject.SetActive(true);
        resultText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
    }

    private IEnumerator ShowEffectStep(Data.EffectStepData step)
    {
        if (step == null)
        {
            yield break;
        }

        SetRole(playerRoleImage, null, true, step.PlayerRoleName, step.PlayerRoleRank);
        SetRole(cpuRoleImage, null, false, step.CpuRoleName, step.CpuRoleRank);
        HighlightSpecialCard(step.OwnerPlayerId);

        specialCallText.text = $"{GetOwnerName(step.OwnerPlayerId)}の{step.EffectName}";
        SetActive(specialCallBackdropImage, true);
        SetActive(resultBackdropImage, false);
        SetActive(resultStampImage, false);
        SetActive(cpuResultStampImage, false);
        specialCallText.gameObject.SetActive(true);
        resultText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
        yield return AnimateScoresTo(step.PlayerScore, step.CpuScore);
    }

    private IEnumerator ShowFinalResult(Data data)
    {
        SetBackground(assetSet != null ? assetSet.cutInBackground : null, Color.black);
        SetRole(playerRoleImage, null, true, data.PlayerFinalRoleName, data.PlayerFinalRoleRank);
        SetRole(cpuRoleImage, null, false, data.CpuFinalRoleName, data.CpuFinalRoleRank);
        ResetSpecialCardHighlights();
        SetActive(specialCallBackdropImage, false);
        SetActive(resultBackdropImage, data.IsMatchDecided);
        SetImage(resultStampImage, GetPlayerResultStampSprite(data));
        SetImage(cpuResultStampImage, GetCpuResultStampSprite(data));
        specialCallText.gameObject.SetActive(false);
        resultText.text = data.IsMatchDecided ? BuildMatchResultText(data) : BuildWinnerText(data);
        damageText.text = data.IsMatchDecided ? BuildMatchLifeText(data) : BuildDamageText(data);
        resultText.gameObject.SetActive(data.IsMatchDecided);
        damageText.gameObject.SetActive(data.IsMatchDecided);
        closeButton.gameObject.SetActive(false);
        yield return AnimateScoresTo(data.PlayerFinalScore, data.CpuFinalScore);
        if (data.IsMatchDecided)
        {
            yield return AnimateMatchDecisionIn();
        }

        SetCloseButtonLabel(data.IsMatchDecided ? "次へ" : "閉じる");
        closeButton.gameObject.SetActive(true);
        closeButton.Select();
    }

    private IEnumerator AnimateMatchDecisionIn()
    {
        RectTransform backdropRect = resultBackdropImage != null ? resultBackdropImage.rectTransform : null;
        RectTransform resultRect = resultText != null ? resultText.rectTransform : null;
        RectTransform damageRect = damageText != null ? damageText.rectTransform : null;

        Vector3 startScale = Vector3.one * 0.72f;
        SetLocalScale(backdropRect, startScale);
        SetLocalScale(resultRect, startScale);
        SetLocalScale(damageRect, startScale);

        Color backdropColor = resultBackdropImage != null ? resultBackdropImage.color : Color.white;
        Color resultColor = resultText != null ? resultText.color : Color.white;
        Color damageColor = damageText != null ? damageText.color : Color.white;
        SetAlpha(resultBackdropImage, 0f);
        SetAlpha(resultText, 0f);
        SetAlpha(damageText, 0f);

        if (matchDecisionInDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < matchDecisionInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / matchDecisionInDuration));
                Vector3 scale = Vector3.LerpUnclamped(startScale, Vector3.one, t);
                SetLocalScale(backdropRect, scale);
                SetLocalScale(resultRect, scale);
                SetLocalScale(damageRect, scale);
                SetAlpha(resultBackdropImage, backdropColor.a * t);
                SetAlpha(resultText, resultColor.a * t);
                SetAlpha(damageText, damageColor.a * t);
                yield return null;
            }
        }

        SetLocalScale(backdropRect, Vector3.one);
        SetLocalScale(resultRect, Vector3.one);
        SetLocalScale(damageRect, Vector3.one);
        if (resultBackdropImage != null) resultBackdropImage.color = backdropColor;
        if (resultText != null) resultText.color = resultColor;
        if (damageText != null) damageText.color = damageColor;
    }

    private IEnumerator AnimateRoleFrameIn()
    {
        RectTransform cpuRect = GetActiveRect(cpuRoleImage);
        RectTransform playerRect = GetActiveRect(playerRoleImage);
        if (cpuRect == null && playerRect == null)
        {
            yield break;
        }

        Vector2 cpuEnd = cpuRect != null ? cpuRect.anchoredPosition : Vector2.zero;
        Vector2 playerEnd = playerRect != null ? playerRect.anchoredPosition : Vector2.zero;
        float slideDistance = GetStageSlideDistance();
        Vector2 cpuStart = cpuEnd + Vector2.right * slideDistance;
        Vector2 playerStart = playerEnd + Vector2.left * slideDistance;

        if (roleFrameInDuration <= 0f)
        {
            SetAnchoredPosition(cpuRect, cpuEnd);
            SetAnchoredPosition(playerRect, playerEnd);
            yield break;
        }

        SetAnchoredPosition(cpuRect, cpuStart);
        SetAnchoredPosition(playerRect, playerStart);

        float elapsed = 0f;
        while (elapsed < roleFrameInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / roleFrameInDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            SetAnchoredPosition(cpuRect, Vector2.LerpUnclamped(cpuStart, cpuEnd, t));
            SetAnchoredPosition(playerRect, Vector2.LerpUnclamped(playerStart, playerEnd, t));
            yield return null;
        }

        SetAnchoredPosition(cpuRect, cpuEnd);
        SetAnchoredPosition(playerRect, playerEnd);
    }

    private float GetStageSlideDistance()
    {
        if (stage != null && stage.rect.width > 0f)
        {
            return stage.rect.width;
        }

        return ReferenceWidth;
    }

    private void SetScoresImmediately(int playerScore, int cpuScore)
    {
        currentPlayerScore = playerScore;
        currentCpuScore = cpuScore;
        SetScoreText(playerScoreText, currentPlayerScore);
        SetScoreText(cpuScoreText, currentCpuScore);
    }

    private IEnumerator AnimateScoresTo(int targetPlayerScore, int targetCpuScore)
    {
        if (currentPlayerScore == targetPlayerScore && currentCpuScore == targetCpuScore)
        {
            yield break;
        }

        WaitForSecondsRealtime wait = scoreStepInterval > 0f
            ? new WaitForSecondsRealtime(scoreStepInterval)
            : null;

        while (currentPlayerScore != targetPlayerScore || currentCpuScore != targetCpuScore)
        {
            currentPlayerScore = MoveScoreOneStep(currentPlayerScore, targetPlayerScore);
            currentCpuScore = MoveScoreOneStep(currentCpuScore, targetCpuScore);
            SetScoreText(playerScoreText, currentPlayerScore);
            SetScoreText(cpuScoreText, currentCpuScore);

            if (wait != null)
            {
                yield return wait;
            }
            else
            {
                yield return null;
            }
        }
    }

    private static int MoveScoreOneStep(int current, int target)
    {
        if (current < target)
        {
            return current + 1;
        }

        if (current > target)
        {
            return current - 1;
        }

        return current;
    }

    private static void SetScoreText(TextMeshProUGUI text, int score)
    {
        if (text != null)
        {
            text.text = $"{score}点";
        }
    }

    private static string GetOwnerName(int ownerPlayerId)
    {
        return ownerPlayerId == 0 ? "プレイヤー" : "CPU";
    }

    private static string BuildDamageText(Data data)
    {
        if (data == null || data.IsDraw || data.Damage <= 0)
        {
            return "ダメージなし";
        }

        string damagedPlayer = data.WinnerIndex == 0 ? "CPU" : "プレイヤー";
        return $"{damagedPlayer} {data.Damage}ダメージ";
    }

    private static string BuildMatchResultText(Data data)
    {
        return data != null && data.WinnerIndex == 0 ? "対戦勝利" : "対戦敗北";
    }

    private static string BuildMatchLifeText(Data data)
    {
        if (data == null)
        {
            return string.Empty;
        }

        return data.WinnerIndex == 0
            ? $"CPU HP {data.CpuLifeBefore} → {data.CpuLifeAfter}"
            : $"プレイヤー HP {data.PlayerLifeBefore} → {data.PlayerLifeAfter}";
    }

    private Sprite GetPlayerResultStampSprite(Data data)
    {
        if (assetSet == null || data == null || data.IsDraw)
        {
            return null;
        }

        return data.WinnerIndex == 0 ? assetSet.winResult : assetSet.loseResult;
    }

    private Sprite GetCpuResultStampSprite(Data data)
    {
        if (assetSet == null || data == null || data.IsDraw)
        {
            return null;
        }

        return data.WinnerIndex == 1 ? assetSet.winResult : assetSet.loseResult;
    }

    private void SetActive(Image image, bool isActive)
    {
        if (image != null)
        {
            image.gameObject.SetActive(isActive);
        }
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

    private void SetRole(Image image, TextMeshProUGUI fallbackText, bool isPlayer, string roleName, HandRank roleRank)
    {
        Sprite roleSprite = assetSet != null ? assetSet.GetRoleSprite(isPlayer, roleRank) : null;
        SetImage(image, roleSprite);
        if (fallbackText != null)
        {
            fallbackText.text = roleName;
            fallbackText.gameObject.SetActive(roleSprite == null);
        }
    }

    private void SetCardImages(Image[] images, IReadOnlyList<Sprite> sprites, IReadOnlyList<bool> highlights)
    {
        if (images == null)
        {
            return;
        }

        bool hasHighlights = HasAnyHighlight(highlights);
        for (int i = 0; i < images.Length; i++)
        {
            Sprite sprite = sprites != null && i < sprites.Count ? sprites[i] : null;
            SetImage(images[i], sprite);
            if (images[i] != null && sprite != null && hasHighlights)
            {
                bool highlighted = highlights != null && i < highlights.Count && highlights[i];
                images[i].color = highlighted ? Color.white : roleCardDimColor;
            }
        }
    }

    private static bool HasAnyHighlight(IReadOnlyList<bool> highlights)
    {
        if (highlights == null)
        {
            return false;
        }

        for (int i = 0; i < highlights.Count; i++)
        {
            if (highlights[i])
            {
                return true;
            }
        }

        return false;
    }

    private void HighlightSpecialCard(int ownerPlayerId)
    {
        ApplySpecialCardHighlight(playerSpecialCardImage, ownerPlayerId == 0);
        ApplySpecialCardHighlight(cpuSpecialCardImage, ownerPlayerId == 1);
    }

    private void ResetSpecialCardHighlights()
    {
        ApplySpecialCardHighlight(playerSpecialCardImage, false, dimInactive: false);
        ApplySpecialCardHighlight(cpuSpecialCardImage, false, dimInactive: false);
    }

    private void ApplySpecialCardHighlight(Image image, bool isActive, bool dimInactive = true)
    {
        if (image == null || !image.gameObject.activeSelf)
        {
            return;
        }

        image.color = isActive ? activeSpecialCardTint : dimInactive ? inactiveSpecialCardTint : Color.white;
        image.rectTransform.localScale = isActive ? Vector3.one * 1.08f : Vector3.one;
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

    private static RectTransform GetActiveRect(Image image)
    {
        if (image == null || !image.gameObject.activeSelf)
        {
            return null;
        }

        return image.rectTransform;
    }

    private static void SetAnchoredPosition(RectTransform rectTransform, Vector2 position)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = position;
        }
    }

    private static void SetLocalScale(RectTransform rectTransform, Vector3 scale)
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = scale;
        }
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

    private void SetCloseButtonLabel(string label)
    {
        if (closeButton == null)
        {
            return;
        }

        TextMeshProUGUI labelText = closeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            labelText.text = label;
        }
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

        SetActive(specialCallBackdropImage, false);
        SetActive(resultBackdropImage, false);
        SetActive(resultStampImage, false);
        SetActive(cpuResultStampImage, false);

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
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(xMin, yMin);
        rectTransform.anchorMax = new Vector2(xMax, yMax);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetNormalizedRect(Image image, float xMin, float yMin, float xMax, float yMax)
    {
        if (image != null)
        {
            SetNormalizedRect(image.rectTransform, xMin, yMin, xMax, yMax);
        }
    }

    private static void SetNormalizedRect(TextMeshProUGUI text, float xMin, float yMin, float xMax, float yMax)
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
        if (rectTransform == null)
        {
            return;
        }

        float x = rect.x;
        float yFromTop = rect.y;
        float width = Mathf.Max(0f, rect.z);
        float height = Mathf.Max(0f, rect.w);

        rectTransform.anchorMin = new Vector2(
            x / ReferenceWidth,
            1f - (yFromTop + height) / ReferenceHeight);
        rectTransform.anchorMax = new Vector2(
            (x + width) / ReferenceWidth,
            1f - yFromTop / ReferenceHeight);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
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
