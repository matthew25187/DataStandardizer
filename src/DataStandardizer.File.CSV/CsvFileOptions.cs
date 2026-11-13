using System;
#if !NET5_0_OR_GREATER
using System.Collections.Generic;
#endif
using System.Globalization;
using System.Text;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    public interface ICsvFileOptions
    {
        /// <summary>
        /// Gets the <see cref="CsvFieldBadValue{TModel}"/> delegate that will be called when a bad value is found in a CSV field.
        /// </summary>
        /// <remarks>
        /// <c>TModel</c> must be the record line type of the reader.  Its constructor throws <see cref="System.ArgumentException"/> for a delegate of
        /// any other type.  A writer never calls the delegate and doesn't check it.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        Delegate? BadValueHandler { get; }
#else
        [MaybeNull]
        Delegate BadValueHandler { get; }
#endif
        /// <summary>
        /// Gets the culture to use for converting field values.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        CultureInfo? Culture { get; }
#else
        [MaybeNull]
        CultureInfo Culture { get; }
#endif
        /// <summary>
        /// Gets the character sequence for line breaks embedded in CSV field values.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        string? EmbeddedLineBreak { get; }
#else
        [MaybeNull]
        string EmbeddedLineBreak { get; }
#endif

        /// <summary>
        /// Gets the encoding for the CSV file.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        Encoding? Encoding { get; }
#else
        [MaybeNull]
        Encoding Encoding { get; }
#endif
        /// <summary>
        /// Gets the character used to separate fields on a CSV line.
        /// </summary>
        /// <remarks>
        /// A writer throws for a double quote, which can't be told apart from the quotes around an escaped field (RFC 4180§2¶5), or for the first
        /// character of <see cref="TerminatorLineBreak"/>, which can't be told apart from the end of a record.
        /// </remarks>
        char FieldDelimiterCharacter { get; }

        /// <summary>
        /// Gets the <see cref="CsvFileHeader"/> delegate that will be called to provide the field names, in place of any header line in the file.
        /// </summary>
        /// <remarks>
        /// A reader calls the delegate whatever <see cref="HeaderHandling"/> is, and passes it the header line read from the file, if any.
        /// The reader constructor throws <see cref="System.ArgumentException"/> for a delegate of any other type.  A writer never calls the delegate
        /// and doesn't check it.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        Delegate? HeaderHandler { get; }
#else
        Delegate HeaderHandler { get; }
#endif
        /// <summary>
        /// Gets the method for handling file header lines.
        /// </summary>
        /// <remarks>
        /// Applies to reading lines only.
        /// </remarks>
        CsvFileHeaderHandling HeaderHandling { get; }

        /// <summary>
        /// Gets the <see cref="CsvFieldCount{TModel}"/> delegate that will be called when a line has a field count inconsistent with other lines in the CSV file,
        /// or a line to be written has a field with a value that isn't one of the fields written.
        /// </summary>
        /// <remarks>
        /// The line the delegate is called for is skipped: a reader doesn't return it, and a writer writes nothing for it.  When the delegate isn't set,
        /// an inconsistent line throws <see cref="CsvFileException"/>.
        /// <para>
        /// <c>TModel</c> must be the record line type of the reader or writer.  Its constructor throws <see cref="System.ArgumentException"/> for a
        /// delegate of any other type.
        /// </para>
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        Delegate? InconsistentFieldCountHandler { get; }
#else
        [MaybeNull]
        Delegate InconsistentFieldCountHandler { get; }
#endif
        /// <summary>
        /// Gets the method for handling quoted field values.
        /// </summary>
        /// <remarks>
        /// Applies to writing lines only.  A writer throws for a value that <see cref="CsvFieldQuoteHandling"/> doesn't define.
        /// </remarks>
        CsvFieldQuoteHandling QuoteHandling { get; }

        /// <summary>
        /// Gets a flag indicating if blank fields at the end of a line are left out when it is written to a CSV file.
        /// </summary>
        /// <remarks>
        /// Applies to writing lines only.  Leaving out the trailing blank fields relaxes RFC 4180§2¶4, which says that each line should contain
        /// the same number of fields throughout the file.
        /// <para>
        /// A field is blank when the text written for it is empty.  A field whose value is whitespace is not blank.  The fields at the end of a record
        /// line are those last in the order they are written: header line order, else mapper order followed by the unmapped fields of the first
        /// record line, else the order of the first record line.  A record line whose fields are all blank is written with all of them, so that it
        /// isn't written as an empty line.
        /// </para>
        /// </remarks>
        bool SuppressTrailingBlankFields { get; }

        /// <summary>
        /// Gets the character sequence for line breaks terminating CSV lines.
        /// </summary>
        /// <remarks>
        /// It must have at least one character, or a reader and a writer throw.  A writer also throws for a sequence that starts with a double
        /// quote, which would read as an escaped quote after a quoted field (RFC 4180§2¶7).
        /// </remarks>
        string TerminatorLineBreak { get; }
    }

    /// <summary>
    /// Options to configure the behaviour of a CSV reader or writer.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 4180.
    /// </remarks>
