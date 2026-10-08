using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Chronology
{
#if NET7_0_OR_GREATER
    public readonly partial struct TzDataTimezone : IParsable<TzDataTimezone>
#else
    public readonly partial struct TzDataTimezone
#endif
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
        /// The country codes, in the order given by zone1970.tab, which lists first the country of the principal location; for a link, the country code given by zone.tab.
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
        /// Gets a value indicating whether the timezone is a link: a timezone of its own countries, listed in zone.tab, that shares the zone lines of a
        /// canonical timezone, as <c>Europe/Oslo</c> shares those of <c>Europe/Berlin</c>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the timezone is a link; otherwise, <see langword="false"/>, for a canonical timezone.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The timezone is the default value, or its identifier is not that of a timezone field.
        /// </exception>
        public bool IsLink
        {
            get
            {
                return GetLinkTarget(out _);
            }
        }

        /// <summary>
        /// Gets the canonical timezone whose zone lines the timezone has: the timezone that a link refers to, or the timezone itself.
        /// </summary>
        /// <value>
        /// The predefined timezone that the link refers to, if the timezone is a link; otherwise, this timezone.
        /// </value>
        /// <remarks>
        /// Timezones are equal only where their identifiers are, so a link is not equal to its canonical timezone. To ask whether two timezones
        /// share their zone lines, compare their canonical timezones.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The timezone is the default value, or its identifier is not that of a timezone field.
        /// </exception>
        public TzDataTimezone Canonical
        {
            get
            {
                if (!GetLinkTarget(out var linkTarget))
                    return this;

                return DefaultRegistry.GetEntries()[linkTarget].Timezone;
            }
        }

        /// <summary>
        /// Converts a TZ Database identifier to the timezone with that identifier.
        /// </summary>
        /// <param name="s">
        /// The identifier, such as <c>Europe/Zurich</c>, or a deprecated name, such as <c>Asia/Calcutta</c>, which converts to the canonical
        /// timezone it refers to. Identifiers are case-sensitive.
        /// </param>
        /// <returns>The predefined timezone with the identifier, which carries its zone lines and location.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException"><paramref name="s"/> is not the identifier of a known timezone.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static TzDataTimezone Parse(string s)
#else
        public static TzDataTimezone Parse([NotNull] string s)
#endif
        {
            if (s is null)
                throw new ArgumentNullException(nameof(s));

            if (!TryParse(s, out var result))
                throw new FormatException($"'{s}' is not the identifier of a known timezone.");

            return result;
        }

        /// <summary>
        /// Tries to convert a TZ Database identifier to the timezone with that identifier.
        /// </summary>
        /// <param name="s">
        /// The identifier, such as <c>Europe/Zurich</c>, or a deprecated name, such as <c>Asia/Calcutta</c>, which converts to the canonical
        /// timezone it refers to. Identifiers are case-sensitive.
        /// </param>
        /// <param name="result">
        /// When this method returns, the predefined timezone with the identifier, which carries its zone lines and location,
        /// if the conversion succeeded; otherwise, the default value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="s"/> is the identifier of a known timezone; otherwise, <see langword="false"/>,
        /// including for <see langword="null"/>.
        /// </returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? s, out TzDataTimezone result)
#else
        public static bool TryParse([CanBeNull] string s, out TzDataTimezone result)
#endif
        {
            if (s != null && (DefaultRegistry.GetEntries().TryGetValue(s, out var entry) || DefaultRegistry.GetDeprecatedLinks().TryGetValue(s, out entry)))
            {
                result = entry.Timezone;
                return true;
            }

            result = default;
            return false;
        }

#if NET7_0_OR_GREATER
        /// <summary>
        /// Converts a TZ Database identifier to the timezone with that identifier.
        /// </summary>
        /// <param name="s">
        /// The identifier, such as <c>Europe/Zurich</c>, or a deprecated name, such as <c>Asia/Calcutta</c>, which converts to the canonical
        /// timezone it refers to. Identifiers are case-sensitive.
        /// </param>
        /// <param name="provider">Ignored, as identifiers do not depend on culture.</param>
        /// <returns>The predefined timezone with the identifier, which carries its zone lines and location.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException"><paramref name="s"/> is not the identifier of a known timezone.</exception>
        public static TzDataTimezone Parse(string s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <summary>
        /// Tries to convert a TZ Database identifier to the timezone with that identifier.
        /// </summary>
        /// <param name="s">
        /// The identifier, such as <c>Europe/Zurich</c>, or a deprecated name, such as <c>Asia/Calcutta</c>, which converts to the canonical
        /// timezone it refers to. Identifiers are case-sensitive.
        /// </param>
        /// <param name="provider">Ignored, as identifiers do not depend on culture.</param>
        /// <param name="result">
        /// When this method returns, the predefined timezone with the identifier, which carries its zone lines and location,
        /// if the conversion succeeded; otherwise, the default value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="s"/> is the identifier of a known timezone; otherwise, <see langword="false"/>,
        /// including for <see langword="null"/>.
        /// </returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out TzDataTimezone result)
        {
            return TryParse(s, out result);
        }
#endif

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

        /// <summary>
        /// Gets the identifier of the canonical timezone that the timezone refers to, if it is a link.
        /// </summary>
        /// <param name="linkTarget">The identifier of the canonical timezone, if the timezone is a link.</param>
        /// <returns><see langword="true"/> if the timezone is a link; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// The timezone is the default value, or its identifier is not that of a timezone field.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        private bool GetLinkTarget([NotNullWhen(true)] out string? linkTarget)
#else
        private bool GetLinkTarget(out string linkTarget)
#endif
        {
            if (_value is null)
                throw new InvalidOperationException("The timezone has no identifier.");

            // An instance created by explicit cast has no zone lines or link target of its own, so they are found by identifier.
            if (_zoneLines is null)
            {
                if (!TryGetRegistryEntry(out var entry))
                    throw new InvalidOperationException($"'{_value}' is not the identifier of a known timezone.");

                linkTarget = entry.LinkTarget;
            }
            else
            {
                linkTarget = _linkTarget;
            }

            return linkTarget != null;
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
        /// The location metadata of a timezone, as listed in zone1970.tab, or in zone.tab for a link.
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
        /// A timezone field, with its value, zone lines, link target and location.
        /// </summary>
        internal sealed class RegistryEntry
        {
#if NETCOREAPP3_0_OR_GREATER
            internal RegistryEntry(FieldInfo field, TzDataTimezone timezone, TzDataZoneLine[] zoneLines, string? linkTarget, Location location)
#else
            internal RegistryEntry(FieldInfo field, TzDataTimezone timezone, TzDataZoneLine[] zoneLines, [CanBeNull] string linkTarget, Location location)
#endif
            {
                Field = field;
                Timezone = timezone;
                ZoneLines = zoneLines;
                LinkTarget = linkTarget;
                Location = location;
            }

            internal FieldInfo Field { get; }

            internal TzDataTimezone Timezone { get; }

            internal TzDataZoneLine[] ZoneLines { get; }

            /// <summary>
            /// Gets the identifier of the canonical timezone that the timezone refers to, if it is a link; otherwise, <see langword="null"/>.
            /// </summary>
#if NETCOREAPP3_0_OR_GREATER
            internal string? LinkTarget { get; }
#else
            [CanBeNull]
            internal string LinkTarget { get; }
#endif

            internal Location Location { get; }
        }

        /// <summary>
        /// The timezone fields, keyed by identifier, built on first use by reflecting over the nested static classes, and the deprecated links, keyed by
        /// name, each with the field of the canonical timezone it refers to.
        /// </summary>
        internal sealed class Registry
        {
            // netstandard1.0 has no ConcurrentDictionary or Lazy with thread safety modes, so the entries are built under a lock.
            private readonly object _lock = new object();
#if NETCOREAPP3_0_OR_GREATER
            private volatile Tables? _tables;
#else
            [CanBeNull]
            private volatile Tables _tables;
#endif

            internal Dictionary<string, RegistryEntry> GetEntries()
            {
                return GetTables().Entries;
            }

            internal Dictionary<string, RegistryEntry> GetDeprecatedLinks()
            {
                return GetTables().DeprecatedLinks;
            }

            private Tables GetTables()
            {
                var tables = _tables;
                if (tables != null)
                    return tables;

                lock (_lock)
                {
                    if (_tables == null)
                        _tables = BuildTables();

                    return _tables;
                }
            }

            private static Tables BuildTables()
            {
                var entries = new Dictionary<string, RegistryEntry>(StringComparer.Ordinal);
                AddEntries(typeof(TzDataTimezone).GetTypeInfo(), entries);

                foreach (var entry in entries.Values)
                {
                    if (entry.LinkTarget != null && !IsCanonicalEntry(entries, entry.LinkTarget))
                        throw new InvalidOperationException($"Link field {entry.Field.Name} refers to '{entry.LinkTarget}', which is not a canonical timezone field.");
                }

                var deprecatedLinks = new Dictionary<string, RegistryEntry>(StringComparer.Ordinal);
                foreach (var deprecatedLink in LinkData.DeprecatedLinks)
                {
                    if (entries.ContainsKey(deprecatedLink.Key) || !IsCanonicalEntry(entries, deprecatedLink.Value))
                        throw new InvalidOperationException($"Deprecated link '{deprecatedLink.Key}' has a timezone field, or refers to '{deprecatedLink.Value}', which is not a canonical timezone field.");

                    deprecatedLinks.Add(deprecatedLink.Key, entries[deprecatedLink.Value]);
                }

                return new Tables(entries, deprecatedLinks);
            }

            private static bool IsCanonicalEntry(Dictionary<string, RegistryEntry> entries, string identifier)
            {
                return entries.TryGetValue(identifier, out var entry) && entry.LinkTarget is null;
            }

            private static void AddEntries(TypeInfo hostType, Dictionary<string, RegistryEntry> entries)
            {
                foreach (var field in hostType.DeclaredFields)
                {
                    if (!field.IsPublic || !field.IsStatic || field.FieldType != typeof(TzDataTimezone))
                        continue;

                    if (!(field.GetValue(null) is TzDataTimezone timezone) || timezone._value is null || timezone._zoneLines is null || timezone._location is null)
                        throw new InvalidOperationException($"Timezone field {hostType.Name}.{field.Name} has no identifier, zone lines or location.");

                    entries.Add(timezone._value, new RegistryEntry(field, timezone, timezone._zoneLines, timezone._linkTarget, timezone._location));
                }

                foreach (var nestedType in hostType.DeclaredNestedTypes)
                {
                    if (nestedType.IsNestedPublic && nestedType.IsClass && nestedType.IsAbstract && nestedType.IsSealed)
                        AddEntries(nestedType, entries);
                }
            }

            /// <summary>
            /// The timezone fields and deprecated links, published together so that a thread that sees one sees both.
            /// </summary>
            private sealed class Tables
            {
                internal Tables(Dictionary<string, RegistryEntry> entries, Dictionary<string, RegistryEntry> deprecatedLinks)
                {
                    Entries = entries;
                    DeprecatedLinks = deprecatedLinks;
                }

                internal Dictionary<string, RegistryEntry> Entries { get; }

                internal Dictionary<string, RegistryEntry> DeprecatedLinks { get; }
            }
        }
    }
}
