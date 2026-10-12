namespace DataStandardizer.File.CSV.Benchmarks;

/// <summary>
/// Plain model used for the CsvHelper comparison scenarios.
/// </summary>
public sealed class Person
{
    public int Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? City { get; set; }

    public decimal Amount { get; set; }

    public string? Notes { get; set; }
}
