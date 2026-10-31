using System;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using DataStandardizer.LanguageTag.SubtagRegistry;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Maps the subtags of a Subtag Registry to their records, for <see cref="SubtagRegistry.SubtagRegistry.TryGetSubtag(SubtagRegistryRecordType, string, out SubtagRegistrySubtagRecord)"/>.
    /// </summary>
    /// <remarks>
    /// The maps are built once, from the registry's records of each type, and never change, so one instance may be used from several threads.
    /// A record whose <c>Subtag</c> denotes a range, such as <c>qaa..qtz</c>, is found by every subtag in the range (ref. RFC 5646 §3.1.1, §3.1.4).
    /// </remarks>
    internal sealed class Bcp47RegistrySubtagIndex
    {
        private readonly Bcp47SubtagMap<SubtagRegistrySubtagRecord> _primaryLanguageSubtags;
        private readonly Bcp47SubtagMap<SubtagRegistrySubtagRecord> _extendedLanguageSubtags;
        private readonly Bcp47SubtagMap<SubtagRegistrySubtagRecord> _scriptSubtags;
        private readonly Bcp47SubtagMap<SubtagRegistrySubtagRecord> _regionSubtags;
        private readonly Bcp47SubtagMap<SubtagRegistrySubtagRecord> _variantSubtags;

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47RegistrySubtagIndex(SubtagRegistry.SubtagRegistry subtagRegistry)
#else
        internal Bcp47RegistrySubtagIndex([NotNull] SubtagRegistry.SubtagRegistry subtagRegistry)
#endif
        {
            var subtags = new Bcp47RegistrySubtagLists(subtagRegistry);

            _primaryLanguageSubtags = subtags.PrimaryLanguageSubtags.ToMap();
            _extendedLanguageSubtags = subtags.ExtendedLanguageSubtags.ToMap();
            _scriptSubtags = subtags.ScriptSubtags.ToMap();
            _regionSubtags = subtags.RegionSubtags.ToMap();
            _variantSubtags = subtags.VariantSubtags.ToMap();
        }

        /// <summary>
        /// Gets the record of a subtag of a type, which may be a record of a range of subtags.
        /// </summary>
        /// <returns><see langword="false"/> when the registry has no such subtag, or the type is not a subtag type.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal bool TryGetSubtag(SubtagRegistryRecordType type, ReadOnlySpan<char> subtag, [NotNullWhen(true)] out SubtagRegistrySubtagRecord? record)
#else
        internal bool TryGetSubtag(SubtagRegistryRecordType type, ReadOnlySpan<char> subtag, [CanBeNull] out SubtagRegistrySubtagRecord record)
#endif
        {
            var subtags = GetSubtagMap(type);
            if (subtags is null)
            {
                record = null;
                return false;
            }

            return subtags.TryGetValue(subtag, out record);
        }

#if NETCOREAPP3_0_OR_GREATER
        private Bcp47SubtagMap<SubtagRegistrySubtagRecord>? GetSubtagMap(SubtagRegistryRecordType type)
#else
        [CanBeNull]
        private Bcp47SubtagMap<SubtagRegistrySubtagRecord> GetSubtagMap(SubtagRegistryRecordType type)
#endif
        {
            switch (type)
            {
                case SubtagRegistryRecordType.Language:
                    return _primaryLanguageSubtags;
                case SubtagRegistryRecordType.ExtendedLanguage:
                    return _extendedLanguageSubtags;
                case SubtagRegistryRecordType.Script:
                    return _scriptSubtags;
                case SubtagRegistryRecordType.Region:
                    return _regionSubtags;
                case SubtagRegistryRecordType.Variant:
                    return _variantSubtags;
                default:
                    return null;
            }
        }
    }
}
