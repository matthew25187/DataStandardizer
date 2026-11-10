using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage.Fuzz;

/// <summary>
/// Parses an input as the body of each kind of field, and as a media type and a run of encoded words, which a whole message reaches only behind its
/// header.  A small input reaches these parsers faster on its own.
/// </summary>
/// <remarks>
/// The <c>TryParse</c> methods document no exception for their value, only for their options.  <see cref="ImfHeaderField.FromRaw(ReadOnlyMemory{byte}, ImfOptions)"/>
/// documents <see cref="ImfFileException"/>, and <see cref="ImfHeaderText.DecodeUnstructured(ReadOnlySpan{byte}, ImfOptions)"/> documents
/// <see cref="FormatException"/> for bytes that aren't UTF-8 under the strict options.  Anything else is a finding.  A value that parses is written
/// back with <c>ToString</c>, which documents no exception.
/// </remarks>
internal static class FieldHarness
{
    private static readonly MimeEncodedWordContext[] EncodedWordContexts =
        [MimeEncodedWordContext.Text, MimeEncodedWordContext.Comment, MimeEncodedWordContext.Phrase];

    private static readonly MimeEncodedWordParseOptions AllEncodedWordOptions =
        MimeEncodedWordParseOptions.AllowLongWords | MimeEncodedWordParseOptions.AllowEmbeddedWords | MimeEncodedWordParseOptions.AllowUnknownCharsets
        | MimeEncodedWordParseOptions.AllowLowercaseHexDigits | MimeEncodedWordParseOptions.AllowInvalidOctets | MimeEncodedWordParseOptions.AllowSplitCharacters;

    private static readonly MimeMediaTypeParseOptions AllMediaTypeOptions =
        MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters | MimeMediaTypeParseOptions.AllowRfc2045Tokens | MimeMediaTypeParseOptions.AllowNonstandardSections
        | MimeMediaTypeParseOptions.AllowUnknownCharsets | MimeMediaTypeParseOptions.AllowNonstandardExtendedValues | MimeMediaTypeParseOptions.AllowInvalidOctets
        | MimeMediaTypeParseOptions.AllowUtf8QuotedStrings;

    public static void ParseFields(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            ParseImfFields(data, options);
        }

        // ISO-8859-1 maps each byte to the character with its value, so every character up to U+00FF is reachable.
        var text = Encoding.Latin1.GetString(data);
        ParseMimeValues(text, MimeMediaTypeParseOptions.None, MimeEncodedWordParseOptions.None);
        ParseMimeValues(text, AllMediaTypeOptions, AllEncodedWordOptions);
    }

    private static void ParseImfFields(byte[] data, ImfOptions options)
    {
        var value = data.AsSpan();

        if (ImfAddress.TryParse(value, options, out var address)) _ = address.ToString();
        if (ImfAddressList.TryParse(value, options, out var addressList)) _ = addressList.ToString();
        if (ImfMailbox.TryParse(value, options, out var mailbox)) _ = mailbox.ToString();
        if (ImfGroup.TryParse(value, options, out var group)) _ = group.ToString();
        if (ImfPhraseList.TryParse(value, options, out var phraseList)) _ = phraseList.ToString();
        if (ImfDateTime.TryParse(value, options, out var dateTime)) _ = dateTime.ToString();
        if (ImfContentDisposition.TryParse(value, options, out var disposition)) _ = disposition.ToString();
        if (ImfMessageId.TryParse(value, out var messageId)) _ = messageId.ToString();
        if (ImfMessageId.TryParseList(value, out var messageIds)) _ = messageIds.Count;
        if (ImfMimeFields.TryParseMimeVersion(value, out var mimeVersion)) _ = mimeVersion.ToString();
        if (ImfMimeFields.TryParseContentType(value, options, out var contentType)) _ = contentType.ToString();
        if (ImfMimeFields.TryParseContentTransferEncoding(value, out var transferEncoding)) _ = transferEncoding.ToString();
        if (ImfMimeFields.TryParseContentLanguage(value, options, out var languages)) _ = languages.Count;
        _ = ImfHeaderText.TryGetComments(value, options, out _);

        try
        {
            _ = ImfHeaderText.DecodeUnstructured(value, options);
        }
        catch (FormatException)
        {
        }

        try
        {
            var field = ImfHeaderField.FromRaw(data, options);
            _ = field.Value;
            _ = field.GetDecodedValue(options);
        }
        catch (ImfFileException)
        {
        }
    }

    private static void ParseMimeValues(string text, MimeMediaTypeParseOptions mediaTypeOptions, MimeEncodedWordParseOptions encodedWordOptions)
    {
        if (MimeMediaType.TryParse(text, mediaTypeOptions, out var mediaType))
        {
            _ = mediaType.ToString();
            foreach (var parameter in mediaType.Parameters)
            {
                _ = parameter.ToString();
            }
        }

        if (ImfMessageId.TryParseCidUri(text, out var contentId)) _ = contentId.ToString();

        foreach (var context in EncodedWordContexts)
        {
            _ = MimeEncodedWord.DecodeText(text, context, encodedWordOptions, MimeCharsetResolver.Default);
            if (MimeEncodedWord.TryParse(text, context, encodedWordOptions, MimeCharsetResolver.Default, out var word)) _ = word.ToString();
        }
    }
}
