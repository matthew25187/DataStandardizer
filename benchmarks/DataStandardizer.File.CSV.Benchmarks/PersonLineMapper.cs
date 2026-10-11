using System.ComponentModel;

namespace DataStandardizer.File.CSV.Benchmarks;

/// <summary>
/// Imperative mapper for <see cref="PersonLine"/>.
/// </summary>
public sealed class PersonLineMapper : CsvFileMapperBase<PersonLine>
{
    public PersonLineMapper()
    {
        this.Map()
            .Property(x => x.Id)
            .HasFieldIndex(0)
            .ConvertUsing(typeof(Int32Converter));
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
            .HasFieldIndex(4)
            .ConvertUsing(typeof(DecimalConverter));
        this.Map()
            .Property(x => x.Notes)
            .HasFieldIndex(5);
    }
}
