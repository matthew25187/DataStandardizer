namespace DataStandardizer.File.InternetMessage.Fuzz;

/// <summary>
/// The options each harness reads an input under: the strict defaults, and every accommodation at once, which reaches the code that only runs past
/// a problem the defaults reject.
/// </summary>
internal static class FuzzOptions
{
    public static readonly ImfOptions Strict = new();

    public static readonly ImfOptions Lenient = new()
    {
        AcceptBareLineFeed = true,
        AllowBinaryTransferEncoding = true,
        AllowCardinalityViolations = true,
        AllowEightBitTransferEncoding = true,
        AllowEightBitMboxContent = true,
        AllowEightBitHeaders = true,
        AllowInvalidBodyOctets = true,
        AllowInvalidValues = true,
        AllowInvalidRelatedStructure = true,
        AllowInvalidMboxContentLength = true,
        AllowInvalidMboxSeparator = true,
        AllowInvalidTextOctets = true,
        AllowLongLines = true,
        AllowMalformedEncodedWords = true,
        AllowMalformedParameters = true,
        AllowMissingBoundaryTerminator = true,
        AllowMissingMboxBlankLine = true,
        AllowMboxCarriageReturnLineFeed = true,
        AllowMissingMboxSeparator = true,
        AllowMissingMimeVersion = true,
        AllowInvalidEmlxByteCount = true,
        AllowInvalidEmlxMetadata = true,
        AllowMissingRequiredFields = true,
        DecodeTransferEncoding = true
    };

    public static readonly ImfOptions[] All = [Strict, Lenient];
}
