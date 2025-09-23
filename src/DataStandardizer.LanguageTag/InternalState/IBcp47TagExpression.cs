using System;
using System.Text.RegularExpressions;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Operations provided by a language tag or subtag regular expression.
    /// </summary>
    internal interface IBcp47TagExpression
    {
        /// <summary>
        /// Indicates whether the regular expression specified in the Regex constructor finds a match in the input string.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <returns><c>true</c> if the regular expression finds a match; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        bool IsMatch(string input);
#else
        bool IsMatch([NotNull] string input);
#endif

        /// <summary>
        /// Indicates whether the specified regular expression finds a match in the specified input string, using the specified time-out interval.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to indicate that the method should not time out.</param>
        /// <returns><c>true</c> if the regular expression finds a match; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        bool IsMatch(string input, TimeSpan matchTimeout);
#else
        bool IsMatch([NotNull] string input, TimeSpan matchTimeout);
#endif

        /// <summary>
        /// Searches the specified input string for the first occurrence of the regular expression specified in the Regex constructor.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <returns>An object that contains information about the match.</returns>
#if NETCOREAPP3_0_OR_GREATER
        Match Match(string input);
#else
        Match Match([NotNull] string input);
#endif

        /// <summary>
        /// Searches the input string for the first occurrence of the specified regular expression, using the specified matching options and time-out interval.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to indicate that the method should not time out.</param>
        /// <returns>An object that contains information about the match.</returns>
#if NETCOREAPP3_0_OR_GREATER
        Match Match(string input, TimeSpan matchTimeout);
#else
        Match Match([NotNull] string input, TimeSpan matchTimeout);
#endif

        /// <summary>
        /// Searches the specified input string for all occurrences of a regular expression.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <returns>A collection of the <see cref="System.Text.RegularExpressions.Match"/> objects found by the search. If no matches are found, the method returns an empty collection object.</returns>
#if NETCOREAPP3_0_OR_GREATER
        MatchCollection Matches(string input);
#else
        MatchCollection Matches([NotNull] string input);
#endif

        /// <summary>
        /// Searches the specified input string for all occurrences of a specified regular expression, using the specified time-out interval.
        /// </summary>
        /// <param name="input">The string to search for a match.</param>
        /// <param name="matchTimeout">A time-out interval, or <see cref="Regex.InfiniteMatchTimeout"/> to indicate that the method should not time out.</param>
        /// <returns>A collection of the <see cref="System.Text.RegularExpressions.Match"/> objects found by the search. If no matches are found, the method returns an empty collection object.</returns>
#if NETCOREAPP3_0_OR_GREATER
        MatchCollection Matches(string input, TimeSpan matchTimeout);
#else
        MatchCollection Matches([NotNull] string input, TimeSpan matchTimeout);
#endif
    }
}