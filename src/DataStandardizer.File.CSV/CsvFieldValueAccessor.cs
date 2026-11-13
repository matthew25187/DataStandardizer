using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Calls the generic field value members for a property type known only at run time, without reflection on each call.
    /// </summary>
    /// <typeparam name="TModel">Type of the model that the mapping's delegates are given, such as the record line a reader or writer handles, or the
    /// custom model that <see cref="CsvExtensions.ToObject{TRecordLine, TModel}"/> builds.</typeparam>
    /// <remarks>
    /// One accessor is created per model and property type, held in <see cref="CsvMapperCache"/> and shared by every reader, writer and conversion.
    /// Accessors hold no state that changes, so sharing them is thread-safe.
    /// </remarks>
    internal abstract class CsvFieldValueAccessor<TModel> where TModel : class
    {
        private static readonly Func<Type, object> AccessorFactory = CreateAccessor;

        internal static CsvFieldValueAccessor<TModel> For(Type propertyType)
        {
            return (CsvFieldValueAccessor<TModel>)CsvMapperCache.GetOrAddFieldValueAccessor(typeof(TModel), propertyType, AccessorFactory);
        }

        private static object CreateAccessor(Type propertyType)
        {
            var accessorType = typeof(CsvFieldValueAccessor<,>).MakeGenericType(typeof(TModel), propertyType);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            return Activator.CreateInstance(accessorType)!;
#else
            return Activator.CreateInstance(accessorType);
#endif
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Converts a mapped field value to the property type.
        /// </summary>
        /// <param name="model">The model that a converter delegate is given.</param>
        /// <param name="fieldName">The name of the field on the line that the mapping resolved to, which a converter delegate is given.</param>
        /// <param name="fieldIndex">The position of that field on the line, which a converter delegate is given; <c>null</c> for a constant or generated value, which has no field.</param>
        /// <param name="value">The converted value, or <c>null</c> if the conversion failed.</param>
        /// <param name="error">The exception a type converter threw, if it failed by throwing; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the value was converted; <c>false</c> if it is not a valid value of the property type.</returns>
        internal abstract bool TryDeserialize(CsvFileHeaderLine? headerLine, TModel model, ICsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
            string fieldName, int? fieldIndex, object? fieldValue, out object? value, out Exception? error);

        internal abstract object? Generate(Delegate generator);
#else
        /// <summary>
        /// Converts a mapped field value to the property type.
        /// </summary>
        /// <param name="model">The model that a converter delegate is given.</param>
        /// <param name="fieldName">The name of the field on the line that the mapping resolved to, which a converter delegate is given.</param>
        /// <param name="fieldIndex">The position of that field on the line, which a converter delegate is given; <c>null</c> for a constant or generated value, which has no field.</param>
        /// <param name="value">The converted value, or <c>null</c> if the conversion failed.</param>
        /// <param name="error">The exception a type converter threw, if it failed by throwing; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the value was converted; <c>false</c> if it is not a valid value of the property type.</returns>
        internal abstract bool TryDeserialize([AllowNull] CsvFileHeaderLine headerLine, [DisallowNull] TModel model, [DisallowNull] ICsvFileOptions options,
            KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, [DisallowNull] string fieldName, int? fieldIndex, [AllowNull] object fieldValue, [MaybeNull] out object value,
            [MaybeNull] out Exception error);

        [return: MaybeNull]
        internal abstract object Generate([DisallowNull] Delegate generator);
#endif

        /// <summary>
        /// Gets a flag indicating if a value is one that <see cref="TryDeserialize"/> can give the property: <c>null</c>, text it converts, or a value
        /// of the property type.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal abstract bool IsConvertible(object? fieldValue);
#else
        internal abstract bool IsConvertible([AllowNull] object fieldValue);
#endif
    }

    internal sealed class CsvFieldValueAccessor<TModel, T> : CsvFieldValueAccessor<TModel> where TModel : class
    {
        // Stands for "the type has no parser" in _parser, so that null can mean "not worked out yet".
        private static readonly object NoParser = new object();

        // The parser for a value with no converter configured, or NoParser.  Worked out the first time it is needed, so that a mapping with a
        // converter, or a writer, never depends on it.  Parsers hold no state, so if two threads race, each works out an equivalent parser.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private object? _parser;

        private CsvFieldParser<T>? GetParser()
        {
            var parser = Volatile.Read(ref _parser);
            if (parser is null)
            {
                parser = (object?)CsvFieldParser.For<T>() ?? NoParser;
                Volatile.Write(ref _parser, parser);
            }

            return parser as CsvFieldParser<T>;
        }

        internal override bool IsConvertible(object? fieldValue) => fieldValue is null || fieldValue is string || fieldValue is T;

        internal override bool TryDeserialize(CsvFileHeaderLine? headerLine, TModel model, ICsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
            string fieldName, int? fieldIndex, object? fieldValue, out object? value, out Exception? error)
#else
        [MaybeNull] private object _parser;

        [return: MaybeNull]
        private CsvFieldParser<T> GetParser()
        {
            var parser = Volatile.Read(ref _parser);
            if (parser is null)
            {
                parser = (object)CsvFieldParser.For<T>() ?? NoParser;
                Volatile.Write(ref _parser, parser);
            }

            return parser as CsvFieldParser<T>;
        }

        internal override bool IsConvertible([AllowNull] object fieldValue) => fieldValue is null || fieldValue is string || fieldValue is T;

        internal override bool TryDeserialize(CsvFileHeaderLine headerLine, TModel model, ICsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
            string fieldName, int? fieldIndex, object fieldValue, out object value, out Exception error)
#endif
        {
            error = null;

            // A value of the property type is used as it is, unless it is a field's text and a converter that converts from a string is configured:
            // such a converter is given the text even when the property is a string.  A constant or generated value is always used as it is.
            var fieldMapping = propertyFieldMapping.Value;
            if (fieldValue is T
                && (fieldMapping.SuppliesValue || !(fieldValue is string)
                    || !CsvMapperCache.HasFromStringConverter(fieldMapping)))
            {
                value = fieldValue;
                return true;
            }

            // Only a string can be converted; there is nothing to convert from anything else.
            var serializedFieldValue = fieldValue as string;
            if (serializedFieldValue is null)
            {
                value = default(T);
                return true;
            }

            if (fieldMapping.FromStringConverter != null)
            {
                var converter = (CsvFieldConvertFromString<TModel, T>)fieldMapping.FromStringConverter;
                var context = new CsvFieldContext<TModel>(options)
                {
                    HeaderLine = headerLine,
                    Model = model,
                    FieldName = fieldName,
                    FieldIndex = fieldIndex,
                    RawFieldValue = serializedFieldValue
                };
                value = converter(context);
                return true;
            }

            var culture = options.Culture ?? CultureInfo.InvariantCulture;
            // A type converter that doesn't convert from a string doesn't apply, so the value is parsed as it is with no converter configured.
            var typeConverter = CsvMapperCache.GetFromStringTypeConverter(fieldMapping.TypeConverterType);
            if (typeConverter != null)
            {
                try
                {
                    value = typeConverter.ConvertFromString(null, culture, serializedFieldValue);
                }
                catch (Exception exception)
                {
                    value = null;
                    error = exception;
                    return false;
                }

                // A converter that gives a value of another type, or null for a property that can't hold it, has not converted the value.
                if (value is T || (value is null && default(T) == null))
                {
                    return true;
                }

                value = null;
                return false;
            }

            // With no converter configured, an empty field is the default value of the property type, and any other value is parsed.
            if (serializedFieldValue.Length == 0)
            {
                value = default(T);
                return true;
            }

            var parser = GetParser();
            if (parser is null)
            {
                value = default(T);
                return true;
            }

            try
            {
                if (parser.TryParse(serializedFieldValue, culture, out var parsedValue))
                {
                    value = parsedValue;
                    return true;
                }
            }
            catch (Exception exception)
            {
                error = exception;
            }

            value = null;
            return false;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal override object? Generate(Delegate generator)
        {
            return ((CsvFieldGenerate<T>)generator)();
        }
#else
        internal override object Generate(Delegate generator)
        {
            return ((CsvFieldGenerate<T>)generator)();
        }
#endif
    }
}
