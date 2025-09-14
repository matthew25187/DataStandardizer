using System;
using System.Linq.Expressions;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.Csv
{
    public class CsvFileMappingBuilder<TModel> where TModel : class
    {
#if NETCOREAPP3_0_OR_GREATER
        private readonly CsvFieldMapping? _fieldMapping;
        private readonly Action<string, CsvFieldMapping>? _mappingCreatedDelegate;
#else
        [CanBeNull] private readonly CsvFieldMapping _fieldMapping;
        [CanBeNull] private readonly Action<string, CsvFieldMapping> _mappingCreatedDelegate;
#endif

#if NETCOREAPP3_0_OR_GREATER
        internal CsvFileMappingBuilder(Action<string, CsvFieldMapping>? mappingCreatedDelegate = null)
        {
            _mappingCreatedDelegate = mappingCreatedDelegate;
        }

        internal CsvFileMappingBuilder(CsvFieldMapping fieldMapping, Action<string, CsvFieldMapping>? mappingCreatedDelegate = null)
        {
            _fieldMapping = fieldMapping;
            _mappingCreatedDelegate = mappingCreatedDelegate;
        }
#else
        public CsvFileMappingBuilder(Action<string, CsvFieldMapping> mappingCreatedDelegate = null)
        {
            _mappingCreatedDelegate = mappingCreatedDelegate;
        }

        public CsvFileMappingBuilder(CsvFieldMapping fieldMapping, Action<string, CsvFieldMapping> mappingCreatedDelegate = null)
        {
            _fieldMapping = fieldMapping;
            _mappingCreatedDelegate = mappingCreatedDelegate;
        }
#endif

        /// <summary>
        /// Map a property on a line model object to a CSV field.
        /// </summary>
        /// <typeparam name="T">Type of the mapped property.</typeparam>
        /// <param name="mappedPropertyExpression">Expression that selects the property to map.</param>
        /// <returns>A mapping builder.</returns>
        public ICsvFieldMappingInitialBuilder<TModel, T> Property<T>(Expression<Func<TModel, T>> mappedPropertyExpression)
        {
            if (mappedPropertyExpression is null)
            {
                throw new ArgumentNullException(nameof(mappedPropertyExpression));
            }

            // Get the name of the member nominated by the caller.
            string mappedPropertyName;
            switch (mappedPropertyExpression.Body)
            {
                case MemberExpression memberExpression:
                    mappedPropertyName = memberExpression.Member.Name;
                    break;
                case UnaryExpression unaryExpression when unaryExpression.Operand is MemberExpression operand:
                    mappedPropertyName = operand.Member.Name;
                    break;

                default:
                    throw new ArgumentException("Expression must be a member access.", nameof(mappedPropertyExpression));
            }

            // Extract property information relevant to the mapping.
            var fieldMapping = _fieldMapping;
            if (fieldMapping is null)
            {
                fieldMapping = new CsvFieldMapping(typeof(T));
            }
            else
            {
                fieldMapping.PropertyType = typeof(T);
            }

            // Notify the declaring type that a mapping has been created.
            if (_mappingCreatedDelegate != null)
            {
                _mappingCreatedDelegate(mappedPropertyName, fieldMapping);
            }

            return new CsvFieldMappingBuilder<TModel, T>(fieldMapping);
        }
    }
}