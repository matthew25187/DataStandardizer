using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace DataStandardizer.LanguageTag.Benchmarks;

/// <summary>
/// Times the first calls in a fresh process, which include the one-off cost of static initialization that BenchmarkDotNet's warmup hides.
/// </summary>
/// <remarks>
/// Each figure is a single sample, so run the process several times and report every run.
/// </remarks>
internal static class ColdStart
{
    public static void Run()
    {
        Console.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription} ({RuntimeInformation.ProcessArchitecture})");
        Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");

        var stopwatch = Stopwatch.StartNew();
        var rulesTag = Bcp47LanguageTag.Create("en");
        stopwatch.Stop();
        Report("First Create(\"en\")", stopwatch.Elapsed);

        // Loading the registry is reported for context, but it is measured properly by RegistryBenchmarks.
        stopwatch.Restart();
        var registry = BenchmarkData.LoadRegistry();
        stopwatch.Stop();
        Report("Registry load (fixture)", stopwatch.Elapsed);

        var jitTime = JitInfo.GetCompilationTime();
        stopwatch.Restart();
        var registryTag = Bcp47LanguageTag.Create("en", registry);
        stopwatch.Stop();
        Report("First Create(\"en\", registry)", stopwatch.Elapsed);
        Report("  of which JIT compilation", JitInfo.GetCompilationTime() - jitTime);

        stopwatch.Restart();
        _ = Bcp47LanguageTag.Create("en", registry);
        stopwatch.Stop();
        Report("Second Create(\"en\", registry)", stopwatch.Elapsed);

        // A second registry builds its own index with code that is already compiled, so this is the cost of building an index alone.
        // Collect first, so that a collection triggered by loading it doesn't land in the measurement.
        var secondRegistry = BenchmarkData.LoadRegistry();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        stopwatch.Restart();
        _ = Bcp47LanguageTag.Create("en", secondRegistry);
        stopwatch.Stop();
        Report("First Create(\"en\", registry 2)", stopwatch.Elapsed);

        if (rulesTag != registryTag)
        {
            throw new InvalidOperationException("The rules-only and registry tags differ.");
        }
    }

    private static void Report(string label, TimeSpan elapsed)
    {
        Console.WriteLine($"{label,-32} {elapsed.TotalMilliseconds,12:N3} ms");
    }
}
