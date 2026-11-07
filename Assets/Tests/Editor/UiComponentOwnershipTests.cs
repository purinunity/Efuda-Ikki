using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UiComponentOwnershipTests
{
    [Test]
    public void MigrationSnapshot_ExcludesNativeReferenceHandlesAndPrefabProvenance()
    {
        GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Game/Gameplay.prefab");
        try
        {
            GameObject deck = root.transform.Find("CardAreas/Deck").gameObject;
            Assert.That(PrefabUtility.IsAnyPrefabInstanceRoot(deck), Is.True);
            UiComponentMigration.Snapshot snapshot = UiComponentMigration.Capture(new[] { deck });
            string[] paths = snapshot.targets.SelectMany(target => target.properties).Select(value => value.path).ToArray();
            Assert.That(paths.Any(path => path.Contains(".m_FileID")), Is.False,
                "A reference's native handle must never be restored as a saved integer property.");
            Assert.That(paths.Any(path => path.StartsWith("m_CorrespondingSourceObject") ||
                path.StartsWith("m_PrefabInstance") || path.StartsWith("m_PrefabAsset")), Is.False);
            Assert.DoesNotThrow(() => UiComponentMigration.AssertMatches(snapshot, new[] { deck }));
            var roundTripped = JsonUtility.FromJson<UiComponentMigration.Snapshot>(JsonUtility.ToJson(snapshot));
            Assert.DoesNotThrow(() => UiComponentMigration.AssertMatches(roundTripped, new[] { deck }),
                "A JSON round-trip must compare float-backed fields in their original precision.");
            Assert.That(PrefabUtility.IsAnyPrefabInstanceRoot(deck), Is.True);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void LatestScene_UsesAllThirteenAuthoredNestedParts()
    {
        Scene scene = SceneManager.GetSceneByPath(UiPrefabMigration.ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(UiPrefabMigration.ScenePath, OpenSceneMode.Additive);
        try
        {
            Assert.DoesNotThrow(() => UiComponentMigration.ValidateScene(scene));
            Assert.That(UiComponentMigration.GameplayParts.Count + UiComponentMigration.ShowdownParts.Count, Is.EqualTo(13));
            Assert.DoesNotThrow(() => UiPrefabBuildValidator.ValidateSceneOrThrow(scene));
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
    }

    [Test]
    public void ShowdownInitialization_PreservesAuthoredTypographyAndImageStyle()
    {
        GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Game/Showdown.prefab");
        try
        {
            var popup = root.GetComponent<ShowdownCutInPopup>();
            var score = root.GetComponentsInChildren<TextMeshProUGUI>(true).Single(text => text.name == "PlayerScore");
            var image = root.transform.Find("Stage/PlayerCards").GetComponentInChildren<Image>(true);
            score.fontSizeMin = 7f;
            score.fontSizeMax = 77f;
            score.fontStyle = FontStyles.Italic;
            score.color = Color.cyan;
            TMP_FontAsset authoredFont = score.font;
            image.preserveAspect = false;
            image.raycastTarget = true;
            image.type = Image.Type.Filled;

            popup.Initialize();
            popup.Initialize();

            Assert.That(score.fontSizeMin, Is.EqualTo(7f));
            Assert.That(score.fontSizeMax, Is.EqualTo(77f));
            Assert.That(score.fontStyle, Is.EqualTo(FontStyles.Italic));
            Assert.That(score.color, Is.EqualTo(Color.cyan));
            Assert.That(score.font, Is.SameAs(authoredFont));
            Assert.That(image.preserveAspect, Is.False);
            Assert.That(image.raycastTarget, Is.True);
            Assert.That(image.type, Is.EqualTo(Image.Type.Filled));
        }
        finally
        {
            // The altered contents are deliberately discarded, never saved into the asset.
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
