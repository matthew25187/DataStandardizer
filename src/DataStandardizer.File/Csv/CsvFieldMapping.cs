using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.Csv
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
#if NETCOREAPP3_0_OR_GREATER
        public string? FieldName { get; internal set; }
#else
        [CanBeNull]
        public string FieldName { get; internal set; }
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
#if NETCOREAPP3_0_OR_GREATER
        public object? ConstantValue { get; internal set; }
#else
        [CanBeNull]
        public object ConstantValue { get; internal set; }
#endif

        /// <summary>
        /// Gets or sets a delegate for deserializing a CSV field value.
        /// </summary>
        /// <remarks>
        /// Applies to reading lines only.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public Delegate? FromStringConverter { get; internal set; }
#else
        [CanBeNull]
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
#if NETCOREAPP3_0_OR_GREATER
        public Delegate? ToStringConverter { get; internal set; }
#else
        [CanBeNull]
        public Delegate ToStringConverter { get; internal set; }
#endif
        /// <summary>
        /// Gets or sets the type of the type converter to use for serializing and deserializing.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public Type? TypeConverterType { get; internal set; }
#else
        [CanBeNull]
        public Type TypeConverterType { get; internal set; }
#endif
        /// <summary>
        /// Gets or sets a delegate for validating incoming CSV field values.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public Delegate? Validator { get; internal set; }
#else
        [CanBeNull]
        public Delegate Validator { get; internal set; }
#endif

        /// <summary>
        /// Gets or sets a delegate for generating field values.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public Delegate? VariableValueGenerator { get; internal set; }
#else
        [CanBeNull]
        public Delegate VariableValueGenerator { get; internal set; }
#endif
    }
}