using BenchmarkDotNet.Attributes;
using DataStandardizer.Geography;
using DataStandardizer.Language;

namespace DataStandardizer.LanguageTag.Benchmarks;

/// <summary>
/// Builds <c>zh-Hant-TW</c> with <see cref="Bcp47LanguageTagBuilder"/>, from enum and from string inputs.
/// </summary>
[MemoryDiagnoser]
public class BuilderBenchmarks
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        // Fail fast if the two benchmarks stop building the same tag.
        if (Builder_Build_Enums() != Builder_Build_Strings())
        {
            throw new InvalidOperationException("The enum and string inputs built different tags.");
        }
    }

    [Benchmark(Baseline = true)]
    public Bcp47LanguageTag Builder_Build_Enums()
    {
        return new Bcp47LanguageTagBuilder()
            .UsingPrimaryLanguageSubtag(Iso639Part1Language.zh)
            .UsingScriptSubtag(Iso15924Script.Hant)
            .UsingRegionSubtag(Iso3166Part1Alpha2Country.TW)
            .Build();
    }

    [Benchmark]
    public Bcp47LanguageTag Builder_Build_Strings()
    {
        return new Bcp47LanguageTagBuilder()
            .UsingPrimaryLanguageSubtag("zh")
            .UsingScriptSubtag("Hant")
            .UsingRegionSubtag("TW")
            .Build();
    }
}
