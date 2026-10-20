using System;
using System.Collections.Generic;
using DataStandardizer.LanguageTag.SubtagRegistry;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;
using Type = DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants.Type;
#if NETSTANDARD1_0
using CharSpan = DataStandardizer.LanguageTag.InternalState.Bcp47CharSpan;
#else
using CharSpan = System.ReadOnlySpan<char>;
#endif

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
            var grandfatheredLanguageTags = new List<string>();
            var primaryLanguageSubtags = new List<string>();
            var extendedLanguageSubtags = new List<string>();
            var scriptSubtags = new List<string>();
            var regionSubtags = new List<string>();
            var variantSubtags = new List<string>();

            foreach (var record in records)
            {
                var subtag = GetFirstValue(record, FieldName.Subtag);
                var tag = GetFirstValue(record, FieldName.Tag);

                for (var index = 0; index < record.Count; index++)
                {
                    var field = record[index];
                    if (field.Item1 != FieldName.Type) continue;

                    switch (field.Item2 as string)
                    {
                        case Type.Grandfathered:
                            Add(grandfatheredLanguageTags, tag);
                            break;
                        case Type.PrimaryLanguage:
                            AddExpanded(primaryLanguageSubtags, subtag);
                            break;
                        case Type.ExtendedLanguage:
                            AddExpanded(extendedLanguageSubtags, subtag);
                            break;
                        case Type.Script:
                            AddExpanded(scriptSubtags, subtag);
                            break;
                        case Type.Region:
                            AddExpanded(regionSubtags, subtag);
                            break;
                        case Type.Variant:
                            AddExpanded(variantSubtags, subtag);
                            break;
                    }
                }
            }

            _grandfatheredLanguageTags = new Bcp47SubtagSet(grandfatheredLanguageTags);
            _primaryLanguageSubtags = new Bcp47SubtagSet(primaryLanguageSubtags);
            _extendedLanguageSubtags = new Bcp47SubtagSet(extendedLanguageSubtags);
            _scriptSubtags = new Bcp47SubtagSet(scriptSubtags);
            _regionSubtags = new Bcp47SubtagSet(regionSubtags);
            _variantSubtags = new Bcp47SubtagSet(variantSubtags);
        }

        internal override bool IsGrandfathered(CharSpan languageTag)
        {
            return _grandfatheredLanguageTags.Contains(languageTag);
        }

        internal override bool IsPrimaryLanguage(CharSpan subtag, out bool allowsExtendedLanguage)
        {
            // The registry's Prefix fields are not checked, so any registered Primary Language subtag may be followed by Extended Language subtags.
            allowsExtendedLanguage = _primaryLanguageSubtags.Contains(subtag);
            return allowsExtendedLanguage;
        }

        internal override bool IsExtendedLanguage(CharSpan subtag)
        {
            return _extendedLanguageSubtags.Contains(subtag);
        }

        internal override bool IsScript(CharSpan subtag)
        {
            return _scriptSubtags.Contains(subtag);
        }

        internal override bool IsRegion(CharSpan subtag)
        {
            return _regionSubtags.Contains(subtag);
        }

        internal override bool IsVariant(CharSpan subtag)
        {
            return _variantSubtags.Contains(subtag);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? GetFirstValue(ISubtagRegistryRecord record, string fieldName)
#else
        [CanBeNull]
        private static string GetFirstValue([NotNull] ISubtagRegistryRecord record, [NotNull] string fieldName)
#endif
        {
            for (var index = 0; index < record.Count; index++)
            {
                var field = record[index];
                if (field.Item1 == fieldName) return field.Item2 as string;
            }

            return null;
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

            foreach (var expandedSubtag in SubtagRegistrySubtagRange.Expand(subtag))
            {
                Add(values, expandedSubtag);
            }
        }
    }
}
