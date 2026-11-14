using System;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Declare a property as mapped to a CSV field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CsvFieldMappingAttribute : Attribute
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? _useFieldName;
#else
        [MaybeNull] private string _useFieldName;
#endif

        public CsvFieldMappingAttribute()
        {
            
        }
        public CsvFieldMappingAttribute(int fieldIndex)
        {
            if (fieldIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldIndex), fieldIndex, "Index must not be negative.");
            }

            FieldIndex = fieldIndex;
        }

        public CsvFieldMappingAttribute(string fieldName)
        {
            FieldName = fieldName;
        }

        /// <summary>
        /// Gets or sets the constant value for the field.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A reader converts the value to the type of the property.  A value of the property type is used as it is, and text is converted as a
        /// field's text is.  A number is converted to a property of another of the types <see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>,
        /// <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="float"/>,
        /// <see cref="double"/> and <see cref="decimal"/>, or a nullable one, if that type holds the number as it is written: <c>5</c> converts to a
        /// <see cref="long"/> or <see cref="byte"/>, and <c>2.0</c> to an <see cref="int"/>, but <c>-1</c> doesn't convert to a <see cref="byte"/>,
        /// nor <c>1.5</c> to an <see cref="int"/>.
        /// </para>
        /// <para>
        /// A <see cref="float"/> or <see cref="double"/> is taken to be written with the fewest significant digits its type converts to a
        /// <see cref="decimal"/> with, up to 7 or 15, so <c>0.1</c> and <c>0.1f</c> both convert to a <see cref="decimal"/>, <see cref="double"/> or
        /// <see cref="float"/> of 0.1.  A whole number keeps its exact value.  A value that needs more digits, such as <c>0.30000000000000004</c>,
        /// converts only to a type that holds it exactly.  An attribute can't hold a <see cref="decimal"/>, so give a decimal that a
        /// <see cref="double"/> can't hold as it is written with <see cref="ICsvFieldMappingConstantBuilder{T}.HasConstantValue"/> in a mapper.
        /// </para>
        /// <para>
        /// A value that the property's type can't hold makes the reader throw a <see cref="CsvFileException"/> for each line it reads.  A writer
        /// writes the value as it is given here.
        /// </para>
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public object? ConstantValue { get; set; }
#else
        [MaybeNull, AllowNull]
        public object ConstantValue { get; set; }
#endif
        /// <summary>
        /// Gets the index of the CSV field.
        /// </summary>
        public int? FieldIndex { get; }

        /// <summary>
        /// Gets the name of the CSV field.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? FieldName { get; }
#else
        [MaybeNull]
        public string FieldName { get; }
#endif

        /// <summary>
        /// Gets or sets a flag indicating if the field is optional.
        /// </summary>
        public bool IsOptional { get; set; }

        /// <summary>
        /// Gets or sets the name the field is known by once read, in place of its name in the file: its key on a line, the name a header handler is
        /// given for it, and the name other mappings find it by.  <c>null</c> if the field keeps the name it has in the file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// RFC 4180 §2 ¶3 doesn't require the names on a header line to be unique, but the fields of a line must have unique names.  Properties
        /// mapped by the same name take the first, second and so on field of that name on the header line, in the order the properties are
        /// declared, and each must give its field a name of its own with this property.  A property past the last field of that name takes the last
        /// one.  A field whose name is repeated and that no mapping renames makes the reader throw a <see cref="CsvFileException"/>.
        /// </para>
        /// <para>
        /// A field whose name isn't repeated can be renamed too.  Properties that share a field must give it the same name.  A writer writes the
        /// field of a property mapped by name under its name in the file, so a header line read and written again keeps the file's names, repeated
        /// as they are in the file.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">The value is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The value is empty or consists only of white space.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? UseFieldName
#else
        [MaybeNull]
        public string UseFieldName
#endif
        {
            get => _useFieldName;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Expected a field name.", nameof(value));
                }

                _useFieldName = value;
            }
        }
    }
}