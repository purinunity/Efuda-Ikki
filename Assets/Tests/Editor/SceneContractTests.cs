using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EfudaIkki.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneContractTests
{
    private const string LatestScenePath = "Assets/Scenes/latest.unity";
    private const string FurihataScenePath = "Assets/Scenes/furihata.unity";
    private const string PlayerControllerGuid = "bd7794a3bda984449b562c5c30aa08eb";
    private const string GameManagerGuid = "fe9c00b4e7643d145a7bd47923f90ecd";
    private const string ShowdownCutInPopupGuid = "8c3dfba0c3a34d1289a9f3e42f8f3d12";

    [Test]
    public void RuntimeScriptGuids_AreStable()
    {
        Assert.That(
            AssetDatabase.AssetPathToGUID("Assets/Scripts/Controller/PlayerController.cs"),
            Is.EqualTo(PlayerControllerGuid));
        Assert.That(
            AssetDatabase.AssetPathToGUID("Assets/Scripts/GameManager.cs"),
            Is.EqualTo(GameManagerGuid));
        Assert.That(
            AssetDatabase.AssetPathToGUID("Assets/Scripts/UI/ShowdownCutInPopup.cs"),
            Is.EqualTo(ShowdownCutInPopupGuid));
    }

    [TestCase(LatestScenePath, true)]
    [TestCase(FurihataScenePath, false)]
    public void SceneYaml_KeepsScriptAndReceiveInputContracts(string scenePath, bool hasCutIn)
    {
        string yaml = ReadProjectText(scenePath);

        StringAssert.Contains($"guid: {PlayerControllerGuid}", yaml);
        StringAssert.Contains($"guid: {GameManagerGuid}", yaml);
        Assert.That(
            Regex.Matches(yaml, "m_TargetAssemblyTypeName: PlayerController, Assembly-CSharp").Count,
            Is.EqualTo(1));
        Assert.That(Regex.Matches(yaml, "m_MethodName: ReceiveInput").Count, Is.EqualTo(1));
        StringAssert.Contains("m_Mode: 1", yaml);

        if (hasCutIn)
        {
            StringAssert.Contains($"guid: {ShowdownCutInPopupGuid}", yaml);
        }
    }

    [Test]
    public void LatestScene_KeepsMajorSerializedFieldNames()
    {
        string yaml = ReadProjectText(LatestScenePath);
        string[] gameManagerFields =
        {
            "allCards", "playerController", "cpuController", "uiManager",
            "showdownCutInPopup", "matchResultPanel", "titleUIManager", "characterManager",
            "specialCardsDeck1", "specialCardsDeck2", "cpuSpecialCardCount",
            "cpuCharacterSettings"
        };
        string[] cutInFields =
        {
            "assetSet", "buildMissingUiAtRuntime", "roleCardDimColor",
            "activeSpecialCardTint", "inactiveSpecialCardTint", "canvasGroup", "stage",
            "playerCharacterImage", "cpuCharacterImage", "playerRoleImage", "cpuRoleImage",
            "playerScoreText", "cpuScoreText", "playerCardImages", "cpuCardImages",
            "playerSpecialCardImage", "cpuSpecialCardImage", "closeButton"
        };
        string[] playerControllerFields =
        {
            "playerHands", "decisionButton", "decisionButtonImage",
            "decisionButtonNormalColor", "decisionButtonPressedSprite"
        };

        AssertYamlFields(yaml, gameManagerFields);
        AssertYamlFields(yaml, cutInFields);
        AssertYamlFields(yaml, playerControllerFields);
        AssertSerializedFieldsExist(typeof(GameManager), gameManagerFields);
        AssertSerializedFieldsExist(typeof(ShowdownCutInPopup), cutInFields);
        AssertSerializedFieldsExist(typeof(PlayerController), playerControllerFields);
    }

    [Test]
    public void PlayerControllerReceiveInput_PublicApiRemainsParameterlessVoidInAssemblyCSharp()
    {
        MethodInfo method = typeof(PlayerController).GetMethod(
            "ReceiveInput",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            Type.EmptyTypes,
            null);

        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
        Assert.That(method.GetParameters(), Is.Empty);
        Assert.That(typeof(PlayerController).Assembly.GetName().Name, Is.EqualTo("Assembly-CSharp"));
    }

    [TestCase(LatestScenePath)]
    [TestCase(FurihataScenePath)]
    public void Scene_HasNoMissingScriptsOrObjectReferences(string scenePath)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        }

        try
        {
            var missingScripts = new System.Collections.Generic.List<string>();
            var missingReferences = new System.Collections.Generic.List<string>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    GameObject gameObject = transform.gameObject;
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) > 0)
                    {
                        missingScripts.Add(GetHierarchyPath(transform));
                    }

                    foreach (Component component in gameObject.GetComponents<Component>())
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        var serializedObject = new SerializedObject(component);
                        SerializedProperty property = serializedObject.GetIterator();
                        while (property.NextVisible(true))
                        {
                            if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                property.objectReferenceValue == null &&
                                property.objectReferenceInstanceIDValue != 0)
                            {
                                missingReferences.Add(
                                    $"{GetHierarchyPath(transform)} :: {component.GetType().Name}.{property.propertyPath}");
                            }
                        }
                    }
                }
            }

            Assert.That(missingScripts, Is.Empty, string.Join("\n", missingScripts));
            Assert.That(missingReferences, Is.Empty, string.Join("\n", missingReferences));
        }
        finally
        {
            if (openedForTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [Test]
    public void LatestScene_StageButtonsAndEverySpriteStateMatchStableCharacterIds()
    {
        Scene scene = SceneManager.GetSceneByPath(LatestScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
        {
            scene = EditorSceneManager.OpenScene(LatestScenePath, OpenSceneMode.Additive);
        }

        try
        {
            StageSelectPanel panel = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StageSelectPanel>(true))
                .FirstOrDefault();
            Assert.That(panel, Is.Not.Null);
            var serializedPanel = new SerializedObject(panel);
            SerializedProperty buttons = serializedPanel.FindProperty("stageButtons");
            SerializedProperty locked = serializedPanel.FindProperty("lockedCharacterSprites");
            SerializedProperty hover = serializedPanel.FindProperty("hoverCharacterSprites");
            SerializedProperty cleared = serializedPanel.FindProperty("clearedCharacterSprites");
            SerializedProperty clearedHover = serializedPanel.FindProperty("clearedHoverCharacterSprites");
            SerializedProperty characterBindings = serializedPanel.FindProperty("characterBindings");

            Assert.That(buttons.arraySize, Is.EqualTo(StageCharacterCatalog.Entries.Count));
            Assert.That(locked.arraySize, Is.EqualTo(StageCharacterCatalog.Entries.Count));
            Assert.That(hover.arraySize, Is.EqualTo(StageCharacterCatalog.Entries.Count));
            Assert.That(cleared.arraySize, Is.EqualTo(StageCharacterCatalog.Entries.Count));
            Assert.That(clearedHover.arraySize, Is.EqualTo(StageCharacterCatalog.Entries.Count));
            Assert.That(characterBindings.arraySize, Is.EqualTo(StageCharacterCatalog.Entries.Count));

            foreach (StageCharacterCatalog.Entry character in StageCharacterCatalog.Entries)
            {
                int index = character.Level - 1;
                var button = buttons.GetArrayElementAtIndex(index).objectReferenceValue as UnityEngine.UI.Button;
                Assert.That(button, Is.Not.Null, character.Id);
                var image = button.targetGraphic as UnityEngine.UI.Image ?? button.GetComponent<UnityEngine.UI.Image>();
                Assert.That(image, Is.Not.Null, character.Id);

                AssertSpritePath(image.sprite, character.Id, "normal.png");
                AssertSpritePath(hover.GetArrayElementAtIndex(index).objectReferenceValue, character.Id, "normal_hover.png");
                AssertSpritePath(locked.GetArrayElementAtIndex(index).objectReferenceValue, character.Id, "locked.png");
                AssertSpritePath(cleared.GetArrayElementAtIndex(index).objectReferenceValue, character.Id, "cleared.png");
                AssertSpritePath(clearedHover.GetArrayElementAtIndex(index).objectReferenceValue, character.Id, "cleared_hover.png");

                SerializedProperty binding = FindCharacterBinding(characterBindings, character.Id);
                Assert.That(binding, Is.Not.Null, character.Id);
                Assert.That(
                    binding.FindPropertyRelative("button").objectReferenceValue,
                    Is.SameAs(button),
                    character.Id);
                AssertSpritePath(binding.FindPropertyRelative("normal").objectReferenceValue, character.Id, "normal.png");
                AssertSpritePath(binding.FindPropertyRelative("normalHover").objectReferenceValue, character.Id, "normal_hover.png");
                AssertSpritePath(binding.FindPropertyRelative("locked").objectReferenceValue, character.Id, "locked.png");
                AssertSpritePath(binding.FindPropertyRelative("cleared").objectReferenceValue, character.Id, "cleared.png");
                AssertSpritePath(binding.FindPropertyRelative("clearedHover").objectReferenceValue, character.Id, "cleared_hover.png");
            }
        }
        finally
        {
            if (openedForTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [Test]
    public void LatestScene_CharacterManagerCpuSpritesMatchStableCharacterIds()
    {
        Scene scene = SceneManager.GetSceneByPath(LatestScenePath);
        bool openedForTest = !scene.IsValid() || !scene.isLoaded;
        if (openedForTest)
        {
            scene = EditorSceneManager.OpenScene(LatestScenePath, OpenSceneMode.Additive);
        }

        try
        {
            CharacterManager manager = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CharacterManager>(true))
                .FirstOrDefault();
            Assert.That(manager, Is.Not.Null);
            Assert.That(manager.CPUSprite, Has.Length.EqualTo(StageCharacterCatalog.Entries.Count));

            foreach (StageCharacterCatalog.Entry character in StageCharacterCatalog.Entries)
            {
                AssertSpritePath(
                    manager.CPUSprite[character.Level - 1],
                    character.Id,
                    "normal.png");
            }
        }
        finally
        {
            if (openedForTest && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void AssertYamlFields(string yaml, string[] fieldNames)
    {
        foreach (string fieldName in fieldNames)
        {
            StringAssert.Contains($"  {fieldName}:", yaml, $"Missing serialized field {fieldName}.");
        }
    }

    private static void AssertSerializedFieldsExist(Type type, string[] fieldNames)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (string fieldName in fieldNames)
        {
            Assert.That(fields.Any(field => field.Name == fieldName), Is.True,
                $"{type.Name}.{fieldName} was renamed without a compatibility contract.");
        }
    }

    private static string ReadProjectText(string assetPath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(ApplicationDataPath, ".."));
        string absolutePath = Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(absolutePath);
    }

    private static string ApplicationDataPath => UnityEngine.Application.dataPath;

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static void AssertSpritePath(UnityEngine.Object sprite, string characterId, string fileName)
    {
        Assert.That(sprite, Is.Not.Null, $"{characterId}/{fileName}");
        string path = AssetDatabase.GetAssetPath(sprite).Replace('\\', '/');
        Assert.That(
            path,
            Is.EqualTo($"Assets/Sprite/production/characters/opponents/{characterId}/{fileName}"));
    }

    private static SerializedProperty FindCharacterBinding(
        SerializedProperty bindings,
        string characterId)
    {
        for (int i = 0; i < bindings.arraySize; i++)
        {
            SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
            if (binding.FindPropertyRelative("characterId").stringValue == characterId)
            {
                return binding;
            }
        }

        return null;
    }
}
