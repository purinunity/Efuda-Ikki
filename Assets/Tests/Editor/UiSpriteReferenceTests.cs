using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class UiSpriteReferenceTests
{
    [TestCase("StageSelectPanel", "ui/backgrounds/character_select.png")]
    [TestCase("SpecialCardSelectPanel", "ui/backgrounds/card_select.png")]
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
}
