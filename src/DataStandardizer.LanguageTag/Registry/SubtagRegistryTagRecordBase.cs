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
    /// The fields are parsed once, when the record is created. A registry where a field other than Description, Comments and Prefix appears more than
    /// once in a record fails to load (ref. RFC 5646 §3.1.2).
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
        internal SubtagRegistryTagRecordBase(KeyValuePair<string, string>[] fields, DateTime added, DateTime? deprecated)
#else
        internal SubtagRegistryTagRecordBase([DisallowNull] KeyValuePair<string, string>[] fields, DateTime added, DateTime? deprecated)
#endif
            : base(fields)
        {
            // the registry parses the Added and Deprecated fields, and fails to load when Added is missing or either is not a full-date
            Added = added;
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
        /// Gets the Added field value, which every tag and subtag record has (ref. RFC 5646 §3.1.2).
        /// </summary>
        public DateTime Added { get; }

        /// <summary>
        /// Gets the Deprecated field value, or <see langword="null"/> when the record is not deprecated.
        /// </summary>
        public DateTime? Deprecated { get; }

        /// <summary>
        /// Gets the Preferred-Value field value: a primary language subtag on a language record, a subtag of the record's type on a script, region or
        /// variant record, the record's Subtag on an extlang record, which always has one (ref. RFC 5646 §2.2.2), and an extended language range
        /// (ref. RFC 4647 §2.2) on a grandfathered or redundant record (ref. RFC 5646 §3.1.2).
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? PreferredValue { get; }
#else
        [MaybeNull] public string PreferredValue { get; }
#endif

        /// <summary>
        /// Gets the Prefix field values, in the order they appear: exactly one on an extlang record, any number on a variant record, and none on any
        /// other record (ref. RFC 5646 §3.1.2, §3.1.8).
        /// </summary>
        public IReadOnlyList<string> Prefix => GetValuesView(_prefix, ref _prefixView);

        /// <summary>
        /// Gets the Suppress-Script field value, a script subtag (ref. RFC 5646 §3.1.2, §3.1.9).
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? SuppressScript { get; }
#else
        [MaybeNull] public string SuppressScript { get; }
#endif

        /// <summary>
        /// Gets the Macrolanguage field value, a primary language subtag (ref. RFC 5646 §3.1.2, §3.1.10).
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Macrolanguage { get; }
#else
        [MaybeNull] public string Macrolanguage { get; }
#endif

        /// <summary>
        /// Gets the Scope field value: <c>macrolanguage</c>, <c>collection</c>, <c>special</c> or <c>private-use</c> (ref. RFC 5646 §3.1.2), on a
        /// language or extlang record alone.
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
