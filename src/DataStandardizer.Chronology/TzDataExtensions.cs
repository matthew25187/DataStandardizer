using System;
using System.Collections.Generic;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Chronology
{
    public static class TzDataExtensions
    {
        /// <summary>
        /// Get the comment on a timezone.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <returns>Comment for the timezone, if available; otherwise, <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetComment(this TzDataTimezone timezone)
#else
        [CanBeNull]
        public static string GetComment(this TzDataTimezone timezone)
#endif
        {
            var timezoneAttribute = GetTimezoneAttribute(timezone);
            return timezoneAttribute?.Comment;
        }

        /// <summary>
        /// Get the ISO country codes for the countries covered by a timezone.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <returns>A collection of ISO country codes related to the timezone.</returns>
        public static string[] GetIsoCountryCodes(this TzDataTimezone timezone)
        {
            var timezoneAttribute = GetTimezoneAttribute(timezone);
#if NET8_0_OR_GREATER
            return timezoneAttribute?.IsoCountryCodes ?? [];
#elif NETSTANDARD1_3_OR_GREATER || NET
            return timezoneAttribute?.IsoCountryCodes ?? Array.Empty<string>();
#else
            return timezoneAttribute?.IsoCountryCodes ?? new string[] { };
#endif
        }

        /// <summary>
        /// Get the latitude of the principal location within a timezone.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <returns>Latitude of the timezone.</returns>
        public static double GetLatitude(this TzDataTimezone timezone)
        {
            var timezoneAttribute = GetTimezoneAttribute(timezone);
            return timezoneAttribute?.Latitude ?? 0;
        }

        /// <summary>
        /// Get the longitude of the principal location within a timezone.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <returns>Longitude of the timezone.</returns>
        public static double GetLongitude(this TzDataTimezone timezone)
        {
            var timezoneAttribute = GetTimezoneAttribute(timezone);
            return timezoneAttribute?.Longitude ?? 0;
        }

        /// <summary>
        /// Get the zone line of a timezone in force at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="utc">The instant, in universal time. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns>The zone line in force at the instant.</returns>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETSTANDARD
        [NotNull]
#endif
        public static TzDataZoneLine GetZoneLine(this TzDataTimezone timezone, DateTime utc)
        {
            var calculator = timezone.GetCalculator();
            return calculator.GetZoneLine(ToUniversalInstant(utc, nameof(utc)));
        }

        /// <summary>
        /// Get the zone line of a timezone in force at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="instant">The instant.</param>
        /// <returns>The zone line in force at the instant.</returns>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETSTANDARD
        [NotNull]
#endif
        public static TzDataZoneLine GetZoneLine(this TzDataTimezone timezone, DateTimeOffset instant)
        {
            return GetZoneLine(timezone, instant.UtcDateTime);
        }

        /// <summary>
        /// Get the offset from universal time in effect in a timezone at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="utc">The instant, in universal time. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns>The total offset from universal time, including any daylight saving.</returns>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static TimeSpan GetUtcOffset(this TzDataTimezone timezone, DateTime utc)
        {
            return GetOffsetInfo(timezone, utc).UtcOffset;
        }

        /// <summary>
        /// Get the offset from universal time in effect in a timezone at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="instant">The instant.</param>
        /// <returns>The total offset from universal time, including any daylight saving.</returns>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static TimeSpan GetUtcOffset(this TzDataTimezone timezone, DateTimeOffset instant)
        {
            return GetUtcOffset(timezone, instant.UtcDateTime);
        }

        /// <summary>
        /// Get the offset of standard time from universal time in effect in a timezone at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="utc">The instant, in universal time. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns>The offset of standard time from universal time, excluding any daylight saving.</returns>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static TimeSpan GetStandardOffset(this TzDataTimezone timezone, DateTime utc)
        {
            return GetOffsetInfo(timezone, utc).StandardOffset;
        }

        /// <summary>
        /// Get the offset of standard time from universal time in effect in a timezone at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="instant">The instant.</param>
        /// <returns>The offset of standard time from universal time, excluding any daylight saving.</returns>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static TimeSpan GetStandardOffset(this TzDataTimezone timezone, DateTimeOffset instant)
        {
            return GetStandardOffset(timezone, instant.UtcDateTime);
        }

        /// <summary>
        /// Get the amount of time added to the standard offset of a timezone at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="utc">The instant, in universal time. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns>The amount of time saved, which may be negative, as for Europe/Dublin in winter.</returns>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static TimeSpan GetDaylightSavings(this TzDataTimezone timezone, DateTime utc)
        {
            return GetOffsetInfo(timezone, utc).DaylightSavings;
        }

        /// <summary>
        /// Get the amount of time added to the standard offset of a timezone at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="instant">The instant.</param>
        /// <returns>The amount of time saved, which may be negative, as for Europe/Dublin in winter.</returns>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static TimeSpan GetDaylightSavings(this TzDataTimezone timezone, DateTimeOffset instant)
        {
            return GetDaylightSavings(timezone, instant.UtcDateTime);
        }

        /// <summary>
        /// Determine whether daylight saving time is in effect in a timezone at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="utc">The instant, in universal time. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns><see langword="true"/> if daylight saving time is in effect; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The TZ Database decides which time is daylight saving time. Europe/Dublin saves a negative amount in winter, so its winter time is daylight saving time.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static bool IsDaylightSavingTime(this TzDataTimezone timezone, DateTime utc)
        {
            return GetOffsetInfo(timezone, utc).IsDaylightSavingTime;
        }

        /// <summary>
        /// Determine whether daylight saving time is in effect in a timezone at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="instant">The instant.</param>
        /// <returns><see langword="true"/> if daylight saving time is in effect; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The TZ Database decides which time is daylight saving time. Europe/Dublin saves a negative amount in winter, so its winter time is daylight saving time.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static bool IsDaylightSavingTime(this TzDataTimezone timezone, DateTimeOffset instant)
        {
            return IsDaylightSavingTime(timezone, instant.UtcDateTime);
        }

        /// <summary>
        /// Get the offset from universal time in effect in a timezone at an instant, with the period over which it is in effect.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="utc">The instant, in universal time. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns>The offset in effect at the instant.</returns>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static TzDataOffsetInfo GetOffsetInfo(this TzDataTimezone timezone, DateTime utc)
        {
            var calculator = timezone.GetCalculator();
            return calculator.GetOffsetInfo(ToUniversalInstant(utc, nameof(utc)));
        }

        /// <summary>
        /// Get the offset from universal time in effect in a timezone at an instant, with the period over which it is in effect.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="instant">The instant.</param>
        /// <returns>The offset in effect at the instant.</returns>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static TzDataOffsetInfo GetOffsetInfo(this TzDataTimezone timezone, DateTimeOffset instant)
        {
            return GetOffsetInfo(timezone, instant.UtcDateTime);
        }

        /// <summary>
        /// Get the abbreviation by which the time in a timezone is known at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="utc">The instant, in universal time. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns>The abbreviation in force at the instant, for example <c>EST</c>, <c>BST</c> or <c>+0545</c>.</returns>
        /// <remarks>
        /// The abbreviation is that of <see cref="TzDataOffsetInfo.Abbreviation"/>, formatted from the FORMAT of the zone line in force as zic formats it.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETSTANDARD
        [NotNull]
#endif
        public static string GetAbbreviation(this TzDataTimezone timezone, DateTime utc)
        {
            return GetOffsetInfo(timezone, utc).Abbreviation;
        }

        /// <summary>
        /// Get the abbreviation by which the time in a timezone is known at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="instant">The instant.</param>
        /// <returns>The abbreviation in force at the instant, for example <c>EST</c>, <c>BST</c> or <c>+0545</c>.</returns>
        /// <remarks>
        /// The abbreviation is that of <see cref="TzDataOffsetInfo.Abbreviation"/>, formatted from the FORMAT of the zone line in force as zic formats it.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETSTANDARD
        [NotNull]
#endif
        public static string GetAbbreviation(this TzDataTimezone timezone, DateTimeOffset instant)
        {
            return GetAbbreviation(timezone, instant.UtcDateTime);
        }

        /// <summary>
        /// Get the abbreviation by which standard time in a timezone is known, for the zone line and rule year in force at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="utc">The instant, in universal time. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns>The standard time abbreviation, for example <c>EST</c> for America/New_York or <c>GMT</c> for Europe/London.</returns>
        /// <remarks>
        /// <para>
        /// The abbreviation is formatted from the FORMAT of the zone line in force at the instant. <c>%s</c> is replaced by the
        /// LETTER of the latest standard time rule in effect in the year on the local clock, being one that takes effect in that
        /// year or, failing that, the latest to take effect before it. A FORMAT of the form <c>A/B</c> gives <c>A</c>, and
        /// <c>%z</c> gives the standard offset.
        /// </para>
        /// <para>
        /// The TZ Database decides which time is standard time. Europe/Dublin saves a negative amount in winter, so its standard time is <c>IST</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETSTANDARD
        [NotNull]
#endif
        public static string GetStandardAbbreviation(this TzDataTimezone timezone, DateTime utc)
        {
            var calculator = timezone.GetCalculator();
            return calculator.GetStandardAbbreviation(ToUniversalInstant(utc, nameof(utc)));
        }

        /// <summary>
        /// Get the abbreviation by which standard time in a timezone is known, for the zone line and rule year in force at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="instant">The instant.</param>
        /// <returns>The standard time abbreviation, for example <c>EST</c> for America/New_York or <c>GMT</c> for Europe/London.</returns>
        /// <remarks>
        /// <para>
        /// The abbreviation is formatted from the FORMAT of the zone line in force at the instant. <c>%s</c> is replaced by the
        /// LETTER of the latest standard time rule in effect in the year on the local clock, being one that takes effect in that
        /// year or, failing that, the latest to take effect before it. A FORMAT of the form <c>A/B</c> gives <c>A</c>, and
        /// <c>%z</c> gives the standard offset.
        /// </para>
        /// <para>
        /// The TZ Database decides which time is standard time. Europe/Dublin saves a negative amount in winter, so its standard time is <c>IST</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETSTANDARD
        [NotNull]
#endif
        public static string GetStandardAbbreviation(this TzDataTimezone timezone, DateTimeOffset instant)
        {
            return GetStandardAbbreviation(timezone, instant.UtcDateTime);
        }

        /// <summary>
        /// Get the abbreviation by which daylight saving time in a timezone is known, for the zone line and rule year in force at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="utc">The instant, in universal time. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns>
        /// The daylight saving time abbreviation, for example <c>EDT</c> for America/New_York or <c>BST</c> for Europe/London,
        /// or <see langword="null"/> where no daylight saving time is in effect in the year.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The abbreviation is formatted from the FORMAT of the zone line in force at the instant. <c>%s</c> is replaced by the
        /// LETTER of the latest daylight saving time rule in effect in the year on the local clock, being one that takes effect in
        /// that year or, failing that, the one in effect as the year begins. A FORMAT of the form <c>A/B</c> gives <c>B</c>, and
        /// <c>%z</c> gives the offset with daylight saving. A zone line that saves a fixed amount observes daylight saving time
        /// where the amount is not zero.
        /// </para>
        /// <para>
        /// The TZ Database decides which time is daylight saving time. Europe/Dublin saves a negative amount in winter, so its daylight saving time is <c>GMT</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetDaylightAbbreviation(this TzDataTimezone timezone, DateTime utc)
#else
        [CanBeNull]
        public static string GetDaylightAbbreviation(this TzDataTimezone timezone, DateTime utc)
#endif
        {
            var calculator = timezone.GetCalculator();
            return calculator.GetDaylightAbbreviation(ToUniversalInstant(utc, nameof(utc)));
        }

        /// <summary>
        /// Get the abbreviation by which daylight saving time in a timezone is known, for the zone line and rule year in force at an instant.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="instant">The instant.</param>
        /// <returns>
        /// The daylight saving time abbreviation, for example <c>EDT</c> for America/New_York or <c>BST</c> for Europe/London,
        /// or <see langword="null"/> where no daylight saving time is in effect in the year.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The abbreviation is formatted from the FORMAT of the zone line in force at the instant. <c>%s</c> is replaced by the
        /// LETTER of the latest daylight saving time rule in effect in the year on the local clock, being one that takes effect in
        /// that year or, failing that, the one in effect as the year begins. A FORMAT of the form <c>A/B</c> gives <c>B</c>, and
        /// <c>%z</c> gives the offset with daylight saving. A zone line that saves a fixed amount observes daylight saving time
        /// where the amount is not zero.
        /// </para>
        /// <para>
        /// The TZ Database decides which time is daylight saving time. Europe/Dublin saves a negative amount in winter, so its daylight saving time is <c>GMT</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetDaylightAbbreviation(this TzDataTimezone timezone, DateTimeOffset instant)
#else
        [CanBeNull]
        public static string GetDaylightAbbreviation(this TzDataTimezone timezone, DateTimeOffset instant)
#endif
        {
            return GetDaylightAbbreviation(timezone, instant.UtcDateTime);
        }

        /// <summary>
        /// Get the transitions between offsets from universal time that occur in a timezone over a period.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="fromUtc">The start of the period, in universal time, which is included. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <param name="toUtc">The end of the period, in universal time, which is excluded. A time of kind <see cref="DateTimeKind.Unspecified"/> is treated as universal time.</param>
        /// <returns>
        /// The transitions, in chronological order. A transition is reported where the offset, the standard offset,
        /// daylight saving time or the abbreviation changes.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="fromUtc"/> or <paramref name="toUtc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toUtc"/> precedes <paramref name="fromUtc"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETSTANDARD
        [NotNull]
#endif
        public static IEnumerable<TzDataTransition> GetTransitions(this TzDataTimezone timezone, DateTime fromUtc, DateTime toUtc)
        {
            // The arguments are checked here, rather than in the iterator, so that an invalid call throws at once.
            var calculator = timezone.GetCalculator();
            var fromInstant = ToUniversalInstant(fromUtc, nameof(fromUtc));
            var toInstant = ToUniversalInstant(toUtc, nameof(toUtc));

            if (toInstant < fromInstant)
                throw new ArgumentOutOfRangeException(nameof(toUtc), toUtc, "The end of the period must not precede its start.");

            return calculator.GetTransitions(fromInstant, toInstant);
        }

        /// <summary>
        /// Get the transitions between offsets from universal time that occur in a timezone over a period.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <param name="from">The start of the period, which is included.</param>
        /// <param name="to">The end of the period, which is excluded.</param>
        /// <returns>
        /// The transitions, in chronological order. A transition is reported where the offset, the standard offset,
        /// daylight saving time or the abbreviation changes.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="to"/> precedes <paramref name="from"/>.</exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
#if NETSTANDARD
        [NotNull]
#endif
        public static IEnumerable<TzDataTransition> GetTransitions(this TzDataTimezone timezone, DateTimeOffset from, DateTimeOffset to)
        {
            return GetTransitions(timezone, from.UtcDateTime, to.UtcDateTime);
        }

        private static DateTime ToUniversalInstant(DateTime utc, string parameterName)
        {
            if (utc.Kind == DateTimeKind.Local)
                throw new ArgumentException("The time must be a universal time, of kind Utc or Unspecified.", parameterName);

            return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static TzDataTimezoneAttribute? GetTimezoneAttribute(TzDataTimezone timezone)
#else
        [CanBeNull]
        private static TzDataTimezoneAttribute GetTimezoneAttribute(TzDataTimezone timezone)
#endif
        {
            if (!timezone.TryGetRegistryEntry(out var entry))
                return null;

            return entry.Field.GetCustomAttribute<TzDataTimezoneAttribute>();
        }
    }
}