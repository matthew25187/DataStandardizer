using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
        /// The identifier of the timezone that the default value stands for: Coordinated Universal Time, defined in the etcetera file of the
        /// TZ Database, with an offset of 0, the abbreviation <c>UTC</c> and no daylight saving time.
        /// </summary>
        private const string DefaultIdentifier = "Etc/UTC";

        /// <summary>
        /// Gets the identifier of the timezone, which is <c>Etc/UTC</c> for the default value.
        /// </summary>
        private string Identifier
        {
            get
            {
                return _value ?? DefaultIdentifier;
            }
        }

        /// <summary>
        /// Gets the full history of the timezone's offsets from universal time, as the zone lines of its Zone entry in the TZ Database.
        /// </summary>
        /// <value>
        /// The zone lines, in chronological order. Every line but the last has an <see cref="TzDataZoneLine.Until"/>.
        /// </value>
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
        /// The latitude, in decimal degrees, positive north of the equator; 0 for <see cref="Etc.UTC"/> and the default value, which have
        /// no location.
        /// </value>
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
        /// The longitude, in decimal degrees, positive east of Greenwich; 0 for <see cref="Etc.UTC"/> and the default value, which have
        /// no location.
        /// </value>
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
        /// Empty for <see cref="Etc.UTC"/> and the default value, which are used in no country.
        /// </value>
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
        /// The comment, if the timezone has one; otherwise, <see langword="null"/>, as for <see cref="Etc.UTC"/> and the default value.
        /// </value>
#if NETCOREAPP3_0_OR_GREATER
        public string? Comment
#else
        [MaybeNull]
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
        public static TzDataTimezone Parse([DisallowNull] string s)
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
        /// if the conversion succeeded; otherwise, the default value, which is equal to <see cref="Etc.UTC"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="s"/> is the identifier of a known timezone; otherwise, <see langword="false"/>,
        /// including for <see langword="null"/>.
        /// </returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? s, out TzDataTimezone result)
#else
        public static bool TryParse([AllowNull, NotNullWhen(true)] string s, out TzDataTimezone result)
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

        /// <summary>
        /// Converts a TZ Database identifier to the timezone with that identifier, for the explicit conversion from <see cref="string"/>.
        /// </summary>
        /// <param name="value">
        /// The identifier, or a deprecated name, which converts to the canonical timezone it refers to. Identifiers are case-sensitive.
        /// </param>
        /// <returns>The predefined timezone with the identifier, which carries its zone lines and location.</returns>
        /// <exception cref="InvalidCastException">
        /// <paramref name="value"/> is <see langword="null"/>, or is not the identifier of a known timezone.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        private static TzDataTimezone ConvertFromIdentifier(string? value)
#else
        private static TzDataTimezone ConvertFromIdentifier([AllowNull] string value)
#endif
        {
            if (value is null)
                throw new InvalidCastException("A null identifier cannot be converted to a timezone.");

            if (!TryParse(value, out var result))
                throw new InvalidCastException($"'{value}' is not the identifier of a known timezone.");

            return result;
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
        /// if the conversion succeeded; otherwise, the default value, which is equal to <see cref="Etc.UTC"/>.
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
#if NETSTANDARD
        [return: NotNull]
#endif
        internal TzDataZoneCalculator GetCalculator()
        {
            var zoneLines = GetZoneLineArray(out var identifier);
            return DefaultCalculatorCache.GetCalculator(identifier, zoneLines);
        }

        /// <summary>
        /// Finds the registry entry of the timezone field with the same identifier as the timezone.
        /// </summary>
        /// <param name="entry">The timezone of the field, with its zone lines, link target and location, if found.</param>
        /// <returns><see langword="true"/> if the field was found, which for the default value is the field of <c>Etc/UTC</c>; otherwise, <see langword="false"/>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal bool TryGetRegistryEntry([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out RegistryEntry? entry)
#else
        internal bool TryGetRegistryEntry([MaybeNullWhen(false)] out RegistryEntry entry)
#endif
        {
            return DefaultRegistry.GetEntries().TryGetValue(Identifier, out entry);
        }

        /// <summary>
        /// Gets the zone lines of the timezone, as the array shared with its field, and its identifier.
        /// </summary>
        /// <param name="identifier">The identifier of the timezone.</param>
        /// <returns>The zone lines, in chronological order.</returns>
        private TzDataZoneLine[] GetZoneLineArray(out string identifier)
        {
            // Only the default value has no identifier; it has the zone lines of Etc/UTC. Every other instance is a timezone field,
            // which has its zone lines.
            var value = _value;
            var zoneLines = _zoneLines;
            if (value is null || zoneLines is null)
                return Etc.UTC.GetZoneLineArray(out identifier);

            identifier = value;
            return zoneLines;
        }

        /// <summary>
        /// Gets the identifier of the canonical timezone that the timezone refers to, if it is a link.
        /// </summary>
        /// <param name="linkTarget">The identifier of the canonical timezone, if the timezone is a link.</param>
        /// <returns><see langword="true"/> if the timezone is a link; otherwise, <see langword="false"/>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private bool GetLinkTarget([NotNullWhen(true)] out string? linkTarget)
#else
        private bool GetLinkTarget([MaybeNullWhen(false)] out string linkTarget)
#endif
        {
            // Only the default value has no zone lines; it is Etc/UTC, which is not a link. Every other instance is a timezone field,
            // which has its link target, if any.
            linkTarget = _linkTarget;
            return linkTarget != null;
        }

        /// <summary>
        /// Gets the location metadata of the timezone, as listed in zone1970.tab, or in zone.tab for a link.
        /// </summary>
        /// <returns>The latitude, longitude, country codes and comment of the timezone.</returns>
#if NETSTANDARD
        [return: NotNull]
#endif
        private Location GetLocation()
        {
            // Only the default value has no location; it has the location of Etc/UTC. Every other instance is a timezone field, which has
            // its location.
            var location = _location;
            if (location is null)
                return Etc.UTC.GetLocation();

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
            internal Location(double latitude, double longitude, string[] isoCountryCodes, [AllowNull] string comment)
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
            [MaybeNull]
            internal string Comment { get; }
#endif
        }

        /// <summary>
        /// The timezone of a timezone field, with its zone lines, link target and location.
        /// </summary>
        internal sealed class RegistryEntry
        {
#if NETCOREAPP3_0_OR_GREATER
            internal RegistryEntry(TzDataTimezone timezone, TzDataZoneLine[] zoneLines, string? linkTarget, Location location)
#else
            internal RegistryEntry(TzDataTimezone timezone, TzDataZoneLine[] zoneLines, [AllowNull] string linkTarget, Location location)
#endif
            {
                Timezone = timezone;
                ZoneLines = zoneLines;
                LinkTarget = linkTarget;
                Location = location;
            }

            internal TzDataTimezone Timezone { get; }

            internal TzDataZoneLine[] ZoneLines { get; }

            /// <summary>
            /// Gets the identifier of the canonical timezone that the timezone refers to, if it is a link; otherwise, <see langword="null"/>.
            /// </summary>
#if NETCOREAPP3_0_OR_GREATER
            internal string? LinkTarget { get; }
#else
            [MaybeNull]
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
            [MaybeNull]
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
                        throw new InvalidOperationException($"Link timezone '{entry.Timezone._value}' refers to '{entry.LinkTarget}', which is not a canonical timezone field.");
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

                    entries.Add(timezone._value, new RegistryEntry(timezone, timezone._zoneLines, timezone._linkTarget, timezone._location));
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
