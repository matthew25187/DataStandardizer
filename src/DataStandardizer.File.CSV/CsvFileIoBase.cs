using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Linq;
using System.Text;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// The shared base of <see cref="CsvFileReader{TRecordLine}"/> and <see cref="CsvFileWriter{TRecordLine}"/>, which holds the mapper registration
    /// members common to both.
    /// </summary>
    /// <typeparam name="TRecordLine">Type of the record lines read or written.</typeparam>
    /// <remarks>
    /// The type can't be derived from outside this package.
    /// </remarks>
    public abstract class CsvFileIoBase<TRecordLine> where TRecordLine : CsvFileRecordLine
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private ICsvFileMapper? _registeredMapper;
        private IReadOnlyDictionary<Type, ICsvFileMapper>? _registeredMappers;
#else
        [MaybeNull] private ICsvFileMapper _registeredMapper;
        [MaybeNull] private IReadOnlyDictionary<Type, ICsvFileMapper> _registeredMappers;
#endif
        private int _mapperVersion;
        private bool _isDisposed;

        private protected CsvFileIoBase()
        {
        }

        /// <summary>
        /// Register a mapper for the record lines this reader or writer reads or writes.
        /// </summary>
        /// <typeparam name="TMapper">Type of the mapper.</typeparam>
        /// <remarks>
        /// The mapper applies to this reader or writer only, from the next record line it reads or writes.  A reader or writer has at most one
        /// registered mapper: registering a mapper of another type replaces it, and registering a <typeparamref name="TMapper"/> when one is already
        /// registered does nothing.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader or writer has been disposed.</exception>
        public void RegisterMapper<TMapper>() where TMapper : CsvFileMapperBase<TRecordLine>, new()
        {
            ThrowIfDisposed();

            if (IsRegistered<TMapper>())
            {
                return;
            }

            _registeredMapper = new TMapper();
            _mapperVersion++;
        }

        /// <summary>
        /// Unregister a mapper from this reader or writer.
        /// </summary>
        /// <typeparam name="TMapper">Type of the mapper.</typeparam>
        /// <remarks>
        /// If the mapper registered on this reader or writer is a <typeparamref name="TMapper"/>, it is removed, and from the next record line the
        /// reader or writer maps lines with the <see cref="CsvFieldMappingAttribute"/> attributes on their type, if any.  If no mapper is registered,
        /// or the registered mapper is of another type, the call does nothing.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader or writer has been disposed.</exception>
        public void UnregisterMapper<TMapper>() where TMapper : CsvFileMapperBase<TRecordLine>, new()
        {
            ThrowIfDisposed();

            if (!IsRegistered<TMapper>())
            {
                return;
            }

            _registeredMapper = null;
            _mapperVersion++;
        }

        // The exact type, so a mapper derived from TMapper counts as a different mapper.
        private bool IsRegistered<TMapper>() => _registeredMapper != null && _registeredMapper.GetType() == typeof(TMapper);

        /// <summary>
        /// Gets a number that changes whenever a mapper is registered on or unregistered from this reader or writer, so it knows to discard plans built
        /// from the mapper it resolved.
        /// </summary>
        private protected int MapperVersion => _mapperVersion;

        /// <summary>
        /// Gets a live, read-only view of the mapper registered on this reader or writer, keyed by <typeparamref name="TRecordLine"/>.
        /// </summary>
        private protected IReadOnlyDictionary<Type, ICsvFileMapper> RegisteredMappers
        {
            get
            {
#pragma warning disable IDE0074
                return _registeredMappers ?? (_registeredMappers = new RegisteredMapperView(this));
#pragma warning restore IDE0074
            }
        }

        /// <summary>
        /// Marks the reader or writer as disposed.
        /// </summary>
        /// <returns><c>true</c> if this call disposed the object; <c>false</c> if it had already been disposed.</returns>
        private protected bool MarkDisposed()
        {
            if (_isDisposed) return false;

            _isDisposed = true;
            return true;
        }

        private protected void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private protected static CsvFileException BuildException(string message, IDictionary<string, object> dataItems, Exception? innerException = null)
#else
        private protected static CsvFileException BuildException(string message, IDictionary<string, object> dataItems, [AllowNull] Exception innerException = null)
