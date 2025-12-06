using System;
using System.Text.RegularExpressions;

namespace DataStandardizer.LanguageTag.InternalState
{
    internal class Bcp47TagExpression<TExpressionFactory> : IBcp47TagExpression where TExpressionFactory : class, IBcp47ExpressionFactory
    {
        private const RegexOptions ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;

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
            var cachedExpression = GetExpression();
            return cachedExpression.IsMatch(input);
        }

        public bool IsMatch(string input, TimeSpan matchTimeout)
        {
            var cachedExpression = _expressionCache.GetCachedExpression(matchTimeout);
            var pattern = _expressionFactory.GetPattern();
            return cachedExpression?.IsMatch(input) ?? Regex.IsMatch(input, $"^{pattern}$", ExpressionOptions, matchTimeout);
        }

        public Match Match(string input)
        {
            var cachedExpression = GetExpression();
            return cachedExpression.Match(input);
        }

        public Match Match(string input, TimeSpan matchTimeout)
        {
            var cachedExpression = _expressionCache.GetCachedExpression(matchTimeout);
            var pattern = _expressionFactory.GetPattern();
            return cachedExpression?.Match(input) ?? Regex.Match(input, $"^{pattern}$", ExpressionOptions, matchTimeout);
        }

        public MatchCollection Matches(string input)
        {
            var cachedExpression = GetExpression();
            return cachedExpression.Matches(input);
        }

        public MatchCollection Matches(string input, TimeSpan matchTimeout)
        {
            var cachedExpression = _expressionCache.GetCachedExpression(matchTimeout);
            var pattern = _expressionFactory.GetPattern();
            return cachedExpression?.Matches(input) ?? Regex.Matches(input, $"^{pattern}$", ExpressionOptions, matchTimeout);
        }

        private Regex GetExpression()
        {
            var cachedExpression = _expressionCache.GetCachedExpression();
            return cachedExpression ?? _expressionFactory.Create();
        }
    }
}