using System;
using System.ComponentModel;
using System.Reflection;

namespace DataStandardizer.File.CSV
{
    public interface ICsvFieldMappingInitialBuilder<TModel, in T>
        : ICsvFieldMappingIdentityBuilder<TModel, T>, ICsvFieldMappingConstantBuilder<T>, ICsvFieldMappingVariableBuilder<T>, ICsvFieldMappingTransformationBuilder<TModel, T>, ICsvFieldMappingOptionalBuilder
        where TModel : class
    {
    }

    public interface ICsvFieldMappingConstantBuilder<in T>
    {
        /// <summary>
        /// Specify the constant value this field will have.
        /// </summary>
        /// <param name="value">Constant field value.</param>
        void HasConstantValue(T value);
    }

    public interface ICsvFieldMappingIdentityBuilder<TModel, in T> where TModel : class
    {
        /// <summary>
        /// Specify the index of the CSV field to which the property is mapped.
        /// </summary>
        /// <param name="fieldIndex">Index of the CSV field.</param>
        /// <returns>This mapping builder.</returns>
        ICsvFieldMappingIdentityNextBuilder<TModel, T> HasFieldIndex(int fieldIndex);

        /// <summary>
        /// Specify the name of the CSV field to which the property is mapped.
        /// </summary>
        /// <param name="fieldName">Name of the CSV field.</param>
        /// <returns>This mapping builder.</returns>
        ICsvFieldMappingIdentityNextBuilder<TModel, T> HasFieldName(string fieldName);
    }

    public interface ICsvFieldMappingIdentityNextBuilder<TModel, in T>
        : ICsvFieldMappingLogicalNameBuilder<TModel, T>, ICsvFieldMappingConstantBuilder<T>, ICsvFieldMappingTransformationBuilder<TModel, T>,
            ICsvFieldMappingVariableBuilder<T>, ICsvFieldMappingOptionalBuilder where TModel : class
    {
    }

    public interface ICsvFieldMappingLogicalNameBuilder<TModel, in T> where TModel : class
    {
        /// <summary>
        /// Specify the name the field is known by once read, in place of its name in the file: its key on a line, the name a header handler is
        /// given for it, and the name other mappings find it by.
        /// </summary>
        /// <param name="fieldName">The field's name once read.</param>
        /// <returns>This mapping builder.</returns>
        /// <remarks>
        /// <para>
        /// RFC 4180 §2 ¶3 doesn't require the names on a header line to be unique, but the fields of a line must have unique names.  Mappings by
        /// the same name take the first, second and so on field of that name on the header line, in mapper order, and each must give its field a
        /// name of its own with this method.  A mapping that gives the same name as an earlier one shares its field, a field that a mapping by
        /// index takes is left to it, and a mapping past the last field of that name takes the last one.  A field whose name is repeated and
        /// that no mapping renames makes the reader throw a <see cref="CsvFileException"/>.
        /// </para>
        /// <para>
        /// A field whose name isn't repeated can be renamed too.  Mappings that share a field must give it the same name, or all keep its name.  A
        /// writer that would write a header line the mapper can't read back with the same names throws a <see cref="CsvFileException"/>.  It writes the field
        /// of a mapping by name under the name the mapping gives with <see cref="ICsvFieldMappingIdentityBuilder{TModel, T}.HasFieldName"/>, so a header line
        /// read and written again keeps the file's names, repeated as they are in the file.
        /// </para>
        /// </remarks>
        ICsvFieldMappingLogicalNameNextBuilder<TModel, T> UseFieldName(string fieldName);
    }

    public interface ICsvFieldMappingLogicalNameNextBuilder<TModel, in T>
        : ICsvFieldMappingConstantBuilder<T>, ICsvFieldMappingTransformationBuilder<TModel, T>, ICsvFieldMappingVariableBuilder<T>, ICsvFieldMappingOptionalBuilder
        where TModel : class
    {
    }

    public interface ICsvFieldMappingOptionalBuilder
    {
        /// <summary>
        /// Specify that the CSV field is optional.  If not found in the CSV file, the property will not be set.
        /// </summary>
        void IsOptional();
    }

    public interface ICsvFieldMappingTransformationBuilder<TModel, in T> where TModel : class
    {
        /// <summary>
        /// Specify a conversion delegate for deserializing a CSV field value.
        /// </summary>
        /// <param name="converter">Value conversion delegate.</param>
        /// <returns>This mapping builder.</returns>
        ICsvFieldMappingTransformationNextBuilder<TModel> ConvertUsing(CsvFieldConvertFromString<TModel, T> converter);

        /// <summary>
        /// Specify a conversion delegate for serializing a CSV field value.
        /// </summary>
        /// <param name="converter">Value conversion delegate.</param>
        /// <returns>This mapping builder.</returns>
        ICsvFieldMappingTransformationNextBuilder<TModel> ConvertUsing(CsvFieldConvertToString<TModel> converter);

        /// <summary>
        /// Specify a type converter for serializing or deserializing a CSV field value.
        /// </summary>
        /// <param name="typeConverterType"><see cref="Type"/> of the type converter.</param>
        /// <returns>This mapping builder.</returns>
        ICsvFieldMappingTransformationNextBuilder<TModel> ConvertUsing(Type typeConverterType);

