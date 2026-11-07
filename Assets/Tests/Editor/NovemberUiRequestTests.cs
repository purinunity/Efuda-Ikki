using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class NovemberUiRequestTests
{
    private static IEnumerable RowCases()
    {
        foreach (Suit suit in new[] { Suit.Birds, Suit.Flowers, Suit.Wind, Suit.Moon })
            foreach (int count in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 13 }) yield return new TestCaseData(count, suit);
    }

    [TestCaseSource(nameof(RowCases))]
    public void DiscardRows_AreSortedCenteredAndStableAcrossReopen(int count, Suit suit)
    {
        GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Game/DiscardPreview.prefab");
        var created = new List<Object>();
        try
        {
            var panel = root.GetComponent<DiscardPreviewPanel>();
            var array = new SerializedObject(panel).FindProperty("previewSlots");
            Image[] slots = Enumerable.Range(0, 52).Select(i => (Image)array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            Vector2[] min = slots.Select(i => i.rectTransform.anchorMin).ToArray();
            Vector2[] max = slots.Select(i => i.rectTransform.anchorMax).ToArray();
            int row = System.Array.IndexOf(new[] { Suit.Birds, Suit.Flowers, Suit.Wind, Suit.Moon }, suit);
            int first = row * 13;
            float center = (Center(slots[first]) + Center(slots[first + 12])) * 0.5f;
            float leftCenter = Center(slots[first]);
            float rightCenter = Center(slots[first + 12]);
            float cardWidth = Enumerable.Range(first, 13).Max(i => max[i].x - min[i].x);
            int[] ranks = count > 1 ? Enumerable.Range(0, count).Select(i => 1 + i * 12 / (count - 1)).ToArray() :
                count == 1 ? new[] { 13 } : new int[0];
            var ordered = ranks.Select(rank => MakeCard(rank, suit, created)).ToList();
            var input = ordered.AsEnumerable().Reverse().ToList();
            if (input.Count > 0) input.Add(input[0]);
            input.Add(null);
            input.Add(MakeCard(14, suit, created));
            input.Add(MakeCard(0, Suit.Joker, created));
            var fullRow = Enumerable.Range(1, 13).Select(rank => MakeCard(rank, suit, created)).ToList();
            for (int repeat = 0; repeat < 3; repeat++)
            {
                panel.Show(fullRow); // Repack a full row directly into a smaller group, without closing first.
                panel.Show(input);
                Assert.That(slots.Count(image => image.gameObject.activeSelf), Is.EqualTo(count));
                float gap = count > 1 ? Center(slots[first + 1]) - Center(slots[first]) : 0f;
                for (int i = 0; i < count; i++)
                {
                    Image image = slots[first + i];
                    Assert.That(image.sprite, Is.SameAs(ordered[i].CardData.Image), "Rank order must ignore input order.");
                    Assert.That(image.gameObject.activeSelf, Is.True);
                    Assert.That(image.rectTransform.anchorMax.x - image.rectTransform.anchorMin.x,
                        Is.EqualTo(max[first + i].x - min[first + i].x).Within(0.000001f));
                    Assert.That(image.rectTransform.anchorMin.y, Is.EqualTo(min[first + i].y));
                    if (i > 0) Assert.That(Center(image) - Center(slots[first + i - 1]), Is.EqualTo(gap).Within(0.000001f));
                }
                if (count > 0)
                {
                    Assert.That(slots.Skip(first).Take(count).Average(Center), Is.EqualTo(center).Within(0.000001f));
                    Assert.That(Center(slots[first]), Is.GreaterThanOrEqualTo(leftCenter - 0.000001f));
                    Assert.That(Center(slots[first + count - 1]), Is.LessThanOrEqualTo(rightCenter + 0.000001f));
                }
                if (count >= 2 && count <= 5)
                {
                    Assert.That(gap - cardWidth, Is.EqualTo(cardWidth * 0.15f).Within(0.000001f),
                        "Small groups must have visible space between card cells.");
                    Assert.That(Center(slots[first]), Is.GreaterThan(leftCenter), "Two cards must not be pushed to both row edges.");
                }
                if (count >= 6)
                {
                    Assert.That(Center(slots[first]), Is.EqualTo(leftCenter).Within(0.000001f));
                    Assert.That(Center(slots[first + count - 1]), Is.EqualTo(rightCenter).Within(0.000001f));
                }
                panel.Close();
                for (int i = 0; i < slots.Length; i++)
                {
                    Assert.That(slots[i].rectTransform.anchorMin, Is.EqualTo(min[i]));
                    Assert.That(slots[i].rectTransform.anchorMax, Is.EqualTo(max[i]));
                }
            }
            panel.Show(new Card[0]);
            Assert.That(slots.All(image => !image.gameObject.activeSelf), Is.True, "A new empty display must clear the previous cards.");
            panel.Close();
        }
        finally
        {
            for (int i = created.Count - 1; i >= 0; i--) Object.DestroyImmediate(created[i]);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void CutInBuilder_PassesActualSelectedCardDataToBothTooltipTargets()
    {
        var created = new List<Object>();
        try
        {
            Card player = MakeCard(1, Suit.Joker, created), cpu = MakeCard(2, Suit.Joker, created);
            var state = new GameState();
            state.InitializePlayerStates();
            state.PlayerStates[0].SetSpecialCardsForMatch(new[] { player });
            state.PlayerStates[1].SetSpecialCardsForMatch(new[] { cpu });
            player.IsSelected = cpu.IsSelected = true;
            var result = new SpecialCardResolver.ShowdownResult(new List<SpecialCardResolver.ResolvedHand>(),
                -1, 0, new List<string>(), new List<SpecialCardResolver.EffectStep>());
            ShowdownCutInPopup.Data data = new ShowdownCutInDataBuilder(state, null).Build(result);
            Assert.That(data.PlayerSpecialCardData, Is.SameAs(player.CardData));
            Assert.That(data.CpuSpecialCardData, Is.SameAs(cpu.CardData));
            Assert.That(data.PlayerSpecialCardSprite, Is.SameAs(player.DisplayFaceSprite));
            state.PlayerStates[0].MarkSpecialCardUsed(player);
            data = new ShowdownCutInDataBuilder(state, null).Build(result);
            Assert.That(data.PlayerSpecialCardData, Is.Null, "A consumed selection must not expose an old tooltip.");
        }
        finally { for (int i = created.Count - 1; i >= 0; i--) Object.DestroyImmediate(created[i]); }
    }

    [Test]
    public void NumericFont_ContainsAllScoreAndNegativeNumberCharacters()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NovemberUiAuthoring.FontPath);
        Assert.That(font, Is.Not.Null);
        Assert.That(font.HasCharacters("0123456789-"), Is.True);
    }

    private static float Center(Image image) => (image.rectTransform.anchorMin.x + image.rectTransform.anchorMax.x) * 0.5f;

    private static Card MakeCard(int rank, Suit suit, List<Object> created)
    {
        var data = ScriptableObject.CreateInstance<CardData>();
        created.Add(data);
        data.number = (Number)rank;
        data.suit = suit;
        data.Image = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        created.Add(data.Image);
        var go = new GameObject("Preview Test Card", typeof(RectTransform), typeof(Image), typeof(Card));
        created.Add(go);
        Card card = go.GetComponent<Card>();
        card.SetCardData(data);
        return card;
    }
}
