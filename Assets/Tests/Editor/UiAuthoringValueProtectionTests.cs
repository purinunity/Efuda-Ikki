using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UiAuthoringValueProtectionTests
{
    private Scene previewScene;
    private GameObject root;
    private Texture2D texture;
    private Sprite authoredSprite;
    private Sprite fallbackSprite;

    [SetUp]
    public void SetUp()
    {
        previewScene = EditorSceneManager.NewPreviewScene();
        root = new GameObject("Authoring value protection", typeof(RectTransform));
        root.SetActive(false);
        SceneManager.MoveGameObjectToScene(root, previewScene);
        texture = new Texture2D(2, 2);
        authoredSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        fallbackSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null) Object.DestroyImmediate(root);
        if (authoredSprite != null) Object.DestroyImmediate(authoredSprite);
        if (fallbackSprite != null) Object.DestroyImmediate(fallbackSprite);
        if (texture != null) Object.DestroyImmediate(texture);
        if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ModeSelect_EditorValidationPreservesAuthoredButtonAndLabelValues(bool hideLabels)
    {
        MainMenuPanel panel = root.AddComponent<MainMenuPanel>();
        Button button = CreateButton("Authored mode button");
        panel.kinouAriButton = button;
        SetField(panel, "hideButtonTextLabels", hideLabels);
        SetField(panel, "ikkiModeButtonSprite", fallbackSprite);
        SetField(panel, "ikkiModeButtonHoverSprite", fallbackSprite);
        Image image = button.image;
        image.sprite = authoredSprite;
        image.type = Image.Type.Filled;
        image.preserveAspect = false;
        Color authoredColor = new Color(0.2f, 0.4f, 0.6f, 0.8f);
        image.color = authoredColor;
        button.transition = Selectable.Transition.ColorTint;
        SpriteState authoredState = button.spriteState;
        authoredState.highlightedSprite = authoredSprite;
        authoredState.pressedSprite = authoredSprite;
        authoredState.selectedSprite = authoredSprite;
        button.spriteState = authoredState;
        Text label = CreateChild(button.transform, "Authored label", typeof(Text)).GetComponent<Text>();
        label.gameObject.SetActive(hideLabels);

        InvokeValidationIfPresent(panel);

        Assert.That(image.sprite, Is.SameAs(authoredSprite));
        Assert.That(image.type, Is.EqualTo(Image.Type.Filled));
        Assert.That(image.preserveAspect, Is.False);
        Assert.That(image.color, Is.EqualTo(authoredColor));
        Assert.That(button.transition, Is.EqualTo(Selectable.Transition.ColorTint));
        Assert.That(button.spriteState.highlightedSprite, Is.SameAs(authoredSprite));
        Assert.That(button.spriteState.pressedSprite, Is.SameAs(authoredSprite));
        Assert.That(button.spriteState.selectedSprite, Is.SameAs(authoredSprite));
        Assert.That(label.gameObject.activeSelf, Is.EqualTo(hideLabels));
        Assert.That(panel.kinouAriButton, Is.SameAs(button));
    }

    [Test]
    public void StageSelect_EditorValidationPreservesVisibleLabelsAndSavedReferences()
    {
        StageSelectPanel panel = root.AddComponent<StageSelectPanel>();
        Button button = CreateButton("Authored stage button");
        Text label = CreateChild(button.transform, "Visible stage label", typeof(Text)).GetComponent<Text>();
        TextMeshProUGUI info = CreateChild(root.transform, "Visible info", typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        panel.stageButtons = new[] { button };
        panel.stageInfoText = info;
        SetField(panel, "hideLegacyTextLabels", true);
        button.image.sprite = authoredSprite;
        label.gameObject.SetActive(true);
        info.gameObject.SetActive(true);
        Button[] authoredButtons = panel.stageButtons;

        InvokeValidationIfPresent(panel);

        Assert.That(label.gameObject.activeSelf, Is.True);
        Assert.That(info.gameObject.activeSelf, Is.True);
        Assert.That(panel.stageButtons, Is.SameAs(authoredButtons));
        Assert.That(panel.stageButtons[0], Is.SameAs(button));
        Assert.That(panel.stageInfoText, Is.SameAs(info));
        Assert.That(button.image.sprite, Is.SameAs(authoredSprite));
    }

    private Button CreateButton(string name)
    {
        return CreateChild(root.transform, name, typeof(Image), typeof(Button)).GetComponent<Button>();
    }

    private static GameObject CreateChild(Transform parent, string name, params System.Type[] components)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        foreach (System.Type component in components) child.AddComponent(component);
        return child;
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        field.SetValue(target, value);
    }

    private static void InvokeValidationIfPresent(MonoBehaviour component)
    {
        // Removing OnValidate is valid; a future validation callback must remain read-only.
        component.GetType().GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(component, null);
    }
}
