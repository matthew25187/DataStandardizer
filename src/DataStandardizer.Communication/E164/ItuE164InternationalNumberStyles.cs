using System;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Specifies the styles that can be used when parsing ITU E.164 international numbers.
    /// </summary>
    /// <summary>
    /// No styles are allowed. Parsing will be strict.
    /// </summary>
    /// <summary>
    /// Allows the presence of an international prefix symbol in the number.
    /// </summary>
    /// <summary>
    /// Allows leading whitespace characters in the number.
    /// </summary>
    /// <summary>
    /// Allows trailing whitespace characters in the number.
    /// </summary>
    /// <summary>
    /// Specifies that the number is in an international format, allowing an international prefix symbol.
    /// </summary>
    /// <summary>
    /// Allows any combination of international prefix symbols, leading whitespace, and trailing whitespace.
    /// </summary>
    [Flags]
    public enum ItuE164InternationalNumberStyles : uint
    {
        None,

        /// <summary>
        /// Allows the presence of an international prefix symbol (e.g., "+") in the ITU E.164 international number.
        /// </summary>
        AllowInternationalPrefixSymbol = 1 << 0,

        /// <summary>
        /// Allows leading whitespace characters in the number when parsing ITU E.164 international numbers.
        /// </summary>
        AllowLeadingWhite = 1 << 1,

        /// <summary>
        /// Allows trailing whitespace characters in the number when parsing ITU E.164 international numbers.
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