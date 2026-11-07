using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UiPrefabMigration
{
    public const string ScenePath = "Assets/Scenes/latest.unity";
    public const string PrefabRoot = "Assets/Prefabs/UI";

    public static readonly IReadOnlyDictionary<string, string> ScreenPrefabs =
        new Dictionary<string, string>
        {
            { "MenuCanvas/Screens/TitleScreen/TitleScreenPanel", PrefabRoot + "/Menu/Title.prefab" },
            { "MenuCanvas/Screens/ModeSelectScreen/ModeSelectPanel", PrefabRoot + "/Menu/ModeSelect.prefab" },
            { "MenuCanvas/Screens/StageSelectScreen/StageSelectPanel", PrefabRoot + "/Menu/StageSelect.prefab" },
            { "MenuCanvas/Screens/SpecialCardSelectScreen/SpecialCardSelectPanel", PrefabRoot + "/Menu/SpecialCardSelect.prefab" },
            { "MenuCanvas/Screens/SettingsScreen/SettingsPanel", PrefabRoot + "/Menu/Settings.prefab" },
            { "GameCanvas/GameplayScreen", PrefabRoot + "/Game/Gameplay.prefab" },
            { "GameCanvas/OverlayLayer/PopUpTrush", PrefabRoot + "/Game/DiscardPreview.prefab" },
            { "GameCanvas/OverlayLayer/RoleListPanel", PrefabRoot + "/Game/RoleList.prefab" },
            { "GameCanvas/OverlayLayer/CoinTossPanel", PrefabRoot + "/Game/CoinToss.prefab" },
            { "GameCanvas/OverlayLayer/PopUpShowdown", PrefabRoot + "/Game/Showdown.prefab" },
            { "GameCanvas/OverlayLayer/BattleGroundRewardPanel", PrefabRoot + "/Game/BattleGroundReward.prefab" },
            { "GameCanvas/OverlayLayer/SurrenderConfirmation", PrefabRoot + "/Game/SurrenderConfirmation.prefab" },
            { "GameCanvas/OverlayLayer/MatchResultPanel", PrefabRoot + "/Game/MatchResult.prefab" },
            { "TooltipCanvas/SpecialCardTooltip", PrefabRoot + "/Game/SpecialCardTooltip.prefab" }
        };

    [MenuItem("Tools/Efuda Ikki/Migrate UI To Screen Prefabs")]
    public static void Run()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Stop Play Mode before migrating UI.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabRoot);
        EnsureFolder(PrefabRoot + "/Menu");
        EnsureFolder(PrefabRoot + "/Game");

        GameObject menuCanvas = FindSceneObject(scene, "MenuCanvas");
        GameObject gameCanvas = FindSceneObject(scene, "GameCanvas");
        GameObject gameManagerObject = FindSceneObject(scene, "GameManager");
        menuCanvas.SetActive(true);
        gameCanvas.SetActive(true);
        UnpackScreenInstances(scene);

        CreateGameplayCardSlots(gameCanvas);
        CreateDiscardPreviewSlots(scene);
        CreateShowdownRoleEffects(scene);
        AssignExplicitUiAssets(scene);

        foreach (KeyValuePair<string, string> item in ScreenPrefabs)
        {
            GameObject root = FindByPath(scene, item.Key);
            if (root == null) throw new InvalidOperationException("Missing UI root: " + item.Key);
            SaveAndConnect(root, item.Value);
        }

        menuCanvas = FindSceneObject(scene, "MenuCanvas");
        gameCanvas = FindSceneObject(scene, "GameCanvas");
        gameManagerObject = FindSceneObject(scene, "GameManager");
        ConfigureReferenceAggregates(scene, menuCanvas, gameCanvas);

        CanvasGroup menuGroup = GetOrAdd<CanvasGroup>(menuCanvas);
        CanvasGroup gameGroup = GetOrAdd<CanvasGroup>(gameCanvas);
        UiRootCoordinator coordinator = GetOrAdd<UiRootCoordinator>(gameManagerObject);
        Assign(coordinator, "menuRoot", menuGroup);
        Assign(coordinator, "gameplayRoot", gameGroup);
        Assign(menuCanvas.GetComponent<TitleUIManager>(), "rootCoordinator", coordinator);
        Assign(gameManagerObject.GetComponent<GameManager>(), "uiRootCoordinator", coordinator);
        SetInitialSceneVisibility(scene, menuGroup, gameGroup);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("UI prefab migration completed: " + ScreenPrefabs.Count + " screen prefabs.");
    }

    private static void CreateGameplayCardSlots(GameObject gameCanvas)
    {
        var capacities = new Dictionary<string, int>
        {
            { "Deck", 52 }, { "Common", 2 }, { "Trash", 52 },
            { "Player1", 5 }, { "Player2", 5 },
            { "SpecialP1", 5 }, { "SpecialP2", 4 }
        };

        foreach (CardArea area in gameCanvas.GetComponentsInChildren<CardArea>(true))
        {
            if (!capacities.TryGetValue(area.name, out int count)) continue;
            UiCardSlot[] slots = EnsureCardSlots(area, count);
            AssignArray(area, "layoutSlots", slots.Cast<UnityEngine.Object>().ToArray());
        }
    }

    private static UiCardSlot[] EnsureCardSlots(CardArea area, int count)
    {
        Transform parent = area.areaRect != null ? area.areaRect : area.transform;
        Transform existing = parent.Find("AuthoringSlots");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("AuthoringSlots", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Stretch((RectTransform)root.transform);

        const float cardWidth = 192f;
        const float cardHeight = 256f;
        float areaWidth = Mathf.Max(cardWidth, area.areaRect != null ? area.areaRect.rect.width : cardWidth);
        var centers = ComputeCenters(areaWidth, cardWidth, count);
        bool special = area is SpecialCardArea;
        var result = new UiCardSlot[count];
        for (int index = 0; index < count; index++)
        {
            GameObject slotObject = new GameObject($"Slot_{index + 1:00}", typeof(RectTransform), typeof(UiCardSlot));
            slotObject.transform.SetParent(root.transform, false);
            RectTransform rect = (RectTransform)slotObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(cardWidth, cardHeight);
            if (special)
            {
                int depth = count - 1 - index;
                rect.anchoredPosition = new Vector2(-depth, depth);
            }
            else
            {
                rect.anchoredPosition = new Vector2(centers[index], 0f);
            }

            UiCardSlot slot = slotObject.GetComponent<UiCardSlot>();
            AssignInt(slot, "index", index);
            result[index] = slot;
        }
        return result;
    }

    private static float[] ComputeCenters(float areaWidth, float cardWidth, int count)
    {
        var result = new float[count];
        if (count == 1) return result;
        float total = cardWidth * count;
        if (total <= areaWidth)
        {
            float spacing = (areaWidth - total) / (count + 1);
            float cursor = -areaWidth * 0.5f + spacing;
            for (int i = 0; i < count; i++)
            {
                result[i] = cursor + cardWidth * 0.5f;
                cursor += cardWidth + spacing;
            }
        }
        else
        {
            float first = -areaWidth * 0.5f + cardWidth * 0.5f;
            float last = areaWidth * 0.5f - cardWidth * 0.5f;
            for (int i = 0; i < count; i++) result[i] = Mathf.Lerp(first, last, i / (float)(count - 1));
        }
        return result;
    }

    private static void CreateDiscardPreviewSlots(Scene scene)
    {
        GameObject popup = FindByPath(scene, "GameCanvas/OverlayLayer/PopUpTrush");
        DiscardPreviewPanel panel = GetOrAdd<DiscardPreviewPanel>(popup);
        Transform area = popup.transform.Find("popupCardArea");
        Transform old = area.Find("PreviewSlots");
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

        GameObject root = new GameObject("PreviewSlots", typeof(RectTransform));
        root.transform.SetParent(area, false);
        Stretch((RectTransform)root.transform);

        Cards deck = FindByPath(scene, "GameCanvas/GameplayScreen/CardAreas/Deck").GetComponent<Cards>();
        List<Card> previewSamples = deck != null && deck.cardList != null
            ? deck.cardList
                .Where(card => card != null && card.CardData != null)
                .OrderBy(card => GetDiscardSuitOrder(card.CardData.suit))
                .ThenBy(card => (int)card.CardData.number)
                .ToList()
            : new List<Card>();
        var slots = new Image[52];
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 13; column++)
            {
                int index = row * 13 + column;
                GameObject slotObject = new GameObject($"Row_{row + 1}/Card_{column + 1:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                slotObject.name = $"Row{row + 1}_Card{column + 1:00}";
                slotObject.transform.SetParent(root.transform, false);
                RectTransform rect = (RectTransform)slotObject.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.18f + column * 0.0525f, 0.84f - row * 0.245f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(46f, 122f);
                Image image = slotObject.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                if (index < previewSamples.Count)
                    image.sprite = previewSamples[index].CardData.Image;
                slots[index] = image;
            }
        }

        AssignArray(panel, "previewSlots", slots.Cast<UnityEngine.Object>().ToArray());
        Assign(panel, "closeButton", popup.transform.Find("closeButton").GetComponent<Button>());
    }

    private static int GetDiscardSuitOrder(Suit suit)
    {
        switch (suit)
        {
            case Suit.Birds: return 0;
            case Suit.Flowers: return 1;
            case Suit.Wind: return 2;
            case Suit.Moon: return 3;
            default: return 4;
        }
    }

    private static void CreateShowdownRoleEffects(Scene scene)
    {
        ShowdownCutInPopup popup = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<ShowdownCutInPopup>(true)).Single();
        SerializedObject serialized = new SerializedObject(popup);
        var assets = serialized.FindProperty("assetSet").objectReferenceValue as ShowdownCutInAssetSet;
        RectTransform effectRoot = serialized.FindProperty("roleEffectRoot").objectReferenceValue as RectTransform;
        Image playerRole = serialized.FindProperty("playerRoleImage").objectReferenceValue as Image;
        Image cpuRole = serialized.FindProperty("cpuRoleImage").objectReferenceValue as Image;
        if (assets == null || effectRoot == null || playerRole == null || cpuRole == null)
            throw new InvalidOperationException("Showdown role-effect references are incomplete.");

        GameObject playerRoot = ReplaceChild(effectRoot, "PlayerRoleEffects");
        GameObject cpuRoot = ReplaceChild(effectRoot, "CpuRoleEffects");
        var player = new GameObject[11];
        var cpu = new GameObject[11];
        for (int index = 0; index < 11; index++)
        {
            GameObject effectPrefab = assets.roleEffectPrefabs[index];
            player[index] = CreateEffect(effectPrefab, playerRoot.transform, playerRole.rectTransform, effectRoot, "Player", index);
            cpu[index] = CreateEffect(effectPrefab, cpuRoot.transform, cpuRole.rectTransform, effectRoot, "Cpu", index);
        }
        AssignArray(popup, "playerRoleEffects", player.Cast<UnityEngine.Object>().ToArray());
        AssignArray(popup, "cpuRoleEffects", cpu.Cast<UnityEngine.Object>().ToArray());
    }

    private static GameObject CreateEffect(GameObject prefab, Transform parent, RectTransform target, RectTransform root, string side, int index)
    {
        if (prefab == null) throw new InvalidOperationException("Missing role effect prefab at index " + index);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        instance.name = $"{side}_RoleEffect_{index:00}";
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = root.InverseTransformPoint(target.position);
        instance.transform.localScale = prefab.transform.localScale * 0.18f;
        foreach (ParticleSystemRenderer renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
            renderer.sortingOrder = 1;
        instance.SetActive(false);
        return instance;
    }

    private static GameObject ReplaceChild(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        Stretch((RectTransform)result.transform);
        return result;
    }

    private static void AssignExplicitUiAssets(Scene scene)
    {
        ShowdownCutInAssetSet assets = AssetDatabase.LoadAssetAtPath<ShowdownCutInAssetSet>(
            "Assets/Data/UI/ShowdownCutInAssets.asset");
        MatchResultVisualAssets resultAssets = AssetDatabase.LoadAssetAtPath<MatchResultVisualAssets>(
            "Assets/Data/UI/MatchResultVisualAssets.asset");
        Assign(FindComponent<StageSelectPanel>(scene), "sharedAssetSet", assets);
        Assign(FindComponent<HandRevealPanel>(scene), "assets", assets);
        Assign(FindComponent<ShowdownCutInPopup>(scene), "assetSet", assets);

        MatchResultPanel result = FindComponent<MatchResultPanel>(scene);
        Assign(result, "visualAssets", resultAssets);
        Assign(result, "sharedUiAssets", assets);

        SpecialCardTooltip tooltip = FindComponent<SpecialCardTooltip>(scene);
        Assign(tooltip, "visualAssets", assets);

        RoleListPanelController roleList = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<RoleListPanelController>(true)).Single();
        SerializedObject roleListSerialized = new SerializedObject(roleList);
        UnityEngine.Object combinedRole = roleListSerialized.FindProperty("combinedRoleSprite").objectReferenceValue;
        Assign(roleList, "firstPageSprite", combinedRole);
        Assign(roleList, "secondPageSprite", combinedRole);

        CoinTossPanel coin = FindComponent<CoinTossPanel>(scene);
        Assign(coin, "parentSprite", LoadSprite("Assets/Sprite/production/ui/coin/parent_coin.png"));
        Assign(coin, "childSprite", LoadSprite("Assets/Sprite/production/ui/coin/child_coin.png"));
        Assign(coin, "backgroundSprite", LoadSprite("Assets/Sprite/production/ui/coin/coin_background.png"));

        string[] selectedNames =
        {
            "aiko_s", "seal_s", "bonus_05_s", "curse_s", "bonus_10_s", "bonus_15_s",
            "double_score_s", "bet_s", "rain_s", "festival_s", "sunny_s", "swap_s"
        };
        UnityEngine.Object[] selectedSprites = selectedNames
            .Select(name => (UnityEngine.Object)LoadSprite($"Assets/Sprite/production/ui/special_card_selection/{name}.png"))
            .ToArray();
        AssignArray(FindComponent<SpecialCardSelectPanel>(scene), "selectedCardSprites", selectedSprites);

        PlayerController playerController = FindComponent<PlayerController>(scene);
        Button discardButton = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Button>(true))
            .FirstOrDefault(button => button.name == "D_button");
        if (discardButton != null)
        {
            Sprite pressed = discardButton.spriteState.pressedSprite ?? discardButton.spriteState.highlightedSprite;
            if (pressed != null) Assign(playerController, "decisionButtonPressedSprite", pressed);
        }

        DiscardPreviewPanel preview = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<DiscardPreviewPanel>(true)).Single();
        Assign(preview, "visualAssets", assets);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("Missing UI sprite: " + path);
        return sprite;
    }

    private static void ConfigureReferenceAggregates(Scene scene, GameObject menuCanvas, GameObject gameCanvas)
    {
        MenuUiReferences menuRefs = GetOrAdd<MenuUiReferences>(menuCanvas);
        Assign(menuRefs, "title", FindComponent<TitleScreenPanel>(scene));
        Assign(menuRefs, "modeSelect", FindComponent<MainMenuPanel>(scene));
        Assign(menuRefs, "stageSelect", FindComponent<StageSelectPanel>(scene));
        Assign(menuRefs, "specialCardSelect", FindComponent<SpecialCardSelectPanel>(scene));
        Assign(menuRefs, "settings", FindComponent<SettingsPanel>(scene));
        Assign(menuCanvas.GetComponent<TitleUIManager>(), "menuUiReferences", menuRefs);

        GameplayUiReferences gameRefs = gameCanvas.GetComponent<GameplayUiReferences>();
        Assign(gameRefs, "gameplayScreen", FindByPath(scene, "GameCanvas/GameplayScreen"));
        Assign(gameRefs, "handRevealPanel", FindComponent<HandRevealPanel>(scene));
        Assign(gameRefs, "battleGroundHud", FindComponent<BattleGroundHud>(scene));
        Assign(gameRefs, "discardPreviewRoot", FindByPath(scene, "GameCanvas/OverlayLayer/PopUpTrush"));
        Assign(gameRefs, "discardSourceArea", FindComponent<PopupPreviewCardArea>(scene));
        Assign(gameRefs, "discardPreviewPanel", FindComponent<DiscardPreviewPanel>(scene));
        Assign(gameRefs, "roleListPanel", gameCanvas.GetComponent<RoleListPanelController>());
        Assign(gameRefs, "coinTossPanel", FindComponent<CoinTossPanel>(scene));
        Assign(gameRefs, "showdownCutInPopup", FindComponent<ShowdownCutInPopup>(scene));
        Assign(gameRefs, "battleGroundRewardPanel", FindComponent<BattleGroundRewardPanel>(scene));
        Assign(gameRefs, "surrenderConfirmationRoot", FindByPath(scene, "GameCanvas/OverlayLayer/SurrenderConfirmation"));
        Assign(gameRefs, "matchResultPanel", FindComponent<MatchResultPanel>(scene));
        Assign(gameRefs, "specialCardTooltip", FindComponent<SpecialCardTooltip>(scene));
    }

    private static void SetInitialSceneVisibility(Scene scene, CanvasGroup menu, CanvasGroup game)
    {
        menu.alpha = 1f; menu.interactable = true; menu.blocksRaycasts = true;
        game.alpha = 0f; game.interactable = false; game.blocksRaycasts = false;
        foreach (CanvasGroup screen in FindByPath(scene, "MenuCanvas/Screens").GetComponentsInChildren<CanvasGroup>(true))
        {
            bool menuScreen = screen.GetComponent<TitleScreenPanel>() != null ||
                              screen.GetComponent<MainMenuPanel>() != null ||
                              screen.GetComponent<StageSelectPanel>() != null ||
                              screen.GetComponent<SpecialCardSelectPanel>() != null ||
                              screen.GetComponent<SettingsPanel>() != null;
            if (!menuScreen) continue;

            bool settings = screen.GetComponent<SettingsPanel>() != null;
            screen.alpha = 1f;
            screen.interactable = true;
            screen.blocksRaycasts = true;
            screen.gameObject.SetActive(true);
            Transform wrapper = screen.transform.parent;
            if (wrapper != null && wrapper.name.EndsWith("Screen"))
                wrapper.gameObject.SetActive(settings);
        }
        string[] hidden = { "PopUpTrush", "RoleListPanel", "CoinTossPanel", "PopUpShowdown", "BattleGroundRewardPanel", "SurrenderConfirmation", "MatchResultPanel" };
        Transform overlays = FindByPath(scene, "GameCanvas/OverlayLayer").transform;
        foreach (string name in hidden) if (overlays.Find(name) != null) overlays.Find(name).gameObject.SetActive(false);
    }

    private static void SaveAndConnect(GameObject root, string path)
    {
        bool active = root.activeSelf;
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        float alpha = group != null ? group.alpha : 1f;
        bool interactable = group != null && group.interactable;
        bool blocks = group != null && group.blocksRaycasts;

        root.SetActive(true);
        if (group != null) { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
        if (PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction) == null)
            throw new InvalidOperationException("Could not save prefab: " + path);

        root.SetActive(active);
        if (group != null) { group.alpha = alpha; group.interactable = interactable; group.blocksRaycasts = blocks; }
    }

    private static void UnpackScreenInstances(Scene scene)
    {
        foreach (string hierarchyPath in ScreenPrefabs.Keys)
        {
            GameObject root = FindByPath(scene, hierarchyPath);
            if (root != null && PrefabUtility.IsAnyPrefabInstanceRoot(root))
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
    }

    private static T FindComponent<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();

    private static GameObject FindSceneObject(Scene scene, string name) =>
        scene.GetRootGameObjects().Single(root => root.name == name);

    private static GameObject FindByPath(Scene scene, string path)
    {
        string[] names = path.Split('/');
        GameObject current = scene.GetRootGameObjects().FirstOrDefault(root => root.name == names[0]);
        for (int i = 1; current != null && i < names.Length; i++)
        {
            Transform child = current.transform.Find(names[i]);
            current = child != null ? child.gameObject : null;
        }
        return current;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T result = target.GetComponent<T>();
        if (result == null) result = target.AddComponent<T>();
        return result;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null) throw new MissingFieldException(target.GetType().Name, field);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void AssignInt(UnityEngine.Object target, string field, int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null) throw new MissingFieldException(target.GetType().Name, field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
