using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class UiEventSubscriptionTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();
    private EfudaIkki.Core.IProgressRepository originalProgressRepository;

    [SetUp]
    public void SetUp()
    {
        originalProgressRepository = GameProgressStore.Repository;
        GameProgressStore.Repository = new EmptyProgressRepository();
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
        button.onClick.Invoke();
        Assert.That(expectedPanel.alpha, Is.Zero);

        panelObject.SetActive(true);
        button.onClick.Invoke();
        Assert.That(expectedPanel.alpha, Is.EqualTo(1f));

        manager.ShowTitleScreen();
        panelObject.SetActive(false);
        button.onClick.Invoke();
        Assert.That(expectedPanel.alpha, Is.Zero, "Disabled panels must not retain their click listener.");

        panelObject.SetActive(true);
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
        createdObjects.Add(gameObject);
        return gameObject;
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
