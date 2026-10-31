using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataStandardizer.LanguageTag.InternalState;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// Collection of records that comprise the Subtag Registry.
    /// </summary>
    /// <remarks>
    /// A registry and its records never change once loaded, so one instance may be used from several threads.
    /// </remarks>
    public class SubtagRegistry : IReadOnlyCollection<ISubtagRegistryRecord>
    {
        private const int StreamReaderBufferSize = 16384;

        /// <summary>
        /// The <see cref="FileStream"/> default, given explicitly by the constructor that takes <see cref="FileOptions"/>.
        /// </summary>
        private const int FileStreamBufferSize = 4096;

        private readonly ISubtagRegistryRecord[] _records;

        /// <summary>
        /// Sets of the subtags and grandfathered tags in <see cref="_records"/>, built once, on first use, and shared by every thread.
        /// </summary>
        private readonly Lazy<Bcp47RegistryBasedSubtagClassifier> _classifier;

        /// <summary>
        /// Maps of the subtags in <see cref="_records"/> to their records, built once, on first lookup, and shared by every thread.
        /// </summary>
        private readonly Lazy<Bcp47RegistrySubtagIndex> _subtagIndex;

        private SubtagRegistry(ISubtagRegistryRecord[] records)
        {
            _records = records;
            _classifier = new Lazy<Bcp47RegistryBasedSubtagClassifier>(() => new Bcp47RegistryBasedSubtagClassifier(this));
            _subtagIndex = new Lazy<Bcp47RegistrySubtagIndex>(() => new Bcp47RegistrySubtagIndex(this));

            var languages = new List<SubtagRegistrySubtagRecord>();
            var extendedLanguages = new List<SubtagRegistrySubtagRecord>();
            var scripts = new List<SubtagRegistrySubtagRecord>();
            var regions = new List<SubtagRegistrySubtagRecord>();
            var variants = new List<SubtagRegistrySubtagRecord>();
            var grandfatheredTags = new List<SubtagRegistryTagRecord>();
            var redundantTags = new List<SubtagRegistryTagRecord>();

            foreach (var record in records)
            {
                if (record is SubtagRegistrySubtagRecord subtagRecord)
                {
                    switch (subtagRecord.Type)
                    {
                        case SubtagRegistryRecordType.Language:
                            languages.Add(subtagRecord);
                            break;
                        case SubtagRegistryRecordType.ExtendedLanguage:
                            extendedLanguages.Add(subtagRecord);
                            break;
                        case SubtagRegistryRecordType.Script:
                            scripts.Add(subtagRecord);
                            break;
                        case SubtagRegistryRecordType.Region:
                            regions.Add(subtagRecord);
                            break;
                        case SubtagRegistryRecordType.Variant:
                            variants.Add(subtagRecord);
                            break;
                    }
                }
                else if (record is SubtagRegistryTagRecord tagRecord)
                {
                    switch (tagRecord.Type)
                    {
                        case SubtagRegistryRecordType.Grandfathered:
                            grandfatheredTags.Add(tagRecord);
                            break;
                        case SubtagRegistryRecordType.Redundant:
                            redundantTags.Add(tagRecord);
                            break;
                    }
                }
                else if (record is SubtagRegistryFileDateRecord fileDateRecord && FileDate is null)
                {
                    FileDate = fileDateRecord.FileDate;
                }
            }

            Languages = new ReadOnlyCollection<SubtagRegistrySubtagRecord>(languages);
            ExtendedLanguages = new ReadOnlyCollection<SubtagRegistrySubtagRecord>(extendedLanguages);
            Scripts = new ReadOnlyCollection<SubtagRegistrySubtagRecord>(scripts);
            Regions = new ReadOnlyCollection<SubtagRegistrySubtagRecord>(regions);
            Variants = new ReadOnlyCollection<SubtagRegistrySubtagRecord>(variants);
            GrandfatheredTags = new ReadOnlyCollection<SubtagRegistryTagRecord>(grandfatheredTags);
            RedundantTags = new ReadOnlyCollection<SubtagRegistryTagRecord>(redundantTags);
        }

        /// <summary>
        /// Gets the classifier that checks language tags against this registry.
        /// </summary>
        internal Bcp47SubtagClassifier Classifier => _classifier.Value;

        #region Public Properties

        /// <summary>
        /// Gets the date of the registry's File-Date record, or <see langword="null"/> when it has none.
        /// </summary>
        public DateTime? FileDate { get; }

        /// <summary>
        /// Gets the records of Primary Language subtags (Type <c>language</c>), in registry order.
        /// </summary>
        public IReadOnlyList<SubtagRegistrySubtagRecord> Languages { get; }

        /// <summary>
        /// Gets the records of Extended Language subtags (Type <c>extlang</c>), in registry order.
        /// </summary>
        public IReadOnlyList<SubtagRegistrySubtagRecord> ExtendedLanguages { get; }

        /// <summary>
        /// Gets the records of Script subtags (Type <c>script</c>), in registry order.
        /// </summary>
        public IReadOnlyList<SubtagRegistrySubtagRecord> Scripts { get; }

        /// <summary>
        /// Gets the records of Region subtags (Type <c>region</c>), in registry order.
        /// </summary>
        public IReadOnlyList<SubtagRegistrySubtagRecord> Regions { get; }

        /// <summary>
        /// Gets the records of Variant subtags (Type <c>variant</c>), in registry order.
        /// </summary>
        public IReadOnlyList<SubtagRegistrySubtagRecord> Variants { get; }

        /// <summary>
        /// Gets the records of grandfathered tags (Type <c>grandfathered</c>), in registry order.
        /// </summary>
        public IReadOnlyList<SubtagRegistryTagRecord> GrandfatheredTags { get; }

        /// <summary>
        /// Gets the records of redundant tags (Type <c>redundant</c>), in registry order.
        /// </summary>
        public IReadOnlyList<SubtagRegistryTagRecord> RedundantTags { get; }

        #endregion

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

        /// <summary>
        /// Gets the record of a subtag.
        /// </summary>
        /// <param name="type">Type of the subtag: <see cref="SubtagRegistryRecordType.Language"/>, <see cref="SubtagRegistryRecordType.ExtendedLanguage"/>,
        /// <see cref="SubtagRegistryRecordType.Script"/>, <see cref="SubtagRegistryRecordType.Region"/> or <see cref="SubtagRegistryRecordType.Variant"/>.</param>
        /// <param name="subtag">Subtag to look up, in any case.</param>
        /// <param name="record">When this method returns <see langword="true"/>, the record of the subtag; otherwise, <see langword="null"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the registry has a record of that type for the subtag; otherwise, <see langword="false"/>, including for a
        /// <see langword="null"/> subtag and for any other type.
        /// </returns>
        /// <remarks>
        /// Subtags are compared case-insensitively (ref. RFC 5646 §2.1.1). A subtag in a range that a record denotes, such as <c>qab</c> in <c>qaa..qtz</c>,
        /// finds that record. The first lookup builds a map of the registry's subtags to their records, which every later lookup uses.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public bool TryGetSubtag(SubtagRegistryRecordType type, string? subtag, [NotNullWhen(true)] out SubtagRegistrySubtagRecord? record)
#else
        public bool TryGetSubtag(SubtagRegistryRecordType type, [CanBeNull] string subtag, [CanBeNull] out SubtagRegistrySubtagRecord record)
#endif
        {
            if (subtag is null)
            {
                record = null;
                return false;
            }

            return _subtagIndex.Value.TryGetSubtag(type, subtag.AsSpan(), out record);
        }

        /// <inheritdoc cref="TryGetSubtag(SubtagRegistryRecordType, string, out SubtagRegistrySubtagRecord)"/>
#if NETCOREAPP3_0_OR_GREATER
        public bool TryGetSubtag(SubtagRegistryRecordType type, ReadOnlySpan<char> subtag, [NotNullWhen(true)] out SubtagRegistrySubtagRecord? record)
#else
        public bool TryGetSubtag(SubtagRegistryRecordType type, ReadOnlySpan<char> subtag, [CanBeNull] out SubtagRegistrySubtagRecord record)
#endif
        {
            return _subtagIndex.Value.TryGetSubtag(type, subtag, out record);
        }

        /// <summary>
        /// Determine whether a language tag is one of the registry's grandfathered tags.
        /// </summary>
        /// <param name="languageTag">Language tag to check, in any case.</param>
        /// <returns>
        /// <see langword="true"/> if the registry has a record of type <c>grandfathered</c> for the tag; otherwise, <see langword="false"/>, including for
        /// <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// Tags are compared case-insensitively (ref. RFC 5646 §2.1.1). This uses the index that validation against the registry builds on first use, and builds it if
        /// it hasn't been built yet.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public bool IsGrandfathered(string? languageTag)
#else
        public bool IsGrandfathered([CanBeNull] string languageTag)
#endif
        {
            return languageTag != null && _classifier.Value.IsGrandfathered(languageTag.AsSpan());
        }

        /// <inheritdoc cref="IsGrandfathered(string)"/>
        public bool IsGrandfathered(ReadOnlySpan<char> languageTag)
        {
            return _classifier.Value.IsGrandfathered(languageTag);
        }

        public override string ToString()
        {
            return string.Join(string.Concat(Environment.NewLine, "%%", Environment.NewLine), (IEnumerable<ISubtagRegistryRecord>)_records);
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
            return ((IEnumerable<ISubtagRegistryRecord>)_records).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _records.GetEnumerator();
        }

        #endregion

        #region Implementation of IReadOnlyCollection<out ISubtagRegistryRecord>

        public int Count => _records.Length;

        #endregion
    }
}