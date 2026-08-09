using System;

namespace DataStandardizer.Money
{
    /// <summary>
    /// Determines which elements are permitted in a string being parsed as a monetary value.
    /// </summary>
    /// <remarks>
    /// These styles serve the same purpose for <see cref="Money"/> as <see cref="System.Globalization.NumberStyles"/>
    /// does for the intrinsic numeric types: they let a caller accept only the elements they expect, so that
    /// input which happens to be well formed but is not of the expected shape is rejected rather than parsed.
    /// </remarks>
    [Flags]
    public enum MoneyStyles : uint
    {
        /// <summary>
        /// No elements other than decimal digits are permitted.
        /// </summary>
        None = 0,

        /// <summary>
        /// Leading white space characters are permitted.
        /// </summary>
        AllowLeadingWhite = 1 << 0,

        /// <summary>
        /// Trailing white space characters are permitted.
        /// </summary>
        AllowTrailingWhite = 1 << 1,

        /// <summary>
        /// A leading negative sign is permitted.
        /// </summary>
        AllowLeadingSign = 1 << 2,

        /// <summary>
        /// A trailing negative sign is permitted.
        /// </summary>
        AllowTrailingSign = 1 << 3,

        /// <summary>
        /// A negative value enclosed in parentheses is permitted.
        /// </summary>
        AllowParentheses = 1 << 4,

        /// <summary>
        /// Group separators are permitted.
        /// </summary>
        AllowThousands = 1 << 5,

        /// <summary>
        /// A decimal separator is permitted.
        /// </summary>
        AllowDecimalPoint = 1 << 6,

        /// <summary>
        /// An ISO 4217 currency code is permitted, for example <c>NZD 1234.50</c>.
        /// </summary>
        AllowCurrencyCode = 1 << 7,

        /// <summary>
        /// A currency symbol is permitted, provided that it denotes exactly one currency.
        /// </summary>
        /// <remarks>
        /// Most currency symbols denote a single currency and are resolved without reference to any culture.
        /// A symbol shared by several currencies requires <see cref="AllowAmbiguousCurrencySymbol"/> as well.
        /// </remarks>
        AllowCurrencySymbol = 1 << 8,

        /// <summary>
        /// A currency symbol shared by several currencies is permitted, and is resolved using the currency
        /// of the culture supplied when parsing.
        /// </summary>
        /// <remarks>
        /// A handful of symbols are shared: <c>$</c> alone is used by some thirty currencies. Resolving one
        /// requires the context of a culture, and a caller must opt in to it, because the currency of an
        /// ambient culture is rarely a safe assumption to make about the origin of a monetary value. Where
        /// the culture does not resolve the symbol, parsing fails rather than choosing a currency.
        /// </remarks>
        AllowAmbiguousCurrencySymbol = 1 << 9,

        /// <summary>
        /// The elements permitted in a plain number: leading and trailing white space, a leading or trailing
        /// sign, group separators and a decimal separator. No currency is permitted.
        /// </summary>
        Number = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign | AllowThousands | AllowDecimalPoint,

        /// <summary>
        /// The elements permitted in a monetary value: those of <see cref="Number"/>, together with
        /// parentheses, a currency code and a currency symbol which denotes exactly one currency.
        /// </summary>
        /// <remarks>
        /// This is the default, and deliberately excludes <see cref="AllowAmbiguousCurrencySymbol"/>.
        /// </remarks>
        Currency = Number | AllowParentheses | AllowCurrencyCode | AllowCurrencySymbol,

        /// <summary>
        /// All elements are permitted, including a currency symbol shared by several currencies.
        /// </summary>
        Any = Currency | AllowAmbiguousCurrencySymbol
    }
}
