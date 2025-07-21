using System;

namespace DataStandardizer.BCP47.SubtagRegistry
{
    /// <summary>
    /// Describes a field on a Subtag Registry record.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SubtagRegistryFieldAttribute : Attribute
    {
        public SubtagRegistryFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }

        /// <summary>
        /// Gets the name of the field.
        /// </summary>
        public string FieldName { get; }
    }
}