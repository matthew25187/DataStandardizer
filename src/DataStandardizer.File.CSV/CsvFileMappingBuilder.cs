using System;
using System.Linq.Expressions;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    public sealed class CsvFileMappingBuilder<TModel> where TModel : class
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly CsvFieldMapping? _fieldMapping;
        private readonly Action<string, CsvFieldMapping>? _mappingCreatedDelegate;
#else
        [MaybeNull] private readonly CsvFieldMapping _fieldMapping;
        [MaybeNull] private readonly Action<string, CsvFieldMapping> _mappingCreatedDelegate;
#endif

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
        internal CsvFileMappingBuilder(Action<string, CsvFieldMapping> mappingCreatedDelegate = null)
        {
            _mappingCreatedDelegate = mappingCreatedDelegate;
        }

        internal CsvFileMappingBuilder(CsvFieldMapping fieldMapping, Action<string, CsvFieldMapping> mappingCreatedDelegate = null)
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

            // Get the member nominated by the caller.
            MemberExpression mappedMember;
            switch (mappedPropertyExpression.Body)
            {
                case MemberExpression memberExpression:
                    mappedMember = memberExpression;
                    break;
                case UnaryExpression unaryExpression when unaryExpression.Operand is MemberExpression operand:
                    mappedMember = operand;
                    break;

                default:
                    throw new ArgumentException("Expression must be a member access.", nameof(mappedPropertyExpression));
            }

            // Key a member of the model as a declarative mapping is keyed: by the name CsvFieldAttribute gives the property, else the property
            // name.  A member of another object, such as x.Address.City, isn't a property of the model, so it keeps its own name.
            var mappedMemberName = mappedMember.Member.Name;
            var mappedPropertyKey = mappedMember.Expression is ParameterExpression
                ? CsvFileLineBase.GetCachedPropertyKey(typeof(TModel), mappedMemberName)
                : mappedMemberName;

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
                _mappingCreatedDelegate(mappedPropertyKey, fieldMapping);
            }

            return new CsvFieldMappingBuilder<TModel, T>(fieldMapping);
        }
    }
}