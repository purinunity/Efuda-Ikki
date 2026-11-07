using UnityEditor;
using UnityEngine;

public sealed class UiAuthoringWindow : EditorWindow
{
    [MenuItem("Tools/Efuda Ikki/UI Authoring")]
    private static void OpenWindow()
    {
        GetWindow<UiAuthoringWindow>("Efuda UI Authoring");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Screen Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Open a screen directly in Prefab Mode. Layout changes belong to the prefab and do not dirty latest.unity.",
            MessageType.Info);

        foreach (var item in UiPrefabMigration.ScreenPrefabs)
        {
            string label = System.IO.Path.GetFileNameWithoutExtension(item.Value);
            using (new EditorGUI.DisabledScope(AssetDatabase.LoadAssetAtPath<GameObject>(item.Value) == null))
            {
                if (GUILayout.Button(label, GUILayout.Height(28f)))
                {
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(item.Value));
                }
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Validate UI Authoring", GUILayout.Height(32f)))
            UiPrefabBuildValidator.ValidateProjectOrThrow();
    }
}
