using BenchmarkDotNet.Attributes;

namespace DataStandardizer.LanguageTag.Benchmarks;

/// <summary>
/// Reads the properties of a <see cref="Bcp47KeyedSubtag"/> built in <see cref="GlobalSetup"/>, so only the reads are measured.
/// </summary>
[MemoryDiagnoser]
public class KeyedSubtagBenchmarks
{
    private Bcp47KeyedSubtag _subtag;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _subtag = (Bcp47KeyedSubtag)BenchmarkData.KeyedSubtag;
    }

    [Benchmark]
    public char? KeyedSubtag_Singleton() => _subtag.Singleton;

    [Benchmark]
    public string[] KeyedSubtag_Subtags() => _subtag.Subtags;
}
