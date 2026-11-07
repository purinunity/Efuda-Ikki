using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>One-time, value-preserving extraction of the approved STR-03 UI parts.</summary>
public static class UiComponentMigration
{
    private const string ComponentRoot = "Assets/Prefabs/UI/Components/";
    private const string EvidenceRoot = "docs/design/evidence/str-03/";
    public static readonly IReadOnlyDictionary<string, string> GameplayParts = new Dictionary<string, string>
    {
        { "CardAreas/Deck", "card_deck_area" },
        { "CardAreas/Common", "common_card_area" },
        { "CardAreas/Trash", "discard_card_area" },
        { "CardAreas/Player1", "player_hand_area" },
        { "CardAreas/Player2", "cpu_hand_area" },
        { "CardAreas/SpecialP1", "player_special_card_area" },
        { "CardAreas/SpecialP2", "cpu_special_card_area" },
        { "CardAreas/SpeciaDeck1", "player_special_card_pool" },
        { "CardAreas/SpeciaDeck2", "cpu_special_card_pool" },
        { "CharacterHud", "character_hud" },
        { "Actions/D_button", "discard_action" }
    };
    public static readonly IReadOnlyDictionary<string, string> ShowdownParts = new Dictionary<string, string>
    {
        { "Stage/PlayerCards", "player_showdown_card_row" },
        { "Stage/CpuCards", "cpu_showdown_card_row" }
    };

    [Serializable]
    public sealed class Snapshot
    {
        public List<TargetState> targets = new List<TargetState>();
    }

    [Serializable]
    public sealed class TargetState
    {
        public string identity;
        public List<ValueState> properties = new List<ValueState>();
    }

    [Serializable]
    public sealed class ValueState
    {
        public string path;
        public SerializedPropertyType type;
        public string text;
        public long integer;
        public double number;
        public bool isDouble;
        public Vector4 vector;
    }

