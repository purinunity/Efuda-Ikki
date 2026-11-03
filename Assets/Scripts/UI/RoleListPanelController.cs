using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the in-game role reference popup (open / close).
/// The page image itself is handled by RoleListPage.
/// </summary>
public sealed class RoleListPanelController : MonoBehaviour
{
    [Header("Button Sprites")]
    [SerializeField] private Sprite openButtonSprite;
    [SerializeField] private Sprite openButtonHighlightedSprite;
    [SerializeField] private Sprite closeButtonSprite;
    [SerializeField] private Sprite closeButtonHighlightedSprite;

    [Header("Scene References")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panelRoot;

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
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void ConfigureSceneUi()
    {
        if (!HasAllReferences())
        {
            Debug.LogError("RoleListPanelController: scene references are incomplete.", this);
            return;
        }

        Image openImage = openButton.targetGraphic as Image;
        if (openImage == null) openImage = openButton.GetComponent<Image>();
        ConfigureButtonSprites(openButton, openImage, openButtonSprite, openButtonHighlightedSprite);
        CloseButtonStyle.Apply(closeButton, closeButtonSprite, closeButtonHighlightedSprite);

        openButton.onClick.RemoveListener(Open);
        closeButton.onClick.RemoveListener(Close);
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(Close);
    }

    private bool HasAllReferences()
    {
        return openButton != null && closeButton != null && panelRoot != null;
    }

    private static void ConfigureButtonSprites(
        Button button,
        Image image,
        Sprite normal,
        Sprite highlighted)
    {
        if (button == null || image == null) return;

        if (normal != null) image.sprite = normal;
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
        if (!Application.isPlaying && !string.IsNullOrEmpty(gameObject.scene.path) && !HasAllReferences())
        {
            Debug.LogWarning("RoleListPanelController: assign all scene-authored references.", this);
        }
    }
}