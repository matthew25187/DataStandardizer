namespace DataStandardizer.File.iCal.Fuzz;

/// <summary>
/// Reads an input to its end, the way an application would that carries on past the errors the readers allow it to.
/// </summary>
/// <remarks>
/// Each reader documents two exceptions for its data.  <see cref="IcalFileException"/> reports data that it can't read.
/// <see cref="InvalidOperationException"/> reports that a read follows one that failed in a way the reader can't continue from.  Any other exception
/// counts as a finding, and so does <see cref="InvalidOperationException"/> when the read before it succeeded.
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
                ReadCurrentLine(reader.CurrentLine);
            }
            catch (IcalFileException)
            {
                previousReadFailed = true;
            }
            catch (InvalidOperationException) when (previousReadFailed)
            {
                return;
            }
        }
    }

    private static void ReadCalendars(Func<IcalCalendar?> readCalendar)
    {
        var previousReadFailed = false;

        while (true)
        {
            try
            {
                if (readCalendar() is null)
                {
                    return;
                }

                previousReadFailed = false;
            }
            catch (IcalFileException)
            {
                previousReadFailed = true;
            }
            catch (InvalidOperationException) when (previousReadFailed)
            {
                return;
            }
        }
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