    [MenuItem("Tools/Efuda Ikki/Migrate UI Components (STR-03)")]
    public static void Run()
    {
        Scene scene = SceneManager.GetSceneByPath(UiPrefabMigration.ScenePath);
        if (EditorApplication.isPlaying || !scene.IsValid() || !scene.isLoaded || scene.isDirty)
            throw new InvalidOperationException("Open a clean latest scene in Edit Mode before the STR-03 migration.");
        foreach (string name in GameplayParts.Values.Concat(ShowdownParts.Values))
            if (File.Exists(ComponentRoot + name + ".prefab"))
                throw new InvalidOperationException("STR-03 migration is not repeatable: asset already exists: " + name);

        Directory.CreateDirectory(EvidenceRoot);
        Snapshot before = Capture(scene.GetRootGameObjects());
        File.WriteAllText(EvidenceRoot + "migration-before.json", JsonUtility.ToJson(before));
        if (!AssetDatabase.IsValidFolder(ComponentRoot.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Prefabs/UI", "Components");

        Extract("Assets/Prefabs/UI/Game/Gameplay.prefab", GameplayParts);
        Extract("Assets/Prefabs/UI/Game/Showdown.prefab", ShowdownParts);
        int restored = Restore(before, scene.GetRootGameObjects());
        AssertMatches(before, scene.GetRootGameObjects());
        UiPrefabBuildValidator.ValidateSceneOrThrow(scene);
        ValidateScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save migrated latest scene.");
        File.WriteAllText(EvidenceRoot + "migration-after.json", JsonUtility.ToJson(Capture(scene.GetRootGameObjects())));
        File.WriteAllText(EvidenceRoot + "migration-result.txt",
            "Preserved all captured properties and references. Extracted 13 nested prefabs. Scene properties reconnected: " + restored);
        Debug.Log("STR-03: extracted 13 UI parts and preserved all captured scene values and references.");
    }

    private static void Extract(string assetPath, IReadOnlyDictionary<string, string> parts)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
        try
        {
            Snapshot before = Capture(new[] { root });
            foreach (var part in parts)
            {
                Transform child = root.transform.Find(part.Key);
                if (child == null) throw new InvalidOperationException("Missing part: " + assetPath + "/" + part.Key);
                string path = ComponentRoot + part.Value + ".prefab";
                if (PrefabUtility.SaveAsPrefabAsset(child.gameObject, path) == null)
                    throw new IOException("Could not create UI component: " + path);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) throw new IOException("Missing exported UI component: " + path);
                // Match the existing objects in place. Replacing live CanvasRenderer
                // hierarchies in a prefab preview can leave stale native UI state.
                PrefabUtility.ConvertToPrefabInstance(child.gameObject, asset,
                    new ConvertToPrefabInstanceSettings
                    {
                        objectMatchMode = ObjectMatchMode.ByHierarchy,
                        changeRootNameToAssetName = false,
                        recordPropertyOverridesOfMatches = true,
                        componentsNotMatchedBecomesOverride = true,
                        gameObjectsNotMatchedBecomesOverride = true
                    }, InteractionMode.AutomatedAction);
            }
            Restore(before, new[] { root });
            AssertMatches(before, new[] { root });
            if (PrefabUtility.SaveAsPrefabAsset(root, assetPath) == null)
                throw new IOException("Could not save UI screen: " + assetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static void ValidateScene(Scene scene)
    {
        GameObject canvas = scene.GetRootGameObjects().Single(root => root.name == "GameCanvas");
        ValidateParts(canvas.transform.Find("GameplayScreen"), GameplayParts);
        ValidateParts(canvas.transform.Find("OverlayLayer/PopUpShowdown"), ShowdownParts);
    }

    private static void ValidateParts(Transform root, IReadOnlyDictionary<string, string> parts)
    {
        if (root == null) throw new InvalidOperationException("UI component owner is missing.");
        foreach (var part in parts)
        {
            Transform instance = root.Find(part.Key);
            string expected = ComponentRoot + part.Value + ".prefab";
            if (instance == null || !PrefabUtility.IsAnyPrefabInstanceRoot(instance.gameObject) ||
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instance.gameObject) != expected)
                throw new InvalidOperationException("UI component is not connected: " + expected);
        }
    }

    public static Snapshot Capture(GameObject[] roots)
    {
        var result = new Snapshot();
        foreach (Object target in Targets(roots))
        {
            var state = new TargetState { identity = Identity(target) };
            var iterator = new SerializedObject(target).GetIterator();
            bool enterChildren = true;
            while (iterator.Next(enterChildren))
            {
                // A reference's native m_FileID child is an Editor instance handle,
                // not a saved field. Restoring it would corrupt prefab provenance.
                enterChildren = iterator.propertyType != SerializedPropertyType.ObjectReference;
                // Unity manages prefab provenance and the component inventory itself.
                if (iterator.propertyPath.StartsWith("m_Component", StringComparison.Ordinal) ||
                    iterator.propertyPath.StartsWith("m_CorrespondingSourceObject", StringComparison.Ordinal) ||
                    iterator.propertyPath.StartsWith("m_PrefabInstance", StringComparison.Ordinal) ||
                    iterator.propertyPath.StartsWith("m_PrefabAsset", StringComparison.Ordinal)) continue;
                if (IsDrivenLayoutValue(target, iterator.propertyPath)) continue;
                ValueState value = Read(iterator);
                if (value != null) state.properties.Add(value);
            }
            result.targets.Add(state);
        }
        return result;
    }

    public static void AssertMatches(Snapshot expected, GameObject[] roots)
    {
        var actual = Capture(roots).targets.ToDictionary(value => value.identity);
        var objects = Targets(roots).ToDictionary(Identity);
        foreach (TargetState target in expected.targets)
        {
            if (!actual.TryGetValue(target.identity, out TargetState current))
                throw new InvalidOperationException("UI target was lost: " + target.identity);
            var values = current.properties.ToDictionary(value => value.path);
            foreach (ValueState value in target.properties)
            {
                if (IsDrivenLayoutValue(objects[target.identity], value.path)) continue;
                if (value.type == SerializedPropertyType.ObjectReference &&
                    value.text != null && value.text.StartsWith("transient:", StringComparison.Ordinal)) continue;
                if (!values.TryGetValue(value.path, out ValueState currentValue) ||
                    !SameValue(value, currentValue))
                    throw new InvalidOperationException("UI value changed: " + target.identity + "/" + value.path);
            }
        }
    }

    private static int Restore(Snapshot before, GameObject[] roots)
    {
        var objects = Targets(roots).ToDictionary(Identity);
        int restored = 0;
        foreach (TargetState target in before.targets)
        {
            if (!objects.TryGetValue(target.identity, out Object instance))
                throw new InvalidOperationException("Cannot reconnect missing UI target: " + target.identity);
            var serialized = new SerializedObject(instance);
            bool changed = false;
            foreach (ValueState value in target.properties)
            {
                if (IsDrivenLayoutValue(instance, value.path)) continue;
                if (value.type == SerializedPropertyType.ObjectReference &&
                    value.text != null && value.text.StartsWith("transient:", StringComparison.Ordinal)) continue;
                SerializedProperty property = serialized.FindProperty(value.path);
                if (property == null) throw new InvalidOperationException("UI property was lost: " + value.path);
                if (SameValue(Read(property), value)) continue;
                Write(property, value, objects);
                changed = true;
                restored++;
            }
            if (!changed) continue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(instance)) PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
        }
        return restored;
    }

    private static IEnumerable<Object> Targets(GameObject[] roots)
    {
        foreach (GameObject root in roots)
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                yield return transform.gameObject;
                foreach (Component component in transform.GetComponents<Component>())
                {
                    if (component == null) throw new InvalidOperationException("Missing Script on " + HierarchyPath(transform));
                    yield return component;
                }
            }
    }

