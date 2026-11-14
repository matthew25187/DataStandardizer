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
        /// <remarks>
        /// When a reader maps several properties to one field, each property's mapping converts the field's text, and the field holds the value of
        /// the last of them in mapper order.  Each of those properties returns its own converted value, or its default value if the reader called
        /// the bad-value handler for it, until the field's value is set again, through a property or the line.  From then on, every one of them
        /// reads the field's value.
        /// </remarks>
        /// <exception cref="InvalidCastException">The field holds a value that isn't a <typeparamref name="T"/>, such as the text that a reader
        /// stores for a field that no mapping converts.</exception>
#if NET5_0_OR_GREATER
        protected virtual T? GetPropertyValue<T>([CallerMemberName] string? propertyName = null)
        {
            var propertyKey = GetCsvFieldKey(propertyName!);
            var fieldName = GetMappedFieldName(propertyKey);

            // A property that shares its field with another property's mapping reads the value the reader converted for it.
            if (TryGetSharedFieldValue(propertyKey, fieldName, out var sharedFieldValue))
            {
                if (sharedFieldValue is T sharedValue)
                {
                    return sharedValue;
                }

                CheckSharedValueReadsAsDefault(fieldName, sharedFieldValue, typeof(T));
                return default;
            }

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
            // The line records that the property set the value, so the writer can tell it from a field's value with the same name.
            var propertyKey = GetCsvFieldKey(propertyName!);
            SetPropertyFieldValue(GetMappedFieldName(propertyKey), propertyKey, value);
        }
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        protected virtual T GetPropertyValue<T>([CallerMemberName] string? propertyName = null)
        {
            var propertyKey = GetCsvFieldKey(propertyName!);
            var fieldName = GetMappedFieldName(propertyKey);

            // A property that shares its field with another property's mapping reads the value the reader converted for it.
            if (TryGetSharedFieldValue(propertyKey, fieldName, out var sharedFieldValue))
            {
                if (sharedFieldValue is T sharedValue)
                {
                    return sharedValue;
                }

                CheckSharedValueReadsAsDefault(fieldName, sharedFieldValue, typeof(T));
                return default!;
            }

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
            // The line records that the property set the value, so the writer can tell it from a field's value with the same name.
            var propertyKey = GetCsvFieldKey(propertyName!);
            SetPropertyFieldValue(GetMappedFieldName(propertyKey), propertyKey, value);
        }
#else
        [return: MaybeNull]
        protected virtual T GetPropertyValue<T>([CallerMemberName] string propertyName = null)
        {
            var propertyKey = GetCsvFieldKey(propertyName);
            var fieldName = GetMappedFieldName(propertyKey);

            // A property that shares its field with another property's mapping reads the value the reader converted for it.
            if (TryGetSharedFieldValue(propertyKey, fieldName, out var sharedFieldValue))
            {
                if (sharedFieldValue is T sharedValue)
                {
                    return sharedValue;
                }

                CheckSharedValueReadsAsDefault(fieldName, sharedFieldValue, typeof(T));
                return default;
            }

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
            // The line records that the property set the value, so the writer can tell it from a field's value with the same name.
            var propertyKey = GetCsvFieldKey(propertyName);
            SetPropertyFieldValue(GetMappedFieldName(propertyKey), propertyKey, value);
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

            throw BuildInvalidCastException(fieldName, fieldValue, propertyType);
        }

        /// <summary>
        /// Throws unless the value a property kept from a shared field reads as the property's default value: when it is <c>null</c>.  Another
        /// mapping's bad value on the field doesn't make this property's value read as its default.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static void CheckSharedValueReadsAsDefault(string fieldName, object? fieldValue, Type propertyType)
#else
        private static void CheckSharedValueReadsAsDefault(string fieldName, [AllowNull] object fieldValue, Type propertyType)
#endif
        {
            if (fieldValue is null)
            {
                return;
            }

            throw BuildInvalidCastException(fieldName, fieldValue, propertyType);
        }

        private static InvalidCastException BuildInvalidCastException(string fieldName, object fieldValue, Type propertyType)
        {
            return new InvalidCastException(
                $"The field '{fieldName}' holds a value of type '{fieldValue.GetType()}', which the property can't return as '{propertyType}'. " +
                "A reader stores a field's text unless a mapping converts it to the property's type.");
        }
    }
}