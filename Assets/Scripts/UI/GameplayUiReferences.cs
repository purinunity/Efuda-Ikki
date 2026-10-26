using UnityEngine;

/// <summary>
/// Scene composition references for the gameplay and overlay prefabs.
/// RectTransforms and hierarchy are authored in the prefabs; this component
/// only exposes stable runtime references.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayUiReferences : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private GameObject gameplayScreen;
    [SerializeField] private HandRevealPanel handRevealPanel;
    [SerializeField] private BattleGroundHud battleGroundHud;

    [Header("Overlays")]
    [SerializeField] private GameObject discardPreviewRoot;
    [SerializeField] private PopupPreviewCardArea discardSourceArea;
    [SerializeField] private DiscardPreviewPanel discardPreviewPanel;
    [SerializeField] private RoleListPanelController roleListPanel;
    [SerializeField] private CoinTossPanel coinTossPanel;
    [SerializeField] private ShowdownCutInPopup showdownCutInPopup;
    [SerializeField] private BattleGroundRewardPanel battleGroundRewardPanel;
    [SerializeField] private GameObject surrenderConfirmationRoot;
    [SerializeField] private MatchResultPanel matchResultPanel;
    [SerializeField] private SpecialCardTooltip specialCardTooltip;

    public GameObject GameplayScreen => gameplayScreen;
    public HandRevealPanel HandRevealPanel => handRevealPanel;
    public BattleGroundHud BattleGroundHud => battleGroundHud;
    public GameObject DiscardPreviewRoot => discardPreviewRoot;
    public DiscardPreviewPanel DiscardPreviewPanel => discardPreviewPanel;
    public RoleListPanelController RoleListPanel => roleListPanel;
    public CoinTossPanel CoinTossPanel => coinTossPanel;
    public ShowdownCutInPopup ShowdownCutInPopup => showdownCutInPopup;
    public BattleGroundRewardPanel BattleGroundRewardPanel => battleGroundRewardPanel;
    public GameObject SurrenderConfirmationRoot => surrenderConfirmationRoot;
    public MatchResultPanel MatchResultPanel => matchResultPanel;
    public SpecialCardTooltip SpecialCardTooltip => specialCardTooltip;

    private void Awake()
    {
        discardSourceArea?.ConfigurePreview(discardPreviewPanel);
        SetActive(handRevealPanel, false);
        SetActive(battleGroundHud, false);
        SetActive(discardPreviewRoot, false);
        SetActive(roleListPanel, false);
        SetActive(coinTossPanel, false);
        SetActive(showdownCutInPopup, false);
        SetActive(battleGroundRewardPanel, false);
        SetActive(surrenderConfirmationRoot, false);
        SetActive(matchResultPanel, false);
        HideTooltip();
    }

    private static void SetActive(Component value, bool active)
    {
        if (value != null) value.gameObject.SetActive(active);
    }

    private static void SetActive(GameObject value, bool active)
    {
        if (value != null) value.SetActive(active);
    }

    private void HideTooltip()
    {
        if (specialCardTooltip == null) return;
        CanvasGroup group = specialCardTooltip.GetComponent<CanvasGroup>();
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void OnValidate()
    {
        if (gameplayScreen == null || handRevealPanel == null || battleGroundHud == null ||
            discardPreviewRoot == null || discardSourceArea == null || discardPreviewPanel == null ||
            roleListPanel == null || coinTossPanel == null ||
            showdownCutInPopup == null || battleGroundRewardPanel == null ||
            surrenderConfirmationRoot == null || matchResultPanel == null || specialCardTooltip == null)
        {
            Debug.LogWarning("GameplayUiReferences: assign every gameplay/overlay prefab instance.", this);
        }
    }
}