#endif
        {
            var exception = innerException is null ? new CsvFileException(message) : new CsvFileException(message, innerException);
            foreach (var dataItem in dataItems)
            {
                exception.Data.Add(dataItem.Key, dataItem.Value);
            }

            return exception;
        }

        /// <summary>
        /// Replaces each line break in a field value with the <see cref="ICsvFileOptions.EmbeddedLineBreak"/> sequence.
        /// </summary>
        private protected static string NormalizeFieldValueLineBreaks(string fieldValue, ICsvFileOptions options)
        {
            var hasTrailingLineBreak = fieldValue.TrimEnd('\n', '\r').Length < fieldValue.Length;

            var fieldValueBuilder = new StringBuilder();
            using (var lineReader = new StringReader(fieldValue))
            using (var lineWriter = new StringWriter(fieldValueBuilder))
            {
                var line = lineReader.ReadLine();
                while (line != null)
                {
                    lineWriter.Write(line);

                    line = lineReader.ReadLine();

                    if (line != null)
                    {
                        lineWriter.Write(options.EmbeddedLineBreak);
                    }
                }

                if (hasTrailingLineBreak)
                {
                    lineWriter.Write(options.EmbeddedLineBreak);
                }
            }

            return fieldValueBuilder.ToString();
        }

        /// <summary>
        /// Gets the mapper for a record line: the mapper registered on this reader or writer if there is one, else the declarative mapper of the line's
        /// own type.
        /// </summary>
        /// <param name="recordLine">The record line.</param>
        /// <param name="isRegistered"><c>true</c> if the mapper is registered, so applies to a line of any type; <c>false</c> if it only applies to lines of the runtime type of <paramref name="recordLine"/>.</param>
        private protected ICsvFileMapper GetMapper(TRecordLine recordLine, out bool isRegistered)
        {
            var registeredMapper = _registeredMapper;
            if (registeredMapper != null)
            {
                isRegistered = true;
                return registeredMapper;
            }

            isRegistered = false;
            return CsvMapperCache.GetOrAddDeclarativeMapper(recordLine);
        }

        /// <summary>
        /// Calls the generic field value members for a property type known only at run time, without reflection on each call.
        /// </summary>
        /// <remarks>
        /// One accessor is created per property type, held in <see cref="CsvMapperCache"/> and shared by every reader and writer.  Accessors hold no
        /// state that changes, so sharing them is thread-safe.
        /// </remarks>
        private protected abstract class FieldValueAccessor
        {
            private static readonly Func<Type, object> AccessorFactory = CreateAccessor;

            internal static FieldValueAccessor For(Type propertyType)
            {
                return (FieldValueAccessor)CsvMapperCache.GetOrAddFieldValueAccessor(typeof(TRecordLine), propertyType, AccessorFactory);
            }

            private static object CreateAccessor(Type propertyType)
            {
                var accessorType = typeof(CsvFileIoBase<>.FieldValueAccessor<>).MakeGenericType(typeof(TRecordLine), propertyType);
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
            /// <param name="fieldName">The name of the field on the line that the mapping resolved to, which a converter delegate is given.</param>
            /// <param name="fieldIndex">The position of that field on the line, which a converter delegate is given; <c>null</c> for a constant or generated value, which has no field.</param>
            /// <param name="value">The converted value, or <c>null</c> if the conversion failed.</param>
            /// <param name="error">The exception a type converter threw, if it failed by throwing; otherwise <c>null</c>.</param>
            /// <returns><c>true</c> if the value was converted; <c>false</c> if it is not a valid value of the property type.</returns>
            internal abstract bool TryDeserialize(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
                string fieldName, int? fieldIndex, object? fieldValue, out object? value, out Exception? error);

            internal abstract object? Generate(Delegate generator);
#else
            /// <summary>
            /// Converts a mapped field value to the property type.
            /// </summary>
            /// <param name="fieldName">The name of the field on the line that the mapping resolved to, which a converter delegate is given.</param>
            /// <param name="fieldIndex">The position of that field on the line, which a converter delegate is given; <c>null</c> for a constant or generated value, which has no field.</param>
            /// <param name="value">The converted value, or <c>null</c> if the conversion failed.</param>
            /// <param name="error">The exception a type converter threw, if it failed by throwing; otherwise <c>null</c>.</param>
            /// <returns><c>true</c> if the value was converted; <c>false</c> if it is not a valid value of the property type.</returns>
            internal abstract bool TryDeserialize([AllowNull] CsvFileHeaderLine headerLine, [DisallowNull] TRecordLine recordLine, [DisallowNull] CsvFileOptions options,
                KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, [DisallowNull] string fieldName, int? fieldIndex, [AllowNull] object fieldValue, [MaybeNull] out object value,
                [MaybeNull] out Exception error);

            [return: MaybeNull]
            internal abstract object Generate([DisallowNull] Delegate generator);
#endif
        }

        private sealed class FieldValueAccessor<T> : FieldValueAccessor
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

            internal override bool TryDeserialize(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
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

            internal override bool TryDeserialize(CsvFileHeaderLine headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
                string fieldName, int? fieldIndex, object fieldValue, out object value, out Exception error)
#endif
            {
                error = null;

                // A value of the property type is used as it is, unless it is a field's text and a converter that converts from a string is configured:
                // such a converter is given the text even when the property is a string.  A constant or generated value is always used as it is.
                var fieldMapping = propertyFieldMapping.Value;
                if (fieldValue is T
                    && (fieldMapping.SuppliesValue || !(fieldValue is string)
                        || !HasFromStringConverter(fieldMapping)))
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
                    var converter = (CsvFieldConvertFromString<TRecordLine, T>)fieldMapping.FromStringConverter;
                    var context = new CsvFieldContext<TRecordLine>(options)
                    {
                        HeaderLine = headerLine,
                        Model = recordLine,
                        FieldName = fieldName,
                        FieldIndex = fieldIndex,
                        RawFieldValue = serializedFieldValue
                    };
                    value = converter(context);
                    return true;
                }

                var culture = options.Culture ?? CultureInfo.InvariantCulture;
                if (fieldMapping.TypeConverterType != null)
                {
                    var typeConverter = CsvMapperCache.GetTypeConverter(fieldMapping.TypeConverterType);
                    if (typeConverter is null || !typeConverter.CanConvertFrom(typeof(string)))
                    {
                        value = default(T);
                        return true;
                    }

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

            /// <summary>
            /// Gets a flag indicating if a mapping has a converter delegate, or a type converter that converts from a string.
            /// </summary>
            private static bool HasFromStringConverter(CsvFieldMapping fieldMapping)
            {
                if (fieldMapping.FromStringConverter != null)
                {
                    return true;
                }

                var typeConverterType = fieldMapping.TypeConverterType;
                if (typeConverterType is null)
                {
                    return false;
                }

                var typeConverter = CsvMapperCache.GetTypeConverter(typeConverterType);
                return typeConverter != null && typeConverter.CanConvertFrom(typeof(string));
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

        /// <summary>
        /// Read-only, live view of the mapper registered on a reader or writer.
        /// </summary>
        private sealed class RegisteredMapperView : IReadOnlyDictionary<Type, ICsvFileMapper>
        {
            private readonly CsvFileIoBase<TRecordLine> _owner;

            internal RegisteredMapperView(CsvFileIoBase<TRecordLine> owner)
            {
                _owner = owner;
            }

            public int Count => Snapshot().Count;

            public ICsvFileMapper this[Type key]
            {
                get
                {
                    if (TryGetValue(key, out var mapper))
                    {
                        return mapper;
                    }

                    throw new KeyNotFoundException();
                }
            }

            public IEnumerable<Type> Keys => Snapshot().Select(entry => entry.Key);

            public IEnumerable<ICsvFileMapper> Values => Snapshot().Select(entry => entry.Value);

            public bool ContainsKey(Type key) => TryGetValue(key, out _);

#if NETCOREAPP3_0_OR_GREATER
            public bool TryGetValue(Type key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out ICsvFileMapper value)
#else
            public bool TryGetValue(Type key, out ICsvFileMapper value)
#endif
            {
                if (key is null)
                    throw new ArgumentNullException(nameof(key));

                foreach (var entry in Snapshot())
                {
                    if (entry.Key == key)
                    {
                        value = entry.Value;
                        return true;
                    }
                }

#if NETCOREAPP3_0_OR_GREATER
                value = null;
#elif NETSTANDARD2_1_OR_GREATER
                value = null!;
#else
                value = null;
#endif
                return false;
            }

            public IEnumerator<KeyValuePair<Type, ICsvFileMapper>> GetEnumerator() => Snapshot().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            // The view's one source of entries: the registered mapper, if any, keyed by the record-line type.
            private List<KeyValuePair<Type, ICsvFileMapper>> Snapshot()
            {
                var snapshot = new List<KeyValuePair<Type, ICsvFileMapper>>(1);
                var registeredMapper = _owner._registeredMapper;
                if (registeredMapper != null)
                {
                    snapshot.Add(new KeyValuePair<Type, ICsvFileMapper>(typeof(TRecordLine), registeredMapper));
                }

                return snapshot;
            }
        }
    }
}
