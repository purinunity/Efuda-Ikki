using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Editor-only authoring for approved STR-06, preserving all unlisted saved properties.</summary>
public static class NovemberUiAuthoring
{
    public const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string Evidence = "docs/design/evidence/str-06/";
    private static readonly string[] NumericNames =
        { "PlayerLife", "CPULife", "PlayerScore", "CpuScore", "Damage", "PlayerLifeDeduction", "CpuLifeDeduction" };
    private static readonly HashSet<string> MutablePaths = new HashSet<string>
    {
        "m_fontAsset", "m_sharedMaterial", "m_fontSize", "m_fontSizeBase", "m_enableAutoSizing",
        "playerLifeDeductionFrameImage", "cpuLifeDeductionFrameImage", "playerSpecialCardTooltip", "cpuSpecialCardTooltip",
        "m_RaycastTarget", "m_RootOrder", "m_Father"
    };

    public static string Apply()
    {
        Scene scene = SceneManager.GetSceneByPath(UiPrefabMigration.ScenePath);
        if (EditorApplication.isPlaying || !scene.IsValid() || !scene.isLoaded || scene.isDirty)
            throw new InvalidOperationException("STR-06 requires a clean latest scene in Edit Mode.");
        if (scene.GetRootGameObjects().Any(root => root.name == "TooltipCanvas"))
            throw new InvalidOperationException("STR-06 is already authored; do not repeat the migration.");
        EditPrefab("Assets/Prefabs/UI/Game/Showdown.prefab", root =>
        {
            ConfigurePopup(root.GetComponent<ShowdownCutInPopup>());
            ConfigureNumbers(root);
        });
        EditPrefab("Assets/Prefabs/UI/Components/character_hud.prefab", ConfigureNumbers);

        GameObject game = scene.GetRootGameObjects().Single(root => root.name == "GameCanvas");
        var tooltip = game.GetComponentsInChildren<SpecialCardTooltip>(true).Single();
        var layer = new GameObject("TooltipCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(layer, scene);
        Canvas canvas = layer.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = layer.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform rect = (RectTransform)tooltip.transform;
        Vector2 min = rect.anchorMin, max = rect.anchorMax, position = rect.anchoredPosition, size = rect.sizeDelta, pivot = rect.pivot;
        Vector3 scale = rect.localScale;
        Quaternion rotation = rect.localRotation;
        rect.SetParent(layer.transform, false);
        rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
        rect.sizeDelta = size; rect.anchoredPosition = position; rect.localScale = scale; rect.localRotation = rotation;
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);

        // Preserve Scene overrides except for the explicitly approved font/material/size changes.
        foreach (GameObject root in scene.GetRootGameObjects()) ConfigureNumbers(root);
        var popup = game.GetComponentsInChildren<ShowdownCutInPopup>(true).Single();
        ValidatePopup(popup);
        AssertSceneProtected(scene);
        ValidateScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save STR-06 scene.");
        AssertSceneProtected(scene);
        return "STR-06: reused 52 slots; authored shared TooltipCanvas, two image targets, numeric fonts and removed two deduction frames.";
    }

