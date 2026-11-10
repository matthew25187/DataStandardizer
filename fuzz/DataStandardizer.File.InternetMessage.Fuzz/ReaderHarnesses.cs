namespace DataStandardizer.File.InternetMessage.Fuzz;

/// <summary>
/// Reads an input to its end under each of <see cref="FuzzOptions.All"/>, the way an application would that carries on past the errors the readers
/// allow it to, and then asks what it read for everything an application would.
/// </summary>
/// <remarks>
/// Each reader documents two exceptions for its data.  <see cref="ImfFileException"/> reports data that it can't read.
/// <see cref="InvalidOperationException"/> reports that a read follows one that failed in a way the reader can't continue from.  Any other exception
/// counts as a finding, and so does <see cref="InvalidOperationException"/> when the reader should have been able to continue.
/// </remarks>
internal static class ReaderHarnesses
{
    private static readonly Uri BaseUri = new("http://example.org/a/");

    private static readonly MboxVariant[] MboxVariants = [MboxVariant.Mboxrd, MboxVariant.Mboxo, MboxVariant.Mboxcl, MboxVariant.Mboxcl2];

    /// <summary>
    /// The content an EMLX part resolver gives for every missing part, so that the harness reaches the code that puts a part into its message.
    /// </summary>
    private static readonly byte[] PartContent = "AAEC\n"u8.ToArray();

    public static void ReadMessage(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new ImfReader(stream, options);

            ImfMessage message;
            try
            {
                message = reader.ReadMessage();
            }
            catch (ImfFileException)
            {
                continue;
            }

            _ = reader.Diagnostics;
            MessageWalker.Walk(message);
        }
    }

    public static void ReadEntityEvents(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new ImfEntityReader(stream, options);

            ReadEvents(reader);
            _ = reader.Diagnostics;
            _ = reader.EntitiesRead;
        }
    }

    public static void ReadMbox(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            foreach (var variant in MboxVariants)
            {
                ReadMboxMessages(data, variant, options);
                ReadMboxEvents(data, variant, options);
            }
        }
    }

    public static void ReadEmlx(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new EmlxReader(stream, options);

            EmlxEntry entry;
            try
            {
                entry = reader.ReadEntry(ResolvePart);
            }
            catch (ImfFileException)
            {
                continue;
            }

            _ = entry.Diagnostics;
            foreach (var part in entry.MissingParts)
            {
                _ = part.Path;
                _ = part.ContentLength;
            }

            ReadMetadata(entry.Metadata);
            MessageWalker.Walk(entry.Message);
        }
    }

    public static void ReadMhtml(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            ReadMhtmlDocument(data, options, null);
            ReadMhtmlDocument(data, options, BaseUri);
        }
    }

    private static void ReadMboxMessages(byte[] data, MboxVariant variant, ImfOptions options)
    {
        using var stream = new MemoryStream(data);
        using var reader = new MboxReader(stream, variant, options);
        var previousReadFailed = false;

        while (true)
        {
            MboxEntry? entry;
            try
            {
                entry = reader.ReadMessage();
            }
            catch (ImfFileException)
            {
                // A message that the file holds correctly ends only that message, and the next read goes on to the next one.
                previousReadFailed = true;
                continue;
            }
            catch (InvalidOperationException) when (previousReadFailed)
            {
                // The reader can continue after an error in a message but not after one in the format of the file, and only the exception's message
                // tells them apart.  So this accepts the exception after either.
                return;
            }

            if (entry is null)
            {
                return;
            }

            previousReadFailed = false;
            _ = entry.Envelope.ToString();
            _ = entry.Envelope.Sender?.ToString();
            _ = entry.Envelope.Date;
            _ = entry.Diagnostics;
            MessageWalker.Walk(entry.Message);
        }
    }

    private static void ReadMboxEvents(byte[] data, MboxVariant variant, ImfOptions options)
    {
        using var stream = new MemoryStream(data);
        using var reader = new MboxReader(stream, variant, options);
        var previousReadFailed = false;

        while (true)
        {
            ImfEntityReader? events;
            try
            {
                events = reader.ReadEntityEvents();
            }
            catch (ImfFileException)
            {
                previousReadFailed = true;
                continue;
            }
            catch (InvalidOperationException) when (previousReadFailed)
            {
                return;
            }

            if (events is null)
            {
                return;
            }

            _ = reader.Envelope?.ToString();
            previousReadFailed = !ReadEvents(events);
        }
    }

    private static void ReadMhtmlDocument(byte[] data, ImfOptions options, Uri? baseUri)
    {
        using var stream = new MemoryStream(data);
        using var reader = new MhtmlReader(stream, options);

        MhtmlDocument document;
        try
        {
            document = reader.ReadDocument(baseUri);
        }
        catch (ImfFileException)
        {
            return;
        }

        _ = reader.Diagnostics;
        _ = document.Diagnostics;
        _ = document.Root;
        _ = document.RootMediaType.ToString();

        foreach (var resource in document.Resources)
        {
            _ = resource.Entity;
            if (resource.Uri is not { } uri)
            {
                continue;
            }

            _ = document.TryResolve(uri, out _);
            _ = document.TryResolve(uri, resource, out _);
        }

        MessageWalker.Walk(document.Message);
    }

    /// <summary>
    /// Reads the events of a message to its end, and gives whether it got there without an <see cref="ImfFileException"/>, which ends reading.
    /// </summary>
    private static bool ReadEvents(ImfEntityReader reader)
    {
        while (true)
        {
            ImfEntityEvent? entityEvent;
            try
            {
                entityEvent = reader.ReadEvent();
            }
            catch (ImfFileException)
            {
                return false;
            }

            if (entityEvent is not { } current)
            {
                return true;
            }

            ReadEvent(current);
        }
    }

    /// <summary>
    /// Asks the view for every part of the event, since it decodes field bodies and media types only when asked.
    /// </summary>
    private static void ReadEvent(ImfEntityEvent entityEvent)
    {
        _ = entityEvent.EntityPath;
        _ = entityEvent.Depth;
        _ = entityEvent.ByteOffset;
        _ = entityEvent.ByteLength;
        _ = entityEvent.Data.Length;
        _ = entityEvent.IsDecoded;

        switch (entityEvent.Kind)
        {
            case ImfEntityEventKind.EntityStart:
                break;
            case ImfEntityEventKind.HeaderField:
                var field = entityEvent.Field;
                _ = field.GetName();
                _ = field.GetValue();
                _ = field.GetDecodedValue();
                _ = new ImfHeaderField(field).Value;
                break;
            default:
                // Every other kind comes after the entity's HeaderEnd event, so it has the entity's media type and transfer encoding.
                _ = entityEvent.ContentType.ToString();
                _ = entityEvent.TransferEncoding;
                break;
        }
    }

    private static void ReadMetadata(EmlxMetadata metadata)
    {
        _ = metadata.Entries;
        _ = metadata.IsWellFormed;
        _ = metadata.Flags;
        _ = metadata.MessageFlags;
        _ = metadata.AttachmentCount;
        _ = metadata.PriorityLevel;
        _ = metadata.DateReceived;
        _ = metadata.DateLastViewed;
        _ = metadata.RemoteId;
        _ = metadata.Subject;
    }

    private static Stream ResolvePart(EmlxMissingPart part)
    {
        return new MemoryStream(PartContent, false);
    }
}
