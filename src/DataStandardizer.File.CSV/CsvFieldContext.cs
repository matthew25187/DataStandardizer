#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
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
        /// <remarks>
        /// When reading, this is the position of the field in the record as read from the file, whether the mapping names the field or gives its
        /// index; <c>null</c> when the file has no such field.  When writing, it is the index configured on the mapping.
        /// </remarks>
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
        /// Gets the name of the CSV field.
        /// </summary>
        /// <remarks>When reading, this is the name of the field that the mapping resolved to on the line.</remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
        [MaybeNull, AllowNull]
        public string FieldName { get; internal set; }
#endif

        /// <summary>
        /// Gets the header line from the CSV file.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
        [MaybeNull, AllowNull]
        public CsvFileHeaderLine HeaderLine { get; internal set; }
#endif

        /// <summary>
        /// Gets the record line from the CSV file.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
        [MaybeNull, AllowNull]
        public TModel Model { get; internal set; }
#endif
        /// <summary>
        /// Gets the options used to configure the reader or writer.
        /// </summary>
        public ICsvFileOptions Options { get; }

        /// <summary>
        /// Gets the raw value of the CSV field.
        /// </summary>
        /// <remarks>
        /// A validator is given the field's text as read from the file, and a converter the text it converts.  <c>null</c> when there is no such
        /// text.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
        [MaybeNull, AllowNull]
        public string RawFieldValue { get; internal set; }
#endif
    }
}