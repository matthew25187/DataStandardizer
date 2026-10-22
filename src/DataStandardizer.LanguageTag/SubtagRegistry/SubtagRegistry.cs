using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataStandardizer.LanguageTag.InternalState;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// Collection of records that comprise the Subtag Registry.
    /// </summary>
    public class SubtagRegistry : IReadOnlyCollection<ISubtagRegistryRecord>
    {
        private const int StreamReaderBufferSize = 16384;

        /// <summary>
        /// The <see cref="FileStream"/> default, given explicitly by the constructor that takes <see cref="FileOptions"/>.
        /// </summary>
        private const int FileStreamBufferSize = 4096;

        private readonly IReadOnlyCollection<ISubtagRegistryRecord> _records;

        /// <summary>
        /// Sets of the subtags and grandfathered tags in <see cref="_records"/>, built once, on first use, and shared by every thread.
        /// </summary>
        private readonly Lazy<Bcp47RegistryBasedSubtagClassifier> _classifier;

        private SubtagRegistry(ISubtagRegistryRecord[] records)
        {
            _records = records;
            _classifier = new Lazy<Bcp47RegistryBasedSubtagClassifier>(() => new Bcp47RegistryBasedSubtagClassifier(records));
        }

        /// <summary>
        /// Gets the classifier that checks language tags against this registry.
        /// </summary>
        internal Bcp47SubtagClassifier Classifier => _classifier.Value;

        #region Public Methods

        /// <summary>
        /// Load the Subtag Registry from a string.
        /// </summary>
        /// <param name="subtagRegistryContent">Content of the Subtag Registry.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The subtag registry content is <c>null</c>.</exception>
        public static SubtagRegistry CreateFromContent(string subtagRegistryContent)
        {
            if (subtagRegistryContent is null)
            {
                throw new ArgumentNullException(nameof(subtagRegistryContent));
            }

            var records = new List<ISubtagRegistryRecord>();

            using (var contentReader = new StringReader(subtagRegistryContent))
            {
                var registryReader = new SubtagRegistryReader(contentReader);

                var record = registryReader.ReadRecord();
                while (record != null)
                {
                    records.Add(record);

                    record = registryReader.ReadRecord();
                }
            }

            return new SubtagRegistry(records.ToArray());
        }

        /// <summary>
        /// Load the Subtag Registry from a string.
        /// </summary>
        /// <param name="subtagRegistryContent">Content of the Subtag Registry.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The subtag registry content is <c>null</c>.</exception>
        public static Task<SubtagRegistry> CreateFromContentAsync(string subtagRegistryContent)
        {
            return CreateFromContentAsync(subtagRegistryContent, CancellationToken.None);
        }

        /// <summary>
        /// Load the Subtag Registry from a string.
        /// </summary>
        /// <param name="subtagRegistryContent">Content of the Subtag Registry.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The subtag registry content is <c>null</c>.</exception>
        /// <exception cref="OperationCanceledException">The cancellation token was cancelled.</exception>
        public static async Task<SubtagRegistry> CreateFromContentAsync(string subtagRegistryContent, CancellationToken cancellationToken)
        {
            if (subtagRegistryContent is null)
            {
                throw new ArgumentNullException(nameof(subtagRegistryContent));
            }

            using (var contentReader = new StringReader(subtagRegistryContent))
            {
                return await ReadAsync(contentReader, cancellationToken).ConfigureAwait(false);
            }
        }

#if NETSTANDARD1_3_OR_GREATER||NET
        /// <summary>
        /// Load the Subtag Registry from a file.
        /// </summary>
        /// <param name="subtagRegistryFilePath">Path to the Subtag Registry file.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The path to the subtag registry is <c>null</c>.</exception>
        public static SubtagRegistry CreateFromFile(string subtagRegistryFilePath)
        {
            if (subtagRegistryFilePath is null)
            {
                throw new ArgumentNullException(nameof(subtagRegistryFilePath));
            }

            var records = new List<ISubtagRegistryRecord>();

            using (var fileStream = new FileStream(subtagRegistryFilePath, FileMode.Open, FileAccess.Read))
            using (var fileReader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: StreamReaderBufferSize))
            {
                var registryReader = new SubtagRegistryReader(fileReader);

                var record = registryReader.ReadRecord();
                while (record != null)
                {
                    records.Add(record);

                    record = registryReader.ReadRecord();
                }
            }

            return new SubtagRegistry(records.ToArray());
        }

        /// <summary>
        /// Load the Subtag Registry from a file.
        /// </summary>
        /// <param name="subtagRegistryFilePath">Path to the Subtag Registry file.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The path to the subtag registry is <c>null</c>.</exception>
        public static Task<SubtagRegistry> CreateFromFileAsync(string subtagRegistryFilePath)
        {
            return CreateFromFileAsync(subtagRegistryFilePath, CancellationToken.None);
        }

        /// <summary>
        /// Load the Subtag Registry from a file.
        /// </summary>
        /// <param name="subtagRegistryFilePath">Path to the Subtag Registry file.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The path to the subtag registry is <c>null</c>.</exception>
        /// <exception cref="OperationCanceledException">The cancellation token was cancelled.</exception>
        public static async Task<SubtagRegistry> CreateFromFileAsync(string subtagRegistryFilePath, CancellationToken cancellationToken)
        {
            if (subtagRegistryFilePath is null)
            {
                throw new ArgumentNullException(nameof(subtagRegistryFilePath));
            }

            using (var fileStream = new FileStream(subtagRegistryFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileStreamBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var fileReader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: StreamReaderBufferSize))
            {
                return await ReadAsync(fileReader, cancellationToken).ConfigureAwait(false);
            }
        }
