using System.Collections.Generic;
using System.Linq;
using EfudaIkki.Core;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UiSpriteReferenceTests
{
    [Test]
    public void MainMenuPanel_BattleGroundButtonUsesLockedArtworkUntilUnlocked()
    {
        IProgressRepository originalRepository = GameProgressStore.Repository;
        var repository = new MemoryProgressRepository();
        GameProgressStore.Repository = repository;

        GameObject panelObject = null;
        try
        {
            panelObject = new GameObject("MainMenuPanel Test");
            MainMenuPanel panel = panelObject.AddComponent<MainMenuPanel>();

            GameObject buttonObject = new GameObject("BattleGround", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panelObject.transform, false);
            Image image = buttonObject.GetComponent<Image>();
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            panel.battleGroundButton = button;

            GameObject labelObject = new GameObject("BattleGroundStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI statusLabel = labelObject.GetComponent<TextMeshProUGUI>();

            Sprite normal = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Sprite/production/ui/buttons/title/menu2(最新)/bg.png");
            Sprite hover = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Sprite/production/ui/buttons/title/menu2(最新)/bg_hover.png");
            Sprite locked = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Sprite/production/ui/buttons/title/menu2(最新)/bg_locked.png");

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("kachinukiModeButtonSprite").objectReferenceValue = normal;
            serialized.FindProperty("kachinukiModeButtonHoverSprite").objectReferenceValue = hover;
            serialized.FindProperty("lockedBattleGroundButtonSprite").objectReferenceValue = locked;
            serialized.FindProperty("battleGroundStatusLabel").objectReferenceValue = statusLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            panel.RefreshModeAvailability();

            Assert.That(image.sprite, Is.SameAs(locked));
            Assert.That(image.color, Is.EqualTo(Color.white));
            Assert.That(button.interactable, Is.False);
            Assert.That(button.transition, Is.EqualTo(Selectable.Transition.None));
            Assert.That(statusLabel.gameObject.activeSelf, Is.False);

            GameProgressStore.UnlockAllProgress();
            panel.RefreshModeAvailability();

            Assert.That(image.sprite, Is.SameAs(normal));
            Assert.That(button.interactable, Is.True);
            Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
            Assert.That(button.spriteState.highlightedSprite, Is.SameAs(hover));
            Assert.That(statusLabel.gameObject.activeSelf, Is.True);
        }
        finally
        {
            GameProgressStore.Repository = originalRepository;
            if (panelObject != null)
            {
                Object.DestroyImmediate(panelObject);
            }
        }
    }

    [Test]
    public void UpdatedCutInAndResultAssetsResolveToRequestedProductionFiles()
    {
        ShowdownCutInAssetSet cutIn = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        MatchResultVisualAssets result = Resources.Load<MatchResultVisualAssets>("MatchResultVisualAssets");

        Assert.That(AssetDatabase.GetAssetPath(cutIn.cutInBackground), Does.EndWith("/ui/backgrounds/新/cut_in.png"));
        Assert.That(AssetDatabase.GetAssetPath(cutIn.roleFrame), Does.EndWith("/ui/frames/汎用panel.png"));
        Assert.That(AssetDatabase.GetAssetPath(cutIn.lifeDeductionFrame), Does.EndWith("/ui/frames/minus2.png"));
        Assert.That(AssetDatabase.GetAssetPath(result.winBackground), Does.EndWith("/ui/backgrounds/results/新/win_background.png"));
        Assert.That(AssetDatabase.GetAssetPath(result.loseBackground), Does.EndWith("/ui/backgrounds/results/新/lose_background.png"));
        Assert.That(cutIn.roleEffectPrefabs, Has.Length.EqualTo(11));
        Assert.That(cutIn.roleEffectPrefabs, Has.All.Not.Null);
    }

    [TestCase("StageSelectPanel", "ui/backgrounds/新/character_select.png")]
    [TestCase("SpecialCardSelectPanel", "ui/backgrounds/新/card_select.png")]
    [TestCase("D_button", "ui/buttons/common/confirm_discard.png")]
    [TestCase("StartButton", "ui/buttons/common/confirm_special.png")]
    public void LatestScene_RequiredImageResolvesToProductionSprite(string objectName, string spritePath)
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/latest.unity");
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
            scene = EditorSceneManager.OpenScene("Assets/Scenes/latest.unity", OpenSceneMode.Additive);
        try
        {
            Image image = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Image>(true))
                .Single(value => value.name == objectName);
            Assert.That(image.sprite, Is.Not.Null, objectName);
            Assert.That(AssetDatabase.GetAssetPath(image.sprite), Is.EqualTo("Assets/Sprite/production/" + spritePath));
        }
        finally
        {
            if (openedForTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void LatestScene_RoleListPanelUsesProductionSprites()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/latest.unity");
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
            scene = EditorSceneManager.OpenScene("Assets/Scenes/latest.unity", OpenSceneMode.Additive);
        try
        {
            RoleListPanelController panel = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<RoleListPanelController>(true))
                .Single();
            var serialized = new SerializedObject(panel);
            AssertSprite(serialized, "openButtonSprite", "ui/buttons/common/show_role_list2.png");
            AssertSprite(serialized, "combinedRoleSprite", "ui/reference/roles/役一覧.png");
            AssertSprite(serialized, "closeButtonSprite", "ui/buttons/common/close.png");
        }
        finally
        {
            if (openedForTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void AssertSprite(SerializedObject serialized, string propertyName, string relativePath)
    {
        var sprite = serialized.FindProperty(propertyName).objectReferenceValue;
        Assert.That(sprite, Is.Not.Null, propertyName);
        Assert.That(AssetDatabase.GetAssetPath(sprite),
            Is.EqualTo("Assets/Sprite/production/" + relativePath));
    }

    private sealed class MemoryProgressRepository : IProgressRepository
    {
        private readonly Dictionary<string, string> strings = new Dictionary<string, string>();
        private readonly Dictionary<string, int> ints = new Dictionary<string, int>();

        public string GetString(string key, string defaultValue) =>
            strings.TryGetValue(key, out string value) ? value : defaultValue;

        public int GetInt(string key, int defaultValue) =>
            ints.TryGetValue(key, out int value) ? value : defaultValue;

        public void SetString(string key, string value) => strings[key] = value;
        public void SetInt(string key, int value) => ints[key] = value;
        public void Save() { }
    }
}
