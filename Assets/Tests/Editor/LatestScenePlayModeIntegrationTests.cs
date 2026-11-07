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
    public IEnumerator InactiveSceneAuthoredShowdownPopup_DisplaysSprites()
    {
        StoreCurrentSceneSetup();
        OpenLatestSceneWithoutDiscardingCurrentSetup();
        yield return new EnterPlayMode();
        yield return null;
        yield return VerifyInactiveShowdownPopup();
    }

    private static IEnumerator VerifyInactiveShowdownPopup()
    {
        var assets = AssetDatabase.LoadAssetAtPath<ShowdownCutInAssetSet>("Assets/Data/UI/ShowdownCutInAssets.asset");
        Assert.That(assets, Is.Not.Null);
        Assert.That(assets.textFont, Is.Not.Null, "Cut-in font asset");
        Assert.That(assets.textFont.material, Is.Not.Null, "Cut-in font material");
        float originalOutline = assets.textFont.material.GetFloat(TMPro.ShaderUtilities.ID_OutlineWidth);
        var popup = UnityEngine.Object.FindObjectOfType<ShowdownCutInPopup>(true);
        UnityEngine.Object.FindObjectOfType<UiRootCoordinator>(true).ShowGameplay();
        Assert.That(popup, Is.Not.Null);
        popup.gameObject.SetActive(false);
        var authoredDeductionOutlines = popup.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true)
            .Where(value => value.name == "CpuLifeDeduction" || value.name == "PlayerLifeDeduction")
            .ToDictionary(value => value.name, value => value.outlineWidth);
        Assert.DoesNotThrow(() => popup.Initialize());
        VerifyLifeDeductionText(popup, authoredDeductionOutlines);

        Assert.That(assets.textFont.material.GetFloat(TMPro.ShaderUtilities.ID_OutlineWidth), Is.EqualTo(originalOutline),
            "Styling a popup must not modify the shared font asset material.");

        Sprite card = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/production/cards/basic/bird/rank_01.png");
        Sprite player = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/production/characters/player/normal.png");
        Sprite cpu = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprite/production/characters/opponents/boy/normal.png");
        Assert.That(card, Is.Not.Null);
        Assert.That(player, Is.Not.Null);
        Assert.That(cpu, Is.Not.Null);
        CardData[] specials = UnityEngine.Object.FindObjectOfType<UIManager>(true).specialCards1.cardList
            .Where(value => value != null && SpecialCardResolver.TryGetSpecialCardId(value.CardData, out _))
            .Take(2).Select(value => value.CardData).ToArray();
        Assert.That(specials.Length, Is.EqualTo(2));
        var data = new ShowdownCutInPopup.Data(player, cpu, "見えず", "見えず",
            HandEvaluator.HandRank.Miezu, HandEvaluator.HandRank.Miezu, 0, 0,
            "見えず", "見えず", HandEvaluator.HandRank.Miezu, HandEvaluator.HandRank.Miezu, 0, 0,
            Enumerable.Repeat(card, 7).ToArray(), Enumerable.Repeat(card, 7).ToArray(),
            new bool[7], new bool[7], specials[0].Image, specials[1].Image, null, -1, 0, 100, 100, 100, 100,
            specials[0], specials[1]);
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
        VerifyCutInTooltips(popup, specials, assets);
        popup.CancelDisplay();
        yield return null;
        yield return VerifyMenuTooltip(assets);
        LogAssert.NoUnexpectedReceived();
    }

    private static void VerifyCutInTooltips(ShowdownCutInPopup popup, CardData[] specials, ShowdownCutInAssetSet assets)
    {
        var serialized = new SerializedObject(popup);
        var tooltip = UnityEngine.Object.FindObjectOfType<SpecialCardTooltip>(true);
        CanvasGroup group = tooltip.GetComponent<CanvasGroup>();
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            { position = new Vector2(Screen.width - 2f, 2f) };
        string[] sides = { "player", "cpu" };
        for (int i = 0; i < sides.Length; i++)
        {
            var target = (SpecialCardTooltipImageTarget)serialized.FindProperty(sides[i] + "SpecialCardTooltip").objectReferenceValue;
            Assert.That(target.GetComponent<Image>().raycastTarget, Is.True);
            target.OnPointerEnter(pointer);
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(tooltip.GetComponent<Image>().sprite, Is.SameAs(assets.GetSpecialCardTooltipSprite(specials[i])));
            Assert.That(group.blocksRaycasts, Is.False);
            target.OnPointerExit(pointer);
            Assert.That(group.alpha, Is.Zero);
            target.OnPointerEnter(pointer);
            target.gameObject.SetActive(false);
            Assert.That(group.alpha, Is.Zero, "Disabling a hovered cut-in card must hide its tooltip.");
            target.gameObject.SetActive(true);
        }
        var blank = (SpecialCardTooltipImageTarget)serialized.FindProperty("playerSpecialCardTooltip").objectReferenceValue;
        blank.Clear();
        blank.OnPointerEnter(pointer);
        Assert.That(group.alpha, Is.Zero);
    }

    private static IEnumerator VerifyMenuTooltip(ShowdownCutInAssetSet assets)
    {
        var title = UnityEngine.Object.FindObjectOfType<TitleUIManager>(true);
        title.ShowSpecialCardSelect();
        yield return null;
        var panel = UnityEngine.Object.FindObjectOfType<SpecialCardSelectPanel>(true);
        var pool = (Cards)new SerializedObject(panel).FindProperty("specialCardsDeck").objectReferenceValue;
        Card card = pool.cardList.First(value => value != null && !SpecialCardResolver.IsNoUseSpecialCard(value.CardData) &&
            !SpecialCardResolver.IsCpuOnlySpecialCard(value.CardData) && GameProgressStore.IsSpecialCardUnlocked(value.CardData));
        var tooltip = UnityEngine.Object.FindObjectOfType<SpecialCardTooltip>(true);
        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) };
        var target = card.GetComponent<SpecialCardTooltipTarget>();
        target.OnPointerEnter(pointer);
        Assert.That(tooltip.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
        Assert.That(tooltip.GetComponent<Image>().sprite, Is.SameAs(assets.GetSpecialCardTooltipSprite(card.CardData)));
        Assert.That(tooltip.GetComponentInParent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(tooltip.GetComponentsInParent<CanvasGroup>().All(group => group.alpha > 0f), Is.True,
            "The menu tooltip must not inherit the hidden gameplay CanvasGroup.");
        Assert.That(panel.gameObject.scene.GetRootGameObjects().Single(root => root.name == "GameCanvas")
            .GetComponent<CanvasGroup>().alpha, Is.Zero);
        title.ShowTitleScreen();
        yield return null;
        Assert.That(tooltip.GetComponent<CanvasGroup>().alpha, Is.Zero);
    }

    private static void VerifyLifeDeductionText(ShowdownCutInPopup popup, System.Collections.Generic.Dictionary<string, float> outlines)
    {
        foreach (string name in new[] { "CpuLifeDeduction", "PlayerLifeDeduction" })
        {
            var text = popup.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Single(value => value.name == name);
            Assert.That(text.canvasRenderer, Is.Not.Null, name);
            Assert.That(text.fontSharedMaterial, Is.Not.Null, name);
            Assert.That(text.outlineWidth, Is.EqualTo(outlines[name]).Within(0.001f), name);
        }
        Assert.DoesNotThrow(() => popup.Initialize());
    }

    [UnityTest]
    public IEnumerator LatestScene_OpensByAssetPathAndEntersPlayMode()
    {
        SessionState.EraseString(SceneSetupSessionKey);
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(LatestScenePath), Is.Not.Null);

        StoreCurrentSceneSetup();
        OpenLatestSceneWithoutDiscardingCurrentSetup();
        Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(LatestScenePath));

        yield return new EnterPlayMode();
        yield return null;

        Assert.That(EditorApplication.isPlaying, Is.True);
        Assert.That(UnityEngine.Object.FindObjectOfType<GameManager>(true), Is.Not.Null);
        Assert.That(UnityEngine.Object.FindObjectOfType<PlayerController>(true), Is.Not.Null);
        UIManager uiManager = UnityEngine.Object.FindObjectOfType<UIManager>(true);
        Assert.That(uiManager, Is.Not.Null);

        VerifyDuplicateUiUpdatesRecover(uiManager);
        VerifyExplicitSnapshotMode(uiManager);
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

    private static void VerifyExplicitSnapshotMode(UIManager ui)
    {
        GameModeData original = GameModeManager.GetGameModeData();
        try
        {
            var state = new GameState();
            state.InitializePlayerStates();
            var battle = new GameModeData(GameModeData.GameMode.BattleGroundMode);
            battle.IncrementWinStreak();
            battle.IncrementWinStreak();
            GameUiSnapshot snapshot = new GameUiPresenter().CreateSnapshot(state, battle);
            GameModeManager.SetGameModeData(new GameModeData());
            ui.Render(snapshot, 0f);
            ui.RecoverFromStalledUpdate();
            var serialized = new SerializedObject(ui);
            var streak = (GameObject)serialized.FindProperty("battleGroundStreakView").objectReferenceValue;
            var round = (GameObject)serialized.FindProperty("ikkiRoundView").objectReferenceValue;
            var text = (TMPro.TextMeshProUGUI)serialized.FindProperty("battleGroundStreakText").objectReferenceValue;
            Assert.That(streak.activeSelf, Is.True);
            Assert.That(round.activeSelf, Is.False);
            Assert.That(text.text, Is.EqualTo("2人抜き"));
            GameModeManager.SetGameModeData(battle);
            ui.Render(new GameUiPresenter().CreateSnapshot(state, new GameModeData()), 0f);
            ui.RecoverFromStalledUpdate();
            Assert.That(streak.activeSelf, Is.False);
            Assert.That(round.activeSelf, Is.True);
        }
        finally { GameModeManager.SetGameModeData(original); }
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

    private static void OpenLatestSceneWithoutDiscardingCurrentSetup()
    {
        Scene latest = SceneManager.GetSceneByPath(LatestScenePath);
        if (!latest.IsValid() || !latest.isLoaded)
        {
            latest = EditorSceneManager.OpenScene(LatestScenePath, OpenSceneMode.Additive);
        }

        SceneManager.SetActiveScene(latest);
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
