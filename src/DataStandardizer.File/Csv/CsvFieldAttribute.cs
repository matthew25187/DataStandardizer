using System;

namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// Configure the behaviour of a property when accessing a field on a CSV line.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CsvFieldAttribute : Attribute
    {
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