#endif
        /// <summary>
        /// Load the Subtag Registry from a stream.
        /// </summary>
        /// <param name="stream">Stream pointing to the Subtag Registry content.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The stream was <c>null</c>.</exception>
        /// <remarks>
        /// The content is read from the stream's current position. The stream is left open; the caller remains responsible for disposing it.
        /// </remarks>
        public static SubtagRegistry CreateFromStream(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var records = new List<ISubtagRegistryRecord>();

            using (var streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: StreamReaderBufferSize, leaveOpen: true))
            {
                var registryReader = new SubtagRegistryReader(streamReader);

                var record = registryReader.ReadRecord();
                while (record != null)
                {
                    records.Add(record);

                    record = registryReader.ReadRecord();
                }
            }

            return new SubtagRegistry(records.ToArray());
        }

        /// <summary>
        /// Load the Subtag Registry from a stream.
        /// </summary>
        /// <param name="stream">Stream pointing to the Subtag Registry content.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The stream was <c>null</c>.</exception>
        /// <remarks>
        /// The content is read from the stream's current position. The stream is left open; the caller remains responsible for disposing it.
        /// </remarks>
        public static Task<SubtagRegistry> CreateFromStreamAsync(Stream stream)
        {
            return CreateFromStreamAsync(stream, CancellationToken.None);
        }

        /// <summary>
        /// Load the Subtag Registry from a stream.
        /// </summary>
        /// <param name="stream">Stream pointing to the Subtag Registry content.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The stream was <c>null</c>.</exception>
        /// <exception cref="OperationCanceledException">The cancellation token was cancelled.</exception>
        /// <remarks>
        /// The content is read from the stream's current position. The stream is left open; the caller remains responsible for disposing it.
        /// </remarks>
        public static async Task<SubtagRegistry> CreateFromStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: StreamReaderBufferSize, leaveOpen: true))
            {
                return await ReadAsync(streamReader, cancellationToken).ConfigureAwait(false);
            }
        }

        public override string ToString()
        {
            return string.Join(string.Concat(Environment.NewLine, "%%", Environment.NewLine), _records);
        }

        #endregion

        private static async Task<SubtagRegistry> ReadAsync(TextReader sourceReader, CancellationToken cancellationToken)
        {
            var records = new List<ISubtagRegistryRecord>();
            var registryReader = new SubtagRegistryReader(sourceReader);

            var record = await registryReader.ReadRecordAsync(cancellationToken).ConfigureAwait(false);
            while (record != null)
            {
                records.Add(record);

                record = await registryReader.ReadRecordAsync(cancellationToken).ConfigureAwait(false);
            }

            return new SubtagRegistry(records.ToArray());
        }

        #region Implementation of IEnumerable

        IEnumerator<ISubtagRegistryRecord> IEnumerable<ISubtagRegistryRecord>.GetEnumerator()
        {
            return _records.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _records.GetEnumerator();
        }

        #endregion

        #region Implementation of IReadOnlyCollection<out ISubtagRegistryRecord>

        public int Count => _records.Count;

        #endregion
    }
}