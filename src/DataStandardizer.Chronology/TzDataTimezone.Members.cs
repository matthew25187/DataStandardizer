using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Chronology
{
    public readonly partial struct TzDataTimezone
    {
        private static readonly Registry DefaultRegistry = new Registry();

        /// <summary>
        /// Gets the full history of the timezone's offsets from universal time, as the zone lines of its Zone entry in the TZ Database.
        /// </summary>
        /// <value>
        /// The zone lines, in chronological order. Every line but the last has an <see cref="TzDataZoneLine.Until"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The timezone is the default value, or its identifier is not that of a timezone field.
        /// </exception>
#if NETSTANDARD
        [NotNull]
#endif
        public IReadOnlyList<TzDataZoneLine> ZoneLines
        {
            get
            {
                if (_value is null)
                    throw new InvalidOperationException("The timezone has no identifier.");

                // An instance created by explicit cast has no zone lines of its own, so they are found by identifier.
                var zoneLines = _zoneLines;
                if (zoneLines is null)
                {
                    if (!DefaultRegistry.GetEntries().TryGetValue(_value, out var entry))
                        throw new InvalidOperationException($"'{_value}' is not the identifier of a known timezone.");

                    zoneLines = entry.ZoneLines;
                }

                // The array is shared, so it is wrapped rather than exposed, to keep it from being cast back and modified.
                return new ReadOnlyCollection<TzDataZoneLine>(zoneLines);
            }
        }

        /// <summary>
        /// A timezone field and its zone lines.
        /// </summary>
        internal sealed class RegistryEntry
        {
            internal RegistryEntry(FieldInfo field, TzDataZoneLine[] zoneLines)
            {
                Field = field;
                ZoneLines = zoneLines;
            }

            internal FieldInfo Field { get; }

            internal TzDataZoneLine[] ZoneLines { get; }
        }

        /// <summary>
        /// The timezone fields, keyed by identifier, built on first use by reflecting over the nested static classes.
        /// </summary>
        internal sealed class Registry
        {
            // netstandard1.0 has no ConcurrentDictionary or Lazy with thread safety modes, so the entries are built under a lock.
            private readonly object _lock = new object();
#if NETCOREAPP3_0_OR_GREATER
            private volatile Dictionary<string, RegistryEntry>? _entries;
#else
            [CanBeNull]
            private volatile Dictionary<string, RegistryEntry> _entries;
#endif

            internal Dictionary<string, RegistryEntry> GetEntries()
            {
                var entries = _entries;
                if (entries != null)
                    return entries;

                lock (_lock)
                {
                    if (_entries == null)
                        _entries = BuildEntries();

                    return _entries;
                }
            }

            private static Dictionary<string, RegistryEntry> BuildEntries()
            {
                var entries = new Dictionary<string, RegistryEntry>(StringComparer.Ordinal);
                AddEntries(typeof(TzDataTimezone).GetTypeInfo(), entries);
                return entries;
            }

            private static void AddEntries(TypeInfo hostType, Dictionary<string, RegistryEntry> entries)
            {
                foreach (var field in hostType.DeclaredFields)
                {
                    if (!field.IsPublic || !field.IsStatic || field.FieldType != typeof(TzDataTimezone))
                        continue;

                    if (!(field.GetValue(null) is TzDataTimezone timezone) || timezone._value is null || timezone._zoneLines is null)
                        throw new InvalidOperationException($"Timezone field {hostType.Name}.{field.Name} has no identifier or zone lines.");

                    entries.Add(timezone._value, new RegistryEntry(field, timezone._zoneLines));
                }

                foreach (var nestedType in hostType.DeclaredNestedTypes)
                {
                    if (nestedType.IsNestedPublic && nestedType.IsClass && nestedType.IsAbstract && nestedType.IsSealed)
                        AddEntries(nestedType, entries);
                }
            }
        }
    }
}
