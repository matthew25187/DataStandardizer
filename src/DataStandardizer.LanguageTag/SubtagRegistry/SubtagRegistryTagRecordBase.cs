using System;
using System.ComponentModel;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    public abstract class SubtagRegistryTagRecordBase : SubtagRegistryRecordBase
    {
        /// <summary>
        /// Gets the Type field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Type)]
#if NETCOREAPP3_0_OR_GREATER
        public string Type => GetPropertyValue<string>()!;
#else
        public string Type => GetPropertyValue<string>();
#endif
        /// <summary>
        /// Gets the Description field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Description)]
        public string[] Description => GetPropertyValues<string>();

        /// <summary>
        /// Gets the Added field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Added)]
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime Added => GetPropertyValue<DateTime>();

        /// <summary>
        /// Gets the Deprecated field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Deprecated)]
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime? Deprecated => GetPropertyValue<DateTime?>();

        /// <summary>
        /// Gets the Preferred-Value field value.
        /// </summary>
        [SubtagRegistryField(FieldName.PreferredValue)]
#if NETCOREAPP3_0_OR_GREATER
        public string? PreferredValue => GetPropertyValue<string?>();
#else
        [CanBeNull]
        public string PreferredValue => GetPropertyValue<string>();
#endif
        /// <summary>
        /// Gets the Prefix field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Prefix)]
        public string[] Prefix => GetPropertyValues<string>();

        /// <summary>
        /// Gets the Suppress-Script field value.
        /// </summary>
        [SubtagRegistryField(FieldName.SuppressScript)]
#if NETCOREAPP3_0_OR_GREATER
        public string? SuppressScript => GetPropertyValue<string?>();
#else
        [CanBeNull]
        public string SuppressScript => GetPropertyValue<string>();
#endif
        /// <summary>
        /// Gets the Macrolanguage field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Macrolanguage)]
#if NETCOREAPP3_0_OR_GREATER
        public string? Macrolanguage => GetPropertyValue<string?>();
#else
        [CanBeNull]
        public string Macrolanguage => GetPropertyValue<string>();
#endif
        /// <summary>
        /// Gets the Scope field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Scope)]
#if NETCOREAPP3_0_OR_GREATER
        public string? Scope => GetPropertyValue<string?>();
#else
        [CanBeNull] public string Scope => GetPropertyValue<string>();
#endif
        /// <summary>
        /// Gets the Comments field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Comments)]
#if NETCOREAPP3_0_OR_GREATER
        public string? Comments => GetPropertyValue<string?>();
#else
        [CanBeNull] public string Comments => GetPropertyValue<string>();
#endif
    }
}