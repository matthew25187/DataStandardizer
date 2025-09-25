using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// Collection of records that comprise the Subtag Registry.
    /// </summary>
    public class SubtagRegistry : IReadOnlyCollection<ISubtagRegistryRecord>
    {
        private readonly IReadOnlyCollection<ISubtagRegistryRecord> _records;

        private SubtagRegistry(ISubtagRegistryRecord[] records)
        {
            _records = records;
        }

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
        public static async Task<SubtagRegistry> CreateFromContentAsync(string subtagRegistryContent)
        {
            if (subtagRegistryContent is null)
            {
                throw new ArgumentNullException(nameof(subtagRegistryContent));
            }

            var records = new List<ISubtagRegistryRecord>();

            using (var contentReader = new StringReader(subtagRegistryContent))
            {
                var registryReader = new SubtagRegistryReader(contentReader);

                var record = await registryReader.ReadRecordAsync();
                while (record != null)
                {
                    records.Add(record);

                    record = await registryReader.ReadRecordAsync();
                }
            }

            return new SubtagRegistry(records.ToArray());
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
            using (var fileReader = new StreamReader(fileStream, Encoding.UTF8))
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
        public static async Task<SubtagRegistry> CreateFromFileAsync(string subtagRegistryFilePath)
        {
            if (subtagRegistryFilePath is null)
            {
                throw new ArgumentNullException(nameof(subtagRegistryFilePath));
            }

            var records = new List<ISubtagRegistryRecord>();

            using (var fileStream = new FileStream(subtagRegistryFilePath, FileMode.Open, FileAccess.Read))
            using (var fileReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                var registryReader = new SubtagRegistryReader(fileReader);

                var record = await registryReader.ReadRecordAsync();
                while (record != null)
                {
                    records.Add(record);

                    record = await registryReader.ReadRecordAsync();
                }
            }

            return new SubtagRegistry(records.ToArray());
        }
#endif
        /// <summary>
        /// Load the Subtag Registry from a stream.
        /// </summary>
        /// <param name="stream">Stream pointing to the Subtag Registry content.</param>
        /// <returns>A copy of the registry.</returns>
        /// <exception cref="ArgumentNullException">The stream was <c>null</c>.</exception>
        public static SubtagRegistry CreateFromStream(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var records = new List<ISubtagRegistryRecord>();

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
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
        public static async Task<SubtagRegistry> CreateFromStreamAsync(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var records = new List<ISubtagRegistryRecord>();

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            {
                var registryReader = new SubtagRegistryReader(streamReader);

                var record = await registryReader.ReadRecordAsync();
                while (record != null)
                {
                    records.Add(record);

                    record = await registryReader.ReadRecordAsync();
                }
            }

            return new SubtagRegistry(records.ToArray());
        }

        public override string ToString()
        {
            return string.Join(string.Concat(Environment.NewLine, "%%", Environment.NewLine), _records);
        }

        #endregion

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