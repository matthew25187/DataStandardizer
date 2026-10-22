using BenchmarkDotNet.Attributes;

namespace DataStandardizer.LanguageTag.Benchmarks;

/// <summary>
/// Parses the fixed sets of valid and invalid tags in <see cref="BenchmarkData"/>.
/// </summary>
[MemoryDiagnoser]
public class CreateBenchmarks
{
    private SubtagRegistry.SubtagRegistry _registry = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _registry = BenchmarkData.LoadRegistry();

        // Fail fast if a change to the package makes the fixed inputs mean something else.
        foreach (var tag in BenchmarkData.ValidTags)
        {
            if (!Bcp47LanguageTag.TryCreate(tag, out _) || !Bcp47LanguageTag.TryCreate(tag, _registry, out _) || !Bcp47LanguageTag.IsWellFormedLanguageTagString(tag)
                || !Bcp47LanguageTag.TryCreate(tag.AsSpan(), out _) || !Bcp47LanguageTag.IsWellFormedLanguageTagString(tag.AsSpan()))
            {
                throw new InvalidOperationException($"Valid tag '{tag}' was rejected.");
            }
        }

        foreach (var tag in BenchmarkData.InvalidTags)
        {
            if (Bcp47LanguageTag.TryCreate(tag, out _) || Bcp47LanguageTag.TryCreate(tag.AsSpan(), out _))
            {
                throw new InvalidOperationException($"Invalid tag '{tag}' was accepted.");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public int Create_Rules()
    {
        var length = 0;
        foreach (var tag in BenchmarkData.ValidTags)
        {
            length += Bcp47LanguageTag.Create(tag).PrimaryLanguageSubtag.Length;
        }

        return length;
    }

    [Benchmark]
    public int Create_Registry()
    {
        var length = 0;
        foreach (var tag in BenchmarkData.ValidTags)
        {
            length += Bcp47LanguageTag.Create(tag, _registry).PrimaryLanguageSubtag.Length;
        }

        return length;
    }

    [Benchmark]
    public int TryCreate_Invalid()
    {
        var count = 0;
        foreach (var tag in BenchmarkData.InvalidTags)
        {
            if (!Bcp47LanguageTag.TryCreate(tag, out _))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int IsWellFormed()
    {
        var count = 0;
        foreach (var tag in BenchmarkData.ValidTags)
        {
            if (Bcp47LanguageTag.IsWellFormedLanguageTagString(tag))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int TryCreate_Span()
    {
        var length = 0;
        foreach (var tag in BenchmarkData.ValidTags)
        {
            if (Bcp47LanguageTag.TryCreate(tag.AsSpan(), out var result))
            {
                length += result.PrimaryLanguageSubtag.Length;
            }
        }

        return length;
    }

    [Benchmark]
    public int TryCreate_Span_Invalid()
    {
        var count = 0;
        foreach (var tag in BenchmarkData.InvalidTags)
        {
            if (!Bcp47LanguageTag.TryCreate(tag.AsSpan(), out _))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int IsWellFormed_Span()
    {
        var count = 0;
        foreach (var tag in BenchmarkData.ValidTags)
        {
            if (Bcp47LanguageTag.IsWellFormedLanguageTagString(tag.AsSpan()))
            {
                count++;
            }
        }

        return count;
    }
}
