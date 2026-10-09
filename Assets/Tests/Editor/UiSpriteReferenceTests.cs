using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UiSpriteReferenceTests
{
    [Test]
    public void UpdatedCutInAndResultAssetsResolveToRequestedProductionFiles()
    {
        ShowdownCutInAssetSet cutIn = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        MatchResultVisualAssets result = Resources.Load<MatchResultVisualAssets>("MatchResultVisualAssets");

        Assert.That(AssetDatabase.GetAssetPath(cutIn.cutInBackground), Does.EndWith("/ui/backgrounds/新/cut_in.png"));
        Assert.That(AssetDatabase.GetAssetPath(cutIn.roleFrame), Does.EndWith("/ui/frames/汎用panel.png"));
        Assert.That(AssetDatabase.GetAssetPath(cutIn.lifeDeductionFrame), Does.EndWith("/ui/frames/minus2.png"));
        Assert.That(AssetDatabase.GetAssetPath(result.winBackground), Does.EndWith("/ui/backgrounds/results/新/win_background.png"));
        Assert.That(AssetDatabase.GetAssetPath(result.loseBackground), Does.EndWith("/ui/backgrounds/results/新/lose_background.png"));
        Assert.That(cutIn.roleEffectPrefabs, Has.Length.EqualTo(11));
        Assert.That(cutIn.roleEffectPrefabs, Has.All.Not.Null);
    }

    [TestCase("StageSelectPanel", "ui/backgrounds/新/character_select.png")]
    [TestCase("SpecialCardSelectPanel", "ui/backgrounds/新/card_select.png")]
    [TestCase("D_button", "ui/buttons/common/confirm_discard.png")]
    [TestCase("StartButton", "ui/buttons/common/confirm_special.png")]
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
            AssertSprite(serialized, "openButtonSprite", "ui/buttons/common/show_role_list2.png");
            AssertSprite(serialized, "combinedRoleSprite", "ui/reference/roles/役一覧.png");
            AssertSprite(serialized, "closeButtonSprite", "ui/buttons/common/close.png");
        }
        finally
        {
            if (openedForTest) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void AssertSprite(SerializedObject serialized, string propertyName, string relativePath)
    {
        var sprite = serialized.FindProperty(propertyName).objectReferenceValue;
        Assert.That(sprite, Is.Not.Null, propertyName);
        Assert.That(AssetDatabase.GetAssetPath(sprite),
            Is.EqualTo("Assets/Sprite/production/" + relativePath));
    }
}
