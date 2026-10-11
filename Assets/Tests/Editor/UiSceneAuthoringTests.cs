using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public sealed class UiSceneAuthoringTests
{
    private const string ScenePath = "Assets/Scenes/latest.unity";

    [Test]
    public void LatestScene_FixedUiIsAuthoredAndReferenced()
    {
        Scene scene = OpenScene(out bool openedForTest);
        try
        {
            GameObject menuCanvas = FindRoot(scene, "MenuCanvas");
            GameObject gameCanvas = FindRoot(scene, "GameCanvas");

            Assert.That(menuCanvas.transform.Find("Screens/TitleScreen/TitleScreenPanel"), Is.Not.Null);
            Assert.That(menuCanvas.transform.Find("Screens/ModeSelectScreen/ModeSelectPanel"), Is.Not.Null);
            Assert.That(menuCanvas.transform.Find("Screens/StageSelectScreen/StageSelectPanel"), Is.Not.Null);
            Assert.That(menuCanvas.transform.Find("Screens/SpecialCardSelectScreen/SpecialCardSelectPanel"), Is.Not.Null);
            Assert.That(menuCanvas.transform.Find("Screens/SettingsScreen/SettingsPanel"), Is.Not.Null);

            Assert.That(gameCanvas.transform.Find("GameplayScreen/CardAreas"), Is.Not.Null);
            Assert.That(gameCanvas.transform.Find("GameplayScreen/CharacterHud"), Is.Not.Null);
            Assert.That(gameCanvas.transform.Find("GameplayScreen/Background/MainBackground"), Is.Not.Null);
            Assert.That(gameCanvas.transform.Find("GameplayScreen/CharacterHud/CharacterManager/IkkiRoundView"), Is.Not.Null);
            Assert.That(gameCanvas.transform.Find("GameplayScreen/CharacterHud/CharacterManager/BattleGroundStreakView"), Is.Not.Null);
            Assert.That(gameCanvas.transform.Find("GameplayScreen/Actions/HandRevealButton"), Is.Not.Null);
            Assert.That(gameCanvas.transform.Find("OverlayLayer/CoinTossPanel"), Is.Not.Null);
            Assert.That(gameCanvas.transform.Find("OverlayLayer/MatchResultPanel"), Is.Not.Null);
            Assert.That(gameCanvas.transform.Find("OverlayLayer/BattleGroundRewardPanel"), Is.Not.Null);
            Assert.That(gameCanvas.transform.Find("OverlayLayer/SpecialCardTooltip"), Is.Not.Null);

            GameplayUiReferences references = gameCanvas.GetComponent<GameplayUiReferences>();
            Assert.That(references, Is.Not.Null);
            Assert.That(references.HandRevealPanel, Is.Not.Null);
            Assert.That(references.BattleGroundHud, Is.Not.Null);
            Assert.That(references.CoinTossPanel, Is.Not.Null);
            Assert.That(references.ShowdownCutInPopup, Is.Not.Null);
            Assert.That(references.MatchResultPanel, Is.Not.Null);
            Assert.That(references.BattleGroundRewardPanel, Is.Not.Null);
            Assert.That(references.RoleListPanel, Is.Not.Null);
            Assert.That(references.SpecialCardTooltip, Is.Not.Null);

            var gameplayReferences = new SerializedObject(references);
            Assert.That(gameplayReferences.FindProperty("menuCanvas").objectReferenceValue, Is.Not.Null);
            Assert.That(gameplayReferences.FindProperty("discardPreviewRoot").objectReferenceValue, Is.Not.Null);
            Assert.That(gameplayReferences.FindProperty("roleListOverlayRoot").objectReferenceValue, Is.Not.Null);
            Assert.That(gameplayReferences.FindProperty("surrenderConfirmationRoot").objectReferenceValue, Is.Not.Null);
            Assert.That(
                gameplayReferences.FindProperty("editorPreview").enumValueIndex,
                Is.EqualTo((int)GameplayUiReferences.EditorPreview.None));

            Transform overlayLayer = gameCanvas.transform.Find("OverlayLayer");
            Assert.That(overlayLayer.Find("PopUpTrush").gameObject.activeSelf, Is.False);
            Assert.That(overlayLayer.Find("RoleListPanel").gameObject.activeSelf, Is.False);
            Assert.That(overlayLayer.Find("CoinTossPanel").gameObject.activeSelf, Is.False);
            Assert.That(overlayLayer.Find("PopUpShowdown").gameObject.activeSelf, Is.False);
            Assert.That(overlayLayer.Find("BattleGroundRewardPanel").gameObject.activeSelf, Is.False);
            Assert.That(overlayLayer.Find("SurrenderConfirmation").gameObject.activeSelf, Is.False);
            Assert.That(overlayLayer.Find("MatchResultPanel").gameObject.activeSelf, Is.False);

            int missingScripts = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject));
            Assert.That(missingScripts, Is.Zero);
        }
        finally
        {
            if (openedForTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void CloseButtonStyle_PreservesAuthoredRectTransform()
    {
        var target = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        try
        {
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.3f);
            rect.anchorMax = new Vector2(0.4f, 0.6f);
            rect.pivot = new Vector2(0.25f, 0.75f);
            rect.anchoredPosition = new Vector2(37f, -21f);
            rect.sizeDelta = new Vector2(91f, 73f);
            rect.localScale = new Vector3(0.8f, 0.9f, 1f);

            Vector2 anchorMin = rect.anchorMin;
            Vector2 anchorMax = rect.anchorMax;
            Vector2 pivot = rect.pivot;
            Vector2 position = rect.anchoredPosition;
            Vector2 size = rect.sizeDelta;
            Vector3 scale = rect.localScale;

            CloseButtonStyle.Apply(target.GetComponent<Button>());

            Assert.That(rect.anchorMin, Is.EqualTo(anchorMin));
            Assert.That(rect.anchorMax, Is.EqualTo(anchorMax));
            Assert.That(rect.pivot, Is.EqualTo(pivot));
            Assert.That(rect.anchoredPosition, Is.EqualTo(position));
            Assert.That(rect.sizeDelta, Is.EqualTo(size));
            Assert.That(rect.localScale, Is.EqualTo(scale));
        }
        finally
        {
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void CloseButtonStyle_HidesLegacyTextLabel()
    {
        var target = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(target.transform, false);
        label.GetComponent<TextMeshProUGUI>().text = "X";

        try
        {
            CloseButtonStyle.Apply(target.GetComponent<Button>());

            Assert.That(label.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void LatestScene_ShowdownForegroundOrderKeepsActivationAndCloseVisible()
    {
        Scene scene = OpenScene(out bool openedForTest);
        try
        {
            Transform stage = FindRoot(scene, "GameCanvas").transform
                .Find("OverlayLayer/PopUpShowdown/Stage");
            Transform callBackdrop = stage.Find("SpecialCallBackdrop");
            Transform cpuSpecial = stage.Find("CpuSpecialCard");
            Transform playerSpecial = stage.Find("PlayerSpecialCard");
            Transform activation = stage.Find("SpecialActivation");
            Transform close = stage.Find("CloseButton");

            Assert.That(callBackdrop.GetSiblingIndex(), Is.LessThan(cpuSpecial.GetSiblingIndex()));
            Assert.That(callBackdrop.GetSiblingIndex(), Is.LessThan(playerSpecial.GetSiblingIndex()));
            Assert.That(cpuSpecial.GetSiblingIndex(), Is.LessThan(activation.GetSiblingIndex()));
            Assert.That(playerSpecial.GetSiblingIndex(), Is.LessThan(activation.GetSiblingIndex()));
            Assert.That(activation.GetSiblingIndex(), Is.LessThan(close.GetSiblingIndex()));
            Assert.That(cpuSpecial.GetComponent<Canvas>(), Is.Null);
            Assert.That(playerSpecial.GetComponent<Canvas>(), Is.Null);

            Button closeButton = close.GetComponent<Button>();
            Assert.That(closeButton.GetComponent<Image>().sprite, Is.Not.Null);
            TMP_Text label = closeButton.GetComponentInChildren<TMP_Text>(true);
            Assert.That(label == null || !label.gameObject.activeSelf, Is.True);
        }
        finally
        {
            if (openedForTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void TitleUiManager_EditorPreviewShowsExactlyOneMenuScreen()
    {
        var root = new GameObject("TitleUiPreviewTest");
        var manager = root.AddComponent<TitleUIManager>();
        manager.titleScreenPanel = CreateCanvasGroup(root.transform, "Title");
        manager.modeSelectPanel = CreateCanvasGroup(root.transform, "ModeSelect");
        manager.stageSelectPanel = CreateCanvasGroup(root.transform, "StageSelect");
        manager.specialCardSelectPanel = CreateCanvasGroup(root.transform, "SpecialCardSelect");
        manager.settingsPanel = CreateCanvasGroup(root.transform, "Settings");

        try
        {
            MethodInfo applyPreview = typeof(TitleUIManager).GetMethod(
                "ApplyEditorPreview",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(applyPreview, Is.Not.Null);

            var serialized = new SerializedObject(manager);
            SerializedProperty preview = serialized.FindProperty("editorPreviewScreen");
            CanvasGroup[] panels =
            {
                manager.titleScreenPanel,
                manager.modeSelectPanel,
                manager.stageSelectPanel,
                manager.specialCardSelectPanel,
                manager.settingsPanel
            };

            for (int index = 0; index < panels.Length; index++)
            {
                preview.enumValueIndex = index;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                applyPreview.Invoke(manager, null);

                for (int panelIndex = 0; panelIndex < panels.Length; panelIndex++)
                {
                    Assert.That(
                        panels[panelIndex].alpha,
                        Is.EqualTo(panelIndex == index ? 1f : 0f),
                        $"Preview {index}, panel {panelIndex}");
                }
            }

            manager.ShowTitleScreen();
            Assert.That(manager.titleScreenPanel.alpha, Is.EqualTo(1f));
            Assert.That(panels.Skip(1).All(panel => panel.alpha == 0f), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static CanvasGroup CreateCanvasGroup(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        child.transform.SetParent(parent, false);
        return child.GetComponent<CanvasGroup>();
    }

    private static Scene OpenScene(out bool openedForTest)
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        openedForTest = !scene.IsValid() || !scene.isLoaded;
        return openedForTest
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
            : scene;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        GameObject root = scene.GetRootGameObjects().Single(value => value.name == name);
        Assert.That(root, Is.Not.Null);
        return root;
    }
}
