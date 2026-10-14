using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Free or busy time information (RFC 5545 §3.6.4): the VFREEBUSY component.
    /// </summary>
    public sealed class IcalFreeBusy : IcalComponent
    {
        /// <summary>
        /// Creates an empty VFREEBUSY component.
        /// </summary>
        public IcalFreeBusy() : base("VFREEBUSY")
        {
        }

        /// <summary>
        /// Gets the free or busy time periods (RFC 5545 §3.8.2.6, <c>FREEBUSY</c>), from every <c>FREEBUSY</c> property.
        /// </summary>
        /// <remarks>
        /// The <c>FBTYPE</c> parameter, which says whether the time is free or busy, is on the properties themselves; see
        /// <see cref="IcalComponent.GetProperties"/>.  Throws <see cref="IcalFileException"/> if a period is not valid.
        /// </remarks>
        public IReadOnlyList<IcalPeriod> Periods
        {
            get
            {
                var periods = new List<IcalPeriod>();
                foreach (var property in GetProperties("FREEBUSY"))
                {
                    foreach (var item in property.Value.Split(','))
                    {
                        IcalPeriod period;
                        if (!IcalPeriod.TryParse(item.AsSpan(), IcalValueFormat.Text, out period))
                            throw IcalPropertyValue.BuildInvalidValueException(property, IcalValueType.Period);

                        periods.Add(period);
                    }
                }

                return periods;
            }
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
            get { return GetProperties("ATTENDEE"); }
        }

        /// <summary>
        /// Gets or sets the contact information (RFC 5545 §3.8.4.2, <c>CONTACT</c>); or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Contact
#else
        public string Contact
#endif
        {
            get { return GetText("CONTACT"); }
            set { SetText("CONTACT", value); }
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
            get { return GetDateOrDateTime("DTEND"); }
            set { SetDateOrDateTime("DTEND", value); }
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
            get { return GetDateTime("DTSTAMP"); }
            set { SetDateTime("DTSTAMP", value); }
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
            get { return GetDateOrDateTime("DTSTART"); }
            set { SetDateOrDateTime("DTSTART", value); }
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
            get { return GetUri("ORGANIZER"); }
            set { SetUri("ORGANIZER", value, false); }
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
    }
}
