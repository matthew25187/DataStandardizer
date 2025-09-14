using System.Runtime.CompilerServices;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// Represents a record line from a CSV file.
    /// </summary>
    public class CsvFileRecordLine : CsvFileLineBase
    {
#if NETCOREAPP3_0_OR_GREATER
        protected virtual T? GetPropertyValue<T>([CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetFieldNameForPropertyName(propertyName!);

            // Get the field value in a form it can be returned as the property value.
            T? propertyValue = default;
            var fieldValue = GetFieldValue(fieldName);
            if (fieldValue is T genericValue)
            {
                propertyValue = genericValue;
            }

            return propertyValue;
        }

        protected virtual void SetPropertyValue<T>(T value, [CallerMemberName] string? propertyName = null)
        {
            var fieldName = GetFieldNameForPropertyName(propertyName!);

            SetFieldValue(fieldName, value);
        } 
#else
        [CanBeNull]
        protected virtual T GetPropertyValue<T>([CallerMemberName] string propertyName = null)
        {
            var fieldName = GetFieldNameForPropertyName(propertyName);

            // Get the field value in a form it can be returned as the property value.
            T propertyValue = default;
            var fieldValue = GetFieldValue(fieldName);
            if (fieldValue is T genericValue)
            {
                propertyValue = genericValue;
            }

            return propertyValue;
        }

        protected virtual void SetPropertyValue<T>(T value, [CallerMemberName] string propertyName = null)
        {
            var fieldName = GetFieldNameForPropertyName(propertyName);
            
            SetFieldValue(fieldName, value);
        }
#endif
    }
}