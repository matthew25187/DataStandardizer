namespace DataStandardizer.Chronology.Tests;

/// <summary>
/// Time zone abbreviations from TZ Database 2026e, as <c>zdump -v</c> reports them.
/// </summary>
internal static class TzAbbreviations
{
    // Named abbreviations.
    internal const string Aedt = "AEDT";
    internal const string Aest = "AEST";
    internal const string Bst = "BST";
    internal const string Edt = "EDT";
    internal const string Ept = "EPT";
    internal const string Est = "EST";
    internal const string Ewt = "EWT";
    internal const string Gmt = "GMT";
    internal const string Ist = "IST";
    internal const string Jst = "JST";
    internal const string Lmt = "LMT";
    internal const string Msd = "MSD";
    internal const string Msk = "MSK";

    // Numeric abbreviations, from the %z format of zic.
    internal const string Minus02 = "-02";
    internal const string Minus03 = "-03";
    internal const string Minus04 = "-04";
    internal const string Minus10 = "-10";
    internal const string Plus00 = "+00";
    internal const string Plus01 = "+01";
    internal const string Plus02 = "+02";
    internal const string Plus04 = "+04";
    internal const string Plus0545 = "+0545";
    internal const string Plus0630 = "+0630";
    internal const string Plus1030 = "+1030";
    internal const string Plus11 = "+11";
    internal const string Plus14 = "+14";
}

/// <summary>
/// Offsets from universal time, and amounts of daylight saving, that TZ Database 2026e uses.
/// </summary>
internal static class TzOffsets
{
    internal static readonly TimeSpan UtcMinus1100 = TimeSpan.FromHours(-11);
    internal static readonly TimeSpan UtcMinus1000 = TimeSpan.FromHours(-10);
    internal static readonly TimeSpan UtcMinus0500 = TimeSpan.FromHours(-5);

    // America/New_York kept local mean time until 1883.
    internal static readonly TimeSpan UtcMinus045602 = new(-4, -56, -2);

    internal static readonly TimeSpan UtcMinus0400 = TimeSpan.FromHours(-4);
    internal static readonly TimeSpan UtcMinus0300 = TimeSpan.FromHours(-3);
    internal static readonly TimeSpan UtcMinus0200 = TimeSpan.FromHours(-2);
    internal static readonly TimeSpan UtcMinus0100 = TimeSpan.FromHours(-1);
    internal static readonly TimeSpan UtcZero = TimeSpan.Zero;
    internal static readonly TimeSpan UtcPlus0030 = TimeSpan.FromMinutes(30);
    internal static readonly TimeSpan UtcPlus0100 = TimeSpan.FromHours(1);
    internal static readonly TimeSpan UtcPlus0200 = TimeSpan.FromHours(2);
    internal static readonly TimeSpan UtcPlus0530 = new(5, 30, 0);
    internal static readonly TimeSpan UtcPlus0630 = new(6, 30, 0);
    internal static readonly TimeSpan UtcPlus1030 = new(10, 30, 0);
    internal static readonly TimeSpan UtcPlus1100 = TimeSpan.FromHours(11);
    internal static readonly TimeSpan UtcPlus1300 = TimeSpan.FromHours(13);
    internal static readonly TimeSpan UtcPlus1400 = TimeSpan.FromHours(14);
}
