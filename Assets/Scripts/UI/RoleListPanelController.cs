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
    [SerializeField] private Sprite closeButtonSprite;
    [SerializeField] private Sprite closeButtonHighlightedSprite;

    private Button openButton;
    private Button closeButton;
    private GameObject panelRoot;

    private void Awake()
    {
        BuildUi();
        Close();
    }

    private void OnDestroy()
    {
        if (openButton != null) openButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (panelRoot == null) BuildUi();
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void BuildUi()
    {
        if (panelRoot != null) return;

        openButton = CreateButton(
            "RoleListButton",
            transform,
            openButtonSprite,
            openButtonHighlightedSprite,
            new Vector2(1f, 1f),
            new Vector2(-204f, -460f),
            new Vector2(280f, 80f));
        openButton.onClick.AddListener(Open);

        panelRoot = new GameObject(
            "RoleListPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panelRoot.transform.SetParent(transform, false);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        Stretch(panelRect);

        Image dimmer = panelRoot.GetComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.82f);
        dimmer.raycastTarget = true;

        CreatePage("RoleListPage1", firstPageSprite, new Vector2(-382f, -6f));
        CreatePage("RoleListPage2", secondPageSprite, new Vector2(382f, -6f));

        closeButton = CreateButton(
            "RoleListCloseButton",
            panelRoot.transform,
            closeButtonSprite,
            closeButtonHighlightedSprite,
            new Vector2(1f, 1f),
            new Vector2(-76f, -58f),
            new Vector2(112f, 80f));
        closeButton.onClick.AddListener(Close);
        closeButton.transform.SetAsLastSibling();
    }

    private void CreatePage(string objectName, Sprite sprite, Vector2 anchoredPosition)
    {
        GameObject page = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        page.transform.SetParent(panelRoot.transform, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(720f, 615f);

        Image image = page.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private static Button CreateButton(
        string objectName,
        Transform parent,
        Sprite normalSprite,
        Sprite highlightedSprite,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = normalSprite;
        image.color = Color.white;
        image.preserveAspect = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        if (highlightedSprite != null)
        {
            SpriteState state = button.spriteState;
            state.highlightedSprite = highlightedSprite;
            state.pressedSprite = highlightedSprite;
            state.selectedSprite = highlightedSprite;
            button.spriteState = state;
            button.transition = Selectable.Transition.SpriteSwap;
        }

        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
