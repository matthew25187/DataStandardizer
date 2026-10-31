namespace DataStandardizer.Money
{
    /// <summary>
    /// Specifies which form of a currency symbol is required.
    /// </summary>
    /// <remarks>
    /// ISO 4217 does not define currency symbols, so the symbols exposed by this library are sourced from
    /// the Unicode Common Locale Data Repository (CLDR), which publishes two forms for many currencies.
    /// </remarks>
    public enum CurrencySymbolKind
    {
        /// <summary>
        /// The standard currency symbol; unambiguous in context.
        /// </summary>
        /// <remarks>
        /// Where a glyph is shared by several currencies, the standard form distinguishes between them,
        /// for example <c>NZ$</c> for the New Zealand Dollar and <c>CA$</c> for the Canadian Dollar.
        /// </remarks>
        Standard = 0,

        /// <summary>
        /// The narrow currency symbol; the shortest recognisable form, which may be ambiguous.
        /// </summary>
        /// <remarks>
        /// The same narrow symbol may be used by several currencies, so it should only be used where the
        /// currency is already clear from the context. For example, <c>$</c> is the narrow symbol of thirty
        /// different currencies. Where a currency has no distinct narrow form, the standard form is used.
        /// </remarks>
        Narrow = 1
    }
}
