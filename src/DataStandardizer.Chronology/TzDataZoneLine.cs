using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// A period in the offset history of a TZ Database zone, corresponding to one line of a Zone entry in the source.
    /// </summary>
    /// <remarks>
    /// A zone line applies from the moment the preceding line ceases to apply, or from the beginning of time for the
    /// first line, until <see cref="Until"/>.
    /// </remarks>
    public sealed class TzDataZoneLine
    {
        private static readonly IReadOnlyList<TzDataRule> NoRules = new ReadOnlyCollection<TzDataRule>(new TzDataRule[0]);

        internal TzDataZoneLine(
            TimeSpan standardOffset,
            TzDataZoneRuleKind ruleKind,
            TimeSpan? fixedSave,
#if NETCOREAPP3_0_OR_GREATER
            TzDataRule[]? rules,
#else
            [CanBeNull] TzDataRule[] rules,
#endif
#if NETSTANDARD
            [NotNull]
#endif
            string format,
            TzDataUntil? until)
        {
            switch (ruleKind)
            {
                case TzDataZoneRuleKind.None:
                    if (fixedSave.HasValue)
                        throw new ArgumentException("A fixed save must not be specified where no rules apply.", nameof(fixedSave));
                    if (rules != null && rules.Length > 0)
                        throw new ArgumentException("Rules must not be specified where no rules apply.", nameof(rules));
                    break;

                case TzDataZoneRuleKind.FixedSave:
                    if (!fixedSave.HasValue)
                        throw new ArgumentException("A fixed save must be specified.", nameof(fixedSave));
                    if (rules != null && rules.Length > 0)
                        throw new ArgumentException("Rules must not be specified where a fixed save applies.", nameof(rules));
                    break;

                case TzDataZoneRuleKind.RuleSet:
                    if (fixedSave.HasValue)
                        throw new ArgumentException("A fixed save must not be specified where a rule set applies.", nameof(fixedSave));
                    if (rules == null)
                        throw new ArgumentNullException(nameof(rules));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(ruleKind), ruleKind, "Rule kind is not defined.");
            }

            if (format == null)
                throw new ArgumentNullException(nameof(format));

            if (format.Length == 0)
                throw new ArgumentException("Format must not be empty.", nameof(format));

            StandardOffset = standardOffset;
            RuleKind = ruleKind;
            FixedSave = fixedSave;
            // The rule array is shared between zone lines, so it is wrapped rather than exposed, to keep it from being cast back and modified.
            Rules = ruleKind == TzDataZoneRuleKind.RuleSet && rules != null ? new ReadOnlyCollection<TzDataRule>(rules) : NoRules;
            Format = format;
            Until = until;
        }

        /// <summary>
        /// Gets the offset of standard time from universal time, corresponding to the STDOFF column of the source.
        /// </summary>
        public TimeSpan StandardOffset { get; }

        /// <summary>
        /// Gets what the RULES column of the source contains.
        /// </summary>
        public TzDataZoneRuleKind RuleKind { get; }

        /// <summary>
        /// Gets the amount of time added to the standard offset throughout the zone line.
        /// </summary>
        /// <value>
        /// The amount, or <see langword="null"/> where <see cref="RuleKind"/> is not <see cref="TzDataZoneRuleKind.FixedSave"/>.
        /// </value>
        public TimeSpan? FixedSave { get; }

        /// <summary>
        /// Gets the daylight saving rules that apply to the zone line.
        /// </summary>
        /// <value>
        /// The rules, which are empty unless <see cref="RuleKind"/> is <see cref="TzDataZoneRuleKind.RuleSet"/>.
        /// </value>
#if NETSTANDARD
        [NotNull]
#endif
        public IReadOnlyList<TzDataRule> Rules { get; }

        /// <summary>
        /// Gets the format of the zone abbreviation, corresponding to the FORMAT column of the source.
        /// </summary>
        /// <value>
        /// The format verbatim from the source, for example <c>E%sT</c>, <c>GMT/BST</c> or <c>%z</c>.
        /// </value>
#if NETSTANDARD
        [NotNull]
#endif
        public string Format { get; }

        /// <summary>
        /// Gets the moment at which the zone line ceases to apply, corresponding to the UNTIL column of the source.
        /// </summary>
        /// <value>
        /// The moment, or <see langword="null"/> for the last line of a zone, which continues to apply indefinitely.
        /// </value>
        public TzDataUntil? Until { get; }
    }
}
