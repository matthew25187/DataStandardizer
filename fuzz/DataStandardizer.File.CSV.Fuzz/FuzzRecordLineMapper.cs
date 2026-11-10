namespace DataStandardizer.File.CSV.Fuzz;

/// <summary>
/// Imperative mapper for <see cref="FuzzRecordLine"/>.
/// </summary>
public sealed class FuzzRecordLineMapper : CsvFileMapperBase<FuzzRecordLine>
{
    public FuzzRecordLineMapper()
    {
        this.Map()
            .Property(x => x.First)
            .HasFieldIndex(0);
        this.Map()
            .Property(x => x.Second)
            .HasFieldIndex(1);
        this.Map()
            .Property(x => x.Third)
            .HasFieldIndex(2);
    }
}
