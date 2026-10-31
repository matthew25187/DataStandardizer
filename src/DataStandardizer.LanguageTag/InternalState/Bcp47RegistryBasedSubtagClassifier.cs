using System;
using System.Collections.Generic;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using DataStandardizer.LanguageTag.SubtagRegistry;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;
using Type = DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants.Type;

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Classifies subtags by the records of a Subtag Registry: a subtag is valid in a position if the registry has a record of that type for it.
    /// </summary>
    /// <remarks>
    /// The sets are built once, from a single pass over the records, and never change, so one instance may be used from several threads.
    /// A record whose <c>Subtag</c> denotes a range, such as <c>qaa..qtz</c>, adds every subtag in the range (ref. RFC 5646 §3.1.1, §3.1.4).
    /// </remarks>
    internal sealed class Bcp47RegistryBasedSubtagClassifier : Bcp47SubtagClassifier
    {
        private readonly Bcp47SubtagSet _grandfatheredLanguageTags;
        private readonly Bcp47SubtagSet _primaryLanguageSubtags;
        private readonly Bcp47SubtagSet _extendedLanguageSubtags;
        private readonly Bcp47SubtagSet _scriptSubtags;
        private readonly Bcp47SubtagSet _regionSubtags;
        private readonly Bcp47SubtagSet _variantSubtags;

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47RegistryBasedSubtagClassifier(IEnumerable<ISubtagRegistryRecord> records)
#else
        internal Bcp47RegistryBasedSubtagClassifier([NotNull] IEnumerable<ISubtagRegistryRecord> records)
#endif
        {
            var subtags = new SubtagLists();
            subtags.AddRecords(records);

            _grandfatheredLanguageTags = new Bcp47SubtagSet(subtags.GrandfatheredLanguageTags);
            _primaryLanguageSubtags = new Bcp47SubtagSet(subtags.PrimaryLanguageSubtags);
            _extendedLanguageSubtags = new Bcp47SubtagSet(subtags.ExtendedLanguageSubtags);
            _scriptSubtags = new Bcp47SubtagSet(subtags.ScriptSubtags);
            _regionSubtags = new Bcp47SubtagSet(subtags.RegionSubtags);
            _variantSubtags = new Bcp47SubtagSet(subtags.VariantSubtags);
        }

        internal override bool IsGrandfathered(ReadOnlySpan<char> languageTag)
        {
            return _grandfatheredLanguageTags.Contains(languageTag);
        }

        internal override bool IsPrimaryLanguage(ReadOnlySpan<char> subtag, out bool allowsExtendedLanguage)
        {
            // The registry's Prefix fields are not checked, so any registered Primary Language subtag may be followed by Extended Language subtags.
            allowsExtendedLanguage = _primaryLanguageSubtags.Contains(subtag);
            return allowsExtendedLanguage;
        }

        internal override bool IsExtendedLanguage(ReadOnlySpan<char> subtag)
        {
            return _extendedLanguageSubtags.Contains(subtag);
        }

        internal override bool IsScript(ReadOnlySpan<char> subtag)
        {
            return _scriptSubtags.Contains(subtag);
        }

        internal override bool IsRegion(ReadOnlySpan<char> subtag)
        {
            return _regionSubtags.Contains(subtag);
        }

        internal override bool IsVariant(ReadOnlySpan<char> subtag)
        {
            return _variantSubtags.Contains(subtag);
        }

        /// <summary>
        /// The subtags and tags of each type, collected from the records before the sets are built from them.
        /// </summary>
        /// <remarks>
        /// The loop over the records runs once per registry, so on .NET Core it is compiled fully optimized on first use rather than recompiled while it runs, and it is kept apart
        /// from building the sets so that compilation doesn't include them. On .NET 10, recompiling a loop that also built the sets and expanded every subtag as a possible range took
        /// about two thirds of the first use of a registry (#339). Fully optimized code has no profile to turn the interface calls on a record into direct calls, so each record's
        /// fields are read in a single pass.
        /// </remarks>
        private sealed class SubtagLists
        {
            internal readonly List<string> GrandfatheredLanguageTags = new List<string>();
            internal readonly List<string> PrimaryLanguageSubtags = new List<string>();
            internal readonly List<string> ExtendedLanguageSubtags = new List<string>();
            internal readonly List<string> ScriptSubtags = new List<string>();
            internal readonly List<string> RegionSubtags = new List<string>();
            internal readonly List<string> VariantSubtags = new List<string>();

#if NETCOREAPP3_0_OR_GREATER
            [MethodImpl(MethodImplOptions.AggressiveOptimization)]
            internal void AddRecords(IEnumerable<ISubtagRegistryRecord> records)
#else
            internal void AddRecords([NotNull] IEnumerable<ISubtagRegistryRecord> records)
#endif
            {
#if NETCOREAPP3_0_OR_GREATER
                var types = new List<string?>();
#else
                var types = new List<string>();
#endif

                foreach (var record in records)
                {
                    // One pass over the fields, as each access is an interface call. The first Subtag and Tag fields are the ones used.
#if NETCOREAPP3_0_OR_GREATER
                    string? subtag = null;
                    string? tag = null;
#else
                    string subtag = null;
                    string tag = null;
#endif
                    var hasSubtag = false;
                    var hasTag = false;
                    types.Clear();

                    var fieldCount = record.Count;
                    for (var index = 0; index < fieldCount; index++)
                    {
                        var field = record[index];
                        if (field.Item1 == FieldName.Type)
                        {
                            types.Add(field.Item2 as string);
                        }
                        else if (!hasSubtag && field.Item1 == FieldName.Subtag)
                        {
                            subtag = field.Item2 as string;
                            hasSubtag = true;
                        }
                        else if (!hasTag && field.Item1 == FieldName.Tag)
                        {
                            tag = field.Item2 as string;
                            hasTag = true;
                        }
                    }

                    for (var index = 0; index < types.Count; index++)
                    {
                        switch (types[index])
                        {
                            case Type.Grandfathered:
                                Add(GrandfatheredLanguageTags, tag);
                                break;
                            case Type.PrimaryLanguage:
                                AddExpanded(PrimaryLanguageSubtags, subtag);
                                break;
                            case Type.ExtendedLanguage:
                                AddExpanded(ExtendedLanguageSubtags, subtag);
                                break;
                            case Type.Script:
                                AddExpanded(ScriptSubtags, subtag);
                                break;
                            case Type.Region:
                                AddExpanded(RegionSubtags, subtag);
                                break;
                            case Type.Variant:
                                AddExpanded(VariantSubtags, subtag);
                                break;
                        }
                    }
                }
            }

            /// <summary>
            /// Add a value to a set, unless it is empty: no subtag or tag is empty, so an empty value would only let an empty subtag through (#253).
            /// </summary>
#if NETCOREAPP3_0_OR_GREATER
            private static void Add(List<string> values, string? value)
#else
            private static void Add([NotNull] List<string> values, [CanBeNull] string value)
#endif
            {
                if (!string.IsNullOrEmpty(value)) values.Add(value);
            }

#if NETCOREAPP3_0_OR_GREATER
            private static void AddExpanded(List<string> values, string? subtag)
#else
            private static void AddExpanded([NotNull] List<string> values, [CanBeNull] string subtag)
#endif
            {
                if (string.IsNullOrEmpty(subtag)) return;

                // Only a handful of records denote a range.
                if (subtag.IndexOf(SubtagRegistrySubtagRange.Separator, StringComparison.Ordinal) < 0)
                {
                    values.Add(subtag);
                    return;
                }

                foreach (var expandedSubtag in SubtagRegistrySubtagRange.Expand(subtag))
                {
                    Add(values, expandedSubtag);
                }
            }
        }
    }
}
