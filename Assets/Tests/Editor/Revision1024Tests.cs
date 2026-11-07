using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class Revision1024Tests
{
    private const string ScenePath = "Assets/Scenes/latest.unity";
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    [Test]
    public void DiscardPreview_UsesBackgroundSuitOrder()
    {
        FieldInfo field = typeof(DiscardPreviewPanel).GetField(
            "SuitOrder",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        CollectionAssert.AreEqual(
            new[] { Suit.Birds, Suit.Flowers, Suit.Wind, Suit.Moon },
            (Suit[])field.GetValue(null));
    }

    [Test]
    public void DiscardPreview_NonAdjacentRanksFillOneCenteredGroup()
    {
        GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Game/DiscardPreview.prefab");
        var objects = new List<Object>();
        try
        {
            var panel = root.GetComponent<DiscardPreviewPanel>();
            var array = new SerializedObject(panel).FindProperty("previewSlots");
            Image[] slots = Enumerable.Range(0, 52).Select(i => (Image)array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            float center = (slots[0].rectTransform.anchorMin.x + slots[0].rectTransform.anchorMax.x +
                slots[12].rectTransform.anchorMin.x + slots[12].rectTransform.anchorMax.x) * 0.25f;
            var cards = new List<Card>();
            foreach (Number number in new[] { Number.King, Number.One })
            {
                var data = ScriptableObject.CreateInstance<CardData>();
                objects.Add(data);
                data.suit = Suit.Birds;
                data.number = number;
                var go = new GameObject("Discard", typeof(RectTransform), typeof(Image), typeof(Card));
                objects.Add(go);
                Card card = go.GetComponent<Card>();
                card.SetCardData(data);
                cards.Add(card);
            }
            panel.Show(cards);
            Assert.That(slots.Take(2).All(image => image.gameObject.activeSelf), Is.True);
            Assert.That(slots.Skip(2).All(image => !image.gameObject.activeSelf), Is.True);
            float groupCenter = slots.Take(2).Average(image => (image.rectTransform.anchorMin.x + image.rectTransform.anchorMax.x) * 0.5f);
            Assert.That(groupCenter, Is.EqualTo(center).Within(0.000001f));
        }
        finally
        {
            foreach (Object item in objects) Object.DestroyImmediate(item);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void OniSprites_AreSeparatedByGameMode()
    {
        CardData oni = AssetDatabase.LoadAssetAtPath<CardData>(
            "Assets/Data/Cards/Special/sp_oni.asset");
        Sprite normalOni = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Sprite/production/cards/special/oni.png");
        Sprite battleGroundOni = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Sprite/production/battle_ground/cards/special/oni.png");
        BattleGroundVisualAssets battleGround = AssetDatabase.LoadAssetAtPath<BattleGroundVisualAssets>(
            "Assets/Resources/BattleGroundVisualAssets.asset");

        Assert.That(oni, Is.Not.Null);
        Assert.That(normalOni, Is.Not.Null);
        Assert.That(battleGroundOni, Is.Not.Null);
        Assert.That(battleGround, Is.Not.Null);
        Assert.That(oni.Image, Is.SameAs(normalOni));
        Assert.That(
            battleGround.GetSpecialCard(SpecialCardResolver.SpecialCardId.Oni),
            Is.SameAs(battleGroundOni));
        Assert.That(normalOni, Is.Not.SameAs(battleGroundOni));
    }

    [Test]
    public void PlayerSpecialDisplay_AlwaysEndsWithExactlyOneNoUseCard()
    {
        GameObject managerObject = new GameObject("UIManager_Test", typeof(UIManager));
        GameObject deckObject = new GameObject("SpecialDeck_Test", typeof(Cards));
        GameObject noUseObject = CreateCardObject(
            "NoUse",
            "Assets/Data/Cards/Special/sp_no_use.asset");
        GameObject selectedObject = CreateCardObject(
            "Selected",
            "Assets/Data/Cards/Special/sp 1.asset");

        try
        {
            UIManager manager = managerObject.GetComponent<UIManager>();
            Cards deck = deckObject.GetComponent<Cards>();
            Card noUse = noUseObject.GetComponent<Card>();
            Card selected = selectedObject.GetComponent<Card>();
            typeof(Cards).GetField("sceneCards", PrivateInstance).SetValue(
                deck,
                new List<Card> { selected, noUse });

            MethodInfo build = typeof(UIManager).GetMethod(
                "BuildSpecialCardDisplayCards",
                PrivateInstance);
            Assert.That(build, Is.Not.Null);

            List<Card> emptyPlayer = InvokeBuild(build, manager, new List<Card>(), deck, true);
            CollectionAssert.AreEqual(new[] { noUse }, emptyPlayer);

            List<Card> selectedPlayer = InvokeBuild(
                build,
                manager,
                new List<Card> { selected, noUse },
                deck,
                true);
            CollectionAssert.AreEqual(new[] { selected, noUse }, selectedPlayer);

            List<Card> emptyCpu = InvokeBuild(build, manager, new List<Card>(), deck, false);
            Assert.That(emptyCpu, Is.Empty);
            Assert.That(noUse.IsFaceUp, Is.True);
            Assert.That(noUse.IsSelectable, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(deckObject);
            Object.DestroyImmediate(noUseObject);
            Object.DestroyImmediate(selectedObject);
        }
    }

    [Test]
    public void LatestScene_MenuScreensKeepActivePrefabRootsAndAuthoredReferences()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            GameObject menu = FindRoot(scene, "MenuCanvas");
            AssertCanvasGroup(menu, 1f, true, true);
            TitleUIManager manager = menu.GetComponent<TitleUIManager>();
            Assert.That(manager, Is.Not.Null);
            AssertAuthoringScreen(Find(menu.transform, "Screens/TitleScreen/TitleScreenPanel"), manager.titleScreenPanel);
            AssertAuthoringScreen(Find(menu.transform, "Screens/ModeSelectScreen/ModeSelectPanel"), manager.modeSelectPanel);
            AssertAuthoringScreen(Find(menu.transform, "Screens/StageSelectScreen/StageSelectPanel"), manager.stageSelectPanel);
            AssertAuthoringScreen(Find(menu.transform, "Screens/SpecialCardSelectScreen/SpecialCardSelectPanel"), manager.specialCardSelectPanel);
            AssertAuthoringScreen(Find(menu.transform, "Screens/SettingsScreen/SettingsPanel"), manager.settingsPanel);
        }
        finally
        {
            if (openedForTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MenuScreenSwitch_ChangesOnlyWrapperVisibility(bool initiallyVisible)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            root = new GameObject("Menu Fixture");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.SetActive(false); // Exercise public switching without lifecycle or save side effects.
            TitleUIManager manager = root.AddComponent<TitleUIManager>();
            var panels = new List<CanvasGroup>();
            foreach (string name in new[] { "Title", "ModeSelect", "StageSelect", "SpecialCardSelect", "Settings" })
            {
                var wrapper = new GameObject(name + "Screen");
                wrapper.transform.SetParent(root.transform, false);
                var panel = new GameObject(name + "Panel", typeof(RectTransform), typeof(CanvasGroup));
                panel.transform.SetParent(wrapper.transform, false);
                panels.Add(panel.GetComponent<CanvasGroup>());
                wrapper.SetActive(initiallyVisible);
            }
            manager.titleScreenPanel = panels[0];
            manager.modeSelectPanel = panels[1];
            manager.stageSelectPanel = panels[2];
            manager.specialCardSelectPanel = panels[3];
            manager.settingsPanel = panels[4];
            manager.ShowSettings();
            AssertVisibleScreen(panels, panels[4]);
            manager.ShowSettings();
            AssertVisibleScreen(panels, panels[4]);
            manager.ShowTitleScreen();
            AssertVisibleScreen(panels, panels[0]);
            manager.ShowTitleScreen();
            AssertVisibleScreen(panels, panels[0]);
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void AssertVisibleScreen(IEnumerable<CanvasGroup> panels, CanvasGroup visible)
    {
        foreach (CanvasGroup panel in panels)
        {
            Assert.That(panel.gameObject.activeSelf, Is.True, panel.name + " prefab root");
            Assert.That(panel.transform.parent.gameObject.activeSelf, Is.EqualTo(panel == visible), panel.name + " wrapper");
            AssertCanvasGroup(panel.gameObject, 1f, true, true);
        }
    }

    [Test]
    public void RoleListAndFramelessDeductions_MatchAuthoredLayout()
    {
        GameObject roleList = PrefabUtility.LoadPrefabContents(
            "Assets/Prefabs/UI/Game/RoleList.prefab");
        GameObject showdown = PrefabUtility.LoadPrefabContents(
            "Assets/Prefabs/UI/Game/Showdown.prefab");
        try
        {
            RectTransform page = FindRecursive(roleList.transform, "RoleListPage") as RectTransform;
            Assert.That(page, Is.Not.Null);
            Assert.That(page.sizeDelta, Is.EqualTo(new Vector2(1748f, 754f)));
            Assert.That(page.GetComponent<Image>().preserveAspect, Is.True);

            Transform stage = FindRecursive(showdown.transform, "Stage");
            RectTransform playerFrame = FindRecursive(stage, "PlayerLifeDeductionFrame") as RectTransform;
            RectTransform playerTextRect = FindRecursive(stage, "PlayerLifeDeduction") as RectTransform;
            RectTransform cpuFrame = FindRecursive(stage, "CpuLifeDeductionFrame") as RectTransform;
            RectTransform cpuTextRect = FindRecursive(stage, "CpuLifeDeduction") as RectTransform;
            Assert.That(playerFrame, Is.Null);
            Assert.That(playerTextRect.anchoredPosition.y, Is.EqualTo(12f));
            Assert.That(cpuFrame, Is.Null);
            Assert.That(cpuTextRect.anchoredPosition.y, Is.EqualTo(-12f));

            TextMeshProUGUI playerText = playerTextRect.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI cpuText = cpuTextRect.GetComponent<TextMeshProUGUI>();
            float playerMin = playerText.fontSizeMin;
            float playerMax = playerText.fontSizeMax;
            float cpuMin = cpuText.fontSizeMin;
            float cpuMax = cpuText.fontSizeMax;
            showdown.GetComponent<ShowdownCutInPopup>().Initialize();
            Assert.That(playerText.enableAutoSizing, Is.False);
            Assert.That(cpuText.enableAutoSizing, Is.False);
            Assert.That(playerText.fontSize, Is.EqualTo(36f));
            Assert.That(cpuText.fontSize, Is.EqualTo(36f));
            Assert.That(playerText.font, Is.EqualTo(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NovemberUiAuthoring.FontPath)));
            Assert.That(playerText.fontSizeMin, Is.EqualTo(playerMin));
            Assert.That(playerText.fontSizeMax, Is.EqualTo(playerMax));
            Assert.That(cpuText.fontSizeMin, Is.EqualTo(cpuMin));
            Assert.That(cpuText.fontSizeMax, Is.EqualTo(cpuMax));
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(roleList);
            PrefabUtility.UnloadPrefabContents(showdown);
        }
    }

    private static GameObject CreateCardObject(string name, string assetPath)
    {
        GameObject result = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Card));
        CardData data = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
        Assert.That(data, Is.Not.Null, assetPath);
        typeof(Card).GetField("authoredCardData", PrivateInstance).SetValue(
            result.GetComponent<Card>(),
            data);
        result.GetComponent<Card>().Initialize();
        return result;
    }

    private static List<Card> InvokeBuild(
        MethodInfo method,
        UIManager manager,
        List<Card> cards,
        Cards deck,
        bool includeNoUse)
    {
        return (List<Card>)method.Invoke(manager, new object[] { cards, deck, includeNoUse });
    }

    private static void AssertCanvasGroup(
        GameObject target,
        float alpha,
        bool interactable,
        bool blocksRaycasts)
    {
        Assert.That(target, Is.Not.Null);
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        Assert.That(group, Is.Not.Null, target.name);
        Assert.That(group.alpha, Is.EqualTo(alpha), target.name);
        Assert.That(group.interactable, Is.EqualTo(interactable), target.name);
        Assert.That(group.blocksRaycasts, Is.EqualTo(blocksRaycasts), target.name);
    }

    private static void AssertAuthoringScreen(GameObject target, CanvasGroup reference)
    {
        Assert.That(target, Is.Not.Null);
        Assert.That(target.activeSelf, Is.True, target.name + " prefab root");
        Assert.That(target.transform.parent, Is.Not.Null, target.name);
        Assert.That(target.transform.parent.name, Does.EndWith("Screen"), target.name + " wrapper");
        Assert.That(reference, Is.SameAs(target.GetComponent<CanvasGroup>()), target.name + " manager reference");
        AssertCanvasGroup(target, 1f, true, true);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().Single(value => value.name == name);
    }

    private static GameObject Find(Transform root, string path)
    {
        Transform result = root.Find(path);
        return result != null ? result.gameObject : null;
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
