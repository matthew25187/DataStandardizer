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

        /// <summary>
        /// Checks that a handler delegate set in the options is of the type the reader or writer calls, so that it is never silently ignored.
        /// </summary>
        /// <typeparam name="THandler">The delegate type the reader or writer calls.</typeparam>
        /// <param name="handler">The handler set in the options, if any.</param>
        /// <param name="propertyName">The name of the options property that holds <paramref name="handler"/>.</param>
        /// <param name="paramName">The name of the constructor parameter that holds the options.</param>
        /// <exception cref="ArgumentException"><paramref name="handler"/> isn't <c>null</c> or a <typeparamref name="THandler"/>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private protected static void ValidateHandler<THandler>(Delegate? handler, string propertyName, string paramName)
#else
        private protected static void ValidateHandler<THandler>([AllowNull] Delegate handler, string propertyName, string paramName)
#endif
            where THandler : class
        {
            if (handler is null || handler is THandler)
            {
                return;
            }

            throw new ArgumentException(
                $"{propertyName} must be a {CsvTypeName.Format(typeof(THandler))} delegate, or null, but it is a {CsvTypeName.Format(handler.GetType())}.", paramName);
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
