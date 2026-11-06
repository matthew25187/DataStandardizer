using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        /// The mapper applies to this reader or writer only, from the next record line it reads or writes.  If a mapper is already registered on it,
        /// the call does nothing.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader or writer has been disposed.</exception>
        public void RegisterMapper<TMapper>() where TMapper : CsvFileMapperBase<TRecordLine>, new()
        {
            ThrowIfDisposed();

            if (_registeredMapper != null)
            {
                return;
            }

            _registeredMapper = new TMapper();
            _mapperVersion++;
        }

        /// <summary>
        /// Unregister the mapper registered on this reader or writer.
        /// </summary>
        /// <typeparam name="TMapper">Type of the mapper.</typeparam>
        /// <remarks>
        /// From the next record line, the reader or writer maps lines with the <see cref="CsvFieldMappingAttribute"/> attributes on their type, if any.
        /// If no mapper is registered, the call does nothing.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader or writer has been disposed.</exception>
        public void UnregisterMapper<TMapper>() where TMapper : CsvFileMapperBase<TRecordLine>, new()
        {
            ThrowIfDisposed();

            if (_registeredMapper is null)
            {
                return;
            }

            _registeredMapper = null;
            _mapperVersion++;
        }

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

        private protected static CsvFileException BuildException(string message, IDictionary<string, object> dataItems)
        {
            var exception = new CsvFileException(message);
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
            return CsvMapperCache.GetOrAddDeclarativeMapper(recordLine, out _);
        }

#if NET5_0_OR_GREATER
        private static T? DeserializeFieldValue<T>(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, object? fieldValue)
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        private static T DeserializeFieldValue<T>(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, object? fieldValue)
#else
        [return: MaybeNull]
        private static T DeserializeFieldValue<T>([AllowNull] CsvFileHeaderLine headerLine, [DisallowNull] TRecordLine recordLine, [DisallowNull] CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
            [AllowNull] object fieldValue)
#endif
        {
            if (fieldValue is null)
            {
                return default; // if there is no field value then there is nothing to deserialize
            }
            else if (fieldValue is T deserializedFieldValue)
            {
                return deserializedFieldValue; // if the field value is already of the target type then just return it
            }

            // Deserialize the field value to the field type.
            var serializedFieldValue = fieldValue as string;
            if (serializedFieldValue is null)
            {
                return default; // if the field value is not a string at this point then it can't be deserialized
            }

            // Attempt to convert the field value to the target data type.
#if NET5_0_OR_GREATER
            T? result = default;
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            T result = default!;
#else
            T result = default;
#endif
            if (propertyFieldMapping.Value.FromStringConverter != null)
            {
                var converter = (CsvFieldConvertFromString<TRecordLine, T>)propertyFieldMapping.Value.FromStringConverter;

                var mappedFieldName = CsvMappingService.GetFieldNameFromMapping(propertyFieldMapping);
                var context = new CsvFieldContext<TRecordLine>(options)
                {
                    HeaderLine = headerLine,
                    Model = recordLine,
                    FieldName = mappedFieldName,
                    FieldIndex = propertyFieldMapping.Value.FieldIndex
                };
                result = converter(context);
            }
            else if (propertyFieldMapping.Value.TypeConverterType != null)
            {
                var typeConverter = CsvMapperCache.GetTypeConverter(propertyFieldMapping.Value.TypeConverterType);
                if ((typeConverter?.CanConvertFrom(typeof(string))).GetValueOrDefault())
                {
                    result = options.Culture != null
#if NET5_0_OR_GREATER
                        ? (T?)typeConverter?.ConvertFromString(null, options.Culture, serializedFieldValue)
                        : (T?)typeConverter?.ConvertFromInvariantString(serializedFieldValue);
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                        ? (T)typeConverter?.ConvertFromString(null, options.Culture, serializedFieldValue)!
                        : (T)typeConverter?.ConvertFromInvariantString(serializedFieldValue)!;
#else
                        ? (T)typeConverter?.ConvertFromString(null, options.Culture, serializedFieldValue)
                        : (T)typeConverter?.ConvertFromInvariantString(serializedFieldValue);
#endif
                }
            }

            return result;
        }

        /// <summary>
        /// Calls the generic field value members for a property type known only at run time, without reflection on each call.
        /// </summary>
        /// <remarks>
        /// One accessor is created per property type and shared by every reader and writer.  Accessors hold no state, so sharing them is thread-safe.
        /// </remarks>
        private protected abstract class FieldValueAccessor
        {
            private static readonly ConcurrentDictionary<Type, FieldValueAccessor> AccessorCache = new ConcurrentDictionary<Type, FieldValueAccessor>();

            internal static FieldValueAccessor For(Type propertyType)
            {
                if (AccessorCache.TryGetValue(propertyType, out var accessor))
                {
                    return accessor;
                }

                return AccessorCache.GetOrAdd(propertyType, CreateAccessor);
            }

            private static FieldValueAccessor CreateAccessor(Type propertyType)
            {
                var accessorType = typeof(CsvFileIoBase<>.FieldValueAccessor<>).MakeGenericType(typeof(TRecordLine), propertyType);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                return (FieldValueAccessor)Activator.CreateInstance(accessorType)!;
#else
                return (FieldValueAccessor)Activator.CreateInstance(accessorType);
#endif
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal abstract object? Deserialize(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, object? fieldValue);

            internal abstract object? Generate(Delegate generator);
#else
            [return: MaybeNull]
            internal abstract object Deserialize([AllowNull] CsvFileHeaderLine headerLine, [DisallowNull] TRecordLine recordLine, [DisallowNull] CsvFileOptions options,
                KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, [AllowNull] object fieldValue);

            [return: MaybeNull]
            internal abstract object Generate([DisallowNull] Delegate generator);
#endif
        }

        private sealed class FieldValueAccessor<T> : FieldValueAccessor
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal override object? Deserialize(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, object? fieldValue)
            {
                return DeserializeFieldValue<T>(headerLine, recordLine, options, propertyFieldMapping, fieldValue);
            }

            internal override object? Generate(Delegate generator)
            {
                return ((CsvFieldGenerate<T>)generator)();
            }
#else
            internal override object Deserialize(CsvFileHeaderLine headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, object fieldValue)
            {
                return DeserializeFieldValue<T>(headerLine, recordLine, options, propertyFieldMapping, fieldValue);
            }

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

            public int Count => _owner._registeredMapper is null ? 0 : 1;

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

            public IEnumerable<Type> Keys
            {
                get
                {
                    var keys = new List<Type>(1);
                    if (_owner._registeredMapper != null)
                    {
                        keys.Add(typeof(TRecordLine));
                    }

                    return keys;
                }
            }

            public IEnumerable<ICsvFileMapper> Values
            {
                get
                {
                    var values = new List<ICsvFileMapper>(1);
                    var registeredMapper = _owner._registeredMapper;
                    if (registeredMapper != null)
                    {
                        values.Add(registeredMapper);
                    }

                    return values;
                }
            }

            public bool ContainsKey(Type key) => TryGetValue(key, out _);

#if NETCOREAPP3_0_OR_GREATER
            public bool TryGetValue(Type key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out ICsvFileMapper value)
#else
            public bool TryGetValue(Type key, out ICsvFileMapper value)
#endif
            {
                if (key is null)
                    throw new ArgumentNullException(nameof(key));

                var registeredMapper = _owner._registeredMapper;
                if (registeredMapper != null && key == typeof(TRecordLine))
                {
                    value = registeredMapper;
                    return true;
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
