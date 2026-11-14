#if !NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text;
#endif
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
    public sealed class CsvFieldContext<TModel> : IEquatable<CsvFieldContext<TModel>>
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
        /// index; <c>null</c> when the file has no such field.  When writing, it is the position of the field in the line written, whether the
        /// mapping names the field or gives its index.  It is <c>null</c> for a constant or generated value, which doesn't come from a field.
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
        /// <remarks>
        /// When reading, this is the name of the field that the mapping resolved to on the line.  When writing, it is the name of the field the
        /// line is written under, as a reader with the same mapper resolves it, so a field renamed with <see cref="CsvFieldMapping.UseFieldName"/>
        /// has the name it is renamed to.
        /// </remarks>
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
#if !NET5_0_OR_GREATER

        // The net10.0 build is a record, so the compiler gives it value equality and a ToString that lists the properties.  The members below
        // do the same on the other targets, so that contexts compare, hash and print the same way whatever the target.

        /// <summary>
        /// Indicates whether two contexts have equal values for every property.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public static bool operator ==(CsvFieldContext<TModel>? left, CsvFieldContext<TModel>? right) =>
#else
        public static bool operator ==(CsvFieldContext<TModel> left, CsvFieldContext<TModel> right) =>
#endif
            ReferenceEquals(left, right) || (left is object && left.Equals(right));

        /// <summary>
        /// Indicates whether two contexts differ in the value of any property.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER
        public static bool operator !=(CsvFieldContext<TModel>? left, CsvFieldContext<TModel>? right) => !(left == right);
#else
        public static bool operator !=(CsvFieldContext<TModel> left, CsvFieldContext<TModel> right) => !(left == right);
#endif

        /// <inheritdoc/>
#if NETSTANDARD2_1_OR_GREATER
        public override bool Equals(object? obj) => Equals(obj as CsvFieldContext<TModel>);
#else
        public override bool Equals(object obj) => Equals(obj as CsvFieldContext<TModel>);
#endif

        // The netstandard2.1 EqualityComparer<T> has no nullable annotations, so it warns for the null values it accepts.
#pragma warning disable CS8604
        /// <summary>
        /// Indicates whether this context has the same value as <paramref name="other"/> for every property.
        /// </summary>
        /// <param name="other">The context to compare with this one.</param>
        /// <returns><see langword="true"/> if every property of both contexts has an equal value; otherwise, <see langword="false"/>.</returns>
#if NETSTANDARD2_1_OR_GREATER
        public bool Equals(CsvFieldContext<TModel>? other) =>
#else
        public bool Equals(CsvFieldContext<TModel> other) =>
#endif
            ReferenceEquals(this, other)
            || (other is object
                && EqualityComparer<int?>.Default.Equals(FieldIndex, other.FieldIndex)
                && EqualityComparer<string>.Default.Equals(FieldName, other.FieldName)
                && EqualityComparer<CsvFileHeaderLine>.Default.Equals(HeaderLine, other.HeaderLine)
                && EqualityComparer<TModel>.Default.Equals(Model, other.Model)
                && EqualityComparer<ICsvFileOptions>.Default.Equals(Options, other.Options)
                && EqualityComparer<string>.Default.Equals(RawFieldValue, other.RawFieldValue));

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            // The same combination as the hash code the compiler generates for a record.
            const int Multiplier = -1521134295;

            unchecked
            {
                var hashCode = EqualityComparer<Type>.Default.GetHashCode(typeof(CsvFieldContext<TModel>));
                hashCode = (hashCode * Multiplier) + EqualityComparer<int?>.Default.GetHashCode(FieldIndex);
                hashCode = (hashCode * Multiplier) + EqualityComparer<string>.Default.GetHashCode(FieldName);
                hashCode = (hashCode * Multiplier) + EqualityComparer<CsvFileHeaderLine>.Default.GetHashCode(HeaderLine);
                hashCode = (hashCode * Multiplier) + EqualityComparer<TModel>.Default.GetHashCode(Model);
                hashCode = (hashCode * Multiplier) + EqualityComparer<ICsvFileOptions>.Default.GetHashCode(Options);
                return (hashCode * Multiplier) + EqualityComparer<string>.Default.GetHashCode(RawFieldValue);
            }
        }

#pragma warning restore CS8604

        /// <summary>
        /// Returns the name of the type and the value of each property, in the form that the <c>net10.0</c> build, a record, returns.
        /// </summary>
        public override string ToString() =>
            new StringBuilder("CsvFieldContext { ")
                .Append("FieldIndex = ").Append(FieldIndex.ToString())
                .Append(", FieldName = ").Append(FieldName)
                .Append(", HeaderLine = ").Append(HeaderLine)
                .Append(", Model = ").Append(Model)
                .Append(", Options = ").Append(Options)
                .Append(", RawFieldValue = ").Append(RawFieldValue)
                .Append(" }")
                .ToString();
#endif
    }
}