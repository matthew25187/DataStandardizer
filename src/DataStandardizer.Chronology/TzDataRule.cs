using System;
using System.Diagnostics;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// A daylight saving rule from the TZ Database, corresponding to one Rule line of the source.
    /// </summary>
    /// <remarks>
    /// A rule describes a transition that recurs once in each year from <see cref="FromYear"/> to <see cref="ToYear"/>
    /// inclusive, after which <see cref="Save"/> is added to the standard offset of any zone line that uses it.
    /// </remarks>
    [DebuggerDisplay("Rule {Name,nq} {FromYear}-{ToYear} month {Month}")]
    public sealed class TzDataRule
    {
        internal TzDataRule(
#if NETSTANDARD
            [NotNull]
#endif
            string name,
            int fromYear,
            int toYear,
            int month,
            TzDataDayKind dayKind,
            int? day,
            DayOfWeek? dayOfWeek,
            TimeSpan atTime,
            TzDataTimeReference atTimeReference,
            TimeSpan save,
            bool isDaylight,
#if NETSTANDARD
            [NotNull]
#endif
            string letter)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (letter == null)
                throw new ArgumentNullException(nameof(letter));

            if (toYear < fromYear)
                throw new ArgumentOutOfRangeException(nameof(toYear), toYear, "The last year must not precede the first year.");

            if (atTimeReference < TzDataTimeReference.Wall || atTimeReference > TzDataTimeReference.Universal)
                throw new ArgumentOutOfRangeException(nameof(atTimeReference), atTimeReference, "Time reference is not defined.");

            TzDataDayRule.Validate(month, dayKind, day, dayOfWeek);

            Name = name;
            FromYear = fromYear;
            ToYear = toYear;
            Month = month;
            DayKind = dayKind;
            Day = day;
            DayOfWeek = dayOfWeek;
            AtTime = atTime;
            AtTimeReference = atTimeReference;
            Save = save;
            IsDaylight = isDaylight;
            Letter = letter;
        }

        /// <summary>
        /// Gets the name of the rule set the rule belongs to, as it appears in the source.
        /// </summary>
        /// <remarks>
        /// Kept for diagnostics only. The TZ Database does not treat rule names as stable, so they are not public.
        /// </remarks>
        internal string Name { get; }

        /// <summary>
        /// Gets the first year in which the rule applies.
        /// </summary>
        public int FromYear { get; }

        /// <summary>
        /// Gets the last year in which the rule applies, or <see cref="int.MaxValue"/> where the rule applies indefinitely (<c>max</c>).
        /// </summary>
        public int ToYear { get; }

        /// <summary>
        /// Gets the month, from 1 to 12, in which the transition occurs.
        /// </summary>
        public int Month { get; }

        /// <summary>
        /// Gets how the day of the transition is specified.
        /// </summary>
        public TzDataDayKind DayKind { get; }

        /// <summary>
        /// Gets the day of the month on which the transition occurs, or from which the weekday is counted.
        /// </summary>
        /// <value>
        /// The day of the month, or <see langword="null"/> where <see cref="DayKind"/> is <see cref="TzDataDayKind.LastWeekday"/>.
        /// </value>
        public int? Day { get; }

        /// <summary>
        /// Gets the weekday on which the transition occurs.
        /// </summary>
        /// <value>
        /// The weekday, or <see langword="null"/> where <see cref="DayKind"/> is <see cref="TzDataDayKind.DayOfMonth"/>.
        /// </value>
        public DayOfWeek? DayOfWeek { get; }

        /// <summary>
        /// Gets the time of day at which the transition occurs, measured against <see cref="AtTimeReference"/>.
        /// </summary>
        /// <remarks>
        /// The time may be 24 hours or more, denoting a time on a following day.
        /// </remarks>
        public TimeSpan AtTime { get; }

        /// <summary>
        /// Gets the clock against which <see cref="AtTime"/> is measured.
        /// </summary>
        public TzDataTimeReference AtTimeReference { get; }

        /// <summary>
        /// Gets the amount of time added to the standard offset once the transition has occurred.
        /// </summary>
        /// <remarks>
        /// The amount may be negative, as for Europe/Dublin, or less than an hour, as for Australia/Lord_Howe.
        /// </remarks>
        public TimeSpan Save { get; }

        /// <summary>
        /// Gets a value indicating whether the time in effect once the transition has occurred is daylight saving time.
        /// </summary>
        public bool IsDaylight { get; }

        /// <summary>
        /// Gets the variable part of the zone abbreviation, substituted for <c>%s</c> in a zone line format.
        /// </summary>
        /// <value>
        /// The letters, for example <c>D</c> or <c>S</c>, or an empty string where the source gives <c>-</c>.
        /// </value>
#if NETSTANDARD
        [NotNull]
#endif
        public string Letter { get; }

        /// <summary>
        /// Gets the local date on which the transition occurs in the given year.
        /// </summary>
        /// <param name="year">The year, which must lie from <see cref="FromYear"/> to <see cref="ToYear"/> inclusive.</param>
        /// <returns>
        /// The date of the transition, at midnight and of kind <see cref="DateTimeKind.Unspecified"/>. The date is
        /// read against <see cref="AtTimeReference"/>, and <see cref="AtTime"/> is not applied to it.
        /// </returns>
        /// <remarks>
        /// A weekday counted from a day of the month may fall outside <see cref="Month"/>, as for <c>Fri&lt;=1</c>,
        /// which falls in the preceding month whenever the first is not itself a Friday.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="year"/> is outside the years in which the rule applies or the range supported by
        /// <see cref="DateTime"/>, or the rule names February 29 and <paramref name="year"/> is not a leap year.
        /// </exception>
        public DateTime GetTransitionDate(int year)
        {
            if (year < FromYear || year > ToYear)
                throw new ArgumentOutOfRangeException(nameof(year), year, $"The rule applies only in the years {FromYear} - {ToYear}.");

            return TzDataDayRule.Resolve(year, Month, DayKind, Day, DayOfWeek);
        }
    }
}