    private static bool IsDrivenLayoutValue(Object target, string path)
    {
        if (!(target is RectTransform rect)) return false;
        // RectTransform localPosition is derived from anchors and parent dimensions.
        if (path.StartsWith("m_LocalPosition", StringComparison.Ordinal)) return true;
        if (rect.parent == null ||
            !rect.parent.TryGetComponent(out UnityEngine.UI.LayoutGroup layout) || !layout.enabled) return false;
        // Like the existing authoring tests, validate LayoutGroup settings rather than
        // the child coordinates that Unity regenerates on the next layout pass.
        return path.StartsWith("m_AnchoredPosition", StringComparison.Ordinal) ||
               path.StartsWith("m_LocalPosition", StringComparison.Ordinal) ||
               path.StartsWith("m_SizeDelta", StringComparison.Ordinal) ||
               path.StartsWith("m_AnchorMin", StringComparison.Ordinal) ||
               path.StartsWith("m_AnchorMax", StringComparison.Ordinal) ||
               path.StartsWith("m_Pivot", StringComparison.Ordinal);
    }

    private static string Identity(Object target)
    {
        if (target == null) return null;
        if (EditorUtility.IsPersistent(target) && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string guid, out long id))
            return "asset:" + guid + ":" + id;
        if (target is GameObject go) return "scene:" + HierarchyPath(go.transform) + "|GameObject";
        if (target is Component component)
        {
            Component[] siblings = component.GetComponents(component.GetType());
            return "scene:" + HierarchyPath(component.transform) + "|" + component.GetType().AssemblyQualifiedName +
                   "|" + Array.IndexOf(siblings, component);
        }
        throw new InvalidOperationException("Unsupported persistent UI reference: " + target);
    }

    private static string HierarchyPath(Transform transform)
    {
        string result = transform.name;
        while (transform.parent != null) { transform = transform.parent; result = transform.name + "/" + result; }
        return result;
    }

    private static ValueState Read(SerializedProperty property)
    {
        var result = new ValueState { path = property.propertyPath, type = property.propertyType };
        switch (property.propertyType)
        {
            case SerializedPropertyType.ObjectReference:
                Object reference = property.objectReferenceValue;
                // Per-label TMP material instances are Editor caches, not saved asset references.
                if (reference != null && !EditorUtility.IsPersistent(reference) &&
                    !(reference is GameObject) && !(reference is Component)) return null;
                result.text = Identity(reference); break;
            case SerializedPropertyType.String: result.text = property.stringValue; break;
            case SerializedPropertyType.Boolean: result.integer = property.boolValue ? 1 : 0; break;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.Character:
            case SerializedPropertyType.LayerMask: result.integer = property.longValue; break;
            case SerializedPropertyType.Float:
                result.number = property.doubleValue; result.isDouble = property.type == "double"; break;
            case SerializedPropertyType.Color:
                Color color = property.colorValue; result.vector = new Vector4(color.r, color.g, color.b, color.a); break;
            case SerializedPropertyType.Vector2: result.vector = property.vector2Value; break;
            case SerializedPropertyType.Vector3: result.vector = property.vector3Value; break;
            case SerializedPropertyType.Vector4: result.vector = property.vector4Value; break;
            case SerializedPropertyType.Quaternion:
                Quaternion q = property.quaternionValue; result.vector = new Vector4(q.x, q.y, q.z, q.w); break;
            case SerializedPropertyType.Rect:
                Rect r = property.rectValue; result.vector = new Vector4(r.x, r.y, r.width, r.height); break;
            default: return null;
        }
        return result;
    }

    private static bool SameValue(ValueState left, ValueState right)
    {
        if (left == null || right == null) return left == right;
        if (left.path != right.path || left.type != right.type) return false;
        // JsonUtility's double parser can round-trip a float-backed value with a
        // different last double digit. Compare in the actual property's precision.
        bool sameNumber = left.type == SerializedPropertyType.Float && !left.isDouble && !right.isDouble
            ? (float)left.number == (float)right.number
            : left.number == right.number;
        return (left.text ?? string.Empty) == (right.text ?? string.Empty) &&
               left.integer == right.integer && sameNumber && left.vector.Equals(right.vector);
    }

    private static void Write(SerializedProperty property, ValueState value, Dictionary<string, Object> objects)
    {
        switch (value.type)
        {
            case SerializedPropertyType.ObjectReference:
                property.objectReferenceValue = Resolve(value.text, objects); break;
            case SerializedPropertyType.String: property.stringValue = value.text; break;
            case SerializedPropertyType.Boolean: property.boolValue = value.integer != 0; break;
            case SerializedPropertyType.ArraySize: property.intValue = (int)value.integer; break;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.Character:
            case SerializedPropertyType.LayerMask: property.longValue = value.integer; break;
            case SerializedPropertyType.Float: property.doubleValue = value.number; break;
            case SerializedPropertyType.Color: property.colorValue = new Color(value.vector.x, value.vector.y, value.vector.z, value.vector.w); break;
            case SerializedPropertyType.Vector2: property.vector2Value = value.vector; break;
            case SerializedPropertyType.Vector3: property.vector3Value = value.vector; break;
            case SerializedPropertyType.Vector4: property.vector4Value = value.vector; break;
            case SerializedPropertyType.Quaternion: property.quaternionValue = new Quaternion(value.vector.x, value.vector.y, value.vector.z, value.vector.w); break;
            case SerializedPropertyType.Rect: property.rectValue = new Rect(value.vector.x, value.vector.y, value.vector.z, value.vector.w); break;
            default: throw new InvalidOperationException("Unsupported changed property: " + property.propertyPath);
        }
    }

    private static Object Resolve(string identity, Dictionary<string, Object> objects)
    {
        if (string.IsNullOrEmpty(identity)) return null;
        if (objects.TryGetValue(identity, out Object result)) return result;
        if (!identity.StartsWith("asset:", StringComparison.Ordinal))
            throw new InvalidOperationException("Cannot resolve reference: " + identity);
        string[] parts = identity.Split(':');
        long id = long.Parse(parts[2], CultureInfo.InvariantCulture);
        string path = AssetDatabase.GUIDToAssetPath(parts[1]);
        var assets = new List<Object>(AssetDatabase.LoadAllAssetsAtPath(path));
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) is GameObject prefab)
        {
            assets.AddRange(prefab.GetComponentsInChildren<Component>(true));
            assets.AddRange(prefab.GetComponentsInChildren<Transform>(true).Select(t => (Object)t.gameObject));
        }
        foreach (Object asset in assets)
            if (asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string _, out long localId) && localId == id)
                return asset;
        throw new InvalidOperationException("Cannot resolve asset: " + identity);
    }
}
