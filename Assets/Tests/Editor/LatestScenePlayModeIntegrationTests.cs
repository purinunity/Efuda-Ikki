using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class LatestScenePlayModeIntegrationTests
{
    private const string LatestScenePath = "Assets/Scenes/latest.unity";
    private const string SceneSetupSessionKey = "EfudaIkki.Tests.LatestScene.OriginalSetup";

    [Serializable]
    private sealed class SetupSnapshot
    {
        public SetupEntry[] entries;
    }

    [Serializable]
    private sealed class SetupEntry
    {
        public string path;
        public bool isLoaded;
        public bool isActive;
        public bool isSubScene;
    }

    [UnityTest]
    public IEnumerator InactiveShowdownPopup_InitializesMissingTextAndDisplaysSprites()
    {
        if (Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Any(scene => scene.isDirty))
            throw new InvalidOperationException("Save scene changes before running this test.");

        StoreCurrentSceneSetup();
        EditorSceneManager.OpenScene(LatestScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;
        yield return VerifyInactiveShowdownPopup();
    }

    private static IEnumerator VerifyInactiveShowdownPopup()
    {
        var assets = Resources.Load<ShowdownCutInAssetSet>("ShowdownCutInAssets");
        Assert.That(assets, Is.Not.Null);
        Assert.That(assets.textFont, Is.Not.Null, "Cut-in font asset");
        Assert.That(assets.textFont.material, Is.Not.Null, "Cut-in font material");
        float originalOutline = assets.textFont.material.GetFloat(TMPro.ShaderUtilities.ID_OutlineWidth);
        var popup = UnityEngine.Object.FindObjectOfType<ShowdownCutInPopup>(true);
        Assert.That(popup, Is.Not.Null);
        popup.gameObject.SetActive(false);
        Assert.DoesNotThrow(() => popup.Initialize());
        VerifyLifeDeductionText(popup);

        // Also exercise a completely absent hierarchy under an inactive parent:
        // TMP.Awake has not run when the fallback text is first configured.
        var parent = new GameObject("Inactive Popup Test", typeof(RectTransform), typeof(Canvas));
        parent.SetActive(false);
        var fallbackObject = new GameObject("Fallback", typeof(RectTransform));
        fallbackObject.transform.SetParent(parent.transform, false);
        var fallback = fallbackObject.AddComponent<ShowdownCutInPopup>();
        Assert.DoesNotThrow(() => fallback.Initialize());
        VerifyLifeDeductionText(fallback);
        Assert.That(parent.activeSelf, Is.False);
        Assert.That(assets.textFont.material.GetFloat(TMPro.ShaderUtilities.ID_OutlineWidth), Is.EqualTo(originalOutline),
            "Styling a popup must not modify the shared font asset material.");

        Sprite card = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/production/cards/basic/bird/rank_01.png");
        Sprite player = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/production/characters/player/normal.png");
        Sprite cpu = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/production/characters/opponents/boy/normal.png");
        Assert.That(card, Is.Not.Null);
        Assert.That(player, Is.Not.Null);
        Assert.That(cpu, Is.Not.Null);
        var data = new ShowdownCutInPopup.Data(player, cpu, "見えず", "見えず",
            HandEvaluator.HandRank.Miezu, HandEvaluator.HandRank.Miezu, 0, 0,
            "見えず", "見えず", HandEvaluator.HandRank.Miezu, HandEvaluator.HandRank.Miezu, 0, 0,
            Enumerable.Repeat(card, 7).ToArray(), Enumerable.Repeat(card, 7).ToArray(),
            new bool[7], new bool[7], null, null, null, -1, 0, 100, 100, 100, 100);
        IEnumerator presentation = popup.Play(data);
        Assert.That(presentation.MoveNext(), Is.True);
        yield return null;
        Assert.That(popup.gameObject.activeInHierarchy, Is.True, "The first Play must not be hidden by deferred Awake.");
        foreach (string name in new[] { "Background", "PlayerCharacter", "CpuCharacter", "PlayerRole", "CpuRole", "PlayerShowdownCard1", "CpuShowdownCard1" })
        {
            Image image = popup.GetComponentsInChildren<Image>(true).First(value => value.name == name);
            Assert.That(image.sprite, Is.Not.Null, name);
            Assert.That(image.gameObject.activeInHierarchy, Is.True, name);
            Assert.That(image.color.a, Is.GreaterThan(0f), name);
        }
        popup.CancelDisplay();
        UnityEngine.Object.Destroy(parent);
        yield return null;
        LogAssert.NoUnexpectedReceived();
    }

    private static void VerifyLifeDeductionText(ShowdownCutInPopup popup)
    {
        foreach (string name in new[] { "CpuLifeDeduction", "PlayerLifeDeduction" })
        {
            var text = popup.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Single(value => value.name == name);
            Assert.That(text.canvasRenderer, Is.Not.Null, name);
            Assert.That(text.fontSharedMaterial, Is.Not.Null, name);
            Assert.That(text.outlineWidth, Is.EqualTo(0.24f).Within(0.001f), name);
        }
        Assert.DoesNotThrow(() => popup.Initialize());
    }

    [UnityTest]
    public IEnumerator LatestScene_OpensByAssetPathAndEntersPlayMode()
    {
        SessionState.EraseString(SceneSetupSessionKey);
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(LatestScenePath), Is.Not.Null);

        Scene[] openScenes = Enumerable.Range(0, SceneManager.sceneCount)
            .Select(SceneManager.GetSceneAt)
            .ToArray();
        if (openScenes.Any(scene => scene.isDirty))
        {
            throw new InvalidOperationException(
                "The current Editor scene setup contains unsaved changes. Save or revert it before running this integration test.");
        }

        StoreCurrentSceneSetup();
        EditorSceneManager.OpenScene(LatestScenePath, OpenSceneMode.Single);
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(LatestScenePath));

        yield return new EnterPlayMode();
        yield return null;

        Assert.That(EditorApplication.isPlaying, Is.True);
        Assert.That(UnityEngine.Object.FindObjectOfType<GameManager>(true), Is.Not.Null);
        Assert.That(UnityEngine.Object.FindObjectOfType<PlayerController>(true), Is.Not.Null);
        UIManager uiManager = UnityEngine.Object.FindObjectOfType<UIManager>(true);
        Assert.That(uiManager, Is.Not.Null);

        VerifyDuplicateUiUpdatesRecover(uiManager);
        yield return VerifyCardListenerAndMotionRecovery();
        VerifyPlayerInputCancellation();
    }

    private static void VerifyDuplicateUiUpdatesRecover(UIManager uiManager)
    {
        var state = new GameState();
        state.InitializePlayerStates();

        uiManager.UIUpdate(state, 30f);
        Assert.That(uiManager.UIUpdateInProgress, Is.True);
        uiManager.UIUpdate(state, 30f);
        Assert.That(uiManager.UIUpdateInProgress, Is.True);
        uiManager.RecoverFromStalledUpdate();
        Assert.That(uiManager.UIUpdateInProgress, Is.False);
    }

    private static IEnumerator VerifyCardListenerAndMotionRecovery()
    {
        var cardObject = new GameObject(
            "Runtime Safety Card",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(Card));
        Card card = cardObject.GetComponent<Card>();
        Button button = cardObject.GetComponent<Button>();
        card.IsSelectable = true;
        card.TargetPosition = new Vector2(300f, 0f);

        card.Initialize();
        card.Initialize();
        button.onClick.Invoke();
        Assert.That(card.IsSelected, Is.True, "One click must toggle selection exactly once.");
        Assert.That(card.MoveComplete, Is.False);

        cardObject.SetActive(false);
        Assert.That(card.MoveComplete, Is.True, "Disabling a moving card must complete its motion state.");
        button.onClick.Invoke();
        Assert.That(card.IsSelected, Is.True, "Disabled cards must not retain click subscriptions.");

        cardObject.SetActive(true);
        card.SnapToTargetPosition();
        button.onClick.Invoke();
        Assert.That(card.IsSelected, Is.False, "Re-enabled cards must register one click listener.");

        UnityEngine.Object.Destroy(cardObject);
        yield return null;
    }

    private static void VerifyPlayerInputCancellation()
    {
        var controllerObject = new GameObject("Runtime Safety PlayerController");
        PlayerController controller = controllerObject.AddComponent<PlayerController>();
        var state = new GameState();
        state.InitializePlayerStates();
        int callbackCount = 0;
        ControllerResponse response = null;
        IEnumerator action = controller.Act(state, result =>
        {
            callbackCount++;
            response = result;
        });

        Assert.That(action.MoveNext(), Is.True, "Player input must wait without a time limit.");
        Assert.That(controller.IsInputReceivable, Is.True);
        controllerObject.SetActive(false);

        Assert.That(callbackCount, Is.EqualTo(1));
        Assert.That(response, Is.Not.Null);
        Assert.That(response.actionCompleted, Is.False);
        Assert.That(response.cardsTrash, Is.Empty);
        Assert.That(action.MoveNext(), Is.False);
        UnityEngine.Object.Destroy(controllerObject);
    }

    [UnityTearDown]
    public IEnumerator RestoreOriginalEditorState()
    {
        if (EditorApplication.isPlaying)
        {
            yield return new ExitPlayMode();
        }

        RestoreStoredSceneSetup();
    }

    private static void StoreCurrentSceneSetup()
    {
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        var snapshot = new SetupSnapshot
        {
            entries = setup.Select(entry => new SetupEntry
            {
                path = entry.path,
                isLoaded = entry.isLoaded,
                isActive = entry.isActive,
                isSubScene = entry.isSubScene
            }).ToArray()
        };
        SessionState.SetString(SceneSetupSessionKey, JsonUtility.ToJson(snapshot));
    }

    private static void RestoreStoredSceneSetup()
    {
        string json = SessionState.GetString(SceneSetupSessionKey, string.Empty);
        SessionState.EraseString(SceneSetupSessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        SetupSnapshot snapshot = JsonUtility.FromJson<SetupSnapshot>(json);
        if (snapshot?.entries == null || snapshot.entries.Length == 0)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return;
        }

        SceneSetup[] setup = snapshot.entries.Select(entry => new SceneSetup
        {
            path = entry.path,
            isLoaded = entry.isLoaded,
            isActive = entry.isActive,
            isSubScene = entry.isSubScene
        }).ToArray();

        if (setup.All(entry => !string.IsNullOrEmpty(entry.path)))
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
            return;
        }

        RestoreSetupContainingUntitledScene(snapshot.entries);
    }

    private static void RestoreSetupContainingUntitledScene(SetupEntry[] entries)
    {
        bool first = true;
        Scene activeScene = default;
        foreach (SetupEntry entry in entries)
        {
            Scene scene;
            if (string.IsNullOrEmpty(entry.path))
            {
                scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    first ? NewSceneMode.Single : NewSceneMode.Additive);
            }
            else
            {
                scene = EditorSceneManager.OpenScene(
                    entry.path,
                    first ? OpenSceneMode.Single : OpenSceneMode.Additive);
            }

            first = false;
            if (entry.isActive)
            {
                activeScene = scene;
            }
        }

        if (activeScene.IsValid())
        {
            SceneManager.SetActiveScene(activeScene);
        }
    }
}
