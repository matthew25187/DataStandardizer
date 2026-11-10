using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The character classes of RFC 5322 §3.2 and §4.1 with RFC 6532 §3.2, and the writing of the forms made from them.
    /// </summary>
    /// <remarks>
    /// The classes take a <see cref="char"/> so that they serve a character and a byte alike: both pass as <c>UTF8-non-ascii</c> above 127.
    /// </remarks>
    internal static class ImfGrammar
    {
        private const char Tab = '\t';
        private const char Space = ' ';

        // ref. RFC 5322 §3.2.3: atext = ALPHA / DIGIT / "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "/" / "=" / "?" / "^" / "_" / "`" / "{" / "|" / "}" / "~"
        internal static bool IsAtext(char c)
        {
            if (c >= 0x80) return true;
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) return true;

            switch (c)
            {
                case '!': case '#': case '$': case '%': case '&': case '\'': case '*': case '+': case '-':
                case '/': case '=': case '?': case '^': case '_': case '`': case '{': case '|': case '}': case '~':
                    return true;
                default:
                    return false;
            }
        }

        // ref. RFC 5322 §3.2.4: qtext = %d33 / %d35-91 / %d93-126
        internal static bool IsQtext(char c)
        {
            return c >= 0x80 || c == 33 || (c >= 35 && c <= 91) || (c >= 93 && c <= 126);
        }

        // ref. RFC 5322 §3.2.2: ctext = %d33-39 / %d42-91 / %d93-126
        internal static bool IsCtext(char c)
        {
            return c >= 0x80 || (c >= 33 && c <= 39) || (c >= 42 && c <= 91) || (c >= 93 && c <= 126);
        }

        // ref. RFC 5322 §3.4.1: dtext = %d33-90 / %d94-126
        internal static bool IsDtext(char c)
        {
            return c >= 0x80 || (c >= 33 && c <= 90) || (c >= 94 && c <= 126);
        }

        // ref. RFC 5322 §4.1: obs-NO-WS-CTL = %d1-8 / %d11 / %d12 / %d14-31 / %d127
        internal static bool IsObsoleteControl(char c)
        {
            return (c >= 1 && c <= 8) || c == 11 || c == 12 || (c >= 14 && c <= 31) || c == 127;
        }

        /// <summary>
        /// Gets the UTF-8 bytes of a string.
        /// </summary>
        /// <returns><c>false</c> if the string is <c>null</c> or holds an unpaired surrogate, which UTF-8 cannot represent.</returns>
        internal static bool TryGetBytes([AllowNull] string value, [MaybeNullWhen(false)] out byte[] bytes)
        {
            if (value is null || !HasOnlyPairedSurrogates(value))
            {
                bytes = null;
                return false;
            }

            bytes = Encoding.UTF8.GetBytes(value);
            return true;
        }

        /// <summary>
        /// Determines if a string can be written in a quoted-string: it holds no control characters but tab, and no unpaired surrogates.
        /// </summary>
        internal static bool IsWritableText(string value)
        {
            foreach (var c in value)
            {
                if (c != Tab && (c < Space || c == 127)) return false;
            }

            return HasOnlyPairedSurrogates(value);
        }

        /// <summary>
        /// Determines if a string is an RFC 5322 §3.2.3 dot-atom-text, without an unpaired surrogate.
        /// </summary>
        internal static bool IsDotAtomText(string value)
        {
            var wasPeriod = true;
            foreach (var c in value)
            {
                if (c == '.')
                {
                    if (wasPeriod) return false;

                    wasPeriod = true;
                }
                else
                {
                    if (!IsAtext(c)) return false;

                    wasPeriod = false;
                }
            }

            return !wasPeriod && HasOnlyPairedSurrogates(value);
        }

        /// <summary>
        /// Determines if a string is a domain as a caller may give one: a dot-atom-text or a domain-literal, without an unpaired surrogate.
        /// </summary>
        internal static bool IsDomain(string value)
        {
            if (!IsBracketed(value)) return IsDotAtomText(value);

            for (var index = 1; index < value.Length - 1; index++)
            {
                var c = value[index];
                if (c != Space && c != Tab && !IsDtext(c)) return false;
            }

            return HasOnlyPairedSurrogates(value);
        }

        /// <summary>
        /// Gets the form of a domain that <see cref="IsDomain"/> accepts that parsing it would give.
        /// </summary>
        internal static string NormalizeDomain(string domain)
        {
            if (!IsBracketed(domain)) return domain;

            var tokenizer = new ImfTokenizer(Encoding.UTF8.GetBytes(domain));
            tokenizer.TryRead(out var token);
            return tokenizer.GetText(token);
        }

        /// <summary>
        /// Determines if a string is an RFC 5322 §3.6.4 no-fold-literal, without an unpaired surrogate.
        /// </summary>
        /// <remarks>
        /// no-fold-literal = "[" *dtext "]"
        /// </remarks>
        internal static bool IsNoFoldLiteral(string value)
        {
            if (!IsBracketed(value)) return false;

            for (var index = 1; index < value.Length - 1; index++)
            {
                if (!IsDtext(value[index])) return false;
            }

            return HasOnlyPairedSurrogates(value);
        }

        /// <summary>
        /// Writes a phrase of plain text in the modern form: atoms if every word can be one, otherwise a quoted-string.  A word in the form of an encoded
        /// word has to be quoted so that it does not become one.
        /// </summary>
        internal static void AppendPhrase(string text, StringBuilder output)
        {
            var words = text.Split(Space);
            foreach (var word in words)
            {
                if (!IsAtom(word) || HasEncodedWordForm(word))
                {
                    AppendQuotedString(text, output);
                    return;
                }
            }

            output.Append(text);
        }

        /// <summary>
        /// Writes an addr-spec in the modern form: the local part as a dot-atom if it can be one and otherwise as a quoted-string, and the domain as a
        /// dot-atom or domain-literal.
        /// </summary>
        internal static void AppendAddrSpec(string localPart, string domain, StringBuilder output)
        {
            if (IsDotAtomText(localPart))
            {
                output.Append(localPart);
            }
            else
            {
                AppendQuotedString(localPart, output);
            }

            output.Append('@');

            if (!IsBracketed(domain))
            {
                output.Append(domain);
                return;
            }

            output.Append('[');
            for (var index = 1; index < domain.Length - 1; index++)
            {
                var c = domain[index];

                // Only the obsolete syntax can put anything else here, and it has no modern form (RFC 5322 §4.4).
                if (c == Space || c == Tab || IsDtext(c)) output.Append(c);
            }

            output.Append(']');
        }

        /// <summary>
        /// Writes a quoted-string, with a quoted-pair for each quote and backslash.
        /// </summary>
        /// <remarks>
        /// A control character can come only from the obsolete syntax.  It has no modern form (RFC 5322 §4.1), and a line break must never get out, so
        /// it is left out.
        /// </remarks>
        internal static void AppendQuotedString(string text, StringBuilder output)
        {
            output.Append('"');
            foreach (var c in text)
            {
                if (c == '"' || c == '\\')
                {
                    output.Append('\\').Append(c);
                }
                else if (c == Space || c == Tab || IsQtext(c))
                {
                    output.Append(c);
                }
            }

            output.Append('"');
        }

        /// <summary>
        /// Combines a hash code with another.
        /// </summary>
        internal static int CombineHashCodes(int first, int second)
        {
            unchecked
            {
                return first * 31 + second;
            }
        }

        /// <summary>
        /// Copies a sequence for a read-only list, and rejects a <c>null</c> element.
        /// </summary>
        internal static T[] CopyElements<T>(IEnumerable<T> elements, string parameterName)
            where T : class
        {
            if (elements is null) throw new ArgumentNullException(parameterName);

            var list = new List<T>(elements);
            foreach (var element in list)
            {
                if (element is null) throw new ArgumentException("The list holds a null element.", parameterName);
            }

            return list.ToArray();
        }

        private static bool IsBracketed(string value)
        {
            return value.Length >= 2 && value[0] == '[' && value[value.Length - 1] == ']';
        }

        private static bool IsAtom(string value)
        {
            if (value.Length == 0) return false;

            foreach (var c in value)
            {
                if (!IsAtext(c)) return false;
            }

            return true;
        }

        private static bool HasEncodedWordForm(string value)
        {
            return value.Length >= 4 && value[0] == '=' && value[1] == '?' && value[value.Length - 2] == '?' && value[value.Length - 1] == '=';
        }

        /// <summary>
        /// Determines if every surrogate in a string is part of a pair.
        /// </summary>
        internal static bool HasOnlyPairedSurrogates(string value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var c = value[index];
                if (char.IsHighSurrogate(c) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                {
                    index++;
                }
                else if (char.IsSurrogate(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
