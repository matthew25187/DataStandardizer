using System;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Details of the mapping between a CSV field and a model property.
    /// </summary>
    public sealed class CsvFieldMapping
    {
        private int? _fieldIndex;

        internal CsvFieldMapping(Type propertyType)
        {
            PropertyType = propertyType;
        }

        /// <summary>
        /// Gets or sets the name of the CSV field.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? FieldName { get; internal set; }
#else
        [MaybeNull, AllowNull]
        public string FieldName { get; internal set; }
#endif

        /// <summary>
        /// Gets the name the field is known by once read: its key on a line, the name a header handler is given for it, and the name other
        /// mappings find it by.  <c>null</c> if the field keeps the name it has in the file.
        /// </summary>
        /// <remarks>
        /// A writer writes a field that a mapping by name renames under its name in the file, so a header line read and written again keeps the
        /// file's names.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? UseFieldName { get; internal set; }
#else
        [MaybeNull, AllowNull]
        public string UseFieldName { get; internal set; }
#endif

        /// <summary>
        /// Gets or sets the index of the CSV field.
        /// </summary>
        public int? FieldIndex
        {
            get => _fieldIndex;
            internal set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Index must not be negative.");
                _fieldIndex = value;
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating if the field is optional.
        /// </summary>
        public bool IsOptional { get; internal set; }

        /// <summary>
        /// Gets or sets a constant value for the field.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public object? ConstantValue { get; internal set; }
#else
        [MaybeNull, AllowNull]
        public object ConstantValue { get; internal set; }
#endif

        /// <summary>
        /// Gets or sets a delegate for deserializing a CSV field value.
        /// </summary>
        /// <remarks>
        /// Applies to reading lines only.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Delegate? FromStringConverter { get; internal set; }
#else
        [MaybeNull, AllowNull]
        public Delegate FromStringConverter { get; internal set; }
#endif
        /// <summary>
        /// Gets or sets the type of the property representing the field on a model object.
        /// </summary>
        public Type PropertyType { get; internal set; }

        /// <summary>
        /// Gets or sets a delegate for serializing a CSV field value.
        /// </summary>
        /// <remarks>
        /// Applies to writing lines only.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Delegate? ToStringConverter { get; internal set; }
#else
        [MaybeNull, AllowNull]
        public Delegate ToStringConverter { get; internal set; }
#endif
        /// <summary>
        /// Gets or sets the type of the type converter to use for serializing and deserializing.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Type? TypeConverterType { get; internal set; }
#else
        [MaybeNull, AllowNull]
        public Type TypeConverterType { get; internal set; }
#endif
        /// <summary>
        /// Gets or sets a delegate for validating incoming CSV field values.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Delegate? Validator { get; internal set; }
#else
        [MaybeNull, AllowNull]
        public Delegate Validator { get; internal set; }
#endif

        /// <summary>
        /// Gets or sets a delegate for generating field values.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Delegate? VariableValueGenerator { get; internal set; }
#else
        [MaybeNull, AllowNull]
        public Delegate VariableValueGenerator { get; internal set; }
#endif

        /// <summary>
        /// Gets a flag indicating if the mapping supplies a constant or generated value, rather than taking it from a field or record line.
        /// </summary>
        internal bool SuppliesValue => ConstantValue != null || VariableValueGenerator != null;
    }
}