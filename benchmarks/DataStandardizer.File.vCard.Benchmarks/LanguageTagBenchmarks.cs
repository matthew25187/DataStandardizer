using BenchmarkDotNet.Attributes;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.vCard.Benchmarks;

/// <summary>
/// Parses LANGUAGE-TAG values (RFC 6350 §4.8) on their own, as a <c>LANG</c> property or a <c>LANGUAGE</c> parameter has them, so the cost of
/// <c>Bcp47LanguageTag</c> shows apart from the rest of a card.  Each benchmark returns the number of tags parsed or formatted.
/// </summary>
[MemoryDiagnoser]
public class LanguageTagBenchmarks
{
    private const int Repeats = 50;

    private string[] _tags = [];
    private Bcp47LanguageTag[] _parsed = [];
    private char[] _destination = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _tags = Enumerable.Repeat(BenchmarkData.LanguageTagValues, Repeats).SelectMany(tags => tags).ToArray();
        _parsed = _tags.Select(tag => VcardValue.TryParseLanguageTag(tag.AsSpan(), VcardValueFormat.Text, out var result)
            ? result
            : throw new FormatException($"'{tag}' isn't a LANGUAGE-TAG value.")).ToArray();
        _destination = new char[_tags.Max(tag => tag.Length)];
        Console.WriteLine($"// {_tags.Length:N0} tags, {BenchmarkData.LanguageTagValues.Length} of them distinct.");
    }

    [Benchmark(Baseline = true)]
    public int Parse_VcardValue()
    {
        var count = 0;
        foreach (var tag in _tags)
        {
            if (VcardValue.TryParseLanguageTag(tag.AsSpan(), VcardValueFormat.Text, out _))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Parse_Bcp47LanguageTag()
    {
        var count = 0;
        foreach (var tag in _tags)
        {
            if (Bcp47LanguageTag.TryParse(tag.AsSpan(), null, out _))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int Format_VcardValue()
    {
        var count = 0;
        foreach (var tag in _parsed)
        {
            if (VcardValue.TryFormatLanguageTag(tag, _destination, out _, VcardValueFormat.Text))
            {
                count++;
            }
        }

        return count;
    }
}
