namespace DataStandardizer.File.iCal.Benchmarks;

/// <summary>
/// The calendar formats the file benchmarks read and write.
/// </summary>
public enum CalendarFormat
{
    /// <summary>
    /// iCalendar (<c>.ics</c>), with <see cref="IcalReader"/> and <see cref="IcalWriter"/>.
    /// </summary>
    Ical,

    /// <summary>
    /// xCal (RFC 6321), with <see cref="XcalReader"/> and <see cref="XcalWriter"/>.
    /// </summary>
    Xcal,

    /// <summary>
    /// jCal (RFC 7265), with <see cref="JcalReader"/> and <see cref="JcalWriter"/>.
    /// </summary>
    Jcal
}

/// <summary>
/// Opens files as the file-path constructors of the readers and writers do, with <c>FileOptions.Asynchronous</c> or without it.
/// </summary>
public static class CalendarFiles
{
    /// <summary>
    /// The <see cref="FileStream"/> buffer size the file-path constructors use for each format.
    /// </summary>
    public static int BufferSize(CalendarFormat format) => format == CalendarFormat.Ical ? 16 * 1024 : 4096;

    public static FileStream OpenRead(string path, CalendarFormat format, bool asynchronous)
    {
        var options = FileOptions.SequentialScan | (asynchronous ? FileOptions.Asynchronous : FileOptions.None);
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize(format), options);
    }

    public static FileStream OpenWrite(string path, CalendarFormat format, bool asynchronous)
    {
        var options = asynchronous ? FileOptions.Asynchronous : FileOptions.None;
        return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, BufferSize(format), options);
    }

    /// <summary>
    /// Writes <paramref name="calendar"/> to <paramref name="stream"/> in <paramref name="format"/>.
    /// </summary>
    public static void Write(Stream stream, CalendarFormat format, IcalCalendar calendar)
    {
        switch (format)
        {
            case CalendarFormat.Ical:
                using (var writer = new IcalWriter(stream))
                {
                    writer.WriteCalendar(calendar);
                }

                break;
            case CalendarFormat.Xcal:
                using (var writer = new XcalWriter(stream))
                {
                    writer.WriteCalendar(calendar);
                }

                break;
            case CalendarFormat.Jcal:
                using (var writer = new JcalWriter(stream))
                {
                    writer.WriteCalendar(calendar);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    /// <summary>
    /// Writes <paramref name="calendar"/> to <paramref name="stream"/> in <paramref name="format"/>, asynchronously.
    /// </summary>
    public static async Task WriteAsync(Stream stream, CalendarFormat format, IcalCalendar calendar)
    {
        switch (format)
        {
            case CalendarFormat.Ical:
                await using (var writer = new IcalWriter(stream))
                {
                    await writer.WriteCalendarAsync(calendar);
                    await writer.FlushAsync();
                }

                break;
            case CalendarFormat.Xcal:
                await using (var writer = new XcalWriter(stream))
                {
                    await writer.WriteCalendarAsync(calendar);
                    await writer.FlushAsync();
                }

                break;
            case CalendarFormat.Jcal:
                await using (var writer = new JcalWriter(stream))
                {
                    await writer.WriteCalendarAsync(calendar);
                    await writer.FlushAsync();
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    /// <summary>
    /// Reads the calendars of <paramref name="stream"/> in <paramref name="format"/> and counts their VEVENT components.
    /// </summary>
    public static int CountEvents(Stream stream, CalendarFormat format)
    {
        switch (format)
        {
            case CalendarFormat.Ical:
                using (var reader = new IcalReader(stream))
                {
                    return reader.ReadCalendars().Sum(calendar => calendar.Events.Count());
                }
            case CalendarFormat.Xcal:
                using (var reader = new XcalReader(stream))
                {
                    return reader.ReadCalendars().Sum(calendar => calendar.Events.Count());
                }
            case CalendarFormat.Jcal:
                using (var reader = new JcalReader(stream))
                {
                    return reader.ReadCalendars().Sum(calendar => calendar.Events.Count());
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    /// <summary>
    /// Reads the calendars of <paramref name="stream"/> in <paramref name="format"/> and counts their VEVENT components, asynchronously.
    /// </summary>
    public static async Task<int> CountEventsAsync(Stream stream, CalendarFormat format)
    {
        switch (format)
        {
            case CalendarFormat.Ical:
                await using (var reader = new IcalReader(stream))
                {
                    return await CountEventsAsync(reader.ReadCalendarsAsync());
                }
            case CalendarFormat.Xcal:
                await using (var reader = new XcalReader(stream))
                {
                    return await CountEventsAsync(reader.ReadCalendarsAsync());
                }
            case CalendarFormat.Jcal:
                await using (var reader = new JcalReader(stream))
                {
                    return await CountEventsAsync(reader.ReadCalendarsAsync());
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    private static async Task<int> CountEventsAsync(IAsyncEnumerable<IcalCalendar> calendars)
    {
        var count = 0;
        await foreach (var calendar in calendars)
        {
            count += calendar.Events.Count();
        }

        return count;
    }
}
