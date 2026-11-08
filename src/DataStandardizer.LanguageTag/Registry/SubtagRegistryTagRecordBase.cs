using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif
using static DataStandardizer.LanguageTag.Registry.SubtagRegistryConstants;
using TypeName = DataStandardizer.LanguageTag.Registry.SubtagRegistryConstants.Type;

namespace DataStandardizer.LanguageTag.Registry
{
    /// <summary>
    /// The shared base of the tag and subtag records of the Subtag Registry, with the fields they have in common.
    /// </summary>
    /// <remarks>
    /// The fields are parsed once, when the record is created. Where a field that appears at most once per record (ref. RFC 5646 §3.1.2) appears more than once, the first is used.
    /// </remarks>
    public abstract class SubtagRegistryTagRecordBase : SubtagRegistryRecordBase
    {
        private readonly string[] _description;
        private readonly string[] _prefix;
        private readonly string[] _comments;

        // Read-only views of the arrays, created on first read. A race only creates an equivalent view, so no lock is taken.
#if NETCOREAPP3_0_OR_GREATER
        private ReadOnlyCollection<string>? _descriptionView;
        private ReadOnlyCollection<string>? _prefixView;
        private ReadOnlyCollection<string>? _commentsView;
#else
        [MaybeNull] private ReadOnlyCollection<string> _descriptionView;
        [MaybeNull] private ReadOnlyCollection<string> _prefixView;
        [MaybeNull] private ReadOnlyCollection<string> _commentsView;
#endif

#if NETCOREAPP3_0_OR_GREATER
        internal SubtagRegistryTagRecordBase(KeyValuePair<string, string>[] fields, DateTime? added, DateTime? deprecated)
#else
        internal SubtagRegistryTagRecordBase([DisallowNull] KeyValuePair<string, string>[] fields, DateTime? added, DateTime? deprecated)
#endif
            : base(fields)
        {
            // the registry parses the first Added and Deprecated fields, and fails to load when any of them is not a full-date; a record with no Added
            // field reads as the default date
            Added = added ?? default(DateTime);
            Deprecated = deprecated;

            var descriptionCount = 0;
            var prefixCount = 0;
            var commentsCount = 0;
            var hasType = false;

            foreach (var field in fields)
            {
                switch (field.Key)
                {
                    case FieldName.Type:
                        if (!hasType) Type = ParseType(field.Value);
                        hasType = true;
                        break;
                    case FieldName.Description:
                        descriptionCount++;
                        break;
                    case FieldName.PreferredValue:
                        if (PreferredValue is null) PreferredValue = field.Value;
                        break;
                    case FieldName.Prefix:
                        prefixCount++;
                        break;
                    case FieldName.SuppressScript:
                        if (SuppressScript is null) SuppressScript = field.Value;
                        break;
                    case FieldName.Macrolanguage:
                        if (Macrolanguage is null) Macrolanguage = field.Value;
                        break;
                    case FieldName.Scope:
                        if (Scope is null) Scope = field.Value;
                        break;
                    case FieldName.Comments:
                        commentsCount++;
                        break;
                }
            }

            _description = GetValues(fields, FieldName.Description, descriptionCount);
            _prefix = GetValues(fields, FieldName.Prefix, prefixCount);
            _comments = GetValues(fields, FieldName.Comments, commentsCount);
        }

        /// <summary>
        /// Gets the Type field value, which is one that RFC 5646 §3.1.3 defines for the kind of record.
        /// </summary>
        public SubtagRegistryRecordType Type { get; }

        /// <summary>
        /// Gets the Description field values, in the order they appear.
        /// </summary>
        public IReadOnlyList<string> Description => GetValuesView(_description, ref _descriptionView);

        /// <summary>
        /// Gets the Added field value, or the default <see cref="DateTime"/> when the record has none.
        /// </summary>
        public DateTime Added { get; }

        /// <summary>
        /// Gets the Deprecated field value, or <see langword="null"/> when the record is not deprecated.
        /// </summary>
        public DateTime? Deprecated { get; }

        /// <summary>
        /// Gets the Preferred-Value field value.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? PreferredValue { get; }
#else
        [MaybeNull] public string PreferredValue { get; }
#endif

        /// <summary>
        /// Gets the Prefix field values, in the order they appear.
        /// </summary>
        public IReadOnlyList<string> Prefix => GetValuesView(_prefix, ref _prefixView);

        /// <summary>
        /// Gets the Suppress-Script field value.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? SuppressScript { get; }
#else
        [MaybeNull] public string SuppressScript { get; }
#endif

        /// <summary>
        /// Gets the Macrolanguage field value.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Macrolanguage { get; }
#else
        [MaybeNull] public string Macrolanguage { get; }
#endif

        /// <summary>
        /// Gets the Scope field value.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Scope { get; }
#else
        [MaybeNull] public string Scope { get; }
#endif

        /// <summary>
        /// Gets the Comments field values, in the order they appear.
        /// </summary>
        public IReadOnlyList<string> Comments => GetValuesView(_comments, ref _commentsView);

        private static SubtagRegistryRecordType ParseType(string value)
        {
            switch (value)
            {
                case TypeName.PrimaryLanguage:
                    return SubtagRegistryRecordType.Language;
                case TypeName.ExtendedLanguage:
                    return SubtagRegistryRecordType.ExtendedLanguage;
                case TypeName.Script:
                    return SubtagRegistryRecordType.Script;
                case TypeName.Region:
                    return SubtagRegistryRecordType.Region;
                case TypeName.Variant:
                    return SubtagRegistryRecordType.Variant;
                case TypeName.Grandfathered:
                    return SubtagRegistryRecordType.Grandfathered;
                case TypeName.Redundant:
                    return SubtagRegistryRecordType.Redundant;
                default:
                    return SubtagRegistryRecordType.Unknown;
            }
        }
    }
}
