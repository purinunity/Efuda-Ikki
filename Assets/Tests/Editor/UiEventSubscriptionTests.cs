using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class UiEventSubscriptionTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();
    private EfudaIkki.Core.IProgressRepository originalProgressRepository;
    private Scene testScene;

    [SetUp]
    public void SetUp()
    {
        originalProgressRepository = GameProgressStore.Repository;
        GameProgressStore.Repository = new EmptyProgressRepository();
        testScene = EditorSceneManager.NewPreviewScene();
    }

    [TearDown]
    public void TearDown()
    {
        GameProgressStore.Repository = originalProgressRepository;
        for (int index = createdObjects.Count - 1; index >= 0; index--)
        {
            if (createdObjects[index] != null)
            {
                Object.DestroyImmediate(createdObjects[index]);
            }
        }

        createdObjects.Clear();
        if (testScene.IsValid())
        {
            EditorSceneManager.ClosePreviewScene(testScene);
        }
    }

    [TestCase(typeof(MainMenuPanel), "settingsButton", "settingsPanel")]
    [TestCase(typeof(SettingsPanel), "backButton", "modeSelectPanel")]
    [TestCase(typeof(TitleScreenPanel), "titleButton", "modeSelectPanel")]
    public void PanelButtonListener_IsRemovedWhileDisabledAndRestoredOnce(
        System.Type panelType,
        string buttonFieldName,
        string expectedPanelFieldName)
    {
        Button button = CreateButton("Panel Button");
        TitleUIManager manager = CreateTitleUiManager();
        GameObject panelObject = CreateObject("Panel");
        MonoBehaviour panel = (MonoBehaviour)panelObject.AddComponent(panelType);
        panelType.GetField(buttonFieldName).SetValue(panel, button);
        panelType.GetField("titleUIManager").SetValue(panel, manager);
        CanvasGroup expectedPanel =
            (CanvasGroup)typeof(TitleUIManager).GetField(expectedPanelFieldName).GetValue(manager);

        panelObject.SetActive(false);
        InvokeLifecycle(panel, "OnDisable");
        button.onClick.Invoke();
        Assert.That(expectedPanel.alpha, Is.Zero);

        panelObject.SetActive(true);
        InvokeLifecycle(panel, "OnEnable");
        button.onClick.Invoke();
        Assert.That(expectedPanel.alpha, Is.EqualTo(1f));

        manager.ShowTitleScreen();
        panelObject.SetActive(false);
        InvokeLifecycle(panel, "OnDisable");
        button.onClick.Invoke();
        Assert.That(expectedPanel.alpha, Is.Zero, "Disabled panels must not retain their click listener.");

        panelObject.SetActive(true);
        InvokeLifecycle(panel, "OnEnable");
        button.onClick.Invoke();
        Assert.That(expectedPanel.alpha, Is.EqualTo(1f), "Re-enabled panels must restore their click listener.");
    }

    [Test]
    public void PopupPreviewCardArea_RebindingAndDisable_RemoveSourceCardListener()
    {
        GameObject areaObject = CreateObject("Preview Area");
        PopupPreviewCardArea area = areaObject.AddComponent<PopupPreviewCardArea>();
        Card card = CreateCard("Source Card");

        area.SetCards(new List<Card> { card }, 0f);
        area.SetCards(new List<Card> { card }, 0f);
        LogAssert.Expect(LogType.Warning, "PopupPreviewCardArea: popup references are not assigned.");
        card.GetComponent<Button>().onClick.Invoke();
        LogAssert.NoUnexpectedReceived();

        areaObject.SetActive(false);
        InvokeLifecycle(area, "OnDisable");
        card.GetComponent<Button>().onClick.Invoke();
        LogAssert.NoUnexpectedReceived();
    }

    [Test]
    public void SpecialCardArea_RebindingKeepsGameplaySwitchListener()
    {
        GameObject areaObject = CreateObject("Special Card Area", typeof(RectTransform));
        SpecialCardArea area = areaObject.AddComponent<SpecialCardArea>();
        area.areaRect = areaObject.GetComponent<RectTransform>();
        Card first = CreateCard("First Special Card");
        Card second = CreateCard("Second Special Card");
        area.cardsInArea.Add(first);
        area.cardsInArea.Add(second);

        MethodInfo setupHandler = typeof(SpecialCardArea).GetMethod(
            "SetupCardClickHandler",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(setupHandler, Is.Not.Null);
        setupHandler.Invoke(area, new object[] { first });
        setupHandler.Invoke(area, new object[] { second });
        setupHandler.Invoke(area, new object[] { first });
        setupHandler.Invoke(area, new object[] { second });

        second.GetComponent<Button>().onClick.Invoke();

        Assert.That(area.cardsInArea[0], Is.SameAs(second),
            "Clicking the top gameplay special card must cycle it behind the stack after rebinding.");
    }

    [Test]
    public void SpecialCardArea_ClickDuringLayoutAnimation_IsNotDiscarded()
    {
        GameObject areaObject = CreateObject("Special Card Area", typeof(RectTransform));
        SpecialCardArea area = areaObject.AddComponent<SpecialCardArea>();
        area.areaRect = areaObject.GetComponent<RectTransform>();
        Card first = CreateCard("First Special Card");
        Card second = CreateCard("Second Special Card");
        area.cardsInArea.Add(first);
        area.cardsInArea.Add(second);

        MethodInfo setupHandler = typeof(SpecialCardArea).GetMethod(
            "SetupCardClickHandler",
            BindingFlags.Instance | BindingFlags.NonPublic);
        PropertyInfo moveComplete = typeof(Card).GetProperty(
            nameof(Card.MoveComplete),
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(setupHandler, Is.Not.Null);
        Assert.That(moveComplete?.GetSetMethod(true), Is.Not.Null);
        setupHandler.Invoke(area, new object[] { first });
        setupHandler.Invoke(area, new object[] { second });
        moveComplete.GetSetMethod(true).Invoke(second, new object[] { false });

        second.GetComponent<Button>().onClick.Invoke();

        Assert.That(area.cardsInArea[0], Is.SameAs(second),
            "A click received before the standalone layout animation finishes must still switch the special card.");
    }

    [Test]
    public void SpecialCardArea_NewMatch_ClearsUsageSelectionAndRestoresDeckOrder()
    {
        GameObject areaObject = CreateObject("Special Card Area", typeof(RectTransform));
        SpecialCardArea area = areaObject.AddComponent<SpecialCardArea>();
        area.areaRect = areaObject.GetComponent<RectTransform>();
        Card first = CreateCard("First Special Card");
        Card second = CreateCard("Second Special Card");
        first.Initialize();
        second.Initialize();
        var originalOrder = new List<Card> { first, second };

        MethodInfo rebuild = typeof(SpecialCardArea).GetMethod(
            "RebuildCardsInAreaKeepingSelection",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo switchTop = typeof(SpecialCardArea).GetMethod(
            "SwitchTopCard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(rebuild, Is.Not.Null);
        Assert.That(switchTop, Is.Not.Null);
        rebuild.Invoke(area, new object[] { originalOrder });
        switchTop.Invoke(area, new object[] { second });
        Assert.That(first.IsSelected, Is.True);

        area.SetUsedCards(new[] { second });
        area.SetInputEnabled(false);
        area.ResetForNewMatch(enableInput: true);
        rebuild.Invoke(area, new object[] { originalOrder });

        Assert.That(area.cardsInArea, Is.EqualTo(originalOrder));
        Assert.That(first.IsSelected, Is.False);
        Assert.That(second.IsSelected, Is.True,
            "A new match must choose from the restored deck order instead of retaining the previous match's top card.");
        Assert.That(first.GetComponent<Button>().interactable, Is.True);
        Assert.That(second.GetComponent<Button>().interactable, Is.True);
    }

    [Test]
    public void SpecialCardArea_TooltipIsEnabledOnlyForTopCard()
    {
        GameObject areaObject = CreateObject("Special Card Area", typeof(RectTransform));
        SpecialCardArea area = areaObject.AddComponent<SpecialCardArea>();
        area.areaRect = areaObject.GetComponent<RectTransform>();
        Card first = CreateCard("First Special Card");
        Card second = CreateCard("Second Special Card");
        first.Initialize();
        second.Initialize();

        MethodInfo rebuild = typeof(SpecialCardArea).GetMethod(
            "RebuildCardsInAreaKeepingSelection",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo switchTop = typeof(SpecialCardArea).GetMethod(
            "SwitchTopCard",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo tooltipEnabled = typeof(SpecialCardTooltipTarget).GetField(
            "tooltipEnabled",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(rebuild, Is.Not.Null);
        Assert.That(switchTop, Is.Not.Null);
        Assert.That(tooltipEnabled, Is.Not.Null);

        rebuild.Invoke(area, new object[] { new List<Card> { first, second } });
        area.SetTooltipsEnabled(true);
        SpecialCardTooltipTarget firstTooltip = first.GetComponent<SpecialCardTooltipTarget>();
        SpecialCardTooltipTarget secondTooltip = second.GetComponent<SpecialCardTooltipTarget>();
        Assert.That(firstTooltip, Is.Not.Null);
        Assert.That(secondTooltip, Is.Not.Null);
        Assert.That(tooltipEnabled.GetValue(firstTooltip), Is.False);
        Assert.That(tooltipEnabled.GetValue(secondTooltip), Is.True);

        switchTop.Invoke(area, new object[] { second });
        Assert.That(tooltipEnabled.GetValue(firstTooltip), Is.True);
        Assert.That(tooltipEnabled.GetValue(secondTooltip), Is.False);
    }

    [Test]
    public void HandCardArea_InputLockSurvivesSelectionLimitRefresh()
    {
        GameObject areaObject = CreateObject("Player Hand", typeof(RectTransform));
        LimitedSelectableCardArea area = areaObject.AddComponent<LimitedSelectableCardArea>();
        area.areaRect = areaObject.GetComponent<RectTransform>();
        Card card = CreateCard("Hand Card");
        area.cardsInArea.Add(card);

        area.SetInputEnabled(false);
        area.SetMaxSelectableCount(5);

        Assert.That(card.IsSelectable, Is.False);
        Assert.That(area.TryToggleSelection(card), Is.False);
        Assert.That(card.IsSelected, Is.False);
    }

    private TitleUIManager CreateTitleUiManager()
    {
        GameObject managerObject = CreateObject("Title UI Manager");
        TitleUIManager manager = managerObject.AddComponent<TitleUIManager>();
        manager.titleScreenPanel = CreateCanvasGroup("Title Panel");
        manager.modeSelectPanel = CreateCanvasGroup("Mode Panel");
        manager.stageSelectPanel = CreateCanvasGroup("Stage Panel");
        manager.specialCardSelectPanel = CreateCanvasGroup("Special Panel");
        manager.settingsPanel = CreateCanvasGroup("Settings Panel");
        manager.ShowTitleScreen();
        return manager;
    }

    private CanvasGroup CreateCanvasGroup(string name)
    {
        GameObject gameObject = CreateObject(name, typeof(RectTransform), typeof(CanvasGroup));
        return gameObject.GetComponent<CanvasGroup>();
    }

    private Button CreateButton(string name)
    {
        GameObject gameObject = CreateObject(name, typeof(RectTransform), typeof(Button));
        return gameObject.GetComponent<Button>();
    }

    private Card CreateCard(string name)
    {
        GameObject gameObject = CreateObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(Card));
        return gameObject.GetComponent<Card>();
    }

    private GameObject CreateObject(string name, params System.Type[] components)
    {
        var gameObject = new GameObject(name, components);
        SceneManager.MoveGameObjectToScene(gameObject, testScene);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static void InvokeLifecycle(MonoBehaviour target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"{target.GetType().Name}.{methodName}");
        method.Invoke(target, null);
    }

    private sealed class EmptyProgressRepository : EfudaIkki.Core.IProgressRepository
    {
        public string GetString(string key, string defaultValue) => defaultValue;
        public int GetInt(string key, int defaultValue) => defaultValue;
        public void SetString(string key, string value) { }
        public void SetInt(string key, int value) { }
        public void Save() { }
    }
}
