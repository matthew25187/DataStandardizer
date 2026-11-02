namespace DataStandardizer.File.CSV.Fuzz;

/// <summary>
/// Record line model that makes the reader map fields to properties.  They are strings because a type converter is chosen by the caller, and what it
/// does with a value is not the parser's behaviour.
/// </summary>
public sealed class FuzzRecordLine : CsvFileRecordLine
{
    public string? First
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public string? Second
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public string? Third
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }
}
