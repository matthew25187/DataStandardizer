using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
            internal const string FieldValue = "FieldValue";
            internal const string PropertyName = "PropertyName";
        }

        /// <summary>
        /// Reflection results for a property that a record line or custom model type declares or inherits.
        /// </summary>
        private sealed class PropertyBinding
        {
            internal PropertyBinding(CsvMappableProperty mappableProperty)
            {
                var property = mappableProperty.Declaration;
                Property = property;
                Getter = mappableProperty.Getter;
                Setter = mappableProperty.Setter;
                Key = property.GetPropertyKey();
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            /// <inheritdoc cref="CsvMappableProperty.Getter"/>
            internal PropertyInfo? Getter { get; }

            /// <inheritdoc cref="CsvMappableProperty.Setter"/>
            internal PropertyInfo? Setter { get; }

            internal CsvFieldMappingAttribute? MappingAttribute { get; }

            internal Type? TypeConverterType { get; }
#else
            /// <inheritdoc cref="CsvMappableProperty.Getter"/>
            internal PropertyInfo Getter { get; }

            /// <inheritdoc cref="CsvMappableProperty.Setter"/>
            internal PropertyInfo Setter { get; }

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

            return PropertyBindingCache.GetOrAdd(type, t => CsvMappableProperty.GetAll(t).Select(property => new PropertyBinding(property)).ToArray());
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
        /// <remarks>
        /// <para>
        /// Each property of the model that <paramref name="mapper"/> maps is set the way a reader sets a record line's property.  A mapping by index
        /// reads the field at that position of the line, whatever its name; a mapping by name reads the field with that name; any other mapping reads
        /// the field named after the property.  A constant or generated value is used in place of the field's value.
        /// </para>
        /// <para>
        /// A validator is given the field's text, or <c>null</c> when the line holds a value that isn't text.  Text is converted to the property type
        /// with the mapping's converter delegate or type converter, or else parsed as a reader parses it, in the invariant culture.  A value the line
        /// already holds as the property type, such as one a reader converted, is used as it is.  The converter and validator delegates are given
        /// the model being built, and options with their default values; they are given no header line.
        /// </para>
        /// </remarks>
        /// <exception cref="CsvFileException">A property's mapping isn't optional and the line has no field for it; a validator rejected a field's
        /// value; a field's text isn't a valid value of the property type; or the line holds a value of another type than the property's, which isn't
        /// text.</exception>
        public static TModel ToObject<TRecordLine, TModel>(this TRecordLine sourceLine, CsvFileCustomMapperBase<TModel> mapper)
            where TRecordLine : CsvFileRecordLine
            where TModel : class, new()
        {
            ICsvFileLine csvLine = sourceLine;
            var targetModel = new TModel();
            ICsvFileMapper csvMapper = mapper;
            var options = new CsvFileOptions();

            foreach (var propertyBinding in GetPropertyBindings(targetModel.GetType()))
            {
                if (!(propertyBinding.Setter is PropertyInfo setter))
                {
                    continue;
                }

                // Get the field mapping for the current property.
                var propertyKey = propertyBinding.Key;
                if (!csvMapper.TryGetValue(propertyKey, out var fieldMapping))
                {
                    continue;
                }

                // Find the field the mapping resolves to.  A mapping that supplies its own value needs no field.
                var propertyFieldMapping = new KeyValuePair<string, CsvFieldMapping>(propertyKey, fieldMapping);
                var fieldPosition = GetFieldPosition(csvLine, propertyFieldMapping);
                if (fieldPosition is null)
                {
                    if (fieldMapping.IsOptional)
                    {
                        continue;
                    }

                    if (!fieldMapping.SuppliesValue)
                    {
                        throw BuildUnmappedPropertyException(propertyFieldMapping);
                    }
                }

                var fieldName = fieldPosition is int position ? csvLine.GetAt(position).Key : CsvMappingService.GetFieldNameFromMapping(propertyFieldMapping);
                var lineValue = fieldPosition is int valuePosition ? csvLine[valuePosition] : null;

                // Validate the field value.
                if (fieldMapping.Validator is CsvFieldValidate<TModel> validator)
                {
                    var fieldContext = new CsvFieldContext<TModel>(options)
                    {
                        Model = targetModel,
                        FieldIndex = fieldPosition,
                        FieldName = fieldName,
                        RawFieldValue = lineValue as string
                    };
                    if (!validator(fieldContext))
                    {
                        throw BuildInvalidValueException(propertyFieldMapping, fieldName, lineValue, null);
                    }
                }

                // Take the constant or generated value, or the field's, then convert it to the property type.
                var accessor = CsvFieldValueAccessor<TModel>.For(fieldMapping.PropertyType);
                var fieldValue = lineValue;
                if (fieldMapping.ConstantValue != null)
                {
                    fieldValue = fieldMapping.ConstantValue;
                }
                else if (fieldMapping.VariableValueGenerator != null)
                {
                    fieldValue = accessor.Generate(fieldMapping.VariableValueGenerator);
                }
                else if (fieldValue is object heldValue && !accessor.IsConvertible(heldValue))
                {
                    throw BuildWrongValueTypeException(propertyFieldMapping, fieldName, heldValue);
                }

                var valuePositionForConverter = fieldMapping.SuppliesValue ? null : fieldPosition;
                if (!accessor.TryDeserialize(null, targetModel, options, propertyFieldMapping, fieldName, valuePositionForConverter, fieldValue, out var propertyValue,
                        out var error))
                {
                    throw BuildInvalidValueException(propertyFieldMapping, fieldName, fieldValue, error);
                }

                setter.SetValue(targetModel, propertyValue);
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
        /// <remarks>
        /// <para>
        /// The line holds a field for each mapping of <paramref name="mapper"/>, in the order of the positions a writer gives them, so a mapping by
        /// index comes before the mappings at later positions.  A field is named as the mapping names it, or after the property.  It holds the
        /// mapping's constant or generated value if it has one, or else the value of the model's property with the same key; a mapping with neither
        /// adds no field.
        /// </para>
        /// <para>
        /// The values keep their types, as a line's properties return them.  A writer converts them to text when it writes the line, and applies the
        /// mapping's converters and positions if <paramref name="mapper"/> is registered on it.  Validators apply only to reading.
        /// </para>
        /// </remarks>
        public static TRecordLine ToCsvLine<TModel, TRecordLine>(this TModel sourceModel, CsvFileMapperBase<TRecordLine> mapper)
            where TModel : class
            where TRecordLine : CsvFileRecordLine, new()
        {
            var targetLine = new TRecordLine();
            ICsvFileLine csvLine = targetLine;
            ICsvFileMapper csvMapper = mapper;

            // The values are stored under their field names, so a property mapped to a field with another name reads that field.
            targetLine.SetMappedFieldNames(CsvMappingService.GetMappedFieldNames(csvMapper));

            var propertyBindings = GetPropertyBindings(sourceModel.GetType());
            foreach (var propertyKey in CsvMappingService.GetPropertyKeysByPosition(csvMapper))
            {
                var fieldMapping = csvMapper[propertyKey];
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                object? fieldValue;
#else
                object fieldValue;
#endif
                if (fieldMapping.ConstantValue != null)
                {
                    fieldValue = fieldMapping.ConstantValue;
                }
                else if (fieldMapping.VariableValueGenerator != null)
                {
                    fieldValue = CsvFieldValueAccessor<TRecordLine>.For(fieldMapping.PropertyType).Generate(fieldMapping.VariableValueGenerator);
                }
                else if (FindGetter(propertyBindings, propertyKey) is PropertyInfo getter)
                {
                    fieldValue = getter.GetValue(sourceModel);
                }
                else
                {
                    continue;
                }

                var fieldKey = CsvMappingService.GetFieldNameFromMapping(propertyKey, fieldMapping);
                csvLine[fieldKey] = fieldValue;
            }

            return targetLine;
        }

        /// <summary>
        /// Gets the position on a line of the field a mapping resolves to, or <c>null</c> if the line has no such field.
        /// </summary>
        private static int? GetFieldPosition(ICsvFileLine csvLine, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping)
        {
            if (propertyFieldMapping.Value.FieldIndex is int fieldIndex)
            {
                return fieldIndex < csvLine.Count ? fieldIndex : (int?)null;
            }

            var position = csvLine.IndexOf(CsvMappingService.GetFieldNameFromMapping(propertyFieldMapping));
            return position < 0 ? (int?)null : position;
        }

        /// <summary>
        /// Gets the getter of the readable property with a key, or <c>null</c> if there is none.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static PropertyInfo? FindGetter(PropertyBinding[] propertyBindings, string propertyKey)
#else
        private static PropertyInfo FindGetter(PropertyBinding[] propertyBindings, string propertyKey)
#endif
        {
            foreach (var propertyBinding in propertyBindings)
            {
                if (propertyBinding.Getter != null && string.Equals(propertyBinding.Key, propertyKey, StringComparison.Ordinal))
                {
                    return propertyBinding.Getter;
                }
            }

            return null;
        }

        private static CsvFileException BuildUnmappedPropertyException(KeyValuePair<string, CsvFieldMapping> propertyFieldMapping)
        {
            var exception = new CsvFileException($"Property '{propertyFieldMapping.Key}' unable to be mapped.");
            exception.Data.Add(DataItemName.PropertyName, propertyFieldMapping.Key);
            exception.Data.Add(DataItemName.FieldIndex, propertyFieldMapping.Value.FieldIndex);
            exception.Data.Add(DataItemName.FieldName, propertyFieldMapping.Value.FieldName);
            return exception;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static CsvFileException BuildInvalidValueException(KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, string fieldName, object? fieldValue,
            Exception? innerException)
#else
        private static CsvFileException BuildInvalidValueException(KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, string fieldName, object fieldValue,
            Exception innerException)
#endif
        {
            var message = $"Encountered invalid value '{fieldValue}' in CSV field {fieldName}.";
            var exception = innerException is null ? new CsvFileException(message) : new CsvFileException(message, innerException);
            exception.Data.Add(DataItemName.PropertyName, propertyFieldMapping.Key);
            exception.Data.Add(DataItemName.FieldName, fieldName);
            exception.Data.Add(DataItemName.FieldValue, fieldValue);
            return exception;
        }

        private static CsvFileException BuildWrongValueTypeException(KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, string fieldName, object fieldValue)
        {
            var exception = new CsvFileException(
                $"CSV field {fieldName} holds a value of type {fieldValue.GetType()}, which property '{propertyFieldMapping.Key}' of type {propertyFieldMapping.Value.PropertyType} can't hold.");
            exception.Data.Add(DataItemName.PropertyName, propertyFieldMapping.Key);
            exception.Data.Add(DataItemName.FieldName, fieldName);
            exception.Data.Add(DataItemName.FieldValue, fieldValue);
            return exception;
        }
    }
}
