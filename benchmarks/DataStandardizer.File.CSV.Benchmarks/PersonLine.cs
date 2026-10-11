namespace DataStandardizer.File.CSV.Benchmarks;

/// <summary>
/// Record line model used for the DataStandardizer mapped scenarios.
/// </summary>
public sealed class PersonLine : CsvFileRecordLine
{
    public int Id
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public string? FirstName
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public string? LastName
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public string? City
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public decimal Amount
    {
        get => GetPropertyValue<decimal>();
        set => SetPropertyValue(value);
    }

    public string? Notes
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }
}
