using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and controls the in-game role reference popup.
/// </summary>
public sealed class RoleListPanelController : MonoBehaviour
{
    [Header("Assets")]
    [SerializeField] private Sprite openButtonSprite;
    [SerializeField] private Sprite openButtonHighlightedSprite;
    [SerializeField] private Sprite firstPageSprite;
    [SerializeField] private Sprite secondPageSprite;
    [SerializeField] private Sprite combinedRoleSprite;
    [SerializeField] private Sprite closeButtonSprite;
    [SerializeField] private Sprite closeButtonHighlightedSprite;

    [Header("Scene References")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image roleListPage;

    private void Awake()
    {
        ConfigureSceneUi();
        Close();
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void ConfigureSceneUi()
    {
        if (openButton == null || closeButton == null || panelRoot == null || roleListPage == null)
        {
            Debug.LogError("RoleListPanelController: scene references are incomplete.", this);
            return;
        }

        Image openImage = openButton.targetGraphic as Image ?? openButton.GetComponent<Image>();
        ConfigureButtonSprites(openButton, openImage, openButtonSprite, openButtonHighlightedSprite);
        roleListPage.sprite = combinedRoleSprite != null ? combinedRoleSprite : firstPageSprite;
        roleListPage.preserveAspect = true;
        CloseButtonStyle.Apply(closeButton, closeButtonSprite, closeButtonHighlightedSprite);

        openButton.onClick.RemoveListener(Open);
        closeButton.onClick.RemoveListener(Close);
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
    }

    private static void ConfigureButtonSprites(
        Button button,
        Image image,
        Sprite normal,
        Sprite highlighted)
    {
        if (button == null || image == null) return;
        image.sprite = normal;
        image.color = Color.white;
        image.preserveAspect = true;
        button.targetGraphic = image;
        if (highlighted != null)
        {
            SpriteState state = button.spriteState;
            state.highlightedSprite = highlighted;
            state.pressedSprite = highlighted;
            state.selectedSprite = highlighted;
            button.spriteState = state;
            button.transition = Selectable.Transition.SpriteSwap;
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && !string.IsNullOrEmpty(gameObject.scene.path) &&
            (openButton == null || closeButton == null || panelRoot == null || roleListPage == null))
        {
            Debug.LogWarning("RoleListPanelController: assign all scene-authored references.", this);
        }
    }
}
