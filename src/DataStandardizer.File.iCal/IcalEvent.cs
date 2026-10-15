using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// An event (RFC 5545 §3.6.1): the VEVENT component.
    /// </summary>
    public sealed class IcalEvent : IcalComponent
    {
        /// <summary>
        /// Creates an empty VEVENT component.
        /// </summary>
        public IcalEvent() : base(IcalComponentNames.Event)
        {
        }

        /// <summary>
        /// Gets the alarms (VALARM, RFC 5545 §3.6.6) of the component.
        /// </summary>
        public IEnumerable<IcalAlarm> Alarms
        {
            get { return GetComponents<IcalAlarm>(); }
        }

        /// <summary>
        /// Gets the attendees (RFC 5545 §3.8.4.1, <c>ATTENDEE</c>), as the properties themselves.
        /// </summary>
        /// <remarks>
        /// Each is a CAL-ADDRESS, whose parameters, such as <c>CN</c> and <c>PARTSTAT</c>, describe the attendee.  Change them through
        /// <see cref="IcalComponent.Properties"/>.
        /// </remarks>
        public IReadOnlyList<IcalContentLine> Attendees
        {
            get { return GetProperties(IcalPropertyNames.Attendee); }
        }

        /// <summary>
        /// Gets or sets the categories (RFC 5545 §3.8.1.2, <c>CATEGORIES</c>), from every <c>CATEGORIES</c> property, with TEXT escaping
        /// removed.
        /// </summary>
        /// <remarks>
        /// Setting a list replaces every <c>CATEGORIES</c> property with one holding the whole list; an empty list removes them.  The
        /// getter throws <see cref="IcalFileException"/> if an item has a backslash that does not start a valid escape.
        /// </remarks>
        public IReadOnlyList<string> Categories
        {
            get { return GetTextList(IcalPropertyNames.Categories); }
            set { SetTextList(IcalPropertyNames.Categories, value); }
        }

        /// <summary>
        /// Gets or sets the access classification (RFC 5545 §3.8.1.3, <c>CLASS</c>), such as <c>PUBLIC</c>; or <c>null</c> if there is
        /// none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Classification
#else
        public string Classification
#endif
        {
            get { return GetText(IcalPropertyNames.Classification); }
            set { SetText(IcalPropertyNames.Classification, value); }
        }

        /// <summary>
        /// Gets or sets the color (RFC 7986 §5.9, <c>COLOR</c>), a CSS3 color name such as <c>turquoise</c>; or <c>null</c> if there is
        /// none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Color
#else
        public string Color
#endif
        {
            get { return GetText(IcalPropertyNames.Color); }
            set { SetText(IcalPropertyNames.Color, value); }
        }

        /// <summary>
        /// Gets the conference systems (RFC 7986 §5.11, <c>CONFERENCE</c>), as the properties themselves.
        /// </summary>
        /// <remarks>
        /// Each is a URI, whose parameters, such as <c>FEATURE</c> and <c>LABEL</c>, describe it.  Change them through
        /// <see cref="IcalComponent.Properties"/>.
        /// </remarks>
        public IReadOnlyList<IcalContentLine> Conferences
        {
            get { return GetProperties(IcalPropertyNames.Conference); }
        }

        /// <summary>
        /// Gets or sets the date and time (RFC 5545 §3.8.7.1, <c>CREATED</c>), in UTC, at which the calendar information was created; or
        /// <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The <c>VALUE</c> and <c>TZID</c> parameters are read and written with the value.  The getter throws
        /// <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDateTime? Created
        {
            get { return GetDateTime(IcalPropertyNames.Created); }
            set { SetDateTime(IcalPropertyNames.Created, value); }
        }

        /// <summary>
        /// Gets or sets the description (RFC 5545 §3.8.1.5, <c>DESCRIPTION</c>); or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Description
#else
        public string Description
#endif
        {
            get { return GetText(IcalPropertyNames.Description); }
            set { SetText(IcalPropertyNames.Description, value); }
        }

        /// <summary>
        /// Gets or sets the end (RFC 5545 §3.8.2.2, <c>DTEND</c>), a DATE or a DATE-TIME; or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The <c>VALUE</c> and <c>TZID</c> parameters are read and written with the value.  The getter throws
        /// <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDateOrDateTime? DtEnd
        {
            get { return GetDateOrDateTime(IcalPropertyNames.DtEnd); }
            set { SetDateOrDateTime(IcalPropertyNames.DtEnd, value); }
        }

        /// <summary>
        /// Gets or sets the date and time (RFC 5545 §3.8.7.2, <c>DTSTAMP</c>), in UTC, at which the iCalendar object was created or the
        /// information last revised; or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The <c>VALUE</c> and <c>TZID</c> parameters are read and written with the value.  The getter throws
        /// <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDateTime? DtStamp
        {
            get { return GetDateTime(IcalPropertyNames.DtStamp); }
            set { SetDateTime(IcalPropertyNames.DtStamp, value); }
        }

        /// <summary>
        /// Gets or sets the start (RFC 5545 §3.8.2.4, <c>DTSTART</c>), a DATE or a DATE-TIME; or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The <c>VALUE</c> and <c>TZID</c> parameters are read and written with the value.  The getter throws
        /// <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDateOrDateTime? DtStart
        {
            get { return GetDateOrDateTime(IcalPropertyNames.DtStart); }
            set { SetDateOrDateTime(IcalPropertyNames.DtStart, value); }
        }

        /// <summary>
        /// Gets or sets the duration (RFC 5545 §3.8.2.5, <c>DURATION</c>); or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDuration? Duration
        {
            get { return GetDuration(IcalPropertyNames.Duration); }
            set { SetDuration(IcalPropertyNames.Duration, value, false); }
        }

        /// <summary>
        /// Gets the images (RFC 7986 §5.10, <c>IMAGE</c>), as the properties themselves.
        /// </summary>
        /// <remarks>
        /// Each is a URI or inline BINARY data, whose parameters, such as <c>DISPLAY</c> and <c>FMTTYPE</c>, say how to use it.  Change
        /// them through <see cref="IcalComponent.Properties"/>.
        /// </remarks>
        public IReadOnlyList<IcalContentLine> Images
        {
            get { return GetProperties(IcalPropertyNames.Image); }
        }

        /// <summary>
        /// Gets or sets the date and time (RFC 5545 §3.8.7.3, <c>LAST-MODIFIED</c>), in UTC, at which the information was last revised; or
        /// <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The <c>VALUE</c> and <c>TZID</c> parameters are read and written with the value.  The getter throws
        /// <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDateTime? LastModified
        {
            get { return GetDateTime(IcalPropertyNames.LastModified); }
            set { SetDateTime(IcalPropertyNames.LastModified, value); }
        }

        /// <summary>
        /// Gets or sets the location (RFC 5545 §3.8.1.7, <c>LOCATION</c>); or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Location
#else
        public string Location
#endif
        {
            get { return GetText(IcalPropertyNames.Location); }
            set { SetText(IcalPropertyNames.Location, value); }
        }

        /// <summary>
        /// Gets or sets the calendar address of the organizer (RFC 5545 §3.8.4.3, <c>ORGANIZER</c>); or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not a valid absolute URI.  The setter takes an absolute URI.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Uri? Organizer
#else
        public Uri Organizer
#endif
        {
            get { return GetUri(IcalPropertyNames.Organizer); }
            set { SetUri(IcalPropertyNames.Organizer, value, false); }
        }

        /// <summary>
        /// Gets or sets the priority (RFC 5545 §3.8.1.9, <c>PRIORITY</c>), from 0 (undefined) and 1 (highest) to 9 (lowest); or <c>null</c>
        /// if there is none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public int? Priority
        {
            get { return GetInteger(IcalPropertyNames.Priority); }
            set { SetInteger(IcalPropertyNames.Priority, value); }
        }

        /// <summary>
        /// Gets or sets the recurrence instance this component overrides (RFC 5545 §3.8.4.4, <c>RECURRENCE-ID</c>), a DATE or a DATE-TIME;
        /// or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The <c>VALUE</c> and <c>TZID</c> parameters are read and written with the value.  The getter throws
        /// <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDateOrDateTime? RecurrenceId
        {
            get { return GetDateOrDateTime(IcalPropertyNames.RecurrenceId); }
            set { SetDateOrDateTime(IcalPropertyNames.RecurrenceId, value); }
        }

        /// <summary>
        /// Gets or sets the recurrence rules (RFC 5545 §3.8.5.3, <c>RRULE</c>).
        /// </summary>
        /// <remarks>
        /// Setting a list replaces every <c>RRULE</c> property with one per rule; an empty list removes them.  The getter throws
        /// <see cref="IcalFileException"/> if a rule is not valid.
        /// </remarks>
        public IReadOnlyList<IcalRecurrenceRule> RecurrenceRules
        {
            get { return GetRecurrenceRules(IcalPropertyNames.RecurrenceRule); }
            set { SetRecurrenceRules(IcalPropertyNames.RecurrenceRule, value); }
        }

        /// <summary>
        /// Gets or sets the resources (RFC 5545 §3.8.1.10, <c>RESOURCES</c>), from every <c>RESOURCES</c> property, with TEXT escaping
        /// removed.
        /// </summary>
        /// <remarks>
        /// Setting a list replaces every <c>RESOURCES</c> property with one holding the whole list; an empty list removes them.  The getter
        /// throws <see cref="IcalFileException"/> if an item has a backslash that does not start a valid escape.
        /// </remarks>
        public IReadOnlyList<string> Resources
        {
            get { return GetTextList(IcalPropertyNames.Resources); }
            set { SetTextList(IcalPropertyNames.Resources, value); }
        }

        /// <summary>
        /// Gets or sets the revision sequence number (RFC 5545 §3.8.7.4, <c>SEQUENCE</c>); or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public int? Sequence
        {
            get { return GetInteger(IcalPropertyNames.Sequence); }
            set { SetInteger(IcalPropertyNames.Sequence, value); }
        }

        /// <summary>
        /// Gets or sets the status (RFC 5545 §3.8.1.11, <c>STATUS</c>), such as <c>CONFIRMED</c>; or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Status
#else
        public string Status
#endif
        {
            get { return GetText(IcalPropertyNames.Status); }
            set { SetText(IcalPropertyNames.Status, value); }
        }

        /// <summary>
        /// Gets or sets the summary (RFC 5545 §3.8.1.12, <c>SUMMARY</c>); or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Summary
#else
        public string Summary
#endif
        {
            get { return GetText(IcalPropertyNames.Summary); }
            set { SetText(IcalPropertyNames.Summary, value); }
        }

        /// <summary>
        /// Gets or sets the time transparency (RFC 5545 §3.8.2.7, <c>TRANSP</c>), <c>OPAQUE</c> or <c>TRANSPARENT</c>; or <c>null</c> if
        /// there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Transparency
#else
        public string Transparency
#endif
        {
            get { return GetText(IcalPropertyNames.Transparency); }
            set { SetText(IcalPropertyNames.Transparency, value); }
        }

        /// <summary>
        /// Gets or sets the persistent (RFC 5545 §3.8.4.7, <c>UID</c>), globally unique identifier; or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Uid
#else
        public string Uid
#endif
        {
            get { return GetText(IcalPropertyNames.Uid); }
            set { SetText(IcalPropertyNames.Uid, value); }
        }

        /// <summary>
        /// Gets or sets the location of a more dynamic rendition of the component (RFC 5545 §3.8.4.6, <c>URL</c>); or <c>null</c> if there
        /// is none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not a valid absolute URI.  The setter takes an absolute URI.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Uri? Url
#else
        public Uri Url
#endif
        {
            get { return GetUri(IcalPropertyNames.Url); }
            set { SetUri(IcalPropertyNames.Url, value, false); }
        }

        /// <summary>
        /// Gets the instances of the event's recurrence set (RFC 5545 §3.8.5) that start in a window, each resolved to UTC.
        /// </summary>
        /// <param name="calendar">The calendar the event is in, which holds the VTIMEZONE components that time zones are resolved
        /// from and the components that override instances; or <c>null</c> if there is none.</param>
        /// <param name="from">The earliest start to give, inclusive; or <c>null</c> for none.</param>
        /// <param name="to">The end of the window, exclusive; or <c>null</c> for none, which needs every <c>RRULE</c> to have COUNT or
        /// UNTIL.</param>
        /// <returns>The instances, in order of their start, generated as they are enumerated.</returns>
        /// <remarks>
        /// Time zones are resolved by <see cref="IcalTimeZoneResolver.Default"/>.  See <see cref="GetOccurrences(IcalCalendar, IcalDateTime?, IcalDateTime?, IIcalTimeZoneResolver, IcalOptions)"/> for how the
        /// instances are found.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="to"/> is <c>null</c> and an <c>RRULE</c> has neither COUNT nor UNTIL; or an
        /// <c>RRULE</c> has a frequency below DAILY, or BYHOUR, BYMINUTE or BYSECOND, and DTSTART is a DATE.</exception>
        /// <exception cref="InvalidOperationException">An <c>RRULE</c> breaks a constraint listed under <see cref="IcalRecurrenceRule.IsValid"/>.
        /// Also thrown while enumerating, when more consecutive periods than <see cref="IcalOptions.MaxRecurrenceIterations"/> give no
        /// occurrence.</exception>
        /// <exception cref="IcalFileException">A property the instances are found from is not valid, or a time zone cannot be
        /// resolved.</exception>
        /// <exception cref="ArgumentOutOfRangeException">While enumerating, an instance or its end falls after 9999-12-31.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IEnumerable<IcalOccurrence<IcalEvent>> GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to)
#else
        public IEnumerable<IcalOccurrence<IcalEvent>> GetOccurrences(IcalCalendar calendar, IcalDateTime? from, IcalDateTime? to)
#endif
        {
            return GetOccurrences(calendar, from, to, IcalTimeZoneResolver.Default, DefaultOptions);
        }

        /// <summary>
        /// Gets the instances of the event's recurrence set (RFC 5545 §3.8.5) that start in a window, each resolved to UTC.
        /// </summary>
        /// <param name="calendar">The calendar the event is in, which holds the VTIMEZONE components that time zones are resolved
        /// from and the components that override instances; or <c>null</c> if there is none.</param>
        /// <param name="from">The earliest start to give, inclusive; or <c>null</c> for none.</param>
        /// <param name="to">The end of the window, exclusive; or <c>null</c> for none, which needs every <c>RRULE</c> to have COUNT or
        /// UNTIL.</param>
        /// <param name="resolver">Resolves the time zones of zoned values, such as <c>Chain(Calendar, TzData)</c>.</param>
        /// <returns>The instances, in order of their start, generated as they are enumerated.</returns>
        /// <remarks>
        /// See <see cref="GetOccurrences(IcalCalendar, IcalDateTime?, IcalDateTime?, IIcalTimeZoneResolver, IcalOptions)"/> for how the instances are found.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="resolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="to"/> is <c>null</c> and an <c>RRULE</c> has neither COUNT nor UNTIL; or an
        /// <c>RRULE</c> has a frequency below DAILY, or BYHOUR, BYMINUTE or BYSECOND, and DTSTART is a DATE.</exception>
        /// <exception cref="InvalidOperationException">An <c>RRULE</c> breaks a constraint listed under <see cref="IcalRecurrenceRule.IsValid"/>.
        /// Also thrown while enumerating, when more consecutive periods than <see cref="IcalOptions.MaxRecurrenceIterations"/> give no
        /// occurrence.</exception>
        /// <exception cref="IcalFileException">A property the instances are found from is not valid, or a time zone cannot be
        /// resolved.</exception>
        /// <exception cref="ArgumentOutOfRangeException">While enumerating, an instance or its end falls after 9999-12-31.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IEnumerable<IcalOccurrence<IcalEvent>> GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver)
#else
        public IEnumerable<IcalOccurrence<IcalEvent>> GetOccurrences(IcalCalendar calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver)
#endif
        {
            return GetOccurrences(calendar, from, to, resolver, DefaultOptions);
        }

        /// <summary>
        /// Gets the instances of the event's recurrence set (RFC 5545 §3.8.5) that start in a window, each resolved to UTC.
        /// </summary>
        /// <param name="calendar">The calendar the event is in, which holds the VTIMEZONE components that time zones are resolved
        /// from and the components that override instances; or <c>null</c> if there is none.</param>
        /// <param name="from">The earliest start to give, inclusive; or <c>null</c> for none.</param>
        /// <param name="to">The end of the window, exclusive; or <c>null</c> for none, which needs every <c>RRULE</c> to have COUNT or
        /// UNTIL.</param>
        /// <param name="resolver">Resolves the time zones of zoned values, such as <c>Chain(Calendar, TzData)</c>.</param>
        /// <param name="options">The options, of which <see cref="IcalOptions.MaxRecurrenceIterations"/> applies.</param>
        /// <returns>The instances, in order of their start, generated as they are enumerated.</returns>
        /// <remarks>
        /// <para>
        /// The recurrence set is DTSTART, which is always the first instance even when no rule gives it, the occurrences of each <c>RRULE</c>,
        /// and the dates, date-times and periods of each <c>RDATE</c>.  DTSTART counts towards the COUNT of a rule that does not give it, as
        /// RFC 5545 §3.3.10 requires, so such a rule gives one occurrence fewer.  Each <c>EXDATE</c> removes an instance, DTSTART among them; a DATE
        /// removes every instance on that local date.  An instance given more than once appears once.
        /// </para>
        /// <para>
        /// Each instance ends as RFC 5545 §3.8.5.3 sets out.  <c>DTEND</c> gives every instance the exact duration it gives DTSTART, so the
        /// local time of the end can change across a daylight saving change.  <c>DURATION</c> is added to each start: its weeks and days are
        /// nominal and keep the local time of day, and its hours, minutes and seconds are exact, so <c>P1D</c> and <c>PT24H</c> differ across a
        /// change.  An <c>RDATE</c> period gives its instance its own end.  With neither <c>DTEND</c> nor <c>DURATION</c>, an instance ends when
        /// it starts, or a day later for a DATE (RFC 5545 §3.6.1).
        /// </para>
        /// <para>
        /// Each event of <paramref name="calendar"/> with the same <c>UID</c> and a <c>RECURRENCE-ID</c> replaces the instance it names, with
        /// the start and end of its own <c>DTSTART</c>, <c>DTEND</c> and <c>DURATION</c>; of two for the same instance, the one with the higher
        /// <c>SEQUENCE</c> wins, then the later.  With <c>RANGE=THISANDFUTURE</c>, it also replaces every later instance: each is moved by the
        /// change it makes to the start, in the local time of DTSTART, and takes its duration.  An override is matched to an instance by its
        /// instant, and is ignored if an <c>EXDATE</c> removes that instance.  An event that has a <c>RECURRENCE-ID</c> itself gives only its
        /// own instance, so when expanding every event of a calendar, skip those whose recurring event is in it too.
        /// </para>
        /// <para>
        /// The instances are generated in local time, and each is resolved to UTC through <paramref name="resolver"/> as RFC 5545 §3.3.5
        /// requires: a time in a daylight saving gap is read with the offset in use before the gap, and a time in an overlap takes its first
        /// occurrence.  So a daily event at 09:00 stays at 09:00 local time while its time in UTC moves by an hour.  Floating times stay
        /// floating, and DATE values stay dates; neither has an instant, so each is compared with the window by its wall-clock time, as though
        /// it were in UTC, as is a floating <paramref name="from"/> or <paramref name="to"/>.
        /// </para>
        /// <para>
        /// An instance is given when its start, after any override, falls in the window.  The properties are read, and the time zones of
        /// DTSTART and the window resolved, when this is called; the instances are generated as they are enumerated.  An event with no
        /// DTSTART has no instances.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="resolver"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="to"/> is <c>null</c> and an <c>RRULE</c> has neither COUNT nor UNTIL; an
        /// <c>RRULE</c> has a frequency below DAILY, or BYHOUR, BYMINUTE or BYSECOND, and DTSTART is a DATE; or
        /// <see cref="IcalOptions.MaxRecurrenceIterations"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">An <c>RRULE</c> breaks a constraint listed under <see cref="IcalRecurrenceRule.IsValid"/>.
        /// Also thrown while enumerating, when more consecutive periods than <see cref="IcalOptions.MaxRecurrenceIterations"/> give no
        /// occurrence.</exception>
        /// <exception cref="IcalFileException">A property the instances are found from is not valid, or a time zone cannot be
        /// resolved.</exception>
        /// <exception cref="ArgumentOutOfRangeException">While enumerating, an instance or its end falls after 9999-12-31.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IEnumerable<IcalOccurrence<IcalEvent>> GetOccurrences(IcalCalendar? calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver,
            IcalOptions options)
#else
        public IEnumerable<IcalOccurrence<IcalEvent>> GetOccurrences(IcalCalendar calendar, IcalDateTime? from, IcalDateTime? to, IIcalTimeZoneResolver resolver,
            IcalOptions options)
#endif
        {
            return IcalOccurrenceExpander<IcalEvent>.GetOccurrences(this, calendar, from, to, resolver, options);
        }
    }
}
