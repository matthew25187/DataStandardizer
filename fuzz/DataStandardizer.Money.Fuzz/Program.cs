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
    /// The largest precision of a standard format that Money passes to <see cref="decimal"/>, which the harness formats with.
    /// <see cref="decimal"/> accepts a precision of up to 999,999,999 and builds a string that long, which exhausts the heap
    /// limit the README sets. Money passes the standard formats it doesn't define to <see cref="decimal"/> unchanged, so that
    /// is <see cref="decimal"/>'s behaviour, not a finding.
    /// </summary>
    private const int MaximumDecimalPrecision = 99;

    /// <summary>
    /// The format specifiers Money defines itself, which reject a precision of more than two digits.
    /// </summary>
    private const string MoneySpecifiers = "CHING";

    /// <summary>
    /// The name of the culture of every thread, so that the providers that fall back to the current culture behave the same on
    /// every machine.
    /// </summary>
    private const string CurrentCultureName = "en-US";

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

    /// <remarks>
    /// Nothing here may read a static field of this class. That would run its initializers, which call the instrumented
    /// libraries, before <see cref="Fuzzer.LibFuzzer.Run(ReadOnlySpanAction)"/> sets up the shared memory their coverage is
    /// written to, and they would throw <see cref="NullReferenceException"/>.
    /// </remarks>
    private static void Main()
    {
        var culture = CultureInfo.GetCultureInfo(CurrentCultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        Fuzzer.LibFuzzer.Run(Run);
    }

    /// <summary>
    /// Reads the input as a header, a format string and a value, all UTF-8. The first header byte chooses the provider,
    /// the next two the styles, and the last the length of the format string.
    /// </summary>
    /// <remarks>
    /// <see cref="Money.TryParse(string, MoneyStyles, IFormatProvider, out Money)"/> and
    /// <see cref="Money.TryParseExact(string, string[], IFormatProvider, MoneyStyles, out Money)"/> document no exceptions,
    /// so any exception from them is a finding. <see cref="Money.ToString(string, IFormatProvider)"/> documents
    /// <see cref="FormatException"/> for a format string that isn't valid, and <see cref="Money.TryFormat"/> throws it too, as
    /// <see cref="decimal.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider)"/> does. Any other exception
    /// from them, or the two disagreeing about a format string, is a finding. So is
    /// <see cref="Money.TryParseExact(string, string[], IFormatProvider, MoneyStyles, out Money)"/>, which accepts a value that
    /// formatting reproduces, rejecting the <c>I</c> rendering of a parsed value with a currency.
    /// </remarks>
    private static void Run(ReadOnlySpan<byte> input)
    {
        if (input.Length < HeaderLength)
        {
            return;
        }

        var provider = Providers[input[0] % Providers.Length];
        var styles = (MoneyStyles)(input[1] | (input[2] << 8)) & MoneyStyles.Any;
        var formatLength = Math.Min(input[3], input.Length - HeaderLength);
        var format = Encoding.UTF8.GetString(input.Slice(HeaderLength, formatLength));
        var text = Encoding.UTF8.GetString(input.Slice(HeaderLength + formatLength));

        Parse(text, styles, provider);

        if (IsDecimalPrecisionTooLarge(format))
        {
            return;
        }

        ParseExact(text, format, styles, provider);

        foreach (var value in FormattedValues)
        {
            Format(value, format, provider);
        }
    }

    /// <summary>
    /// Parses the text, then formats a parsed value with each standard format and reads it back.
    /// </summary>
    private static void Parse(string text, MoneyStyles styles, IFormatProvider? provider)
    {
        if (!Money.TryParse(text, styles, provider, out var parsed))
        {
            return;
        }

        foreach (var standardFormat in StandardFormats)
        {
            var formatted = parsed.ToString(standardFormat, provider);
            var isReadBack = Money.TryParseExact(formatted, [standardFormat], provider, MoneyStyles.Currency, out _);
            _ = Money.TryParse(formatted, MoneyStyles.Any, provider, out _);

            if (standardFormat == "I" && parsed.IsoCurrencyCode != Iso4217CurrencyCurrent.XXX && !isReadBack)
            {
                throw new InvalidOperationException("TryParseExact rejected the I rendering of a parsed value.");
            }
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
    /// Formats the value with the input's format string, which may not be valid, with
    /// <see cref="Money.ToString(string, IFormatProvider)"/>, and with <see cref="Money.TryFormat"/> into a span one character
    /// too short for the result, and checks that they agree.
    /// </summary>
    private static void Format(Money value, string format, IFormatProvider? provider)
    {
        string? formatted;
        try
        {
            formatted = value.ToString(format, provider);
        }
        catch (FormatException)
        {
            formatted = null;
        }

        if (formatted is { Length: 0 })
        {
            return;
        }

        var tooShort = new char[formatted is null ? 0 : formatted.Length - 1];
        bool isWritten;
        try
        {
            isWritten = value.TryFormat(tooShort, out _, format, provider);
        }
        catch (FormatException)
        {
            if (formatted is not null)
            {
                throw new InvalidOperationException("TryFormat threw FormatException for a format string ToString accepted.");
            }

            return;
        }

        if (formatted is null)
        {
            throw new InvalidOperationException("TryFormat accepted a format string ToString rejected.");
        }

        if (isWritten)
        {
            throw new InvalidOperationException("TryFormat wrote a value into a span shorter than its ToString result.");
        }
    }

    /// <summary>
    /// Determines whether the format string is a standard format that Money passes to <see cref="decimal"/>, with a precision
    /// greater than <see cref="MaximumDecimalPrecision"/>.
    /// </summary>
    /// <remarks>
    /// .NET number formatting reads a format string only as far as its first NUL character, so <c>P999</c> followed by a NUL
    /// and anything else is <c>P999</c>.
    /// </remarks>
    private static bool IsDecimalPrecisionTooLarge(string format)
    {
        var end = format.IndexOf('\0');
        if (end >= 0)
        {
            format = format.Substring(0, end);
        }

        if (format.Length < 2
            || !char.IsAsciiLetter(format[0])
            || MoneySpecifiers.Contains(char.ToUpperInvariant(format[0])))
        {
            return false;
        }

        var digits = format.AsSpan(1);
        if (digits.ContainsAnyExceptInRange('0', '9'))
        {
            return false;
        }

        // A precision too long for an int is larger than any precision the harness formats with.
        return !int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var precision)
               || precision > MaximumDecimalPrecision;
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