        /// <summary>
        /// Specify a type converter for serializing or deserializing a CSV field value.
        /// </summary>
        /// <typeparam name="TConverter"></typeparam>
        /// <returns></returns>
        ICsvFieldMappingTransformationNextBuilder<TModel> ConvertUsing<TConverter>() where TConverter : TypeConverter;
    }

    public interface ICsvFieldMappingTransformationNextBuilder<TModel>
        : ICsvFieldMappingValidationBuilder<TModel>, ICsvFieldMappingOptionalBuilder where TModel : class
    {
    }

    public interface ICsvFieldMappingValidationBuilder<TModel> where TModel : class
    {
        /// <summary>
        /// Specify a delegate for validating incoming CSV field values.
        /// </summary>
        /// <param name="validator">Value validation delegate.</param>
        /// <returns>This mapping builder.</returns>
        ICsvFieldMappingValidationNextBuilder ValidateUsing(CsvFieldValidate<TModel> validator);
    }

    public interface ICsvFieldMappingValidationNextBuilder : ICsvFieldMappingOptionalBuilder
    {
    }

    public interface ICsvFieldMappingVariableBuilder<in T>
    {
        /// <summary>
        /// Specify a delegate for generating values.
        /// </summary>
        /// <param name="valueGenerator">Value generation delegate.</param>
        void HasVariableValue(CsvFieldGenerate<T> valueGenerator);
    }

    public sealed class CsvFieldMappingBuilder<TModel, T> : ICsvFieldMappingInitialBuilder<TModel,T>, ICsvFieldMappingIdentityNextBuilder<TModel, T>,
        ICsvFieldMappingLogicalNameNextBuilder<TModel, T>, ICsvFieldMappingTransformationNextBuilder<TModel>, ICsvFieldMappingValidationNextBuilder
        where TModel : class
    {
        private readonly CsvFieldMapping _fieldMapping;

        public CsvFieldMappingBuilder(CsvFieldMapping fieldMapping)
        {
            _fieldMapping = fieldMapping;
        }

        ICsvFieldMappingTransformationNextBuilder<TModel> ICsvFieldMappingTransformationBuilder<TModel, T>.ConvertUsing(CsvFieldConvertFromString<TModel, T> converter)
        {
            _fieldMapping.FromStringConverter = converter;
            return this;
        }

        ICsvFieldMappingTransformationNextBuilder<TModel> ICsvFieldMappingTransformationBuilder<TModel, T>.ConvertUsing(CsvFieldConvertToString<TModel> converter)
        {
            _fieldMapping.ToStringConverter = converter;
            return this;
        }

        ICsvFieldMappingTransformationNextBuilder<TModel> ICsvFieldMappingTransformationBuilder<TModel, T>.ConvertUsing(Type typeConverterType)
        {
            if (!typeConverterType.GetTypeInfo().IsSubclassOf(typeof(TypeConverter)))
            {
                throw new ArgumentException("Expected the type of a type converter.", nameof(typeConverterType));
            }

            _fieldMapping.TypeConverterType = typeConverterType;
            return this;
        }

        ICsvFieldMappingTransformationNextBuilder<TModel> ICsvFieldMappingTransformationBuilder<TModel, T>.ConvertUsing<TConverter>()
        {
            _fieldMapping.TypeConverterType = typeof(TConverter);
            return this;
        }

        void ICsvFieldMappingConstantBuilder<T>.HasConstantValue(T value)
        {
            _fieldMapping.ConstantValue = value;
        }

        ICsvFieldMappingIdentityNextBuilder<TModel, T> ICsvFieldMappingIdentityBuilder<TModel, T>.HasFieldIndex(int fieldIndex)
        {
            if (fieldIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fieldIndex), fieldIndex, "Index must not be negative.");
            }

            _fieldMapping.FieldIndex = fieldIndex;
            return this;
        }

        ICsvFieldMappingIdentityNextBuilder<TModel, T> ICsvFieldMappingIdentityBuilder<TModel, T>.HasFieldName(string fieldName)
        {
            if (fieldName is null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }
            else if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Expected a field name.", nameof(fieldName));
            }

            _fieldMapping.FieldName = fieldName;
            return this;
        }

        ICsvFieldMappingLogicalNameNextBuilder<TModel, T> ICsvFieldMappingLogicalNameBuilder<TModel, T>.UseFieldName(string fieldName)
        {
            if (fieldName is null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }
            else if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Expected a field name.", nameof(fieldName));
            }

            _fieldMapping.UseFieldName = fieldName;
            return this;
        }

        void ICsvFieldMappingOptionalBuilder.IsOptional()
        {
            _fieldMapping.IsOptional = true;
        }

        ICsvFieldMappingValidationNextBuilder ICsvFieldMappingValidationBuilder<TModel>.ValidateUsing(CsvFieldValidate<TModel> validator)
        {
            _fieldMapping.Validator = validator;
            return this;
        }

        void ICsvFieldMappingVariableBuilder<T>.HasVariableValue(CsvFieldGenerate<T> valueGenerator)
        {
            _fieldMapping.VariableValueGenerator = valueGenerator;
        }
    }
}