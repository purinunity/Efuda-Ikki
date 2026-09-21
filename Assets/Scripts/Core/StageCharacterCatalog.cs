using System;
using System.Collections.Generic;

namespace EfudaIkki.Core
{
    public static class StageCharacterCatalog
    {
        public const string Bartender = "bartender";
        public const string Host = "host";
        public const string CottonCandy = "cotton_candy";
        public const string Boy = "boy";
        public const string GoldBob = "gold_bob";
        public const string Intellectual = "intellectual";
        public const string BlackLong = "black_long";
        public const string Bandman = "bandman";
        public const string GameMaster = "game_master";

        public sealed class Entry
        {
            public string Id { get; }
            public int Level { get; }

            internal Entry(string id, int level)
            {
                Id = id;
                Level = level;
            }
        }

        private static readonly Entry[] EntriesInternal =
        {
            new Entry(Bartender, 1),
            new Entry(Host, 2),
            new Entry(CottonCandy, 3),
            new Entry(Boy, 4),
            new Entry(GoldBob, 5),
            new Entry(Intellectual, 6),
            new Entry(BlackLong, 7),
            new Entry(Bandman, 8),
            new Entry(GameMaster, 9)
        };

        private static readonly Dictionary<string, Entry> ById = BuildById();

        public static IReadOnlyList<Entry> Entries => EntriesInternal;

        public static bool TryGet(string id, out Entry entry)
        {
            entry = null;
            return !string.IsNullOrWhiteSpace(id) && ById.TryGetValue(id, out entry);
        }

        public static Entry GetByLevel(int level)
        {
            int index = level - 1;
            return index >= 0 && index < EntriesInternal.Length ? EntriesInternal[index] : null;
        }

        public static int ClampLevel(int level)
        {
            return Math.Max(1, Math.Min(EntriesInternal.Length, level));
        }

        private static Dictionary<string, Entry> BuildById()
        {
            var result = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (Entry entry in EntriesInternal)
            {
                result.Add(entry.Id, entry);
            }

            return result;
        }
    }
}
