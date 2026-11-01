#if NETCOREAPP3_0_OR_GREATER
using System;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    /// <summary>
    /// Measures how many bytes an action allocates on the managed heap.
    /// </summary>
    /// <remarks>
    /// Its own tests are in <see cref="AllocationMeterTests"/>. An allocation test makes one call to <see cref="MeasureAllocatedBytes"/>.
    /// </remarks>
    internal static class AllocationMeter
    {
        /// <summary>
        /// Run an action once, so that the one-off work of compiling it and filling the caches it uses is not counted, then run it again and
        /// return the number of bytes the second run allocated on the current thread.
        /// </summary>
        internal static long MeasureAllocatedBytes(Action action)
        {
            action();
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            action();
            return GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        }
    }
}
#endif
