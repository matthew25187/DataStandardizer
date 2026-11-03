namespace DataStandardizer.File.iCal.Fuzz;

/// <summary>
/// Reads an input to its end, the way an application would that carries on past the errors the readers allow it to.
/// </summary>
/// <remarks>
/// Each reader documents two exceptions for its data.  <see cref="IcalFileException"/> reports data that it can't read.
/// <see cref="InvalidOperationException"/> reports that a read follows one that failed in a way the reader can't continue from.  Any other exception
/// counts as a finding, and so does <see cref="InvalidOperationException"/> when the reader should have been able to continue.
/// </remarks>
internal static class ReaderHarnesses
{
    public static void ReadIcal(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new IcalReader(stream);

        ReadCalendars(reader.ReadCalendar);
    }

    public static void ReadJcal(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new JcalReader(stream);

        ReadCalendars(reader.ReadCalendar);
    }

    public static void ReadXcal(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new XcalReader(stream);

        ReadCalendars(reader.ReadCalendar);
    }

    public static void ReadContentLines(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new IcalContentLineReader(stream);
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
            catch (IcalFileException)
            {
                previousReadFailed = true;
                continue;
            }
            catch (InvalidOperationException) when (previousReadFailed)
            {
                // The reader can continue after a syntax error but not after a line break or line length error, and only the exception's message tells
                // them apart.  So this accepts the exception after either.
                return;
            }

            try
            {
                ReadCurrentLine(reader.CurrentLine);
            }
            catch (IcalFileException)
            {
                // IcalContentLineView.GetTextValue documents this for a value with a bad escape sequence.  It isn't a failed read, so the next read must work.
            }
        }
    }

    private static void ReadCalendars(Func<IcalCalendar?> readCalendar)
    {
        while (true)
        {
            try
            {
                if (readCalendar() is null)
                {
                    return;
                }
            }
            catch (IcalFileException exception) when (!CanContinueAfter(exception))
            {
                return;
            }
            catch (IcalFileException)
            {
            }
        }
    }

    /// <summary>
    /// Gets whether a calendar reader can read on after an exception: only after a conformance problem with a calendar it has read in full.
    /// <see cref="IcalDiagnosticKind.UnsupportedVersion"/> on the outermost VCALENDAR has a diagnostic kind too, but it stops the reader as soon as the
    /// VERSION is read.  On a VCALENDAR nested in another component, it is an ordinary conformance problem, and its component path has a parent.
    /// </summary>
    private static bool CanContinueAfter(IcalFileException exception)
    {
        return exception.Data[IcalFileException.DataItemName.DiagnosticKind] is IcalDiagnosticKind kind
            && (kind != IcalDiagnosticKind.UnsupportedVersion
                || (exception.Data[IcalFileException.DataItemName.ComponentPath] is string componentPath && componentPath.Contains('/')));
    }

    /// <summary>
    /// Asks the view for every part of the line, since it decodes parameter and text values only when asked.
    /// </summary>
    private static void ReadCurrentLine(IcalContentLineView line)
    {
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

        line.ToContentLine();
        line.GetTextValue();
    }
}
