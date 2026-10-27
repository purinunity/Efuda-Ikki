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
    public void DiscardPreview_MapsEverySuitAndNumberToItsAuthoredGridCell()
    {
        MethodInfo map = typeof(DiscardPreviewPanel).GetMethod(
            "TryGetSuitSlotIndex",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(map, Is.Not.Null);

        Suit[] suits = { Suit.Birds, Suit.Flowers, Suit.Wind, Suit.Moon };
        for (int row = 0; row < suits.Length; row++)
        {
            for (int number = 1; number <= 13; number++)
            {
                CardData data = ScriptableObject.CreateInstance<CardData>();
                try
                {
                    data.suit = suits[row];
                    data.number = (Number)number;
                    object[] arguments = { data, -1 };
                    Assert.That((bool)map.Invoke(null, arguments), Is.True);
                    Assert.That((int)arguments[1], Is.EqualTo(row * 13 + number - 1));
                }
                finally
                {
                    Object.DestroyImmediate(data);
                }
            }
        }
    }

    [Test]
    public void OniSprites_AreSeparatedByGameMode()
    {
        CardData oni = AssetDatabase.LoadAssetAtPath<CardData>(
            "Assets/ScriptableObject/SpecialCard/sp_oni.asset");
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
            "Assets/ScriptableObject/SpecialCard/sp_no_use.asset");
        GameObject selectedObject = CreateCardObject(
            "Selected",
            "Assets/ScriptableObject/SpecialCard/sp 1.asset");

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
    public void LatestScene_EditorPreviewUsesActiveStateToShowOnlySettingsScreen()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            GameObject menu = FindRoot(scene, "MenuCanvas");
            AssertCanvasGroup(menu, 1f, true, true);
            AssertAuthoringScreen(Find(menu.transform, "Screens/TitleScreen/TitleScreenPanel"), false);
            AssertAuthoringScreen(Find(menu.transform, "Screens/ModeSelectScreen/ModeSelectPanel"), false);
            AssertAuthoringScreen(Find(menu.transform, "Screens/StageSelectScreen/StageSelectPanel"), false);
            AssertAuthoringScreen(Find(menu.transform, "Screens/SpecialCardSelectScreen/SpecialCardSelectPanel"), false);
            AssertAuthoringScreen(Find(menu.transform, "Screens/SettingsScreen/SettingsPanel"), true);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void RoleListAndDeductionFrames_MatchAuthoredLayout()
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
            Assert.That(playerFrame.anchoredPosition.y, Is.EqualTo(12f));
            Assert.That(playerTextRect.anchoredPosition.y, Is.EqualTo(12f));
            Assert.That(cpuFrame.anchoredPosition.y, Is.EqualTo(-12f));
            Assert.That(cpuTextRect.anchoredPosition.y, Is.EqualTo(-12f));
            Assert.That(playerFrame.sizeDelta, Is.EqualTo(cpuFrame.sizeDelta));
            Assert.That(playerFrame.GetSiblingIndex(), Is.LessThan(playerTextRect.GetSiblingIndex()));
            Assert.That(cpuFrame.GetSiblingIndex(), Is.LessThan(cpuTextRect.GetSiblingIndex()));

            MethodInfo configure = typeof(ShowdownCutInPopup).GetMethod(
                "ConfigureLifeDeductionText",
                BindingFlags.Static | BindingFlags.NonPublic);
            TextMeshProUGUI playerText = playerTextRect.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI cpuText = cpuTextRect.GetComponent<TextMeshProUGUI>();
            configure.Invoke(null, new object[] { playerText });
            configure.Invoke(null, new object[] { cpuText });
            Assert.That(playerText.enableAutoSizing, Is.True);
            Assert.That(playerText.fontSizeMin, Is.EqualTo(18f));
            Assert.That(playerText.fontSizeMax, Is.EqualTo(32f));
            Assert.That(cpuText.fontSizeMin, Is.EqualTo(18f));
            Assert.That(cpuText.fontSizeMax, Is.EqualTo(32f));
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

    private static void AssertAuthoringScreen(GameObject target, bool active)
    {
        Assert.That(target, Is.Not.Null);
        Assert.That(target.activeSelf, Is.True, target.name + " prefab root");
        Assert.That(target.transform.parent, Is.Not.Null, target.name);
        Assert.That(target.transform.parent.gameObject.activeSelf, Is.EqualTo(active), target.name + " wrapper");
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
