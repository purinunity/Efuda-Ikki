using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(Cards))]
public sealed class CardsSceneAuthoringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "カードはPlay開始時に生成されません。CardsDataを変更した場合は、" +
            "Edit ModeでSceneカードを再構築してください。",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Rebuild Scene Cards"))
            {
                Rebuild((Cards)target);
            }
        }
    }

    [MenuItem("Tools/Efuda Ikki/Rebuild Scene Card Pools")]
    private static void RebuildAllInOpenScenes()
    {
        Cards[] cardPools = Object.FindObjectsOfType<Cards>(true);
        foreach (Cards cards in cardPools)
        {
            if (cards != null && cards.gameObject.scene.IsValid())
            {
                Rebuild(cards);
            }
        }

        Debug.Log($"Rebuilt {cardPools.Length} scene-authored card pools.");
    }

    public static void Rebuild(Cards cards)
    {
        if (cards == null || cards.CardsData == null || cards.CardsData.cards == null)
        {
            Debug.LogError("CardsSceneAuthoring: CardsData is not assigned.", cards);
            return;
        }

        SerializedObject serializedCards = new SerializedObject(cards);
        SerializedProperty prefabProperty = serializedCards.FindProperty("cardPrefab");
        SerializedProperty listProperty = serializedCards.FindProperty("sceneCards");
        Card prefab = prefabProperty.objectReferenceValue as Card;
        if (prefab == null)
        {
            Debug.LogError("CardsSceneAuthoring: cardPrefab is not assigned.", cards);
            return;
        }

        Undo.SetCurrentGroupName($"Rebuild {cards.name} Scene Cards");
        int undoGroup = Undo.GetCurrentGroup();

        var oldCards = new List<Card>();
        for (int index = 0; index < listProperty.arraySize; index++)
        {
            Card oldCard = listProperty.GetArrayElementAtIndex(index).objectReferenceValue as Card;
            if (oldCard != null && !oldCards.Contains(oldCard))
            {
                oldCards.Add(oldCard);
            }
        }

        foreach (Card oldCard in oldCards)
        {
            if (oldCard != null)
            {
                Undo.DestroyObjectImmediate(oldCard.gameObject);
            }
        }

        Transform pool = FindOrCreatePool(cards.transform);
        var createdCards = new List<Card>();
        listProperty.arraySize = cards.CardsData.cards.Length;
        for (int index = 0; index < cards.CardsData.cards.Length; index++)
        {
            CardData data = cards.CardsData.cards[index];
            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefab.gameObject,
                cards.gameObject.scene) as GameObject;
            Undo.RegisterCreatedObjectUndo(instance, "Create Scene card");
            instance.name = data != null
                ? $"Card_{index + 1:00}_{data.name}"
                : $"Card_{index + 1:00}";
            Undo.SetTransformParent(instance.transform, pool, "Parent Scene card");
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            Card card = instance.GetComponent<Card>();
            card.SetCardData(data);
            EditorUtility.SetDirty(card);
            listProperty.GetArrayElementAtIndex(index).objectReferenceValue = card;
            createdCards.Add(card);
        }

        serializedCards.ApplyModifiedProperties();
        PlaceSpecialSelectionCards(cards, createdCards);
        pool.gameObject.SetActive(false);
        EditorUtility.SetDirty(cards);
        EditorSceneManager.MarkSceneDirty(cards.gameObject.scene);
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static void PlaceSpecialSelectionCards(Cards cards, List<Card> createdCards)
    {
        SpecialCardSelectPanel panel = cards.GetComponentInParent<SpecialCardSelectPanel>(true);
        if (panel == null)
        {
            return;
        }

        SerializedObject serializedPanel = new SerializedObject(panel);
        if (serializedPanel.FindProperty("specialCardsDeck").objectReferenceValue != cards)
        {
            return;
        }

        SerializedProperty slots = serializedPanel.FindProperty("selectableCardSlots");
        var displayCards = new List<Card>();
        foreach (Card card in createdCards)
        {
            if (card == null ||
                SpecialCardResolver.IsNoUseSpecialCard(card.CardData) ||
                SpecialCardResolver.IsCpuOnlySpecialCard(card.CardData))
            {
                continue;
            }

            displayCards.Add(card);
        }

        displayCards.Sort((left, right) =>
        {
            int leftOrder = SpecialCardResolver.TryGetUnlockOrder(left.CardData, out int leftValue)
                ? leftValue
                : int.MaxValue;
            int rightOrder = SpecialCardResolver.TryGetUnlockOrder(right.CardData, out int rightValue)
                ? rightValue
                : int.MaxValue;
            return leftOrder.CompareTo(rightOrder);
        });

        int visibleCount = Mathf.Min(displayCards.Count, slots.arraySize);
        for (int index = 0; index < visibleCount; index++)
        {
            RectTransform slot =
                slots.GetArrayElementAtIndex(index).objectReferenceValue as RectTransform;
            Card card = displayCards[index];
            if (slot == null || card == null)
            {
                continue;
            }

            Undo.SetTransformParent(card.transform, slot, "Place special card in Scene slot");
            card.gameObject.SetActive(true);
            card.ForceSetFaceUp(true);
            FitCardToSlot(card, slot);
            EditorUtility.SetDirty(card);
        }
    }

    private static void FitCardToSlot(Card card, RectTransform slot)
    {
        RectTransform cardRect = card != null ? card.GetComponent<RectTransform>() : null;
        UnityEngine.UI.Image image = card != null ? card.GetComponent<UnityEngine.UI.Image>() : null;
        if (cardRect == null || image == null || image.sprite == null || slot == null)
        {
            return;
        }

        image.SetNativeSize();
        Vector2 nativeSize = cardRect.rect.size;
        Vector2 slotSize = slot.rect.size;
        if (nativeSize.x <= 0f || nativeSize.y <= 0f || slotSize.x <= 0f || slotSize.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(slotSize.x / nativeSize.x, slotSize.y / nativeSize.y);
        card.DisplayScale = scale;
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localScale = Vector3.one * scale;
    }

    private static Transform FindOrCreatePool(Transform owner)
    {
        Transform existing = owner.Find("SceneCardPool");
        if (existing != null)
        {
            var children = new List<GameObject>();
            for (int index = 0; index < existing.childCount; index++)
            {
                children.Add(existing.GetChild(index).gameObject);
            }

            foreach (GameObject child in children)
            {
                Undo.DestroyObjectImmediate(child);
            }

            existing.gameObject.SetActive(true);
            return existing;
        }

        GameObject pool = new GameObject("SceneCardPool", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(pool, "Create Scene card pool");
        Undo.SetTransformParent(pool.transform, owner, "Parent Scene card pool");
        RectTransform rect = (RectTransform)pool.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }
}

/// <summary>
/// Sceneカード参照が壊れたPlayerを作らせないためのBuild検証。
/// 実行時フォールバック生成は行わず、修正箇所を明示してBuildを停止する。
/// </summary>
public sealed class CardsSceneBuildValidator : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (!scene.IsValid())
        {
            return;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Cards cards in root.GetComponentsInChildren<Cards>(true))
            {
                if (cards != null && cards.TryGetAuthoringError(out string error))
                {
                    throw new BuildFailedException(
                        $"{scene.path}: {GetPath(cards.transform)}: {error}");
                }
            }
        }
    }

    private static string GetPath(Transform target)
    {
        var names = new Stack<string>();
        while (target != null)
        {
            names.Push(target.name);
            target = target.parent;
        }

        return string.Join("/", names);
    }
}
