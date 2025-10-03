#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Context for a CSV field being processed.
    /// </summary>
    /// <typeparam name="TModel">Type of the CSV line model.</typeparam>
#if NET5_0_OR_GREATER
    public sealed record CsvFieldContext<TModel>
#else
    public sealed class CsvFieldContext<TModel>
#endif
        where TModel : class
    {
        internal CsvFieldContext(ICsvFileOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Gets the index of the CSV field.
        /// </summary>
        public int? FieldIndex
        {
            get;
#if NET5_0_OR_GREATER
            internal init;
#else
            internal set;
#endif
        }

        /// <summary>
        /// Gets the name of rhe CSV field.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FieldName
        {
            get;
#if NET5_0_OR_GREATER
            internal init;
#else
internal set;
#endif
        }
#else
        [CanBeNull]
        public string FieldName { get; internal set; }
#endif

        /// <summary>
        /// Gets the header line from the CSV file.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public CsvFileHeaderLine? HeaderLine
        {
            get;
#if NET5_0_OR_GREATER
            internal init;
#else
internal set;
#endif
        }
#else
        [CanBeNull]
        public CsvFileHeaderLine HeaderLine { get; internal set; }
#endif

        /// <summary>
        /// Gets the record line from the CSV file.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public TModel? Model
        {
            get;
#if NET5_0_OR_GREATER
            internal init;
#else
internal set;
#endif
        }
#else
        [CanBeNull]
        public TModel Model { get; internal set; }
#endif
        /// <summary>
        /// Gets the options used to configure the reader or writer.
        /// </summary>
        public ICsvFileOptions Options { get; }

        /// <summary>
        /// Gets the raw value of the CSV field.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? RawFieldValue
        {
            get;
#if NET5_0_OR_GREATER
            internal init;
#else
internal set;
#endif
        }
#else
        [CanBeNull]
        public string RawFieldValue { get; internal set; }
#endif
    }
}