using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// An alarm (RFC 5545 §3.6.6): the VALARM component.
    /// </summary>
    public sealed class IcalAlarm : IcalComponent
    {
        /// <summary>
        /// Creates an empty VALARM component.
        /// </summary>
        public IcalAlarm() : base("VALARM")
        {
        }

        /// <summary>
        /// Gets or sets an absolute trigger (RFC 5545 §3.8.6.3, <c>TRIGGER</c>), a DATE-TIME in UTC; or <c>null</c> if there is no trigger, or it
        /// is relative.
        /// </summary>
        /// <remarks>
        /// The setter writes the <c>VALUE=DATE-TIME</c> parameter that RFC 5545 requires.  Setting <c>null</c> removes the trigger, whichever
        /// kind it is.  A getter throws <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDateTime? TriggerDateTime
        {
            get
            {
                var trigger = GetProperty("TRIGGER");
                return trigger is null || IcalPropertyValue.GetValueType(trigger) != IcalValueType.DateTime ? null : GetDateTime("TRIGGER");
            }
            set
            {
                if (value.HasValue)
                {
                    RemoveProperties("TRIGGER");
                    Properties.Add(new IcalContentLine("TRIGGER", new[] { new IcalContentLineParameter("VALUE", "DATE-TIME") },
                        value.Value.ToString(IcalValueFormat.Text)));
                }
                else
                {
                    RemoveProperties("TRIGGER");
                }
            }
        }

        /// <summary>
        /// Gets or sets a relative trigger (RFC 5545 §3.8.6.3, <c>TRIGGER</c>), a DURATION; or <c>null</c> if there is no trigger, or it is
        /// absolute.
        /// </summary>
        /// <remarks>
        /// The duration is relative to the start of the event or to-do, or to its end when the property has a <c>RELATED=END</c> parameter.
        /// Setting <c>null</c> removes the trigger, whichever kind it is.  A getter throws <see cref="IcalFileException"/> if the value is not
        /// valid.
        /// </remarks>
        public IcalDuration? TriggerDuration
        {
            get
            {
                var trigger = GetProperty("TRIGGER");
                return trigger is null || IcalPropertyValue.GetValueType(trigger) != IcalValueType.Duration ? null : GetDuration("TRIGGER");
            }
            set
            {
                var trigger = GetProperty("TRIGGER");
                if (trigger != null && IcalPropertyValue.GetValueType(trigger) != IcalValueType.Duration)
                {
                    RemoveProperties("TRIGGER");
                }

                SetDuration("TRIGGER", value, false);
            }
        }

        /// <summary>
        /// Gets or sets the action (RFC 5545 §3.8.6.1, <c>ACTION</c>), such as <c>AUDIO</c>, <c>DISPLAY</c> or <c>EMAIL</c>; or <c>null</c>
        /// if there is none.
        /// </summary>
        /// <remarks>
        /// TEXT escaping is removed by the getter and added by the setter.  The getter throws <see cref="IcalFileException"/> if the value
        /// has a backslash that does not start a valid escape.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Action
#else
        public string Action
#endif
        {
            get { return GetText("ACTION"); }
            set { SetText("ACTION", value); }
        }

        /// <summary>
        /// Gets the attachments (RFC 5545 §3.8.1.1, <c>ATTACH</c>), as the properties themselves.
        /// </summary>
        /// <remarks>
        /// Change them through <see cref="IcalComponent.Properties"/>.
        /// </remarks>
        public IReadOnlyList<IcalContentLine> Attachments
        {
            get { return GetProperties("ATTACH"); }
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
        /// Gets or sets the duration (RFC 5545 §3.8.2.5, <c>DURATION</c>); or <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public IcalDuration? Duration
        {
            get { return GetDuration("DURATION"); }
            set { SetDuration("DURATION", value, false); }
        }

        /// <summary>
        /// Gets or sets the number of times the alarm repeats after it is first triggered (RFC 5545 §3.8.6.2, <c>REPEAT</c>); or
        /// <c>null</c> if there is none.
        /// </summary>
        /// <remarks>
        /// The getter throws <see cref="IcalFileException"/> if the value is not valid.
        /// </remarks>
        public int? Repeat
        {
            get { return GetInteger("REPEAT"); }
            set { SetInteger("REPEAT", value); }
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
            get { return GetText("SUMMARY"); }
            set { SetText("SUMMARY", value); }
        }
    }
}
