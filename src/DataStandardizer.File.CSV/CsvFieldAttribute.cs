using System;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Configure the behaviour of a property when accessing a field on a CSV line.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CsvFieldAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CsvFieldAttribute"/> class.
        /// </summary>
        /// <param name="fieldName">The name of the field accessed by the property.</param>
        public CsvFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }

        /// <summary>
        /// Gets the name of the field accessed by the property.
        /// </summary>
        public string FieldName { get; }
    }
}