using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Cache for storing pre-compiled regular expressions by expression factory.
    /// </summary>
    internal class Bcp47ExpressionCache
    {
        private static readonly RegexOptions ExpressionOptions;
        private const int MaxCacheSize = 10;

        private readonly Dictionary<long, Regex> _cachedExpressions = new Dictionary<long, Regex>();
        private readonly IBcp47ExpressionFactory _factory;
        private readonly object _syncRoot = new object();

        static Bcp47ExpressionCache()
        {
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            ExpressionOptions |= RegexOptions.Compiled;
#endif
        }

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47ExpressionCache(IBcp47ExpressionFactory factory)
#else
        internal Bcp47ExpressionCache([NotNull] IBcp47ExpressionFactory factory)
#endif
        {
            _factory = factory;
        }

        /// <summary>
        /// Get a regular expression from the cache.
        /// </summary>
        /// <returns>A regular expression, if found; <c>null</c> if the regular expression had not already been cached and couldn't be cached.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal Regex? GetCachedExpression()
#else
        [CanBeNull]
        internal Regex GetCachedExpression()
#endif
        {
            return GetCachedExpression(Regex.InfiniteMatchTimeout);
        }

        /// <summary>
        /// Get a regular expression from the cache by its match timeout.
        /// </summary>
        /// <param name="matchTimeout">Time limit for evaluation of the regular expression.</param>
        /// <returns>A regular expression, if found; <c>null</c> if the regular expression had not already been cached and couldn't be cached.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal Regex? GetCachedExpression(TimeSpan matchTimeout)
#else
        [CanBeNull]
        internal Regex GetCachedExpression(TimeSpan matchTimeout)
#endif
        {
            lock (_syncRoot)
            {
                // If an expression for the match timeout is already in the cache, return it.
                if (_cachedExpressions.TryGetValue(matchTimeout.Ticks, out var expression))
                {
                    return expression;
                }

                // If the cache has not yet reached its limit, add a new expression for the timeout to the cache.
                if (_cachedExpressions.Count < MaxCacheSize)
                {
                    var pattern = _factory.GetPattern();
                    expression = matchTimeout == Regex.InfiniteMatchTimeout ? new Regex($"^{pattern}$", ExpressionOptions) : new Regex($"^{pattern}$", ExpressionOptions, matchTimeout);
                    _cachedExpressions.Add(matchTimeout.Ticks, expression);
                }

                return expression;
            }
        }
    }
}