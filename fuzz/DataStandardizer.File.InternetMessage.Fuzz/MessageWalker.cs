using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage.Fuzz;

/// <summary>
/// Asks a message that was read for everything an application would: the typed value of each field, the check of the whole message, and the decoded
/// content of each entity, since a message parses its field bodies and decodes its bodies only when asked.
/// </summary>
/// <remarks>
/// The accessors of the fields document no exception for a field body that breaks its grammar: they give <c>null</c>.  The body accessors document
/// <see cref="ImfFileException"/> for a body that can't be decoded or a structure that can't be read, and nothing else is expected.
/// </remarks>
internal static class MessageWalker
{
    public static void Walk(ImfMessage message)
    {
        ReadHeader(message);
        _ = message.Validate();

        try
        {
            _ = message.Body;
            _ = message.TextBody;
            _ = message.HtmlBody;
            _ = message.Attachments;

            foreach (var node in message.EnumerateEntities())
            {
                _ = node.Path;
                _ = node.Depth;
                ReadEntity(node.Entity);
            }
        }
        catch (ImfFileException)
        {
            // A message documents this for a body whose structure can't be read.  A reader has read the structure already, so the reader should
            // have thrown it instead, but it isn't a finding here.
        }
    }

    private static void ReadHeader(ImfMessage message)
    {
        ReadFields(message.Fields, message.Options);

        _ = message.Date?.ToString();
        _ = message.From?.ToString();
        _ = message.Sender?.ToString();
        _ = message.ReplyTo?.ToString();
        _ = message.To?.ToString();
        _ = message.Cc?.ToString();
        _ = message.Bcc?.ToString();
        _ = message.MessageId?.ToString();
        _ = message.InReplyTo;
        _ = message.References;
        _ = message.Subject;
        _ = message.Comments;

        foreach (var keywords in message.Keywords ?? [])
        {
            _ = keywords.ToString();
        }

        foreach (var block in message.ResentBlocks)
        {
            _ = block.Date?.ToString();
            _ = block.From?.ToString();
            _ = block.Sender?.ToString();
            _ = block.To?.ToString();
            _ = block.Cc?.ToString();
            _ = block.Bcc?.ToString();
            _ = block.MessageId?.ToString();
        }

        foreach (var trace in message.TraceFields)
        {
            _ = trace.Kind;
            _ = trace.ReturnPath?.ToString();
            _ = trace.ReceivedPairs;
            _ = trace.ReceivedDate?.ToString();
        }
    }

    private static void ReadEntity(ImfEntity entity)
    {
        ReadFields(entity.Fields, entity.Options);

        var contentType = entity.ContentType;
        _ = contentType.ToString();
        _ = entity.ContentTransferEncoding;
        _ = entity.ContentDisposition?.ToString();
        _ = entity.ContentId?.ToString();
        _ = entity.ContentDescription;
        _ = entity.ContentLanguage;
        _ = entity.ContentLocation;
        _ = entity.FileName;

        switch (entity)
        {
            case ImfMultipart multipart:
                _ = multipart.Preamble;
                _ = multipart.Epilogue;
                return;
            case ImfMessagePart part:
                // Its entities come next in the enumeration, but its header doesn't.
                ReadHeader(part.Message);
                break;
        }

        try
        {
            _ = entity.GetContent();
        }
        catch (ImfFileException)
        {
        }

        if (!string.Equals(contentType.Type, MimeTopLevelTypes.Text, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            _ = entity.GetText();
        }
        catch (ImfFileException)
        {
        }
    }

    private static void ReadFields(ImfHeaderFieldCollection fields, ImfOptions options)
    {
        foreach (var field in fields)
        {
            _ = field.Name;
            _ = field.Value;
            _ = field.GetDecodedValue(options);
        }
    }
}
