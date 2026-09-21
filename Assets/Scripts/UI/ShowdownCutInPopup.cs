using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HandEvaluator;

public partial class ShowdownCutInPopup : MonoBehaviour
{
    [SerializeField] private ShowdownCutInAssetSet assetSet;
    [SerializeField] private bool buildMissingUiAtRuntime = true;
    [SerializeField] private Color roleCardDimColor = new Color(1f, 1f, 1f, 0.34f);
    [SerializeField] private Color activeSpecialCardTint = new Color(1f, 0.86f, 0.18f, 1f);
    [SerializeField] private Color inactiveSpecialCardTint = new Color(1f, 1f, 1f, 0.42f);
    [SerializeField] private Vector4 fallbackCpuRoleSpriteRect = new Vector4(304f, 176f, 416f, 82f);
    [SerializeField] private Vector4 fallbackPlayerRoleSpriteRect = new Vector4(304f, 318f, 416f, 82f);
    [SerializeField] private float scoreStepInterval = 0.02f;
    [SerializeField] private float roleFrameInDuration = 0.28f;
    [SerializeField] private float specialActivationFadeDuration = 0.45f;
#pragma warning disable CS0414 // Retained for existing scene serialization compatibility.
    [SerializeField] private float matchDecisionInDuration = 0.42f;
#pragma warning restore CS0414
    [SerializeField] private float lifeDeductionInDuration = 0.28f;

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
    [SerializeField] private TextMeshProUGUI playerLifeDeductionText;
    [SerializeField] private TextMeshProUGUI cpuLifeDeductionText;
    [SerializeField] private Image specialActivationImage;
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
    private Button subscribedCloseButton;
    private bool closeRequested;
    private bool initialized;
    private int currentPlayerScore;
    private int currentCpuScore;
    private bool closeButtonDefaultVisualCached;
    private Sprite closeButtonDefaultSprite;
    private Image.Type closeButtonDefaultImageType;
    private Color closeButtonDefaultColor;
    private Selectable.Transition closeButtonDefaultTransition;
    private ColorBlock closeButtonDefaultColors;

    private void Awake()
    {
        CacheRootComponents();
        // An inactive scene popup can be initialized by its provider before
        // Unity invokes Awake. Do not hide it again on its first Play call.
        if (!initialized)
        {
            HideImmediately();
        }
    }

    private void OnEnable()
    {
        if (initialized)
        {
            WireCloseButton();
        }
    }

    private void OnDisable()
    {
        UnwireCloseButton();
    }

    private void OnDestroy()
    {
        UnwireCloseButton();
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
        CacheCloseButtonDefaultVisual();
        WireCloseButton();
        HideImmediately();
        initialized = true;
    }

    /// <summary>
    /// Immediately releases any presentation wait and hides the cut-in.
    /// This is used when the owning session or component is cancelled.
    /// </summary>
    public void CancelDisplay()
    {
        closeRequested = true;
        HideImmediately();
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
               playerLifeDeductionText != null &&
               cpuLifeDeductionText != null &&
               specialActivationImage != null &&
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


    private void WireCloseButton()
    {
        UnwireCloseButton();
        if (closeButton == null)
        {
            return;
        }

        subscribedCloseButton = closeButton;
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void UnwireCloseButton()
    {
        if (subscribedCloseButton != null)
        {
            subscribedCloseButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        subscribedCloseButton = null;
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
        return parent != null ? parent.GetComponentInParent<Canvas>(true) : null;
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
        SetActive(specialActivationImage, false);
        SetActive(resultBackdropImage, false);
        SetActive(resultStampImage, false);
        SetActive(cpuResultStampImage, false);
        SetTextActive(playerLifeDeductionText, false);
        SetTextActive(cpuLifeDeductionText, false);

        gameObject.SetActive(false);
    }

}
