using System.Globalization;
using System.Text;
using SharpFuzz;

namespace DataStandardizer.Money.Fuzz;

internal static class Program
{
    /// <summary>
    /// The number of bytes at the start of an input that choose the provider, the styles and the length of the format string.
    /// </summary>
    private const int HeaderLength = 4;

    /// <summary>
    /// Every style <see cref="MoneyStyles"/> defines.
    /// </summary>
    private const MoneyStyles DefinedStyles = MoneyStyles.Any;

    /// <summary>
    /// The providers an input chooses from: none, cultures whose currencies, symbols, separators, signs and patterns differ,
    /// currency formatting information with multi-character separators and sign, and number formatting information alone.
    /// </summary>
    private static readonly IFormatProvider?[] Providers =
    [
        null,
        CultureInfo.InvariantCulture,
        CultureInfo.GetCultureInfo("en-US"),
        CultureInfo.GetCultureInfo("en-NZ"),
        CultureInfo.GetCultureInfo("en-AU"),
        CultureInfo.GetCultureInfo("en-GB"),
        CultureInfo.GetCultureInfo("en-CA"),
        CultureInfo.GetCultureInfo("en-IN"),
        CultureInfo.GetCultureInfo("de-DE"),
        CultureInfo.GetCultureInfo("de-CH"),
        CultureInfo.GetCultureInfo("fr-FR"),
        CultureInfo.GetCultureInfo("da-DK"),
        CultureInfo.GetCultureInfo("hu-HU"),
        CultureInfo.GetCultureInfo("ja-JP"),
        CultureInfo.GetCultureInfo("ko-KR"),
        CultureInfo.GetCultureInfo("pt-BR"),
        CultureInfo.GetCultureInfo("ar-SA"),
        CultureInfo.GetCultureInfo("fa-IR"),
        CultureInfo.GetCultureInfo("he-IL"),
        new MoneyInfo(CultureInfo.GetCultureInfo("en-NZ")),
        CreateCurrencyFormat(),
        CultureInfo.GetCultureInfo("de-DE").NumberFormat,
    ];

    /// <summary>
    /// Values formatted with the input's format string whether or not the input parses: in a currency with minor units,
    /// negative and rounding to zero, the largest amount in a currency with none, with three minor units, and with no currency.
    /// </summary>
    private static readonly Money[] FormattedValues =
    [
        Money.Create(1234.5678m, Iso4217CurrencyCurrent.USD),
        Money.Create(-0.004m, Iso4217CurrencyCurrent.USD),
        Money.Create(decimal.MaxValue, Iso4217CurrencyCurrent.JPY),
        Money.Create(-1234567.891m, Iso4217CurrencyCurrent.BHD),
        Money.Create(1m),
    ];

    /// <summary>
    /// The standard formats that every parsed value is formatted with, and read back from.
    /// </summary>
    private static readonly string[] StandardFormats = ["C", "H", "I", "N", "G"];

    private static void Main()
    {
        Fuzzer.LibFuzzer.Run(Run);
    }

    /// <summary>
    /// Reads the input as a header, a format string and a value, all UTF-8. The first header byte chooses the provider,
    /// the next two the styles, and the last the length of the format string.
    /// </summary>
    /// <remarks>
    /// <see cref="Money.TryParse(string, MoneyStyles, IFormatProvider, out Money)"/>,
    /// <see cref="Money.TryParseExact(string, string[], IFormatProvider, MoneyStyles, out Money)"/> and
    /// <see cref="Money.TryFormat"/> document no exceptions, so any exception from them is a finding.
    /// <see cref="Money.Parse(string, MoneyStyles, IFormatProvider)"/> documents <see cref="FormatException"/>, and so does
    /// <see cref="Money.ToString(string, IFormatProvider)"/>, for a format string that isn't valid. So is
    /// <see cref="Money.Parse(string, MoneyStyles, IFormatProvider)"/> disagreeing with
    /// <see cref="Money.TryParse(string, MoneyStyles, IFormatProvider, out Money)"/>.
    /// </remarks>
    private static void Run(ReadOnlySpan<byte> input)
    {
        if (input.Length < HeaderLength)
        {
            return;
        }

        var provider = Providers[input[0] % Providers.Length];
        var styles = (MoneyStyles)(input[1] | (input[2] << 8)) & DefinedStyles;
        var formatLength = Math.Min(input[3], input.Length - HeaderLength);
        var format = Encoding.UTF8.GetString(input.Slice(HeaderLength, formatLength));
        var text = Encoding.UTF8.GetString(input.Slice(HeaderLength + formatLength));

        Parse(text, styles, provider);
        ParseExact(text, format, styles, provider);

        foreach (var value in FormattedValues)
        {
            Format(value, format, provider);
        }
    }

