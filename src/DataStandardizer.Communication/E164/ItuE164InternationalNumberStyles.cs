using System;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Specifies the styles that can be used when parsing ITU E.164 international numbers.
    /// </summary>
    /// <remarks>
    /// ITU-T E.164 defines an international number as a string of decimal digits. With every style, the digits of the
    /// number are consecutive: no white space, punctuation or other character may separate them. Of the standard
    /// formats of <see cref="ItuE164InternationalNumber"/>, only the general format with no separators, <c>g</c>,
    /// parses again, with <see cref="InternationalNumber"/>; formats that group the digits, such as the default
    /// <c>G</c>, don't.
    /// </remarks>
    [Flags]
    public enum ItuE164InternationalNumberStyles : uint
    {
        /// <summary>
        /// No styles are allowed: the number is parsed strictly, as digits only, with no international prefix symbol
        /// and no leading or trailing white space.
        /// </summary>
        None,

        /// <summary>
        /// Allows the presence of an international prefix symbol (e.g., "+") in the ITU E.164 international number.
        /// </summary>
        AllowInternationalPrefixSymbol = 1 << 0,

        /// <summary>
        /// Allows leading whitespace characters in the number when parsing ITU E.164 international numbers.
        /// White space is a space separator (Unicode category Zs) or a control character from U+0009 to U+000D.
        /// </summary>
        AllowLeadingWhite = 1 << 1,

        /// <summary>
        /// Allows trailing whitespace characters in the number when parsing ITU E.164 international numbers.
        /// White space is a space separator (Unicode category Zs) or a control character from U+0009 to U+000D.
        /// </summary>
        AllowTrailingWhite = 1 << 2,

        /// <summary>
        /// Specifies that the number is in an international format, allowing an international prefix symbol.
        /// </summary>
        InternationalNumber = AllowInternationalPrefixSymbol,

        /// <summary>
        /// Allows any combination of international prefix symbols, leading whitespace, and trailing whitespace
        /// when parsing ITU E.164 international numbers.
        /// </summary>
        Any = AllowInternationalPrefixSymbol | AllowLeadingWhite | AllowTrailingWhite
    }
}