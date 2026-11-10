namespace DataStandardizer.File.vCard.Fuzz;

/// <summary>
/// Reads an input to its end under each of <see cref="FuzzOptions.All"/>, the way an application would that carries on past the errors the readers
/// allow it to, and then asks each card it read for everything an application would.
/// </summary>
/// <remarks>
/// Each reader documents two exceptions for its data.  <see cref="VcardFileException"/> reports data that it can't read.
/// <see cref="InvalidOperationException"/> reports that a read follows one that failed in a way the reader can't continue from.  Any other exception
/// counts as a finding, and so does <see cref="InvalidOperationException"/> when no read has failed.
/// </remarks>
internal static class ReaderHarnesses
{
    public static void ReadVcard(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new VcardReader(stream, options);

            ReadCards(reader.ReadCard);
        }
    }

    public static void ReadJcard(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new JcardReader(stream, options);

            ReadCards(reader.ReadCard);
        }
    }

    public static void ReadXcard(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new XcardReader(stream, options);

            ReadCards(reader.ReadCard);
        }
    }

    public static void ReadContentLines(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new VcardContentLineReader(stream, options);

            ReadContentLines(reader);
        }
    }

    /// <summary>
    /// Reads cards until the end of the data, or until a read fails in a way the reader can't continue from.
    /// </summary>
    /// <remarks>
    /// A <see cref="VcardFileException"/> is a conformance problem with a card the reader has read in full, after which it can continue, or a problem
    /// with the data, after which it can't.  Which one it is depends on the reader and the problem, so this accepts
    /// <see cref="InvalidOperationException"/> after either.
    /// </remarks>
    private static void ReadCards(Func<VcardCard?> readCard)
    {
        var previousReadFailed = false;

        while (true)
        {
            VcardCard? card;
            try
            {
                card = readCard();
            }
            catch (VcardFileException)
            {
                previousReadFailed = true;
                continue;
            }
            catch (InvalidOperationException) when (previousReadFailed)
            {
                return;
            }

            if (card is null)
            {
                return;
            }

            previousReadFailed = false;
            CardWalker.Walk(card);
        }
    }

    private static void ReadContentLines(VcardContentLineReader reader)
    {
        var previousReadFailed = false;

        while (true)
        {
            try
            {
                if (!reader.ReadContentLine())
                {
                    return;
                }

                previousReadFailed = false;
            }
            catch (VcardFileException)
            {
                previousReadFailed = true;
                continue;
            }
            catch (InvalidOperationException) when (previousReadFailed)
            {
                // The reader can continue after a syntax error but not after a line break, line length or decoding error, and only the exception's
                // message tells them apart.  So this accepts the exception after either.
                return;
            }

            ReadCurrentLine(reader.CurrentLine);
        }
    }

    /// <summary>
    /// Asks the view for every part of the line, since it decodes parameter and text values only when asked.
    /// </summary>
    private static void ReadCurrentLine(VcardContentLineView line)
    {
        _ = line.LegacySyntax;
        _ = line.Group;
        _ = line.Name;
        _ = line.RawValue;

        for (var parameterIndex = 0; parameterIndex < line.ParameterCount; parameterIndex++)
        {
            line.IndexOfParameter(line.GetParameterName(parameterIndex));

            var valueCount = line.GetParameterValueCount(parameterIndex);
            for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
            {
                line.GetRawParameterValue(parameterIndex, valueIndex);
                line.GetParameterValue(parameterIndex, valueIndex);
                line.IsParameterValueQuoted(parameterIndex, valueIndex);
            }
        }

        // The view's decoding members copy the line first, so this reads the copy, which documents the same exceptions.
        CardWalker.ReadLine(line.ToContentLine());
    }
}