    /// <summary>
    /// Parses the text, checks that the throwing overload agrees, and formats a parsed value with each standard format.
    /// </summary>
    private static void Parse(string text, MoneyStyles styles, IFormatProvider? provider)
    {
        var isParsed = Money.TryParse(text, styles, provider, out var parsed);

        Money thrown;
        try
        {
            thrown = Money.Parse(text, styles, provider);
        }
        catch (FormatException)
        {
            if (isParsed)
            {
                throw new InvalidOperationException("Parse threw FormatException for a value TryParse accepted.");
            }

            return;
        }

        if (!isParsed || !thrown.Equals(parsed))
        {
            throw new InvalidOperationException("Parse and TryParse gave different results.");
        }

        foreach (var standardFormat in StandardFormats)
        {
            var formatted = parsed.ToString(standardFormat, provider);
            _ = Money.TryParseExact(formatted, [standardFormat], provider, MoneyStyles.Any, out _);
            _ = Money.TryParse(formatted, MoneyStyles.Any, provider, out _);
        }
    }

    /// <summary>
    /// Parses the text against the input's format string, and against it together with the standard formats.
    /// </summary>
    private static void ParseExact(string text, string format, MoneyStyles styles, IFormatProvider? provider)
    {
        _ = Money.TryParseExact(text, [format], provider, styles, out _);
        _ = Money.TryParseExact(text, [format, .. StandardFormats], provider, styles, out _);
    }

    /// <summary>
    /// Formats the value with the input's format string, which may not be valid, then into spans too short and long enough
    /// for the result.
    /// </summary>
    private static void Format(Money value, string format, IFormatProvider? provider)
    {
        string formatted;
        try
        {
            formatted = value.ToString(format, provider);
        }
        catch (FormatException)
        {
            return;
        }

        var tooShort = new char[Math.Max(formatted.Length - 1, 0)];
        var longEnough = new char[formatted.Length];
        if (formatted.Length > 0 && value.TryFormat(tooShort, out _, format, provider))
        {
            throw new InvalidOperationException("TryFormat wrote a value into a span shorter than its ToString result.");
        }

        if (!value.TryFormat(longEnough, out var charsWritten, format, provider)
            || !longEnough.AsSpan(0, charsWritten).SequenceEqual(formatted))
        {
            throw new InvalidOperationException("TryFormat and ToString gave different results.");
        }
    }

    /// <summary>
    /// Creates currency formatting information whose separators and negative sign are more than one character long, with
    /// the negative pattern that puts the sign after the symbol.
    /// </summary>
    private static CurrencyFormatInfo CreateCurrencyFormat()
    {
        return new CurrencyFormatInfo
        {
            CurrencyCode = "CHF",
            CurrencyDecimalSeparator = "\x066B.",
            CurrencyGroupSeparator = "'\x2009",
            CurrencyGroupSizes = [3, 2, 0],
            CurrencyNegativePattern = 16,
            CurrencyPositivePattern = 3,
            NegativeSign = "\x200E-",
        };
    }
}
