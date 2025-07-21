using System.Text.RegularExpressions;

namespace DataStandardizer.BCP47.InternalState
{
    /// <summary>
    /// Produce regular expressions for evaluating a component of a language tag.
    /// </summary>
    internal interface IBcp47ExpressionFactory
    {
        /// <summary>
        /// Create a language tag regular expression.
        /// </summary>
        /// <returns>A regular expression.</returns>
        Regex Create();

        /// <summary>
        /// Retrieve the search pattern for the regular expression.
        /// </summary>
        /// <returns>Search pattern for the regular expression.</returns>
        string GetPattern();
    }
}