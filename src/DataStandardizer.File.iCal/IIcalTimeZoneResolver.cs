#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Resolves a time zone identifier, as given by a <c>TZID</c> parameter, to the rules of the time zone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IcalTimeZoneResolver"/> provides the resolvers of this package and <see cref="IcalTimeZoneResolver.Chain"/>, which combines
    /// them.  <see cref="IcalTimeZoneResolver.Default"/> resolves only through the calendar's own VTIMEZONE components, as RFC 5545 requires.
    /// </para>
    /// <para>
    /// Resolution is synchronous.  A resolver must be safe to use from several threads at once.
    /// </para>
    /// </remarks>
    public interface IIcalTimeZoneResolver
    {
        /// <summary>
        /// Tries to resolve a time zone identifier.
        /// </summary>
        /// <param name="timeZoneId">The time zone identifier, as given by a <c>TZID</c> parameter.</param>
        /// <param name="calendar">The calendar the identifier appears in; or <c>null</c> if there is none.</param>
        /// <param name="rules">The rules of the time zone, if it was resolved.</param>
        /// <returns><c>true</c> if this resolver knows the time zone; otherwise <c>false</c>, so that the next resolver of a chain is tried.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="timeZoneId"/> is <c>null</c>.</exception>
        /// <exception cref="IcalFileException">The resolver found a definition of the time zone that is not valid.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        bool TryResolve(string timeZoneId, IcalCalendar? calendar, [NotNullWhen(true)] out IcalTimeZoneRules? rules);
#else
        bool TryResolve(string timeZoneId, IcalCalendar calendar, out IcalTimeZoneRules rules);
#endif
    }
}
