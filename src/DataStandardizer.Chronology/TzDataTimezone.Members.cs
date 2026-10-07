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
        private static readonly TzDataZoneCalculator.Cache DefaultCalculatorCache = new TzDataZoneCalculator.Cache();

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
                // The array is shared, so it is wrapped rather than exposed, to keep it from being cast back and modified.
                return new ReadOnlyCollection<TzDataZoneLine>(GetZoneLineArray(out _));
            }
        }

        /// <summary>
        /// Gets the latitude of the principal location in the timezone.
        /// </summary>
        /// <value>
        /// The latitude, in decimal degrees, positive north of the equator.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The timezone is the default value, or its identifier is not that of a timezone field.
        /// </exception>
        public double Latitude
        {
            get
            {
                return GetLocation().Latitude;
            }
        }

        /// <summary>
        /// Gets the longitude of the principal location in the timezone.
        /// </summary>
        /// <value>
        /// The longitude, in decimal degrees, positive east of Greenwich.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The timezone is the default value, or its identifier is not that of a timezone field.
        /// </exception>
        public double Longitude
        {
            get
            {
                return GetLocation().Longitude;
            }
        }

        /// <summary>
        /// Gets the ISO 3166 Part 1 Alpha-2 country codes for the countries covered by the timezone.
        /// </summary>
        /// <value>
        /// The country codes, in the order given by zone1970.tab, which lists first the country of the principal location.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The timezone is the default value, or its identifier is not that of a timezone field.
        /// </exception>
#if NETSTANDARD
        [NotNull]
#endif
        public IReadOnlyList<string> IsoCountryCodes
        {
            get
            {
                // The array is shared, so it is wrapped rather than exposed, to keep it from being cast back and modified.
                return new ReadOnlyCollection<string>(GetLocation().IsoCountryCodes);
            }
        }

        /// <summary>
        /// Gets the comment on the timezone, which distinguishes it from the other timezones of its countries.
        /// </summary>
        /// <value>
        /// The comment, if the timezone has one; otherwise, <see langword="null"/>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The timezone is the default value, or its identifier is not that of a timezone field.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        public string? Comment
#else
        [CanBeNull]
        public string Comment
#endif
        {
            get
            {
                return GetLocation().Comment;
            }
        }

        /// <summary>
        /// Gets the calculator of the timezone's offsets from universal time, building it on first use.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The timezone is the default value, or its identifier is not that of a timezone field.
        /// </exception>
#if NETSTANDARD
        [NotNull]
#endif
        internal TzDataZoneCalculator GetCalculator()
        {
            var zoneLines = GetZoneLineArray(out var identifier);
            return DefaultCalculatorCache.GetCalculator(identifier, zoneLines);
        }

        /// <summary>
        /// Finds the timezone field with the same identifier as the timezone.
        /// </summary>
        /// <param name="entry">The field and its zone lines, if found.</param>
        /// <returns><see langword="true"/> if the field was found; otherwise, <see langword="false"/>, including for the default value.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal bool TryGetRegistryEntry([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out RegistryEntry? entry)
#else
        internal bool TryGetRegistryEntry(out RegistryEntry entry)
#endif
        {
            if (_value is null)
            {
                entry = null;
                return false;
            }

            return DefaultRegistry.GetEntries().TryGetValue(_value, out entry);
        }

        private TzDataZoneLine[] GetZoneLineArray(out string identifier)
        {
            if (_value is null)
                throw new InvalidOperationException("The timezone has no identifier.");

            identifier = _value;

            // An instance created by explicit cast has no zone lines of its own, so they are found by identifier.
            var zoneLines = _zoneLines;
            if (zoneLines is null)
            {
                if (!TryGetRegistryEntry(out var entry))
                    throw new InvalidOperationException($"'{_value}' is not the identifier of a known timezone.");

                zoneLines = entry.ZoneLines;
            }

            return zoneLines;
        }

#if NETSTANDARD
        [NotNull]
#endif
        private Location GetLocation()
        {
            if (_value is null)
                throw new InvalidOperationException("The timezone has no identifier.");

            // An instance created by explicit cast has no location of its own, so it is found by identifier.
            var location = _location;
            if (location is null)
            {
                if (!TryGetRegistryEntry(out var entry))
                    throw new InvalidOperationException($"'{_value}' is not the identifier of a known timezone.");

                location = entry.Location;
            }

            return location;
        }

        /// <summary>
        /// The location metadata of a timezone, as listed in zone1970.tab.
        /// </summary>
        internal sealed class Location
        {
#if NETCOREAPP3_0_OR_GREATER
            internal Location(double latitude, double longitude, string[] isoCountryCodes, string? comment)
#else
            internal Location(double latitude, double longitude, string[] isoCountryCodes, [CanBeNull] string comment)
#endif
            {
                Latitude = latitude;
                Longitude = longitude;
                IsoCountryCodes = isoCountryCodes;
                Comment = comment;
            }

            internal double Latitude { get; }

            internal double Longitude { get; }

            internal string[] IsoCountryCodes { get; }

#if NETCOREAPP3_0_OR_GREATER
            internal string? Comment { get; }
#else
            [CanBeNull]
            internal string Comment { get; }
#endif
        }

        /// <summary>
        /// A timezone field, with its zone lines and location.
        /// </summary>
        internal sealed class RegistryEntry
        {
            internal RegistryEntry(FieldInfo field, TzDataZoneLine[] zoneLines, Location location)
            {
                Field = field;
                ZoneLines = zoneLines;
                Location = location;
            }

            internal FieldInfo Field { get; }

            internal TzDataZoneLine[] ZoneLines { get; }

            internal Location Location { get; }
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

                    if (!(field.GetValue(null) is TzDataTimezone timezone) || timezone._value is null || timezone._zoneLines is null || timezone._location is null)
                        throw new InvalidOperationException($"Timezone field {hostType.Name}.{field.Name} has no identifier, zone lines or location.");

                    entries.Add(timezone._value, new RegistryEntry(field, timezone._zoneLines, timezone._location));
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
