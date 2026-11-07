using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class UiPrefabAuthoringTests
{
    [Test]
    public void AllScreenPrefabsExistAndAreConnectedExactlyOnce()
    {
        Scene scene = EditorSceneManager.OpenScene(UiPrefabMigration.ScenePath, OpenSceneMode.Additive);
        try
        {
            foreach (var item in UiPrefabMigration.ScreenPrefabs)
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(item.Value);
                Assert.That(asset, Is.Not.Null, item.Value);
                GameObject instance = Find(scene, item.Key);
                Assert.That(instance, Is.Not.Null, item.Key);
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance), Is.EqualTo(item.Value));
            }

            GameObject menu = Find(scene, "MenuCanvas");
            GameObject game = Find(scene, "GameCanvas");
            Assert.That(menu.activeSelf, Is.True);
            Assert.That(game.activeSelf, Is.True);
            Assert.That(menu.GetComponent<CanvasGroup>(), Is.Not.Null);
            Assert.That(game.GetComponent<CanvasGroup>(), Is.Not.Null);
            Assert.That(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<UiRootCoordinator>(true)).Count(), Is.EqualTo(1));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void AuthoredCardAndEffectSlotCountsMatchRuntimeLimits()
    {
        Scene scene = EditorSceneManager.OpenScene(UiPrefabMigration.ScenePath, OpenSceneMode.Additive);
        try
        {
            GameObject game = Find(scene, "GameCanvas");
            var expected = new System.Collections.Generic.Dictionary<string, int>
            {
                { "Deck", 52 }, { "Common", 2 }, { "Trash", 52 },
                { "Player1", 5 }, { "Player2", 5 }, { "SpecialP1", 5 }, { "SpecialP2", 4 }
            };
            foreach (CardArea area in game.GetComponentsInChildren<CardArea>(true))
            {
                if (expected.TryGetValue(area.name, out int count))
                    Assert.That(area.LayoutSlots.Count, Is.EqualTo(count), area.name);

                if (area.name == "Deck" || area.name == "Trash")
                {
                    Assert.That(
                        area.LayoutSlots.All(slot => slot.RectTransform.anchoredPosition == Vector2.zero),
                        Is.True,
                        area.name + " cards must overlap at one exact authored position.");
                }
            }

            DiscardPreviewPanel discard = game.GetComponentInChildren<DiscardPreviewPanel>(true);
            SerializedProperty previewSlots = new SerializedObject(discard).FindProperty("previewSlots");
            Assert.That(previewSlots.arraySize, Is.EqualTo(52));
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 12; column++)
                {
                    var left = previewSlots.GetArrayElementAtIndex(row * 13 + column)
                        .objectReferenceValue as UnityEngine.UI.Image;
                    var right = previewSlots.GetArrayElementAtIndex(row * 13 + column + 1)
                        .objectReferenceValue as UnityEngine.UI.Image;
                    Assert.That(left, Is.Not.Null);
                    Assert.That(right, Is.Not.Null);
                    Assert.That(
                        left.transform.GetSiblingIndex(),
                        Is.GreaterThan(right.transform.GetSiblingIndex()),
                        $"Discard row {row + 1} must draw left cards above right cards so numbers remain visible.");
                }
            }

            ShowdownCutInPopup showdown = game.GetComponentInChildren<ShowdownCutInPopup>(true);
            SerializedObject showdownSerialized = new SerializedObject(showdown);
            Assert.That(showdownSerialized.FindProperty("playerRoleEffects").arraySize, Is.EqualTo(11));
            Assert.That(showdownSerialized.FindProperty("cpuRoleEffects").arraySize, Is.EqualTo(11));
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void PrefabInstancesHaveNoChildRectTransformOverrides()
    {
        Scene scene = EditorSceneManager.OpenScene(UiPrefabMigration.ScenePath, OpenSceneMode.Additive);
        try
        {
            foreach (var item in UiPrefabMigration.ScreenPrefabs)
            {
                GameObject instance = Find(scene, item.Key);
                PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(instance) ?? new PropertyModification[0];
                RectTransform sourceRoot = PrefabUtility.GetCorrespondingObjectFromSource(instance.transform as RectTransform);
                var childLayoutOverrides = modifications.Where(modification =>
                    modification.target is RectTransform rect &&
                    rect != sourceRoot &&
                    !IsDrivenByEnabledLayoutGroup(rect)).ToArray();
                Assert.That(childLayoutOverrides, Is.Empty, item.Key);
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void FixedUiRuntimeCodeDoesNotConstructHierarchy()
    {
        string[] files =
        {
            "Assets/Scripts/UI/Gameplay/GameplayUiReferences.cs",
            "Assets/Scripts/UI/Overlays/DiscardPreviewPanel.cs",
            "Assets/Scripts/UI/Cards/PopupPreviewCardArea.cs",
            "Assets/Scripts/UI/Showdown/ShowdownCutInPopup.cs",
            "Assets/Scripts/UI/Overlays/MatchResultPanel.cs",
            "Assets/Scripts/UI/Overlays/CoinTossPanel.cs"
        };
        foreach (string file in files)
        {
            string source = File.ReadAllText(file);
            Assert.That(source, Does.Not.Contain("Instantiate("), file);
            Assert.That(source, Does.Not.Contain("new GameObject("), file);
            Assert.That(source, Does.Not.Contain("AddComponent<"), file);
            Assert.That(source, Does.Not.Contain("Resources.Load"), file);
        }
    }

    [Test]
    public void OpeningPrefabContentsDoesNotDirtyLatestScene()
    {
        Scene scene = EditorSceneManager.OpenScene(UiPrefabMigration.ScenePath, OpenSceneMode.Single);
        Assert.That(scene.isDirty, Is.False);
        foreach (string path in UiPrefabMigration.ScreenPrefabs.Values)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Assert.That(root, Is.Not.Null, path);
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root), Is.Zero, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Assert.That(scene.isDirty, Is.False, path);
        }
    }

    private static bool IsDrivenByEnabledLayoutGroup(RectTransform rect)
    {
        return rect != null &&
               rect.parent != null &&
               rect.parent.TryGetComponent(out UnityEngine.UI.LayoutGroup layout) &&
               layout.enabled;
    }

    private static GameObject Find(Scene scene, string path)
    {
        string[] names = path.Split('/');
        GameObject current = scene.GetRootGameObjects().FirstOrDefault(root => root.name == names[0]);
        for (int index = 1; current != null && index < names.Length; index++)
        {
            Transform child = current.transform.Find(names[index]);
            current = child != null ? child.gameObject : null;
        }
        return current;
    }
}
