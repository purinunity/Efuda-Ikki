using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuPanel : MonoBehaviour
{
    public Button kinouAriButton;
    public Button battleGroundButton;
    public Button settingsButton;
    public TitleUIManager titleUIManager;

    [Header("Mode Button Images")]
    [SerializeField] private Sprite ikkiModeButtonSprite;
    [SerializeField] private Sprite kachinukiModeButtonSprite;
    [SerializeField] private Sprite settingsButtonSprite;
    [SerializeField] private bool hideButtonTextLabels = true;

    private void Awake()
    {
        ApplyModeButtonImages();
    }

    private void Start()
    {
        if (kinouAriButton != null)
        {
            kinouAriButton.onClick.AddListener(() => titleUIManager.SelectIkkiMode());
        }
        if (battleGroundButton != null)
        {
            battleGroundButton.onClick.AddListener(() => titleUIManager.SelectKachinukiMode());
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(() => titleUIManager.ShowSettings());
        }

        ApplyModeButtonImages();
        RefreshModeAvailability();
    }

    private void OnValidate()
    {
        ApplyModeButtonImages();
    }

    public void RefreshModeAvailability()
    {
        if (battleGroundButton != null)
        {
            battleGroundButton.interactable = GameProgressStore.IsKachinukiUnlocked;
        }
    }

    private void ApplyModeButtonImages()
    {
        ApplyButtonImage(kinouAriButton, ikkiModeButtonSprite);
        ApplyButtonImage(battleGroundButton, kachinukiModeButtonSprite);
        ApplyButtonImage(settingsButton, settingsButtonSprite);
    }

    private void ApplyButtonImage(Button button, Sprite sprite)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        if (image != null && sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            button.targetGraphic = image;
        }

        SetTextLabelsActive(button.transform, !hideButtonTextLabels);
    }

    private static void SetTextLabelsActive(Transform root, bool isActive)
    {
        if (root == null)
        {
            return;
        }

        foreach (Text label in root.GetComponentsInChildren<Text>(true))
        {
            label.gameObject.SetActive(isActive);
        }

        foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            label.gameObject.SetActive(isActive);
        }
    }
}
