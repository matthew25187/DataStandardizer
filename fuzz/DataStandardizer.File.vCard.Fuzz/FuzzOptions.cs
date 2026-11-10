namespace DataStandardizer.File.vCard.Fuzz;

/// <summary>
/// The options each harness reads an input under: the strict defaults; every accommodation a reader honours at once, which reaches the code that
/// only runs past a problem the defaults reject; and the conformance accommodations alone, with the lexical ones off.
/// </summary>
/// <remarks>
/// The third set matters because the lexical accommodations change what a value is.  Under <see cref="VcardOptions.AcceptLegacySyntax"/>, a form of
/// vCard 2.1 is read as that form, so a value that is invalid without it reaches <see cref="VcardOptions.AllowInvalidValues"/> only when it is off.
/// <para>
/// All of them leave <see cref="VcardOptions.MaxLineLength"/> unset, as the constructors that take no options do, so a long line is read in full.  The
/// options that only a writer reads are left at their defaults.  So are <see cref="VcardOptions.Encoding"/>, which a reader passes to the .NET
/// decoder, and <see cref="VcardOptions.CharsetResolver"/>, whose only alternative is code of the caller's.
/// </para>
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

    public static readonly VcardOptions Conformance = new()
    {
        MaxLineLength = null,
        AllowCardinalityViolations = true,
        AllowInvalidValues = true,
        AllowMissingRequiredProperties = true
    };

    public static readonly VcardOptions[] All = [Strict, Lenient, Conformance];
}
