using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// An iCalendar object (RFC 5545 §3.4): the VCALENDAR component, holding calendar properties and calendar components.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The calendar properties include those that RFC 7986 adds: <c>NAME</c>, <c>DESCRIPTION</c>, <c>UID</c>, <c>LAST-MODIFIED</c>,
    /// <c>URL</c>, <c>CATEGORIES</c>, <c>REFRESH-INTERVAL</c>, <c>SOURCE</c>, <c>COLOR</c> and <c>IMAGE</c>.
    /// </para>
    /// <para>
    /// RFC 5545 defines iCalendar 2.0 only.  <see cref="IcalReader"/> rejects a calendar whose <c>VERSION</c> does not include 2.0, and
    /// <see cref="IcalWriter"/> always writes <c>VERSION:2.0</c>.
    /// </para>
    /// </remarks>
    public sealed class IcalCalendar : IcalComponent
    {
        /// <summary>
        /// Creates a calendar with a <c>VERSION:2.0</c> property and nothing else.
        /// </summary>
        public IcalCalendar() : base("VCALENDAR")
        {
            Properties.Add(new IcalContentLine("VERSION", IcalConformance.SupportedVersion));
        }

        /// <summary>
        /// Creates a calendar with no properties, for a reader to fill.
        /// </summary>
        internal IcalCalendar(bool isEmpty) : base("VCALENDAR")
        {
        }

        /// <summary>
        /// Gets the events (VEVENT) of the calendar.
        /// </summary>
        public IEnumerable<IcalEvent> Events
        {
            get { return GetComponents<IcalEvent>(); }
        }

        /// <summary>
        /// Gets the free or busy time information (VFREEBUSY) of the calendar.
        /// </summary>
        public IEnumerable<IcalFreeBusy> FreeBusy
        {
            get { return GetComponents<IcalFreeBusy>(); }
        }

        /// <summary>
        /// Gets the journal entries (VJOURNAL) of the calendar.
        /// </summary>
        public IEnumerable<IcalJournal> Journals
        {
            get { return GetComponents<IcalJournal>(); }
        }

        /// <summary>
        /// Gets the time zone definitions (VTIMEZONE) of the calendar.
        /// </summary>
        public IEnumerable<IcalTimeZone> TimeZones
        {
            get { return GetComponents<IcalTimeZone>(); }
        }

        /// <summary>
        /// Gets the to-dos (VTODO) of the calendar.
        /// </summary>
        public IEnumerable<IcalTodo> Todos
        {
            get { return GetComponents<IcalTodo>(); }
        }

        /// <summary>
        /// Gets or sets the name of the calendar (RFC 7986 §5.1, <c>NAME</c>), from the first <c>NAME</c> property; or <c>null</c> if there
        /// is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? CalendarName
#else
        public string CalendarName
#endif
        {
            get { return GetText("NAME"); }
            set { SetText("NAME", value); }
        }

        /// <summary>
        /// Gets or sets the calendar scale (RFC 5545 §3.7.1, <c>CALSCALE</c>), such as <c>GREGORIAN</c>; or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? CalendarScale
#else
        public string CalendarScale
#endif
        {
            get { return GetText("CALSCALE"); }
            set { SetText("CALSCALE", value); }
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
            get { return GetTextList("CATEGORIES"); }
            set { SetTextList("CATEGORIES", value); }
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
            get { return GetText("COLOR"); }
            set { SetText("COLOR", value); }
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
            get { return GetText("DESCRIPTION"); }
            set { SetText("DESCRIPTION", value); }
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
            get { return GetProperties("IMAGE"); }
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
            get { return GetDateTime("LAST-MODIFIED"); }
            set { SetDateTime("LAST-MODIFIED", value); }
        }

        /// <summary>
        /// Gets or sets the iTIP method of the calendar (RFC 5545 §3.7.2, <c>METHOD</c>), such as <c>PUBLISH</c>; or <c>null</c> if there
        /// is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Method
#else
        public string Method
#endif
        {
            get { return GetText("METHOD"); }
            set { SetText("METHOD", value); }
        }

        /// <summary>
        /// Gets or sets the identifier of the product that created the calendar (RFC 5545 §3.7.3, <c>PRODID</c>); or <c>null</c> if there
        /// is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? ProductId
#else
        public string ProductId
#endif
        {
            get { return GetText("PRODID"); }
            set { SetText("PRODID", value); }
        }

        /// <summary>
        /// Gets or sets the suggested minimum interval between polls for changes to the calendar (RFC 7986 §5.7, <c>REFRESH-INTERVAL</c>);
        /// or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The property needs a <c>VALUE=DURATION</c> parameter, which the setter writes.  The getter throws
        /// <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDuration? RefreshInterval
        {
            get { return GetDuration("REFRESH-INTERVAL"); }
            set { SetDuration("REFRESH-INTERVAL", value, true); }
        }

        /// <summary>
        /// Gets or sets the location from which the calendar can be refreshed (RFC 7986 §5.8, <c>SOURCE</c>); or <c>null</c> if there is
        /// none.
        /// </summary>
        /// <remarks>
        /// The property needs a <c>VALUE=URI</c> parameter, which the setter writes.  The getter throws <see cref="IcalFileException"/> if
        /// the value is not a valid absolute URI.  The setter takes an absolute URI.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Uri? Source
#else
        public Uri Source
#endif
        {
            get { return GetUri("SOURCE"); }
            set { SetUri("SOURCE", value, true); }
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
            get { return GetText("UID"); }
            set { SetText("UID", value); }
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
            get { return GetUri("URL"); }
            set { SetUri("URL", value, false); }
        }

        /// <summary>
        /// Gets or sets the iCalendar version (RFC 5545 §3.7.4, <c>VERSION</c>), as written: <c>2.0</c>, or a range such as <c>1.0;2.0</c>;
        /// or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Version
#else
        public string Version
#endif
        {
            get { return GetText("VERSION"); }
            set { SetText("VERSION", value); }
        }
    }
}
