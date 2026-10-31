using System;
using System.Collections.Generic;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using DataStandardizer.LanguageTag.SubtagRegistry;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// The subtags and grandfathered tags of a Subtag Registry, each with its record, collected before the sets or maps that index them are built.
    /// </summary>
    /// <remarks>
    /// A record whose <c>Subtag</c> denotes a range, such as <c>qaa..qtz</c>, is added under every subtag in the range (ref. RFC 5646 §3.1.1, §3.1.4).
    /// The loops over the records run once per registry, so on .NET Core they are compiled fully optimized on first use rather than recompiled while they run, and they are kept
    /// apart from building the sets so that compilation doesn't include them. On .NET 10, recompiling a loop that also built the sets and expanded every subtag as a possible
    /// range took about two thirds of the first use of a registry (#339). The registry has already sorted the records by type and parsed their fields, so each loop reads one
    /// property of a sealed record type.
    /// </remarks>
    internal sealed class Bcp47RegistrySubtagLists
    {
        internal readonly Bcp47RegistrySubtagList<SubtagRegistryTagRecord> GrandfatheredLanguageTags = new Bcp47RegistrySubtagList<SubtagRegistryTagRecord>();
        internal readonly Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord> PrimaryLanguageSubtags = new Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord>();
        internal readonly Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord> ExtendedLanguageSubtags = new Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord>();
        internal readonly Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord> ScriptSubtags = new Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord>();
        internal readonly Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord> RegionSubtags = new Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord>();
        internal readonly Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord> VariantSubtags = new Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord>();

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47RegistrySubtagLists(SubtagRegistry.SubtagRegistry subtagRegistry)
#else
        internal Bcp47RegistrySubtagLists([NotNull] SubtagRegistry.SubtagRegistry subtagRegistry)
#endif
        {
            AddTagRecords(GrandfatheredLanguageTags, subtagRegistry.GrandfatheredTags);
            AddSubtagRecords(PrimaryLanguageSubtags, subtagRegistry.Languages);
            AddSubtagRecords(ExtendedLanguageSubtags, subtagRegistry.ExtendedLanguages);
            AddSubtagRecords(ScriptSubtags, subtagRegistry.Scripts);
            AddSubtagRecords(RegionSubtags, subtagRegistry.Regions);
            AddSubtagRecords(VariantSubtags, subtagRegistry.Variants);
        }

        /// <summary>
        /// Add each record under its tag.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void AddTagRecords(Bcp47RegistrySubtagList<SubtagRegistryTagRecord> tags, IReadOnlyList<SubtagRegistryTagRecord> records)
#else
        private static void AddTagRecords([NotNull] Bcp47RegistrySubtagList<SubtagRegistryTagRecord> tags, [NotNull] IReadOnlyList<SubtagRegistryTagRecord> records)
#endif
        {
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                tags.Add(record.Tag, record);
            }
        }

        /// <summary>
        /// Add each record under its subtag, or under every subtag of the range it denotes.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void AddSubtagRecords(Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord> subtags, IReadOnlyList<SubtagRegistrySubtagRecord> records)
#else
        private static void AddSubtagRecords([NotNull] Bcp47RegistrySubtagList<SubtagRegistrySubtagRecord> subtags, [NotNull] IReadOnlyList<SubtagRegistrySubtagRecord> records)
#endif
        {
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                var subtag = record.Subtag;

                // Only a handful of records denote a range.
                if (subtag.IndexOf(SubtagRegistrySubtagRange.Separator, StringComparison.Ordinal) < 0)
                {
                    subtags.Add(subtag, record);
                    continue;
                }

                foreach (var expandedSubtag in SubtagRegistrySubtagRange.Expand(subtag))
                {
                    subtags.Add(expandedSubtag, record);
                }
            }
        }
    }

    /// <summary>
    /// Subtags or tags of one type, each with its record, in registry order.
    /// </summary>
    internal sealed class Bcp47RegistrySubtagList<TRecord>
        where TRecord : class
    {
        private readonly List<string> _keys = new List<string>();
        private readonly List<TRecord> _records = new List<TRecord>();

        /// <summary>
        /// Add a record under a key, unless the key is empty: no subtag or tag is empty, so it would only let an empty one through (#253).
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal void Add(string key, TRecord record)
#else
        internal void Add([NotNull] string key, [NotNull] TRecord record)
#endif
        {
            if (key.Length == 0) return;

            _keys.Add(key);
            _records.Add(record);
        }

        /// <summary>
        /// Build the set of the subtags or tags.
        /// </summary>
        internal Bcp47SubtagSet ToSet()
        {
            return new Bcp47SubtagSet(_keys);
        }

        /// <summary>
        /// Build the map of the subtags or tags to their records, in which the first record for a subtag or tag is the one found.
        /// </summary>
        internal Bcp47SubtagMap<TRecord> ToMap()
        {
            return new Bcp47SubtagMap<TRecord>(_keys, _records);
        }
    }
}
