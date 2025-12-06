using System;
using System.Collections.Generic;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Service for persisting caches for regular expression factories.
    /// </summary>
    internal static class Bcp47ExpressionCacheService
    {
        private static readonly Dictionary<Tuple<Type, int>, Bcp47ExpressionCache> ExpressionCaches = new Dictionary<Tuple<Type, int>, Bcp47ExpressionCache>();

        /// <summary>
        /// Register a regular expression factory with the cache service.
        /// </summary>
        /// <param name="factory">A regular expression factory.</param>
        /// <returns>The cache for the factory.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static Bcp47ExpressionCache RegisterFactory(IBcp47ExpressionFactory factory) 
        #else
        internal static Bcp47ExpressionCache RegisterFactory([NotNull] IBcp47ExpressionFactory factory) 
#endif
        {
            var key = GetFactoryKey(factory);
            if (ExpressionCaches.TryGetValue(key, out var cache))
            {
                return cache;
            }

            cache = new Bcp47ExpressionCache(factory);
            ExpressionCaches.Add(key, cache);

            return cache;
        }

        private static Tuple<Type, int> GetFactoryKey(IBcp47ExpressionFactory factory)
        {
            var pattern = factory.GetPattern();
            return Tuple.Create(factory.GetType(), pattern.GetHashCode());
        }
    }
}