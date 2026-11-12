using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace DataStandardizer.File.CSV
{
    public static class CsvExtensions
    {
        private static class DataItemName
        {
            internal const string FieldIndex = "FieldIndex";
            internal const string FieldName = "FieldName";
            internal const string PropertyName = "PropertyName";
        }

        /// <summary>
        /// Reflection results for a property that a record line or custom model type declares or inherits.
        /// </summary>
        private sealed class PropertyBinding
        {
            internal PropertyBinding(PropertyInfo property)
            {
                Property = property;
                Key = property.GetPropertyKey();
                CanRead = property.CanRead;
                CanWrite = property.CanWrite;
                MappingAttribute = property.GetCustomAttribute<CsvFieldMappingAttribute>();
                if (MappingAttribute is null)
                {
                    return;
                }

                var typeConverterAttribute = property.GetCustomAttribute<TypeConverterAttribute>();
                TypeConverterType = !string.IsNullOrEmpty(typeConverterAttribute?.ConverterTypeName) ? Type.GetType(typeConverterAttribute.ConverterTypeName) : null;
            }

            internal PropertyInfo Property { get; }

            internal string Key { get; }

            internal bool CanRead { get; }

            internal bool CanWrite { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal CsvFieldMappingAttribute? MappingAttribute { get; }

            internal Type? TypeConverterType { get; }
#else
            internal CsvFieldMappingAttribute MappingAttribute { get; }

            internal Type TypeConverterType { get; }
#endif
        }

        // Mappable-property bindings keyed by type, so conversions and mapper creation reflect at most once per type.
        private static readonly ConcurrentDictionary<Type, PropertyBinding[]> PropertyBindingCache = new ConcurrentDictionary<Type, PropertyBinding[]>();

        /// <summary>
        /// Removes every cached property binding.  Called by <see cref="CsvMapperCache.Clear"/>.
        /// </summary>
        internal static void ClearPropertyBindings() => PropertyBindingCache.Clear();

        private static PropertyBinding[] GetPropertyBindings(Type type)
        {
            if (PropertyBindingCache.TryGetValue(type, out var propertyBindings))
            {
                return propertyBindings;
            }

            return PropertyBindingCache.GetOrAdd(type, t => GetMappableProperties(t).Select(pi => new PropertyBinding(pi)).ToArray());
        }

        /// <summary>
        /// Gets the properties of a record line or custom model type that mapping considers: those the type declares, and those it inherits.
        /// </summary>
        /// <remarks>
        /// Where a type redeclares a property of its base type, by overriding or hiding it, only the most derived declaration is returned, so a
        /// property name has one key on a type.  The properties of the line types that this library defines aren't mappable, so they aren't
        /// returned.
        /// </remarks>
        internal static IEnumerable<PropertyInfo> GetMappableProperties(Type type)
        {
            var propertyNames = new HashSet<string>(StringComparer.Ordinal);
            for (var currentType = type; IsMappableType(currentType); currentType = currentType.GetTypeInfo().BaseType)
            {
                foreach (var pi in currentType.GetTypeInfo().DeclaredProperties)
                {
                    if (propertyNames.Add(pi.Name))
                    {
                        yield return pi;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the property with the given name that mapping considers on a record line type, or <c>null</c> if it has none.
        /// </summary>
        /// <remarks>
        /// The property is the one <see cref="GetMappableProperties"/> returns with that name: the most derived declaration of it.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static PropertyInfo? FindMappableProperty(Type type, string propertyName)
#else
        internal static PropertyInfo FindMappableProperty(Type type, string propertyName)
#endif
        {
            for (var currentType = type; IsMappableType(currentType); currentType = currentType.GetTypeInfo().BaseType)
            {
                var pi = currentType.GetTypeInfo().GetDeclaredProperty(propertyName);
                if (!(pi is null))
                {
                    return pi;
                }
            }

            return null;
        }

        // The line types that this library defines declare no mappable properties, so the walk up the hierarchy stops at them.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static bool IsMappableType([NotNullWhen(true)] Type? type)
#else
        private static bool IsMappableType([NotNullWhen(true)] Type type)
#endif
        {
            return type != null && type != typeof(object) && type != typeof(CsvFileLineBase);
        }

        internal static string GetPropertyKey(this PropertyInfo pi)
        {
            var fieldAttribute = pi.GetCustomAttribute<CsvFieldAttribute>();
            return fieldAttribute?.FieldName ?? pi.Name;
        }

        /// <summary>
        /// Create a CSV field mapper based on declarative configuration on the record line object.
        /// </summary>
        /// <param name="recordLine">Record line object to be mapped.</param>
        /// <returns>Mapper containing collection of field mappings.</returns>
        public static ICsvFileMapper CreateMapper<TRecordLine>(this TRecordLine recordLine) where TRecordLine : CsvFileRecordLine
        {
            var propertyFieldMappings = new Dictionary<string, CsvFieldMapping>();

            foreach (var propertyBinding in GetPropertyBindings(recordLine.GetType()))
            {
                var fieldAttribute = propertyBinding.MappingAttribute;
                if (fieldAttribute is null)
                {
                    continue;
                }

                var fieldMapping = new CsvFieldMapping(propertyBinding.Property.PropertyType)
                {
                    FieldIndex = fieldAttribute.FieldIndex,
                    FieldName = fieldAttribute.FieldName,
                    ConstantValue = fieldAttribute.ConstantValue,
                    IsOptional = fieldAttribute.IsOptional,
                    TypeConverterType = propertyBinding.TypeConverterType,
                };
                propertyFieldMappings.Add(propertyBinding.Key, fieldMapping);
            }

            return new CsvFileMapper(propertyFieldMappings);
        }

        /// <summary>
        /// Convert a CSV line object to a custom line model object.
        /// </summary>
        /// <typeparam name="TRecordLine">Type of the CSV line.</typeparam>
        /// <typeparam name="TModel">Type of the custom line model.</typeparam>
        /// <param name="sourceLine">CSV line read from a CSV file.</param>
        /// <param name="mapper">Field mapping for the custom line model.</param>
        /// <returns>Custom object model representing the CSV line.</returns>
        public static TModel ToObject<TRecordLine, TModel>(this TRecordLine sourceLine, CsvFileCustomMapperBase<TModel> mapper)
            where TRecordLine : CsvFileRecordLine
            where TModel : class, new()
        {
            ICsvFileLine csvLine = sourceLine;
            var targetModel = new TModel();
            ICsvFileMapper csvMapper = mapper;

            foreach (var propertyBinding in GetPropertyBindings(targetModel.GetType()))
            {
                if (!propertyBinding.CanWrite)
                {
                    continue;
                }

                // Get the field mapping for the current property.
                var propertyKey = propertyBinding.Key;
                if (!csvMapper.TryGetValue(propertyKey, out var fieldMapping))
                {
                    continue;
                }

                var fieldKey = CsvMappingService.GetFieldNameFromMapping(new KeyValuePair<string, CsvFieldMapping>(propertyKey, fieldMapping));
                if (!csvLine.Contains(fieldKey) && csvLine.Contains(propertyKey))
                {
                    fieldKey = propertyKey;
                }

                // If a field name was not found for the property, do not attempt to set the property on the custom model.
                if (!csvLine.Contains(fieldKey))
                {
                    if (fieldMapping.IsOptional)
                    {
                        continue;
                    }

                    var exception = new CsvFileException($"Property '{propertyKey}' unable to be mapped.");
                    exception.Data.Add(DataItemName.PropertyName, propertyKey);
                    exception.Data.Add(DataItemName.FieldIndex, fieldMapping.FieldIndex);
                    exception.Data.Add(DataItemName.FieldName, fieldMapping.FieldName);
                    throw exception;
                }

                // Copy the field value from the CSV line to the corresponding property on the custom model.
                // Assumes that the raw string value from the CSV file has already been converted in the CSV line.
                var fieldValue = csvLine[fieldKey];
                propertyBinding.Property.SetValue(targetModel, fieldValue);
            }

            return targetModel;
        }

        /// <summary>
        /// Convert a custom model object to a CSV line object.
        /// </summary>
        /// <typeparam name="TModel">Type of the custom model.</typeparam>
        /// <typeparam name="TRecordLine">Type of the CSV line.</typeparam>
        /// <param name="sourceModel">Custom model object with values to be copied to a CSV line.</param>
        /// <param name="mapper">Field mapping for the CSV line.</param>
        /// <returns>CSV line representing the custom model object.</returns>
        public static TRecordLine ToCsvLine<TModel, TRecordLine>(this TModel sourceModel, CsvFileMapperBase<TRecordLine> mapper)
            where TModel : class
            where TRecordLine : CsvFileRecordLine, new()
        {
            var targetLine = new TRecordLine();
            ICsvFileLine csvLine = targetLine;
            ICsvFileMapper csvMapper = mapper;

            // The values are stored under their field names, so a property mapped to a field with another name reads that field.
            targetLine.SetMappedFieldNames(CsvMappingService.GetMappedFieldNames(csvMapper));

            foreach (var propertyBinding in GetPropertyBindings(sourceModel.GetType()))
            {
                if (!propertyBinding.CanRead)
                {
                    continue;
                }

                // Get the field mapping for the current property.
                var propertyKey = propertyBinding.Key;
                if (!csvMapper.TryGetValue(propertyKey, out var fieldMapping))
                {
                    continue;
                }

                var fieldKey = CsvMappingService.GetFieldNameFromMapping(propertyKey, fieldMapping);

                // Copy the field value from the CSV line to the corresponding property on the custom model.
                var fieldValue = propertyBinding.Property.GetValue(sourceModel);
                csvLine[fieldKey] = fieldValue;
            }

            return targetLine;
        }
    }
}