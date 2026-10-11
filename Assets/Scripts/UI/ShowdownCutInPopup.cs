using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static HandEvaluator;

public partial class ShowdownCutInPopup : MonoBehaviour
{
    [SerializeField] private ShowdownCutInAssetSet assetSet;
#pragma warning disable CS0414 // Serialized for existing scene-contract compatibility; runtime fallback is intentionally disabled.
    [SerializeField, HideInInspector] private bool buildMissingUiAtRuntime = false;
#pragma warning restore CS0414
    [SerializeField] private Color roleCardDimColor = new Color(1f, 1f, 1f, 0.34f);
    [SerializeField] private Color activeSpecialCardTint = new Color(1f, 0.86f, 0.18f, 1f);
    [SerializeField] private Color inactiveSpecialCardTint = new Color(1f, 1f, 1f, 0.42f);
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
    [SerializeField] private Image playerLifeDeductionFrameImage;
    [SerializeField] private Image cpuLifeDeductionFrameImage;
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
    [SerializeField] private RectTransform roleEffectRoot;
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
    private Vector3 playerSpecialCardInitialScale = Vector3.one;
    private Vector3 cpuSpecialCardInitialScale = Vector3.one;
    private readonly List<GameObject> activeRoleEffects = new List<GameObject>();

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
        ClearRoleEffects();
    }

    private void OnDestroy()
    {
        UnwireCloseButton();
        ClearRoleEffects();
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

        if (!HasRequiredUiReferences())
            Debug.LogError("ShowdownCutInPopup: hierarchy references are incomplete.", this);

        ConfigureUiReferences();
        if (playerSpecialCardImage != null)
        {
            playerSpecialCardInitialScale = playerSpecialCardImage.rectTransform.localScale;
        }
        if (cpuSpecialCardImage != null)
        {
            cpuSpecialCardInitialScale = cpuSpecialCardImage.rectTransform.localScale;
        }
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

    private void CacheRootComponents()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
        {
            Debug.LogError("ShowdownCutInPopup: CanvasGroup is not assigned.", this);
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
               playerLifeDeductionFrameImage != null &&
               cpuLifeDeductionFrameImage != null &&
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
               roleEffectRoot != null &&
               closeButton != null;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && !string.IsNullOrEmpty(gameObject.scene.path) &&
            !HasRequiredUiReferences())
        {
            Debug.LogWarning("ShowdownCutInPopup: assign all fixed cut-in references in the Inspector.", this);
        }
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
        ClearRoleEffects();
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
        SetLifeDeductionActive(playerLifeDeductionText, false);
        SetLifeDeductionActive(cpuLifeDeductionText, false);

        gameObject.SetActive(false);
    }

    private void PlayRoleEffects(HandRank playerRank, HandRank cpuRank)
    {
        ClearRoleEffects();
        SpawnRoleEffect(playerRank, playerRoleImage != null ? playerRoleImage.rectTransform : null);
        SpawnRoleEffect(cpuRank, cpuRoleImage != null ? cpuRoleImage.rectTransform : null);
    }

    private void SpawnRoleEffect(HandRank rank, RectTransform target)
    {
        if (assetSet == null || stage == null || roleEffectRoot == null || target == null)
        {
            return;
        }

        GameObject prefab = assetSet.GetRoleEffectPrefab(rank);
        if (prefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(prefab, roleEffectRoot, false);
        effect.name = $"{rank}CutInEffect";
        effect.transform.localPosition = roleEffectRoot.InverseTransformPoint(target.position);
        effect.transform.localScale = prefab.transform.localScale * 0.18f;

        // The effect root is deliberately behind cards and text.  A modest
        // scale keeps the world-space particle prefab inside its role frame.
        foreach (ParticleSystemRenderer renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            renderer.sortingOrder = 1;
        }
        activeRoleEffects.Add(effect);
    }

    private void ClearRoleEffects()
    {
        foreach (GameObject effect in activeRoleEffects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
        activeRoleEffects.Clear();
    }

}
