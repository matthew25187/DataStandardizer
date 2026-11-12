using System;
using System.Runtime.CompilerServices;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Represents a record line from a CSV file.
    /// </summary>
    public class CsvFileRecordLine : CsvFileLineBase
    {
#if NET5_0_OR_GREATER
        /// <summary>
        /// Gets the value of the field that a property reads.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="propertyName">The name of the property, which the compiler supplies.</param>
        /// <returns>The field's value, or the default value of <typeparamref name="T"/> if the line has no such field or its value is
        /// <c>null</c>.</returns>
        /// <exception cref="InvalidCastException">The field holds a value that isn't a <typeparamref name="T"/>, such as the raw
        /// <see cref="string"/> that a reader stores for a field that no mapping converts.</exception>
        protected virtual T? GetPropertyValue<T>([CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName!);

            // Get the field value in a form it can be returned as the property value.
            T? propertyValue = default;
            var fieldValue = GetFieldValue(fieldName);
            if (fieldValue is T genericValue)
            {
                propertyValue = genericValue;
            }
            else if (!(fieldValue is null))
            {
                throw CreateValueNotOfTypeException(fieldName, fieldValue, typeof(T));
            }

            return propertyValue;
        }

        protected virtual void SetPropertyValue<T>(T value, [CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName!);

            SetFieldValue(fieldName, value);
        } 
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the value of the field that a property reads.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="propertyName">The name of the property, which the compiler supplies.</param>
        /// <returns>The field's value, or the default value of <typeparamref name="T"/> if the line has no such field or its value is
        /// <c>null</c>.</returns>
        /// <exception cref="InvalidCastException">The field holds a value that isn't a <typeparamref name="T"/>, such as the raw
        /// <see cref="string"/> that a reader stores for a field that no mapping converts.</exception>
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        protected virtual T GetPropertyValue<T>([CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName!);

            // Get the field value in a form it can be returned as the property value.
            T propertyValue = default!;
            var fieldValue = GetFieldValue(fieldName);
            if (fieldValue is T genericValue)
            {
                propertyValue = genericValue;
            }
            else if (!(fieldValue is null))
            {
                throw CreateValueNotOfTypeException(fieldName, fieldValue, typeof(T));
            }

            return propertyValue;
        }

        protected virtual void SetPropertyValue<T>(T value, [CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName!);

            SetFieldValue(fieldName, value);
        } 
#else
        /// <summary>
        /// Gets the value of the field that a property reads.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="propertyName">The name of the property, which the compiler supplies.</param>
        /// <returns>The field's value, or the default value of <typeparamref name="T"/> if the line has no such field or its value is
        /// <c>null</c>.</returns>
        /// <exception cref="InvalidCastException">The field holds a value that isn't a <typeparamref name="T"/>, such as the raw
        /// <see cref="string"/> that a reader stores for a field that no mapping converts.</exception>
        [return: MaybeNull]
        protected virtual T GetPropertyValue<T>([CallerMemberName] string propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName);

            // Get the field value in a form it can be returned as the property value.
            T propertyValue = default;
            var fieldValue = GetFieldValue(fieldName);
            if (fieldValue is T genericValue)
            {
                propertyValue = genericValue;
            }
            else if (!(fieldValue is null))
            {
                throw CreateValueNotOfTypeException(fieldName, fieldValue, typeof(T));
            }

            return propertyValue;
        }

        protected virtual void SetPropertyValue<T>(T value, [CallerMemberName] string propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName);
            
            SetFieldValue(fieldName, value);
        }
#endif

        private static InvalidCastException CreateValueNotOfTypeException(string fieldName, object fieldValue, Type propertyType)
        {
            return new InvalidCastException(
                $"The field '{fieldName}' holds a value of type '{fieldValue.GetType()}', which can't be returned as '{propertyType}'. " +
                "Map the property to the field so the reader converts the field's value.");
        }
    }
}