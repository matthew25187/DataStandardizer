using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.LanguageTag.Registry
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
        [MaybeNull] private ReadOnlyCollection<KeyValuePair<string, string>> _fieldsView;
#endif

#if NETCOREAPP3_0_OR_GREATER
        internal SubtagRegistryRecordBase(KeyValuePair<string, string>[] fields)
#else
        internal SubtagRegistryRecordBase([DisallowNull] KeyValuePair<string, string>[] fields)
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
        /// Gets a read-only view of the bodies of a field that may appear more than once, creating it if it doesn't exist yet.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal static IReadOnlyList<string> GetValuesView(string[] values, ref ReadOnlyCollection<string>? view)
#else
        internal static IReadOnlyList<string> GetValuesView([DisallowNull] string[] values, [MaybeNull, AllowNull] ref ReadOnlyCollection<string> view)
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
        [return: MaybeNull]
        internal static string GetFirstValue([DisallowNull] KeyValuePair<string, string>[] fields, [DisallowNull] string fieldName)
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
        internal static string[] GetValues([DisallowNull] KeyValuePair<string, string>[] fields, [DisallowNull] string fieldName, int count)
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
    }
}