    private static void EditPrefab(string path, Action<GameObject> edit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        var before = UiComponentMigration.Capture(new[] { root });
        WriteSnapshot(Path.GetFileNameWithoutExtension(path) + "-before.json.gz", before);
        try
        {
            edit(root);
            AssertProtected(before, new[] { root });
            if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new IOException(path);
            AssertProtected(before, new[] { root });
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigurePopup(ShowdownCutInPopup popup)
    {
        var serialized = new SerializedObject(popup);
        var assets = (ShowdownCutInAssetSet)serialized.FindProperty("assetSet").objectReferenceValue;
        foreach (string side in new[] { "player", "cpu" })
        {
            var image = (Image)serialized.FindProperty(side + "SpecialCardImage").objectReferenceValue;
            var target = image.GetComponent<SpecialCardTooltipImageTarget>();
            if (target == null) target = image.gameObject.AddComponent<SpecialCardTooltipImageTarget>();
            var targetData = new SerializedObject(target);
            targetData.FindProperty("visualAssets").objectReferenceValue = assets;
            targetData.ApplyModifiedPropertiesWithoutUndo();
            var imageData = new SerializedObject(image);
            imageData.FindProperty("m_RaycastTarget").boolValue = true;
            imageData.ApplyModifiedPropertiesWithoutUndo();
            serialized.FindProperty(side + "SpecialCardTooltip").objectReferenceValue = target;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (string name in new[] { "PlayerLifeDeductionFrame", "CpuLifeDeductionFrame" })
        {
            Transform frame = popup.transform.Find("Stage/" + name);
            if (frame == null) throw new InvalidOperationException("Missing original frame: " + name);
            AssertNoExternalFrameReferences(popup.gameObject, frame);
            Object.DestroyImmediate(frame.gameObject);
        }
    }

    private static void AssertNoExternalFrameReferences(GameObject root, Transform frame)
    {
        foreach (MonoBehaviour owner in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (owner == null || owner.transform == frame || owner.transform.IsChildOf(frame)) continue;
            var property = new SerializedObject(owner).GetIterator();
            bool children = true;
            while (property.Next(children))
            {
                children = property.propertyType != SerializedPropertyType.ObjectReference;
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.propertyPath.StartsWith("m_")) continue;
                Object reference = property.objectReferenceValue;
                if (reference == frame.gameObject || reference is Component component &&
                    (component.transform == frame || component.transform.IsChildOf(frame)))
                    throw new InvalidOperationException("Unapproved frame reference: " + owner.name + "/" + property.propertyPath);
            }
        }
    }

    private static void ConfigureNumbers(GameObject root)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) throw new InvalidOperationException("LiberationSans is missing.");
        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (!NumericNames.Contains(text.name)) continue;
            var data = new SerializedObject(text);
            data.FindProperty("m_fontAsset").objectReferenceValue = font;
            data.FindProperty("m_sharedMaterial").objectReferenceValue = font.material;
            if (text.name == "PlayerScore" || text.name == "CpuScore" || text.name.EndsWith("LifeDeduction"))
            {
                data.FindProperty("m_enableAutoSizing").boolValue = false;
                data.FindProperty("m_fontSize").floatValue = 36f;
                data.FindProperty("m_fontSizeBase").floatValue = 36f;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
    }

    public static void ValidateScene(Scene scene)
    {
        GameObject layer = scene.GetRootGameObjects().SingleOrDefault(root => root.name == "TooltipCanvas");
        if (layer == null || !layer.activeSelf) throw new InvalidOperationException("Shared TooltipCanvas is missing.");
        Canvas canvas = layer.GetComponent<Canvas>();
        CanvasScaler scaler = layer.GetComponent<CanvasScaler>();
        if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay || canvas.sortingOrder != 100 ||
            scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
            scaler.referenceResolution != new Vector2(1920f, 1080f) || scaler.matchWidthOrHeight != 0.5f ||
            layer.GetComponent<GraphicRaycaster>() != null)
            throw new InvalidOperationException("Shared tooltip layer settings are invalid.");
        var tooltip = layer.GetComponentsInChildren<SpecialCardTooltip>(true).Single();
        GameObject game = scene.GetRootGameObjects().Single(root => root.name == "GameCanvas");
        if (game.GetComponent<GameplayUiReferences>().SpecialCardTooltip != tooltip)
            throw new InvalidOperationException("Tooltip reference changed.");
        ValidatePopup(game.GetComponentsInChildren<ShowdownCutInPopup>(true).Single());
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var texts = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<TextMeshProUGUI>(true))
            .Where(text => NumericNames.Contains(text.name)).ToArray();
        if (texts.Length != 7 || texts.Any(text => text.font != font || text.fontSharedMaterial != font.material))
            throw new InvalidOperationException("Numeric fonts/materials are inconsistent.");
        foreach (TextMeshProUGUI text in texts.Where(text => text.name.Contains("Score") || text.name.EndsWith("LifeDeduction")))
            if (text.enableAutoSizing || text.fontSize != 36f) throw new InvalidOperationException("Score/deduction size changed.");
    }

    private static void ValidatePopup(ShowdownCutInPopup popup)
    {
        var serialized = new SerializedObject(popup);
        foreach (string side in new[] { "player", "cpu" })
        {
            var image = (Image)serialized.FindProperty(side + "SpecialCardImage").objectReferenceValue;
            var target = (SpecialCardTooltipImageTarget)serialized.FindProperty(side + "SpecialCardTooltip").objectReferenceValue;
            if (target == null || target.gameObject != image.gameObject || !image.raycastTarget ||
                new SerializedObject(target).FindProperty("visualAssets").objectReferenceValue != serialized.FindProperty("assetSet").objectReferenceValue)
                throw new InvalidOperationException("Special-card tooltip is not explicitly connected.");
        }
        if (popup.transform.Find("Stage/PlayerLifeDeductionFrame") != null || popup.transform.Find("Stage/CpuLifeDeductionFrame") != null)
            throw new InvalidOperationException("Deduction frames remain.");
    }

    public static void AssertSceneProtected(Scene scene)
    {
        using (var file = File.OpenRead(Evidence + "scene-before.json.gz"))
        using (var gzip = new GZipStream(file, CompressionMode.Decompress))
        using (var reader = new StreamReader(gzip))
            AssertProtected(JsonUtility.FromJson<UiComponentMigration.Snapshot>(reader.ReadToEnd()), scene.GetRootGameObjects());
    }

    private static void AssertProtected(UiComponentMigration.Snapshot before, GameObject[] roots)
    {
        var expected = new UiComponentMigration.Snapshot();
        foreach (var target in before.targets)
        {
            if (target.identity.Contains("/PlayerLifeDeductionFrame|") || target.identity.Contains("/CpuLifeDeductionFrame|")) continue;
            target.properties.RemoveAll(value => ApprovedProperty(target.identity, value.path));
            target.identity = Remap(target.identity);
            foreach (var value in target.properties) value.text = Remap(value.text);
            expected.targets.Add(target);
        }
        UiComponentMigration.AssertMatches(expected, roots);
    }

    private static string Remap(string text) => text?.Replace("scene:GameCanvas/OverlayLayer/SpecialCardTooltip", "scene:TooltipCanvas/SpecialCardTooltip");

    private static bool ApprovedProperty(string identity, string path)
    {
        if (!MutablePaths.Contains(path) && !path.StartsWith("m_Children", StringComparison.Ordinal)) return false;
        string node = identity.Split('|')[0];
        bool numeric = NumericNames.Any(name => node.EndsWith("/" + name, StringComparison.Ordinal));
        if (numeric && identity.Contains("|TMPro.TextMeshProUGUI,") &&
            (path == "m_fontAsset" || path == "m_sharedMaterial" ||
             (node.EndsWith("Score") || node.EndsWith("LifeDeduction")) &&
             (path == "m_fontSize" || path == "m_fontSizeBase" || path == "m_enableAutoSizing"))) return true;
        if (identity.Contains("|ShowdownCutInPopup,") &&
            (path == "playerLifeDeductionFrameImage" || path == "cpuLifeDeductionFrameImage" ||
             path == "playerSpecialCardTooltip" || path == "cpuSpecialCardTooltip")) return true;
        if ((node.EndsWith("/PlayerSpecialCard") || node.EndsWith("/CpuSpecialCard")) &&
            identity.Contains("|UnityEngine.UI.Image,") && path == "m_RaycastTarget") return true;
        if (identity.Contains("|UnityEngine.RectTransform,"))
        {
            if ((node.EndsWith("/Stage") || node.EndsWith("/OverlayLayer")) && path.StartsWith("m_Children")) return true;
            if (node.Contains("/Stage/") && path == "m_RootOrder") return true;
            if (node.EndsWith("/SpecialCardTooltip") && (path == "m_Father" || path == "m_RootOrder")) return true;
        }
        return false;
    }

    private static void WriteSnapshot(string name, UiComponentMigration.Snapshot snapshot)
    {
        Directory.CreateDirectory(Evidence);
        using (var file = File.Create(Evidence + name))
        using (var gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal))
        using (var writer = new StreamWriter(gzip)) writer.Write(JsonUtility.ToJson(snapshot));
    }
}
