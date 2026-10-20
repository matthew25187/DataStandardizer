using System;
using System.Text.RegularExpressions;

namespace DataStandardizer.LanguageTag.InternalState
{
    internal class Bcp47TagExpression<TExpressionFactory> : IBcp47TagExpression where TExpressionFactory : class, IBcp47ExpressionFactory
    {
        private const RegexOptions ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline;

        private readonly Bcp47ExpressionCache _expressionCache;
        private readonly IBcp47ExpressionFactory _expressionFactory;

        internal Bcp47TagExpression()
        {
            _expressionFactory = Activator.CreateInstance<TExpressionFactory>();
            _expressionCache = Bcp47ExpressionCacheService.RegisterFactory(_expressionFactory);
        }

        internal Bcp47TagExpression(params object[] factoryArguments)
        {
            var expressionFactory = Activator.CreateInstance(typeof(TExpressionFactory), factoryArguments) as IBcp47ExpressionFactory;
            _expressionFactory = expressionFactory ?? throw new InvalidOperationException("Failed to create regular expression factory.");

            _expressionCache = Bcp47ExpressionCacheService.RegisterFactory(_expressionFactory);
        }

        public bool IsMatch(string input)
        {
            if (!IsAscii(input))
            {
                return false;
            }

            var cachedExpression = GetExpression();
            return cachedExpression.IsMatch(input);
        }

        public bool IsMatch(string input, TimeSpan matchTimeout)
        {
            if (!IsAscii(input))
            {
                return false;
            }

            var cachedExpression = _expressionCache.GetCachedExpression(matchTimeout);
            var pattern = _expressionFactory.GetPattern();
            return cachedExpression?.IsMatch(input) ?? Regex.IsMatch(input, $"^{pattern}\\z", ExpressionOptions, matchTimeout);
        }

        public Match Match(string input)
        {
            if (!IsAscii(input))
            {
                return System.Text.RegularExpressions.Match.Empty;
            }

            var cachedExpression = GetExpression();
            return cachedExpression.Match(input);
        }

        public Match Match(string input, TimeSpan matchTimeout)
        {
            if (!IsAscii(input))
            {
                return System.Text.RegularExpressions.Match.Empty;
            }

            var cachedExpression = _expressionCache.GetCachedExpression(matchTimeout);
            var pattern = _expressionFactory.GetPattern();
            return cachedExpression?.Match(input) ?? Regex.Match(input, $"^{pattern}\\z", ExpressionOptions, matchTimeout);
        }

        public MatchCollection Matches(string input)
        {
            if (!IsAscii(input))
            {
                // Every pattern is anchored at the start of the input, so searching from its end finds no match.
                return GetExpression().Matches(input, input.Length);
            }

            var cachedExpression = GetExpression();
            return cachedExpression.Matches(input);
        }

        public MatchCollection Matches(string input, TimeSpan matchTimeout)
        {
            if (!IsAscii(input))
            {
                // Every pattern is anchored at the start of the input, so searching from its end finds no match.
                return GetExpression().Matches(input, input.Length);
            }

            var cachedExpression = _expressionCache.GetCachedExpression(matchTimeout);
            var pattern = _expressionFactory.GetPattern();
            return cachedExpression?.Matches(input) ?? Regex.Matches(input, $"^{pattern}\\z", ExpressionOptions, matchTimeout);
        }

        private Regex GetExpression()
        {
            var cachedExpression = _expressionCache.GetCachedExpression();
            return cachedExpression ?? _expressionFactory.Create();
        }

        /// <summary>
        /// Language tags and subtags have only ASCII letters, digits and hyphens (ref. RFC 5646 §2.1). Rejecting any other character
        /// before matching stops <see cref="RegexOptions.IgnoreCase"/> from matching a non-ASCII case equivalent of an ASCII letter,
        /// such as the Kelvin sign U+212A for K.
        /// </summary>
        private static bool IsAscii(string input)
        {
            foreach (var character in input)
            {
                if (character > '\u007F')
                {
                    return false;
                }
            }

            return true;
        }
    }
}