using System.Text;
using SharpFuzz;

namespace DataStandardizer.LanguageTag.Fuzz;

internal static class Program
{
    private static void Main()
    {
        Fuzzer.LibFuzzer.Run(Run);
    }

    /// <summary>
    /// Parses the input as UTF-8 with the string and span overloads of <see cref="Bcp47LanguageTag.TryParse(string, IFormatProvider, out Bcp47LanguageTag)"/>,
    /// which document no exceptions, so any exception counts as a finding.  So does the overloads disagreeing.
    /// </summary>
    private static void Run(ReadOnlySpan<byte> input)
    {
        var text = Encoding.UTF8.GetString(input);

        var parsedFromString = Bcp47LanguageTag.TryParse(text, null, out var tagFromString);
        var parsedFromSpan = Bcp47LanguageTag.TryParse(text.AsSpan(), null, out var tagFromSpan);

        if (parsedFromString != parsedFromSpan || tagFromString != tagFromSpan)
        {
            throw new InvalidOperationException("The string and span overloads of TryParse gave different results.");
        }

        if (parsedFromString)
        {
            ReadSubtags(tagFromString);
        }
    }

    /// <summary>
    /// Asks for every part of a language tag that was parsed, none of which documents an exception for an initialised tag.
    /// </summary>
    private static void ReadSubtags(Bcp47LanguageTag tag)
    {
        _ = tag.PrimaryLanguageSubtag;
        _ = tag.ExtendedLanguageSubtags;
        _ = tag.ScriptSubtag;
        _ = tag.RegionSubtag;
        _ = tag.VariantSubtags;
        _ = tag.ExtensionSubtags;
        _ = tag.PrivateUseSubtag;
    }
}
