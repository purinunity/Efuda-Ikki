using UnityEngine;

/// <summary>
/// Scene-authored references for the fixed gameplay UI in latest.unity.
/// Layout is owned by RectTransforms in the scene; runtime code only changes state and content.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayUiReferences : MonoBehaviour
{
    public enum EditorPreview
    {
        None,
        Gameplay,
        HandReveal,
        DiscardPreview,
        RoleList,
        CoinToss,
        Showdown,
        BattleGroundHud,
        BattleGroundReward,
        SurrenderConfirmation,
        MatchResult,
        SpecialCardTooltip
    }

    [Header("Gameplay actions")]
    [SerializeField] private HandRevealPanel handRevealPanel;
    [SerializeField] private BattleGroundHud battleGroundHud;

    [Header("Overlays")]
    [SerializeField] private CoinTossPanel coinTossPanel;
    [SerializeField] private ShowdownCutInPopup showdownCutInPopup;
    [SerializeField] private MatchResultPanel matchResultPanel;
    [SerializeField] private BattleGroundRewardPanel battleGroundRewardPanel;
    [SerializeField] private RoleListPanelController roleListPanel;
    [SerializeField] private SpecialCardTooltip specialCardTooltip;

    [Header("Additional Scene Roots")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private GameObject discardPreviewRoot;
    [SerializeField] private GameObject roleListOverlayRoot;
    [SerializeField] private GameObject surrenderConfirmationRoot;

    [Header("Editor Preview")]
    [SerializeField] private EditorPreview editorPreview = EditorPreview.None;
    [SerializeField] private Sprite tooltipPreviewSprite;

    public HandRevealPanel HandRevealPanel => handRevealPanel;
    public BattleGroundHud BattleGroundHud => battleGroundHud;
    public CoinTossPanel CoinTossPanel => coinTossPanel;
    public ShowdownCutInPopup ShowdownCutInPopup => showdownCutInPopup;
    public MatchResultPanel MatchResultPanel => matchResultPanel;
    public BattleGroundRewardPanel BattleGroundRewardPanel => battleGroundRewardPanel;
    public RoleListPanelController RoleListPanel => roleListPanel;
    public SpecialCardTooltip SpecialCardTooltip => specialCardTooltip;

    private void Awake()
    {
        if (menuCanvas != null)
        {
            menuCanvas.enabled = true;
        }

        HidePreviewOnlyUi();
    }

    private void HidePreviewOnlyUi()
    {
        SetActive(handRevealPanel, false);
        SetActive(battleGroundHud, false);
        SetActive(discardPreviewRoot, false);
        SetActive(roleListOverlayRoot, false);
        SetActive(coinTossPanel, false);
        SetActive(showdownCutInPopup, false);
        SetActive(matchResultPanel, false);
        SetActive(battleGroundRewardPanel, false);
        SetActive(surrenderConfirmationRoot, false);
        SetTooltipVisible(false);
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null) component.gameObject.SetActive(active);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }

    private void SetTooltipVisible(bool visible)
    {
        if (specialCardTooltip == null) return;
        CanvasGroup group = specialCardTooltip.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        if (visible && tooltipPreviewSprite != null)
        {
            UnityEngine.UI.Image image = specialCardTooltip.GetComponent<UnityEngine.UI.Image>();
            if (image != null) image.sprite = tooltipPreviewSprite;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateReference(handRevealPanel, nameof(handRevealPanel));
        ValidateReference(battleGroundHud, nameof(battleGroundHud));
        ValidateReference(coinTossPanel, nameof(coinTossPanel));
        ValidateReference(showdownCutInPopup, nameof(showdownCutInPopup));
        ValidateReference(matchResultPanel, nameof(matchResultPanel));
        ValidateReference(battleGroundRewardPanel, nameof(battleGroundRewardPanel));
        ValidateReference(roleListPanel, nameof(roleListPanel));
        ValidateReference(specialCardTooltip, nameof(specialCardTooltip));
        ValidateReference(menuCanvas, nameof(menuCanvas));
        ValidateReference(discardPreviewRoot, nameof(discardPreviewRoot));
        ValidateReference(roleListOverlayRoot, nameof(roleListOverlayRoot));
        ValidateReference(surrenderConfirmationRoot, nameof(surrenderConfirmationRoot));

        if (!Application.isPlaying && gameObject.scene.IsValid())
        {
            UnityEditor.EditorApplication.delayCall -= ApplyEditorPreviewDelayed;
            UnityEditor.EditorApplication.delayCall += ApplyEditorPreviewDelayed;
        }
    }

    private void ApplyEditorPreviewDelayed()
    {
        UnityEditor.EditorApplication.delayCall -= ApplyEditorPreviewDelayed;
        if (this == null || Application.isPlaying || !gameObject.scene.IsValid()) return;
        ApplyEditorPreview();
    }

    [ContextMenu("Apply Editor Preview")]
    private void ApplyEditorPreview()
    {
        HidePreviewOnlyUi();
        if (menuCanvas != null) menuCanvas.enabled = editorPreview == EditorPreview.None;

        switch (editorPreview)
        {
            case EditorPreview.Gameplay:
                break;
            case EditorPreview.HandReveal:
                SetActive(handRevealPanel, true);
                break;
            case EditorPreview.DiscardPreview:
                SetActive(discardPreviewRoot, true);
                break;
            case EditorPreview.RoleList:
                SetActive(roleListOverlayRoot, true);
                break;
            case EditorPreview.CoinToss:
                if (coinTossPanel != null) coinTossPanel.ApplyEditorPreview();
                SetActive(coinTossPanel, true);
                break;
            case EditorPreview.Showdown:
                SetActive(showdownCutInPopup, true);
                CanvasGroup showdownGroup = showdownCutInPopup != null
                    ? showdownCutInPopup.GetComponent<CanvasGroup>()
                    : null;
                if (showdownGroup != null) showdownGroup.alpha = 1f;
                break;
            case EditorPreview.BattleGroundHud:
                SetActive(battleGroundHud, true);
                break;
            case EditorPreview.BattleGroundReward:
                SetActive(battleGroundRewardPanel, true);
                break;
            case EditorPreview.SurrenderConfirmation:
                SetActive(surrenderConfirmationRoot, true);
                break;
            case EditorPreview.MatchResult:
                if (matchResultPanel != null) matchResultPanel.ApplyEditorPreview();
                SetActive(matchResultPanel, true);
                break;
            case EditorPreview.SpecialCardTooltip:
                SetTooltipVisible(true);
                break;
        }
    }

    private void ValidateReference(Object value, string fieldName)
    {
        if (value == null)
        {
            Debug.LogWarning($"GameplayUiReferences: {fieldName} is not assigned.", this);
        }
    }
#endif
}
