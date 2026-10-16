using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// One instance of the recurrence set (RFC 5545 §3.8.5) of an event, to-do or journal entry: when it starts and ends, and the component
    /// that describes it.
    /// </summary>
    /// <typeparam name="TComponent">The type of the component: <see cref="IcalEvent"/>, <see cref="IcalTodo"/> or
    /// <see cref="IcalJournal"/>.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="Start"/> and <see cref="End"/> are local values, in the form the calendar gives them: a DATE for an all-day instance, a
    /// floating DATE-TIME, or one in UTC or in a named time zone.  <see cref="StartUtc"/> and <see cref="EndUtc"/> give the same instants in
    /// UTC, and are <c>null</c> for a DATE or a floating time, which has no single instant.
    /// </para>
    /// <para>
    /// A local time is kept as the recurrence set gives it, even where it falls in a daylight saving gap; its instant in UTC follows RFC 5545
    /// §3.3.5, so 02:30 on the day clocks go forward from 02:00 to 03:00 is read with the offset in use before the gap.
    /// </para>
    /// </remarks>
    public sealed class IcalOccurrence<TComponent> where TComponent : IcalComponent
    {
        internal IcalOccurrence(TComponent component, bool isOverride, IcalDateOrDateTime recurrenceId, IcalDateOrDateTime start,
            IcalDateTime? startUtc, IcalDateOrDateTime? end, IcalDateTime? endUtc)
        {
            Component = component;
            IsOverride = isOverride;
            RecurrenceId = recurrenceId;
            Start = start;
            StartUtc = startUtc;
            End = end;
            EndUtc = endUtc;
        }

        /// <summary>
        /// Gets the component that describes the instance: the recurring component itself, or the component with a <c>RECURRENCE-ID</c>
        /// (RFC 5545 §3.8.4.4) that overrides it.
        /// </summary>
        public TComponent Component { get; }

        /// <summary>
        /// Gets a flag indicating if <see cref="Component"/> overrides the instance, either by naming it in its <c>RECURRENCE-ID</c> or, with
        /// <c>RANGE=THISANDFUTURE</c>, by naming an earlier instance.
        /// </summary>
        public bool IsOverride { get; }

        /// <summary>
        /// Gets the start the recurrence set gives the instance, before any override moves it: the value a <c>RECURRENCE-ID</c> names it by.
        /// </summary>
        public IcalDateOrDateTime RecurrenceId { get; }

        /// <summary>
        /// Gets the start, as a local value.
        /// </summary>
        public IcalDateOrDateTime Start { get; }

        /// <summary>
        /// Gets the start in UTC; or <c>null</c> if <see cref="Start"/> is a DATE or a floating time.
        /// </summary>
        public IcalDateTime? StartUtc { get; }

        /// <summary>
        /// Gets the end, as a local value; or <c>null</c> if the instance has none, as for a journal entry or a to-do with neither <c>DUE</c>
        /// nor <c>DURATION</c>.
        /// </summary>
        /// <remarks>
        /// The end is exclusive, so an all-day event on one date ends on the next.
        /// </remarks>
        public IcalDateOrDateTime? End { get; }

        /// <summary>
        /// Gets the end in UTC; or <c>null</c> if there is no <see cref="End"/>, or it is a DATE or a floating time.
        /// </summary>
        public IcalDateTime? EndUtc { get; }

        /// <summary>
        /// Gets the start and end of the instance.
        /// </summary>
        public override string ToString()
        {
            return End.HasValue ? Start + "/" + End.Value : Start.ToString();
        }
    }
}
