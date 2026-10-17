using BenchmarkDotNet.Attributes;

namespace DataStandardizer.LanguageTag.Benchmarks;

/// <summary>
/// One call to each static <c>Check*</c> method of <see cref="Bcp47LanguageTag"/>, with a valid subtag.
/// </summary>
[MemoryDiagnoser]
public class CheckBenchmarks
{
    [GlobalSetup]
    public void GlobalSetup()
    {
        // Fail fast if a change to the package makes the fixed inputs mean something else.
        if (!(Check_PrimaryLanguage() && Check_ExtendedLanguage() && Check_Script() && Check_Region() && Check_Variant() && Check_Extension() && Check_PrivateUse()))
        {
            throw new InvalidOperationException("A valid subtag was rejected.");
        }
    }

    [Benchmark]
    public bool Check_PrimaryLanguage() => Bcp47LanguageTag.CheckPrimaryLanguageSubtag("zh");

    [Benchmark]
    public bool Check_ExtendedLanguage() => Bcp47LanguageTag.CheckExtendedLanguageSubtag("yue");

    [Benchmark]
    public bool Check_Script() => Bcp47LanguageTag.CheckScriptSubtag("Hant");

    [Benchmark]
    public bool Check_Region() => Bcp47LanguageTag.CheckRegionSubtag("TW");

    [Benchmark]
    public bool Check_Variant() => Bcp47LanguageTag.CheckVariantSubtag("1994");

    [Benchmark]
    public bool Check_Extension() => Bcp47LanguageTag.CheckExtensionSubtag("u-ca-hebrew-tz-jeruslm");

    [Benchmark]
    public bool Check_PrivateUse() => Bcp47LanguageTag.CheckPrivateUseSubtag("x-private");
}
