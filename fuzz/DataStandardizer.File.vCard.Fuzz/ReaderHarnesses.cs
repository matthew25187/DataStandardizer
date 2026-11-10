namespace DataStandardizer.File.vCard.Fuzz;

/// <summary>
/// Reads an input to its end under each of <see cref="FuzzOptions.All"/>, the way an application would that carries on past the errors the readers
/// allow it to, and then asks each card it read for everything an application would.
/// </summary>
/// <remarks>
/// Each reader documents two exceptions for its data.  <see cref="VcardFileException"/> reports data that it can't read.
/// <see cref="InvalidOperationException"/> reports that a read follows one that failed in a way the reader can't continue from.  Any other exception
/// counts as a finding, and so does <see cref="InvalidOperationException"/> when the reader should have been able to continue.
/// </remarks>
internal static class ReaderHarnesses
{
    public static void ReadVcard(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new VcardReader(stream, options);

            ReadCards(reader.ReadCard, () => reader.CardsRead);
        }
    }

    public static void ReadJcard(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new JcardReader(stream, options);

            ReadCards(reader.ReadCard, () => reader.CardsRead);
        }
    }

    public static void ReadXcard(byte[] data)
    {
        foreach (var options in FuzzOptions.All)
        {
            using var stream = new MemoryStream(data);
            using var reader = new XcardReader(stream, options);

            ReadCards(reader.ReadCard, () => reader.CardsRead);
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
    /// Reads cards until the end of the data, or until a read fails in a way the reader can't continue from, then walks every card it read.
    /// </summary>
    /// <remarks>
    /// A <see cref="VcardFileException"/> is either a conformance problem with a card the reader has read in full, after which it can continue, or a
    /// problem with the data, after which it can't.  Both can carry the same <see cref="VcardDiagnosticKind"/>, so the kind doesn't tell them apart.
    /// <c>CardsRead</c> does: it counts a card that failed a conformance check, and a card is counted only once it has been read in full.  So the
    /// reader must continue after an exception from a read that counted a card, and an <see cref="InvalidOperationException"/> from the next read is
    /// a finding.
    /// </remarks>
    private static void ReadCards(Func<VcardCard?> readCard, Func<long> cardsRead)
    {
        var cards = new List<VcardCard>();
        var canContinue = true;

        while (true)
        {
            var cardsReadBefore = cardsRead();
            VcardCard? card;
            try
            {
                card = readCard();
            }
            catch (VcardFileException)
            {
                canContinue = cardsRead() > cardsReadBefore;
                continue;
            }
            catch (InvalidOperationException) when (!canContinue)
            {
                break;
            }

            if (card is null)
            {
                break;
            }

            canContinue = true;
            cards.Add(card);
        }

        CardWalker.Walk(cards);
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
        _ = line.LineNumber;
        _ = line.PhysicalLineNumber;
        _ = line.LegacySyntax;
        _ = line.Group;
        _ = line.Name;
        _ = line.RawValue;

        for (var parameterIndex = 0; parameterIndex < line.ParameterCount; parameterIndex++)
        {
            line.GetParameterName(parameterIndex);

            var valueCount = line.GetParameterValueCount(parameterIndex);
            for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
            {
                line.GetRawParameterValue(parameterIndex, valueIndex);
                line.GetParameterValue(parameterIndex, valueIndex);
                line.IsParameterValueQuoted(parameterIndex, valueIndex);
            }
        }

        // By the names this package defines, not by the names on the line, so that this stays linear in the number of parameters.
        foreach (var name in CardWalker.ParameterNames)
        {
            line.IndexOfParameter(name);
        }

        // The view unescapes TEXT itself, by other code than the copy's GetTextValue, so both are read.
        try
        {
            line.GetTextValue();
        }
        catch (VcardFileException)
        {
        }

        // The view's GetDecodedValue and GetDecodedText copy the line first, so the copy is read instead.
        CardWalker.ReadLine(line.ToContentLine());
    }
}
