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

            GameObject buttonObject = new GameObject(
                "BattleGround",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LocalizeSprite));
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

            // A normal sprite arriving before the localized locked sprite must
            // not replace the immediate locked fallback.
            LocalizeSprite localizeSprite = buttonObject.GetComponent<LocalizeSprite>();
            typeof(LocalizeSprite)
                .GetMethod("OnNormalChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(localizeSprite, new object[] { normal });
            Assert.That(image.sprite, Is.SameAs(locked));

            GameProgressStore.UnlockAllProgress();
            panel.RefreshModeAvailability();

            Assert.That(image.sprite, Is.SameAs(normal));
            Assert.That(button.interactable, Is.True);
            Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap));
            Assert.That(button.spriteState.highlightedSprite, Is.SameAs(hover));
            Assert.That(button.spriteState.selectedSprite, Is.Null);
            Assert.That(statusLabel.gameObject.activeSelf, Is.True);

            typeof(LocalizeSprite)
                .GetMethod("OnNormalChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(localizeSprite, new object[] { normal });
            typeof(LocalizeSprite)
                .GetMethod("OnHighlightedChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(localizeSprite, new object[] { hover });
            Assert.That(image.overrideSprite, Is.SameAs(normal),
                "Loading the hover asset must not activate the hover artwork.");
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
        ShowdownCutInAssetSet cutIn = AssetDatabase.LoadAssetAtPath<ShowdownCutInAssetSet>("Assets/Data/UI/ShowdownCutInAssets.asset");
        MatchResultVisualAssets result = AssetDatabase.LoadAssetAtPath<MatchResultVisualAssets>("Assets/Data/UI/MatchResultVisualAssets.asset");

        Assert.That(AssetDatabase.GetAssetPath(cutIn.cutInBackground), Does.EndWith("/ui/backgrounds/common/cut_in.png"));
        Assert.That(AssetDatabase.GetAssetPath(cutIn.roleFrame), Does.EndWith("/ui/cut_in/汎用panel.png"));
        Assert.That(AssetDatabase.GetAssetPath(cutIn.lifeDeductionFrame), Does.EndWith("/ui/parts/minus2.png"));
        Assert.That(AssetDatabase.GetAssetPath(result.winBackground), Does.EndWith("/ui/backgrounds/common/ja/win_background.png"));
        Assert.That(AssetDatabase.GetAssetPath(result.loseBackground), Does.EndWith("/ui/backgrounds/common/ja/lose_background.png"));
        Assert.That(cutIn.roleEffectPrefabs, Has.Length.EqualTo(11));
        Assert.That(cutIn.roleEffectPrefabs, Has.All.Not.Null);
    }

    [TestCase("StageSelectPanel", "ui/backgrounds/common/character_select.png")]
    [TestCase("SpecialCardSelectPanel", "ui/backgrounds/common/ja/card_select.png")]
    [TestCase("D_button", "ui/button/common/ja/confirm_discard②.png")]
    [TestCase("StartButton", "ui/button/common/ja/confirm_special.png")]
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
            AssertSprite(serialized, "openButtonSprite", "ui/button/common/ja/show_role_list2.png");
            AssertSprite(serialized, "closeButtonSprite", "ui/button/common/close.png");

            GameObject panelRoot = ReadReference<GameObject>(serialized, "panelRoot");
            Image page = panelRoot.GetComponentsInChildren<Image>(true)
                .Single(value => value.name == "RoleListPage");
            const string pagePath = "Assets/Sprite/production/ui/backgrounds/common/ja/役一覧.png";
            Assert.That(page.sprite, Is.Not.Null, "RoleListPage Image.sprite");
            Assert.That(AssetDatabase.GetAssetPath(page.sprite), Is.EqualTo(pagePath));

            LocalizeSprite localize = page.GetComponent<LocalizeSprite>();
            Assert.That(localize, Is.Not.Null, "RoleListPage LocalizeSprite");
            var field = typeof(LocalizeSprite).GetField("normal",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "LocalizeSprite.normal");
            var normal = (UnityEngine.Localization.LocalizedSprite)field.GetValue(localize);
            Assert.That(normal, Is.Not.Null);
            Assert.That(normal.IsEmpty, Is.False);
            Assert.That(normal.TableReference.TableCollectionName, Is.EqualTo("UIImages"));
            var table = AssetDatabase.LoadAssetAtPath<UnityEngine.Localization.Tables.AssetTable>(
                "Assets/Localization/UIImages_ja.asset");
            Assert.That(table, Is.Not.Null);
            Assert.That(normal.TableEntryReference.ResolveKeyName(table.SharedData), Is.EqualTo("role"));
            var entry = table.GetEntry("role");
            Assert.That(entry, Is.Not.Null, "UIImages/ja/role");
            Assert.That(AssetDatabase.GUIDToAssetPath(entry.Guid), Is.EqualTo(pagePath));
        }
        finally
        {
            if (openedForTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void AssertSprite(SerializedObject serialized, string propertyName, string relativePath)
    {
        Sprite sprite = ReadReference<Sprite>(serialized, propertyName);
        Assert.That(AssetDatabase.GetAssetPath(sprite),
            Is.EqualTo("Assets/Sprite/production/" + relativePath));
    }

    private static T ReadReference<T>(SerializedObject serialized, string propertyName) where T : Object
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        Assert.That(property, Is.Not.Null, serialized.targetObject.GetType().Name + "." + propertyName);
        Assert.That(property.propertyType, Is.EqualTo(SerializedPropertyType.ObjectReference), propertyName);
        Assert.That(property.objectReferenceValue, Is.InstanceOf<T>(), propertyName);
        return (T)property.objectReferenceValue;
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
