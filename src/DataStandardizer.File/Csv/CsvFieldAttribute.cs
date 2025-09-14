using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// Associate field information with a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CsvFieldAttribute : Attribute
    {
        public CsvFieldAttribute(int fieldIndex)
        {
            if (fieldIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldIndex), fieldIndex, "Index must not be negative.");
            }

            FieldIndex = fieldIndex;
        }

        public CsvFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }

        /// <summary>
        /// Gets or sets the constant value for the field.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public object? ConstantValue { get; set; }
#else
        [CanBeNull]
        public object ConstantValue { get; set; }
#endif
        /// <summary>
        /// Gets the index of the CSV field.
        /// </summary>
        public int? FieldIndex { get; }

        /// <summary>
        /// Gets the name of the CSV field.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FieldName { get; }
#else
        [CanBeNull]
        public string FieldName { get; }
#endif

        /// <summary>
        /// Gets or sets a flag indicating if the field is optional.
        /// </summary>
        public bool IsOptional { get; set; }
    }
}