namespace DataStandardizer.File.CSV.Benchmarks;

/// <summary>
/// Imperative mapper for <see cref="PersonLine"/> with no converters, so the reader parses <c>Id</c> and <c>Amount</c> with its built-in parse.
/// </summary>
public sealed class PersonLineParsedMapper : CsvFileMapperBase<PersonLine>
{
    public PersonLineParsedMapper()
    {
        this.Map()
            .Property(x => x.Id)
            .HasFieldIndex(0);
        this.Map()
            .Property(x => x.FirstName)
            .HasFieldIndex(1);
        this.Map()
            .Property(x => x.LastName)
            .HasFieldIndex(2);
        this.Map()
            .Property(x => x.City)
            .HasFieldIndex(3);
        this.Map()
            .Property(x => x.Amount)
            .HasFieldIndex(4);
        this.Map()
            .Property(x => x.Notes)
            .HasFieldIndex(5);
    }
}
