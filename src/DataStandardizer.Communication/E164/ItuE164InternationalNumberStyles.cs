using System;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Specifies the styles that can be used when parsing ITU E.164 international numbers.
    /// </summary>
    [Flags]
    public enum ItuE164InternationalNumberStyles : uint
    {
        /// <summary>
        /// No styles are allowed: the number is parsed strictly, with no international prefix symbol and no leading or
        /// trailing white space.
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