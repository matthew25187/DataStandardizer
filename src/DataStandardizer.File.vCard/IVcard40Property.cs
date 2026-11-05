using System;
using System.Collections.Generic;
using DataStandardizer.Communication.Mime;
using DataStandardizer.Geography;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A property of a vCard 4.0 card, in the shape RFC 6350 gives it.
    /// </summary>
    /// <remarks>
    /// <see cref="VcardProperty{TValue}"/> implements this explicitly, as a versioned view of its common members.  It exposes only what vCard 4.0 defines, not the underlying
    /// content line, so code written against it can use nothing else.  See <see cref="IVcard40Card"/>.
    /// </remarks>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public interface IVcard40Property<out TValue>
    {
        /// <summary>
        /// Gets the name of the property, as written.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        TValue Value { get; }

        /// <summary>
        /// Gets the <c>CC</c> (RFC 8605 §3.1), the country code of the country name of an address; or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        Iso3166Part1Alpha2Country? CountryCode { get; }

        /// <summary>
        /// Gets the parameters of the property that RFC 6350 and its extensions don't define, in the order they appear.
        /// </summary>
        IReadOnlyList<VcardContentLineParameter> ExtensionParameters { get; }

        /// <summary>
        /// Gets the <c>INDEX</c> (RFC 6715 §3.1), the position of the value among the instances of the property; or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        long? Index { get; }

        /// <summary>
        /// Gets the <c>LANGUAGE</c> (RFC 6350 §5.1); or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        Bcp47LanguageTag? Language { get; }

        /// <summary>
        /// Gets the <c>LEVEL</c> (RFC 6715 §3.2); or <c>null</c> if the property doesn't have one that is valid for it.
        /// </summary>
        VcardLevel? Level { get; }

        /// <summary>
        /// Gets the <c>MEDIATYPE</c> (RFC 6350 §5.7); or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        MimeMediaType? MediaType { get; }

        /// <summary>
        /// Gets the <c>PID</c> values (RFC 6350 §5.5) that are valid.
        /// </summary>
        IReadOnlyList<VcardPid> Pid { get; }

        /// <summary>
        /// Gets the <c>PREF</c> (RFC 6350 §5.3), from 1 (most preferred) to 100; or <c>null</c> if the property doesn't have a valid one, which makes it least preferred.
        /// </summary>
        int? Pref { get; }

        /// <summary>
        /// Gets the <c>SORT-AS</c> strings (RFC 6350 §5.9).
        /// </summary>
        IReadOnlyList<string> SortAs { get; }

        /// <summary>
        /// Gets the <c>TYPE</c> values (RFC 6350 §5.6), as written.  Whether they were written as a parameter list or a value list makes no difference.
        /// </summary>
        IReadOnlyList<string> Types { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the <c>ALTID</c> (RFC 6350 §5.4); or <c>null</c> if the property doesn't have one.
        /// </summary>
        string? AltId { get; }

        /// <summary>
        /// Gets the <c>CALSCALE</c> (RFC 6350 §5.8); or <c>null</c> if the property doesn't have one.
        /// </summary>
        string? CalendarScale { get; }

        /// <summary>
        /// Gets the <c>GEO</c> (RFC 6350 §5.10); or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        Uri? GeoLocation { get; }

        /// <summary>
        /// Gets the group (RFC 6350 §3.3), as written and without its '.'; or <c>null</c> if the property has none.
        /// </summary>
        string? Group { get; }

        /// <summary>
        /// Gets the <c>LABEL</c> (RFC 6350 §6.3.1); or <c>null</c> if the property doesn't have one.
        /// </summary>
        string? Label { get; }

        /// <summary>
        /// Gets the <c>TZ</c> (RFC 6350 §5.11); or <c>null</c> if the property doesn't have one.
        /// </summary>
        string? TimeZone { get; }
#else
        /// <summary>
        /// Gets the <c>ALTID</c> (RFC 6350 §5.4); or <c>null</c> if the property doesn't have one.
        /// </summary>
        string AltId { get; }

        /// <summary>
        /// Gets the <c>CALSCALE</c> (RFC 6350 §5.8); or <c>null</c> if the property doesn't have one.
        /// </summary>
        string CalendarScale { get; }

        /// <summary>
        /// Gets the <c>GEO</c> (RFC 6350 §5.10); or <c>null</c> if the property doesn't have a valid one.
        /// </summary>
        Uri GeoLocation { get; }

        /// <summary>
        /// Gets the group (RFC 6350 §3.3), as written and without its '.'; or <c>null</c> if the property has none.
        /// </summary>
        string Group { get; }

        /// <summary>
        /// Gets the <c>LABEL</c> (RFC 6350 §6.3.1); or <c>null</c> if the property doesn't have one.
        /// </summary>
        string Label { get; }

        /// <summary>
        /// Gets the <c>TZ</c> (RFC 6350 §5.11); or <c>null</c> if the property doesn't have one.
        /// </summary>
        string TimeZone { get; }
#endif
    }
}
