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
        /// <summary>
        /// Gets the value of the field that a property reads.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="propertyName">The name of the property, which the compiler supplies.</param>
        /// <returns>The field's value, or the default value of <typeparamref name="T"/> if the line has no such field, its value is
        /// <c>null</c>, or the reader called the bad-value handler for it and it isn't a <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidCastException">The field holds a value that isn't a <typeparamref name="T"/>, such as the text that a reader
        /// stores for a field that no mapping converts.</exception>
#if NET5_0_OR_GREATER
        protected virtual T? GetPropertyValue<T>([CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName!);
            var fieldValue = GetFieldValue(fieldName);
            if (fieldValue is T genericValue)
            {
                return genericValue;
            }

            CheckReadsAsDefault(fieldName, fieldValue, typeof(T));
            return default;
        }

        protected virtual void SetPropertyValue<T>(T value, [CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName!);

            SetFieldValue(fieldName, value);
        }
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        protected virtual T GetPropertyValue<T>([CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName!);
            var fieldValue = GetFieldValue(fieldName);
            if (fieldValue is T genericValue)
            {
                return genericValue;
            }

            CheckReadsAsDefault(fieldName, fieldValue, typeof(T));
            return default!;
        }

        protected virtual void SetPropertyValue<T>(T value, [CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName!);

            SetFieldValue(fieldName, value);
        }
#else
        [return: MaybeNull]
        protected virtual T GetPropertyValue<T>([CallerMemberName] string propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName);
            var fieldValue = GetFieldValue(fieldName);
            if (fieldValue is T genericValue)
            {
                return genericValue;
            }

            CheckReadsAsDefault(fieldName, fieldValue, typeof(T));
            return default;
        }

        protected virtual void SetPropertyValue<T>(T value, [CallerMemberName] string propertyName = null)
        {
            var fieldName = GetPropertyKey(propertyName);

            SetFieldValue(fieldName, value);
        }
#endif

        /// <summary>
        /// Throws unless a field value that isn't a value of the property type reads as the property's default value: when it is <c>null</c>, or
        /// the reader called the bad-value handler for it.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private void CheckReadsAsDefault(string fieldName, object? fieldValue, Type propertyType)
#else
        private void CheckReadsAsDefault(string fieldName, [AllowNull] object fieldValue, Type propertyType)
#endif
        {
            if (fieldValue is null || IsBadValueField(fieldName))
            {
                return;
            }

            throw new InvalidCastException(
                $"The field '{fieldName}' holds a value of type '{fieldValue.GetType()}', which the property can't return as '{propertyType}'. " +
                "A reader stores a field's text unless a mapping converts it to the property's type.");
        }
    }
}