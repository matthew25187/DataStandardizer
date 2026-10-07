namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Identifies what the RULES column of a TZ Database zone line contains.
    /// </summary>
    public enum TzDataZoneRuleKind
    {
        /// <summary>
        /// No rules apply, and standard time is always in effect. Written as <c>-</c>.
        /// </summary>
        None,

        /// <summary>
        /// A fixed amount of saved time is always in effect, for example <c>1:00</c>.
        /// </summary>
        FixedSave,

        /// <summary>
        /// The named set of daylight saving rules applies.
        /// </summary>
        RuleSet
    }
}
