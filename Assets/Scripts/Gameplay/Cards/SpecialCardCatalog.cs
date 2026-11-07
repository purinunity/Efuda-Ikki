using System;
using System.Collections.Generic;
using UnityEngine;

public static class SpecialCardCatalog
{
    [Serializable]
    public sealed class AssetBinding
    {
        [SerializeField] private string id;
        [SerializeField] private CardData cardData;
        [SerializeField] private Sprite tooltipSprite;

        public string Id => id;
        public CardData CardData => cardData;
        public Sprite TooltipSprite => tooltipSprite;
    }

    public sealed class Entry
    {
        public SpecialCardResolver.SpecialCardId CardId { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Priority { get; }
        public int UnlockOrder { get; }
        public int TooltipIndex { get; }
        public bool IsCpuOnly { get; }
        public IReadOnlyList<string> LegacyAssetNames { get; }

        internal Entry(
            SpecialCardResolver.SpecialCardId cardId,
            string id,
            string displayName,
            string description,
            int priority,
            int unlockOrder,
            bool isCpuOnly,
            params string[] legacyAssetNames)
        {
            CardId = cardId;
            Id = id;
            DisplayName = displayName;
            Description = description;
            Priority = priority;
            UnlockOrder = unlockOrder;
            TooltipIndex = (int)cardId;
            IsCpuOnly = isCpuOnly;
            LegacyAssetNames = legacyAssetNames ?? Array.Empty<string>();
        }
    }

    private static readonly Entry[] EntriesInternal =
    {
        new Entry(SpecialCardResolver.SpecialCardId.Seal, "seal", "封札", "この札より後に発動する特殊札をすべて無効にする。", 100, 4, false, "sp 2", "sp_seal"),
        new Entry(SpecialCardResolver.SpecialCardId.Rain, "rain", "雨札", "相手の役を1段階下げる。", 200, 6, false, "sp 9", "sp_rain"),
        new Entry(SpecialCardResolver.SpecialCardId.Sunny, "sunny", "晴札", "自分の役を1段階上げる。", 300, 5, false, "sp 11", "sp_sunny"),
        new Entry(SpecialCardResolver.SpecialCardId.Swap, "swap", "換札", "自分と相手の役・得点を入れ替える。", 400, 9, false, "sp 12", "sp_swap"),
        new Entry(SpecialCardResolver.SpecialCardId.Bonus5, "bonus_05", "副札5", "自分の得点を5点上げる。", 500, 1, false, "sp 3", "sp_bonus5"),
        new Entry(SpecialCardResolver.SpecialCardId.Bonus10, "bonus_10", "副札10", "自分の得点を10点上げる。", 600, 2, false, "sp 5", "sp_bonus10"),
        new Entry(SpecialCardResolver.SpecialCardId.Bonus15, "bonus_15", "副札15", "自分の得点を15点上げる。", 700, 7, false, "sp 6", "sp_bonus15"),
        new Entry(SpecialCardResolver.SpecialCardId.Oni, "oni", "鬼札", "自分の得点を30点上げる。", 750, 0, true, "sp_oni"),
        new Entry(SpecialCardResolver.SpecialCardId.Festival, "festival", "祭札", "自分の得点がランダムで20点上がるか、20点下がる。", 800, 8, false, "sp 10", "sp_festival"),
        new Entry(SpecialCardResolver.SpecialCardId.Curse, "curse", "呪い札", "相手の得点を10点下げる。", 900, 10, false, "sp 4", "sp_curse"),
        new Entry(SpecialCardResolver.SpecialCardId.DoubleScore, "double_score", "倍札", "自分の得点を2倍にする。", 1000, 12, false, "sp 7", "sp_double_score"),
        new Entry(SpecialCardResolver.SpecialCardId.Bet, "bet", "賭札", "自分の得点がランダムで0倍または2倍になる。", 1100, 11, false, "sp 8", "sp_bet"),
        new Entry(SpecialCardResolver.SpecialCardId.Aiko, "aiko", "相子札", "この勝負を引き分けにする。ダメージは発生しない。", 1200, 3, false, "sp 1", "sp_aiko")
    };

    private static readonly Dictionary<SpecialCardResolver.SpecialCardId, Entry> ByCardId = BuildByCardId();
    private static readonly Dictionary<string, Entry> ByStableId = BuildByStableId();
    private static readonly Dictionary<string, Entry> ByAssetName = BuildByAssetName();
    private static readonly Dictionary<CardData, Entry> ByCardData =
        new Dictionary<CardData, Entry>();

    public static IReadOnlyList<Entry> Entries => EntriesInternal;
    public static int UnlockableCardCount => 12;

    public static bool TryGet(SpecialCardResolver.SpecialCardId cardId, out Entry entry)
    {
        return ByCardId.TryGetValue(cardId, out entry);
    }

    public static bool TryGet(string stableId, out Entry entry)
    {
        entry = null;
        return !string.IsNullOrWhiteSpace(stableId) && ByStableId.TryGetValue(stableId, out entry);
    }

    public static bool TryGet(CardData cardData, out Entry entry)
    {
        entry = null;
        if (cardData == null)
        {
            return false;
        }

        if (ByCardData.TryGetValue(cardData, out entry))
        {
            return true;
        }

        // Compatibility fallback for existing assets and older content that has
        // not yet been added to the serialized CardData catalog.
        return !string.IsNullOrWhiteSpace(cardData.name) &&
               ByAssetName.TryGetValue(cardData.name, out entry);
    }

    public static void RegisterBindings(IReadOnlyList<AssetBinding> bindings)
    {
        if (bindings == null)
        {
            return;
        }

        foreach (AssetBinding binding in bindings)
        {
            if (binding?.CardData == null || !TryGet(binding.Id, out Entry entry))
            {
                continue;
            }

            ByCardData[binding.CardData] = entry;
        }
    }

    public static bool TryGet(
        CardData cardData,
        IReadOnlyList<AssetBinding> bindings,
        out Entry entry)
    {
        if (cardData != null && bindings != null)
        {
            foreach (AssetBinding binding in bindings)
            {
                if (binding != null && binding.CardData == cardData && TryGet(binding.Id, out entry))
                {
                    return true;
                }
            }
        }

        return TryGet(cardData, out entry);
    }

    public static Sprite GetTooltipSprite(
        CardData cardData,
        IReadOnlyList<AssetBinding> bindings)
    {
        if (cardData == null || bindings == null)
        {
            return null;
        }

        foreach (AssetBinding binding in bindings)
        {
            if (binding != null && binding.CardData == cardData && binding.TooltipSprite != null)
            {
                return binding.TooltipSprite;
            }
        }

        return null;
    }

    private static Dictionary<SpecialCardResolver.SpecialCardId, Entry> BuildByCardId()
    {
        var result = new Dictionary<SpecialCardResolver.SpecialCardId, Entry>();
        foreach (Entry entry in EntriesInternal) result.Add(entry.CardId, entry);
        return result;
    }

    private static Dictionary<string, Entry> BuildByStableId()
    {
        var result = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (Entry entry in EntriesInternal) result.Add(entry.Id, entry);
        return result;
    }

    private static Dictionary<string, Entry> BuildByAssetName()
    {
        var result = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (Entry entry in EntriesInternal)
        {
            foreach (string assetName in entry.LegacyAssetNames)
            {
                if (!string.IsNullOrWhiteSpace(assetName)) result[assetName] = entry;
            }
        }

        return result;
    }
}
