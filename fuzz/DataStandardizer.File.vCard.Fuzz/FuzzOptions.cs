namespace DataStandardizer.File.vCard.Fuzz;

/// <summary>
/// The options each harness reads an input under: the strict defaults, and every accommodation a reader honours at once, which reaches the code that
/// only runs past a problem the defaults reject.
/// </summary>
/// <remarks>
/// Both leave <see cref="VcardOptions.MaxLineLength"/> unset, as the constructors that take no options do, so a long line is read in full.  The
/// options that only a writer reads are left at their defaults.
/// </remarks>
internal static class FuzzOptions
{
    public static readonly VcardOptions Strict = new() { MaxLineLength = null };

    public static readonly VcardOptions Lenient = new()
    {
        MaxLineLength = null,
        AcceptBareLineFeed = true,
        AcceptByteOrderMark = true,
        AcceptCaretEncodingInAllVersions = true,
        AcceptEightBitWithoutEncoding = true,
        AcceptLegacySyntax = true,
        AcceptMissingFinalLineBreak = true,
        AcceptOrgUriProperty = true,
        AcceptRfc6350FoldsInVersion21 = true,
        AcceptUnescapedCommasInUris = true,
        AllowCardinalityViolations = true,
        AllowInvalidValues = true,
        AllowMissingRequiredProperties = true,
        AllowUnknownCharsetAsLatin1 = true,
        AllowVersionNotFirst = true
    };

    public static readonly VcardOptions[] All = [Strict, Lenient];
}
