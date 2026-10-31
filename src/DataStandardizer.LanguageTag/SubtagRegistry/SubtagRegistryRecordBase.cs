using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// The shared base of the records of the Subtag Registry.
    /// </summary>
    /// <remarks>
    /// Records are created only by <see cref="SubtagRegistry"/>, and never change once it has loaded them.
    /// </remarks>
    public abstract class SubtagRegistryRecordBase : ISubtagRegistryRecord
    {
        private static readonly string[] NoValues = new string[0];
        private static readonly ReadOnlyCollection<string> NoValuesView = new ReadOnlyCollection<string>(NoValues);

        private readonly KeyValuePair<string, string>[] _fields;

        // A read-only view of the fields, created on first read. A race only creates an equivalent view, so no lock is taken.
#if NETCOREAPP3_0_OR_GREATER
        private ReadOnlyCollection<KeyValuePair<string, string>>? _fieldsView;
#else
        [CanBeNull] private ReadOnlyCollection<KeyValuePair<string, string>> _fieldsView;
#endif

#if NETCOREAPP3_0_OR_GREATER
        internal SubtagRegistryRecordBase(KeyValuePair<string, string>[] fields)
#else
        internal SubtagRegistryRecordBase([NotNull] KeyValuePair<string, string>[] fields)
#endif
        {
            _fields = fields;
        }

        /// <inheritdoc />
        public IReadOnlyList<KeyValuePair<string, string>> Fields
        {
            get { return _fieldsView ?? (_fieldsView = new ReadOnlyCollection<KeyValuePair<string, string>>(_fields)); }
        }

        public override string ToString()
        {
            var recordBuilder = new StringBuilder();
            foreach (var field in _fields)
            {
                recordBuilder.Append(field.Key).Append(": ").Append(field.Value).AppendLine();
            }

            return recordBuilder.ToString().TrimEnd();
        }

        /// <summary>
        /// Parse a registry date, in the <c>yyyy-MM-dd</c> form of the RFC 3339 full-date (ref. RFC 5646 §3.1.1).
        /// </summary>
        /// <returns>The date, or <see langword="null"/> when the value is not a date in that form.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static DateTime? ParseDate(string value)
#else
        internal static DateTime? ParseDate([NotNull] string value)
#endif
        {
            if (value.Length != 10 || value[4] != '-' || value[7] != '-') return null;
            if (!TryParseDigits(value, 0, 4, out var year) || !TryParseDigits(value, 5, 2, out var month) || !TryParseDigits(value, 8, 2, out var day)) return null;
            if (year < 1 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return null;

            return new DateTime(year, month, day);
        }

        /// <summary>
        /// Gets a read-only view of the bodies of a field that may appear more than once, creating it if it doesn't exist yet.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal static IReadOnlyList<string> GetValuesView(string[] values, ref ReadOnlyCollection<string>? view)
#else
        internal static IReadOnlyList<string> GetValuesView([NotNull] string[] values, [CanBeNull] ref ReadOnlyCollection<string> view)
#endif
        {
            return values.Length == 0 ? NoValuesView : view ?? (view = new ReadOnlyCollection<string>(values));
        }

        /// <summary>
        /// Gets the first body of a field, or <see langword="null"/> when the record has no such field.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal static string? GetFirstValue(KeyValuePair<string, string>[] fields, string fieldName)
#else
        [CanBeNull]
        internal static string GetFirstValue([NotNull] KeyValuePair<string, string>[] fields, [NotNull] string fieldName)
#endif
        {
            foreach (var field in fields)
            {
                if (field.Key == fieldName) return field.Value;
            }

            return null;
        }

        /// <summary>
        /// Collect the bodies of every field with a name, in the order they appear.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal static string[] GetValues(KeyValuePair<string, string>[] fields, string fieldName, int count)
#else
        internal static string[] GetValues([NotNull] KeyValuePair<string, string>[] fields, [NotNull] string fieldName, int count)
#endif
        {
            if (count == 0) return NoValues;

            var values = new string[count];
            var valueCount = 0;
            for (var index = 0; index < fields.Length && valueCount < count; index++)
            {
                if (fields[index].Key == fieldName) values[valueCount++] = fields[index].Value;
            }

            return values;
        }

        private static bool TryParseDigits(string value, int start, int length, out int number)
        {
            number = 0;
            for (var index = start; index < start + length; index++)
            {
                var digit = value[index] - '0';
                if (digit < 0 || digit > 9) return false;

                number = number * 10 + digit;
            }

            return true;
        }
    }
}
