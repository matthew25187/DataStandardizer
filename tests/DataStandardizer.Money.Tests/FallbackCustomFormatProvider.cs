namespace DataStandardizer.Money.Tests;

/// <summary>
/// A format provider whose custom formatter follows the usual pattern: it formats every value through
/// <see cref="IFormattable.ToString(string, IFormatProvider)"/> with the provider it was given.
/// </summary>
/// <remarks>
/// A call to <see cref="Format"/> made while another is in progress returns <see cref="ReentryResult"/>
/// rather than formatting again, so that a formatter which calls back into this one ends in a result a
/// test can compare, not in a stack overflow that ends the test host.
/// </remarks>
internal sealed class FallbackCustomFormatProvider : IFormatProvider, ICustomFormatter
{
    /// <summary>
    /// The result of a call to <see cref="Format"/> made while another is in progress.
    /// </summary>
    public const string ReentryResult = "<re-entered>";

    private readonly CurrencyFormatInfo _currencyFormat;
    private bool _formatting;

    public FallbackCustomFormatProvider(CurrencyFormatInfo currencyFormat)
    {
        _currencyFormat = currencyFormat;
    }

    public object? GetFormat(Type? formatType)
    {
        if (formatType == typeof(CurrencyFormatInfo))
        {
            return _currencyFormat;
        }

        return formatType == typeof(ICustomFormatter) ? this : null;
    }

    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        if (_formatting)
        {
            return ReentryResult;
        }

        _formatting = true;
        using var reset = new FormattingScope(this);

        return arg is IFormattable formattable ? formattable.ToString(format, formatProvider) : arg?.ToString() ?? string.Empty;
    }

    private readonly struct FormattingScope : IDisposable
    {
        private readonly FallbackCustomFormatProvider _owner;

        public FormattingScope(FallbackCustomFormatProvider owner)
        {
            _owner = owner;
        }

        public void Dispose() => _owner._formatting = false;
    }
}