#if NET5_0_OR_GREATER
    public sealed record CsvFileOptions : ICsvFileOptions
#else
    public sealed class CsvFileOptions : ICsvFileOptions, IEquatable<CsvFileOptions>
#endif
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER

        public Delegate? BadValueHandler
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }
#else
        public Delegate BadValueHandler { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public CultureInfo? Culture
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public CultureInfo Culture { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? EmbeddedLineBreak
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public string EmbeddedLineBreak { get; set; }
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Encoding? Encoding
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public Encoding Encoding { get; set; }
#endif
        public char FieldDelimiterCharacter
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = ',';


#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Delegate? HeaderHandler
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public Delegate HeaderHandler { get; set; }
#endif
        public CsvFileHeaderHandling HeaderHandling
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = CsvFileHeaderHandling.None;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Delegate? InconsistentFieldCountHandler
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public Delegate InconsistentFieldCountHandler { get; set; }
#endif

        public CsvFieldQuoteHandling QuoteHandling
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = CsvFieldQuoteHandling.Required;

        public bool SuppressTrailingBlankFields
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = false;

        public string TerminatorLineBreak
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = "\r\n";
#if !NET5_0_OR_GREATER

        // The net10.0 build is a record, so the compiler gives it value equality and a ToString that lists the properties.  The members below
        // do the same on the other targets, so that options compare, hash and print the same way whatever the target.

        /// <summary>
        /// Indicates whether two options objects have equal values for every property.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public static bool operator ==(CsvFileOptions? left, CsvFileOptions? right) =>
#else
        public static bool operator ==(CsvFileOptions left, CsvFileOptions right) =>
#endif
            ReferenceEquals(left, right) || (left is object && left.Equals(right));

        /// <summary>
        /// Indicates whether two options objects differ in the value of any property.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public static bool operator !=(CsvFileOptions? left, CsvFileOptions? right) => !(left == right);
#else
        public static bool operator !=(CsvFileOptions left, CsvFileOptions right) => !(left == right);
#endif

        /// <inheritdoc/>
#if NETSTANDARD2_1_OR_GREATER
        public override bool Equals(object? obj) => Equals(obj as CsvFileOptions);
#else
        public override bool Equals(object obj) => Equals(obj as CsvFileOptions);
#endif

        // The netstandard2.1 EqualityComparer<T> has no nullable annotations, so it warns for the null values it accepts.
#pragma warning disable CS8604
        /// <summary>
        /// Indicates whether this options object has the same value as <paramref name="other"/> for every property.
        /// </summary>
        /// <param name="other">The options object to compare with this one.</param>
        /// <returns><see langword="true"/> if every property of both objects has an equal value; otherwise, <see langword="false"/>.</returns>
#if NETSTANDARD2_1_OR_GREATER
        public bool Equals(CsvFileOptions? other) =>
#else
        public bool Equals(CsvFileOptions other) =>
#endif
            ReferenceEquals(this, other)
            || (other is object
                && EqualityComparer<Delegate>.Default.Equals(BadValueHandler, other.BadValueHandler)
                && EqualityComparer<CultureInfo>.Default.Equals(Culture, other.Culture)
                && EqualityComparer<string>.Default.Equals(EmbeddedLineBreak, other.EmbeddedLineBreak)
                && EqualityComparer<Encoding>.Default.Equals(Encoding, other.Encoding)
                && FieldDelimiterCharacter == other.FieldDelimiterCharacter
                && EqualityComparer<Delegate>.Default.Equals(HeaderHandler, other.HeaderHandler)
                && HeaderHandling == other.HeaderHandling
                && EqualityComparer<Delegate>.Default.Equals(InconsistentFieldCountHandler, other.InconsistentFieldCountHandler)
                && QuoteHandling == other.QuoteHandling
                && SuppressTrailingBlankFields == other.SuppressTrailingBlankFields
                && EqualityComparer<string>.Default.Equals(TerminatorLineBreak, other.TerminatorLineBreak));

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // The same combination as the hash code the compiler generates for a record.
            const int Multiplier = -1521134295;

            unchecked
            {
                var hashCode = EqualityComparer<Type>.Default.GetHashCode(typeof(CsvFileOptions));
                hashCode = (hashCode * Multiplier) + EqualityComparer<Delegate>.Default.GetHashCode(BadValueHandler);
                hashCode = (hashCode * Multiplier) + EqualityComparer<CultureInfo>.Default.GetHashCode(Culture);
                hashCode = (hashCode * Multiplier) + EqualityComparer<string>.Default.GetHashCode(EmbeddedLineBreak);
                hashCode = (hashCode * Multiplier) + EqualityComparer<Encoding>.Default.GetHashCode(Encoding);
                hashCode = (hashCode * Multiplier) + EqualityComparer<char>.Default.GetHashCode(FieldDelimiterCharacter);
                hashCode = (hashCode * Multiplier) + EqualityComparer<Delegate>.Default.GetHashCode(HeaderHandler);
                hashCode = (hashCode * Multiplier) + EqualityComparer<CsvFileHeaderHandling>.Default.GetHashCode(HeaderHandling);
                hashCode = (hashCode * Multiplier) + EqualityComparer<Delegate>.Default.GetHashCode(InconsistentFieldCountHandler);
                hashCode = (hashCode * Multiplier) + EqualityComparer<CsvFieldQuoteHandling>.Default.GetHashCode(QuoteHandling);
                hashCode = (hashCode * Multiplier) + EqualityComparer<bool>.Default.GetHashCode(SuppressTrailingBlankFields);
                return (hashCode * Multiplier) + EqualityComparer<string>.Default.GetHashCode(TerminatorLineBreak);
            }
        }

#pragma warning restore CS8604

        /// <summary>
        /// Returns the name of the type and the value of each property, in the form that the <c>net10.0</c> build, a record, returns.
        /// </summary>
        public override string ToString() =>
            new StringBuilder("CsvFileOptions { ")
                .Append("BadValueHandler = ").Append(BadValueHandler)
                .Append(", Culture = ").Append(Culture)
                .Append(", EmbeddedLineBreak = ").Append(EmbeddedLineBreak)
                .Append(", Encoding = ").Append(Encoding)
                .Append(", FieldDelimiterCharacter = ").Append(FieldDelimiterCharacter)
                .Append(", HeaderHandler = ").Append(HeaderHandler)
                .Append(", HeaderHandling = ").Append(HeaderHandling.ToString())
                .Append(", InconsistentFieldCountHandler = ").Append(InconsistentFieldCountHandler)
                .Append(", QuoteHandling = ").Append(QuoteHandling.ToString())
                .Append(", SuppressTrailingBlankFields = ").Append(SuppressTrailingBlankFields.ToString())
                .Append(", TerminatorLineBreak = ").Append(TerminatorLineBreak)
                .Append(" }")
                .ToString();
#endif
    }
}