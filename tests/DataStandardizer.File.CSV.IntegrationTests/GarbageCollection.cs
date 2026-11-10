namespace DataStandardizer.File.CSV.IntegrationTests;

/// <summary>
/// Runs the garbage collector to find out whether an object can be collected.
/// </summary>
internal static class GarbageCollection
{
    /// <summary>
    /// Most full collections to run.  A collectible assembly can take more than one collection to be unloaded, once nothing refers to it.
    /// </summary>
    internal const int MaxCollections = 10;

    /// <summary>
    /// Runs full, blocking collections until the target of a weak reference has been collected, or <see cref="MaxCollections"/> have run.
    /// </summary>
    /// <returns><c>true</c> if the target has been collected; <c>false</c> if something still refers to it.</returns>
    internal static bool IsCollected(WeakReference reference)
    {
        for (var collection = 0; collection < MaxCollections && reference.IsAlive; collection++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return !reference.IsAlive;
    }
}
