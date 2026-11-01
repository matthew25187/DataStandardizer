using BenchmarkDotNet.Attributes;

namespace DataStandardizer.LanguageTag.Benchmarks;

/// <summary>
/// Reads the subtag properties of a tag built in <see cref="GlobalSetup"/>, so only the reads are measured.
/// </summary>
[MemoryDiagnoser]
public class PropertyBenchmarks
{
    private Bcp47LanguageTag _tag;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _tag = Bcp47LanguageTag.Create(BenchmarkData.AllSubtagsTag);
    }

    [Benchmark]
    public int Properties_All()
    {
        var length = _tag.PrimaryLanguageSubtag?.Length ?? 0;
        length += _tag.ExtendedLanguageSubtags.Count;
        length += _tag.ScriptSubtag?.Length ?? 0;
        length += _tag.RegionSubtag?.Length ?? 0;
        length += _tag.VariantSubtags.Count;
        length += _tag.ExtensionSubtags.Count;
        length += _tag.PrivateUseSubtag.HasValue ? 1 : 0;

        return length;
    }
}
