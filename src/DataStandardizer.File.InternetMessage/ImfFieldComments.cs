using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Takes the comments (RFC 5322 §3.2.2) out of the value of a MIME header field, which is what the entity reader needs before it parses one.  The
    /// full field rules, with the RFC 5322 tokenizer, are for the typed header values.
    /// </summary>
    internal static class ImfFieldComments
    {
        /// <summary>
        /// Replaces each comment outside a quoted string with a space, so that the tokens on either side stay apart, and drops the white space around
        /// the first <c>/</c>, which the media type parser does not accept.
        /// </summary>
        /// <param name="value">The unfolded field value.</param>
        /// <param name="result">The value without comments.</param>
        /// <returns><c>false</c> if a comment or quoted string is not closed.</returns>
        internal static bool TryRemove(string value, out string result)
        {
            var text = new StringBuilder(value.Length);
            var commentDepth = 0;
            var isInQuotedString = false;
            var isEscaped = false;
            var seenSlash = false;
            var skipWhiteSpace = false;

            foreach (var character in value)
            {
                if (isEscaped)
                {
                    isEscaped = false;
                    if (commentDepth == 0)
                    {
                        text.Append(character);
                    }

                    continue;
                }

                if (commentDepth > 0)
                {
                    if (character == '\\') isEscaped = true;
                    else if (character == '(') commentDepth++;
                    else if (character == ')') commentDepth--;

                    continue;
                }

                if (isInQuotedString)
                {
                    text.Append(character);
                    if (character == '\\') isEscaped = true;
                    else if (character == '"') isInQuotedString = false;

                    continue;
                }

                if (character == '(')
                {
                    commentDepth = 1;
                    if (!skipWhiteSpace)
                    {
                        text.Append(' ');
                    }

                    continue;
                }

                if (skipWhiteSpace && (character == ' ' || character == '\t')) continue;

                skipWhiteSpace = false;
                if (character == '/' && !seenSlash)
                {
                    seenSlash = true;
                    TrimEndWhiteSpace(text);
                    text.Append(character);
                    skipWhiteSpace = true;
                    continue;
                }

                text.Append(character);
                if (character == '"')
                {
                    isInQuotedString = true;
                }
            }

            result = TrimStartWhiteSpace(text);
            return commentDepth == 0 && !isInQuotedString && !isEscaped;
        }

        private static void TrimEndWhiteSpace(StringBuilder text)
        {
            var length = text.Length;
            while (length > 0 && (text[length - 1] == ' ' || text[length - 1] == '\t'))
            {
                length--;
            }

            text.Length = length;
        }

        private static string TrimStartWhiteSpace(StringBuilder text)
        {
            return text.ToString().TrimStart(' ', '\t');
        }
    }
}
