using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class UiPrefabBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        // Validate the source scene before Unity creates its export copy. The
        // export copy intentionally strips prefab-instance metadata.
        ValidateProjectOrThrow();
    }

    public static void ValidateProjectOrThrow()
    {
        foreach (string path in UiPrefabMigration.ScreenPrefabs.Values)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                throw new BuildFailedException("Missing UI screen prefab: " + path);
        }

        Scene scene = SceneManager.GetSceneByPath(UiPrefabMigration.ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(UiPrefabMigration.ScenePath, OpenSceneMode.Additive);
        try
        {
            ValidateSceneOrThrow(scene, true);
            Debug.Log("UI authoring validation passed.");
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void ValidateSceneOrThrow(Scene scene, bool requireActiveRoots = true)
    {
        GameObject menu = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "MenuCanvas");
        GameObject game = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "GameCanvas");
        if (menu == null || game == null) Fail("MenuCanvas or GameCanvas is missing.");
        if (requireActiveRoots && (!menu.activeSelf || !game.activeSelf))
            Fail("MenuCanvas and GameCanvas must remain active on disk.");
        if (menu.GetComponent<CanvasGroup>() == null || game.GetComponent<CanvasGroup>() == null)
            Fail("Root CanvasGroups are missing.");
        if (menu.GetComponent<MenuUiReferences>() == null || game.GetComponent<GameplayUiReferences>() == null)
            Fail("UI reference aggregate is missing.");

        UiRootCoordinator coordinator = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<UiRootCoordinator>(true)).SingleOrDefault();
        if (coordinator == null) Fail("UiRootCoordinator is missing.");

        foreach (var item in UiPrefabMigration.ScreenPrefabs)
        {
            GameObject instance = Find(scene, item.Key);
            if (instance == null) Fail("Missing screen instance: " + item.Key);
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance) != item.Value)
                Fail(item.Key + " is not connected to " + item.Value);
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(instance) > 0)
                Fail("Missing Script on " + item.Key);
        }

        var expected = new System.Collections.Generic.Dictionary<string, int>
        {
            { "Deck", 52 }, { "Common", 2 }, { "Trash", 52 },
            { "Player1", 5 }, { "Player2", 5 }, { "SpecialP1", 5 }, { "SpecialP2", 4 }
        };
        foreach (CardArea area in game.GetComponentsInChildren<CardArea>(true))
        {
            if (!expected.TryGetValue(area.name, out int count)) continue;
            SerializedProperty slots = new SerializedObject(area).FindProperty("layoutSlots");
            if (slots == null || slots.arraySize != count) Fail($"{area.name} requires {count} authored slots.");
            for (int i = 0; i < count; i++)
                if (slots.GetArrayElementAtIndex(i).objectReferenceValue == null) Fail($"{area.name} slot {i + 1} is missing.");
        }

        DiscardPreviewPanel preview = game.GetComponentsInChildren<DiscardPreviewPanel>(true).SingleOrDefault();
        ValidateObjectArray(preview, "previewSlots", 52);
        ShowdownCutInPopup showdown = game.GetComponentsInChildren<ShowdownCutInPopup>(true).SingleOrDefault();
        ValidateObjectArray(showdown, "playerRoleEffects", 11);
        ValidateObjectArray(showdown, "cpuRoleEffects", 11);

        int missing = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Sum(value => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(value.gameObject));
        if (missing != 0) Fail(scene.path + " contains " + missing + " missing script(s).");
    }

    private static void ValidateObjectArray(UnityEngine.Object target, string field, int count)
    {
        if (target == null) Fail(field + " owner is missing.");
        SerializedProperty property = new SerializedObject(target).FindProperty(field);
        if (property == null || property.arraySize != count) Fail(field + " must contain " + count + " entries.");
        for (int i = 0; i < count; i++)
            if (property.GetArrayElementAtIndex(i).objectReferenceValue == null) Fail(field + " contains a missing reference.");
    }

    private static GameObject Find(Scene scene, string path)
    {
        string[] names = path.Split('/');
        GameObject current = scene.GetRootGameObjects().FirstOrDefault(root => root.name == names[0]);
        for (int i = 1; current != null && i < names.Length; i++)
        {
            Transform child = current.transform.Find(names[i]);
            current = child != null ? child.gameObject : null;
        }
        return current;
    }

    private static void Fail(string message) => throw new BuildFailedException("UI authoring: " + message);
}
