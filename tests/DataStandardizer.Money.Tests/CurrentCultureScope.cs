using System.Globalization;

namespace DataStandardizer.Money.Tests;

/// <summary>
/// Sets <see cref="CultureInfo.CurrentCulture"/> for the lifetime of the scope, and restores the culture it replaced when disposed.
/// </summary>
/// <remarks>
/// Use it with a <c>using</c> declaration, so a test that depends on the current culture is deterministic
/// and leaves the culture as it found it.
/// </remarks>
internal sealed class CurrentCultureScope : IDisposable
{
    private readonly CultureInfo _originalCulture;

    public CurrentCultureScope(CultureInfo culture)
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
    }

    public void Dispose() => CultureInfo.CurrentCulture = _originalCulture;
}
