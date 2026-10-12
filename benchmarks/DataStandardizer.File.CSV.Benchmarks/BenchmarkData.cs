using System.Globalization;
using System.Text;

namespace DataStandardizer.File.CSV.Benchmarks;

/// <summary>
/// Generates the seeded, in-memory input shared by all benchmarks.
/// </summary>
public static class BenchmarkData
{
    public const int RowCount = 100_000;
    public const int Seed = 20260101;

    public static readonly string[] FieldNames = ["Id", "FirstName", "LastName", "City", "Amount", "Notes"];

    private static readonly string[] FirstNames = ["Alice", "Bob", "Carol", "Dave", "Erin", "Frank", "Grace", "Heidi", "Ivan", "Judy"];
    private static readonly string[] LastNames = ["Smith", "Jones", "Brown", "Taylor", "Wilson", "Walker", "White", "Harris", "Lewis", "Young"];
    private static readonly string[] Cities = ["Auckland", "Wellington", "Christchurch", "Hamilton", "Dunedin", "Napier", "Nelson", "Tauranga"];
    private static readonly string[] Notes = ["", "Preferred customer", "Call before delivery", "Account on hold", "Paid in full"];

    /// <summary>
    /// Builds the people that make up the data set.  Roughly 1 in 7 rows has a notes field containing the delimiter,
    /// and 1 in 11 has a notes field containing a quote that must be escaped.
    /// </summary>
    public static List<Person> CreatePeople()
    {
        var random = new Random(Seed);
        var people = new List<Person>(RowCount);
        for (var i = 0; i < RowCount; i++)
        {
            var notes = Notes[random.Next(Notes.Length)];
            if (i % 7 == 0)
            {
                notes = $"{notes}, see account {random.Next(1000, 9999)}";
            }

            if (i % 11 == 0)
            {
                notes = $"{notes} \"urgent\"";
            }

            people.Add(new Person
            {
                Id = i + 1,
                FirstName = FirstNames[random.Next(FirstNames.Length)],
                LastName = LastNames[random.Next(LastNames.Length)],
                City = Cities[random.Next(Cities.Length)],
                Amount = Math.Round((decimal)(random.NextDouble() * 10_000), 2),
                Notes = notes
            });
        }

        return people;
    }

    /// <summary>
    /// Serializes the people as an RFC 4180 CSV document with a header line.
    /// </summary>
    public static byte[] CreateCsv(IEnumerable<Person> people)
    {
        var builder = new StringBuilder();
        builder.Append(string.Join(",", FieldNames)).Append("\r\n");
        foreach (var person in people)
        {
            builder
                .Append(person.Id.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Quote(person.FirstName)).Append(',')
                .Append(Quote(person.LastName)).Append(',')
                .Append(Quote(person.City)).Append(',')
                .Append(person.Amount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Quote(person.Notes)).Append("\r\n");
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public static CsvFileHeaderLine CreateHeaderLine()
    {
        var headerLine = new CsvFileHeaderLine();
        foreach (var fieldName in FieldNames)
        {
            headerLine.Add(fieldName, fieldName);
        }

        return headerLine;
    }

    public static CsvFileRecordLine ToRecordLine(Person person)
    {
        var recordLine = new CsvFileRecordLine();
        recordLine.Add("Id", person.Id.ToString(CultureInfo.InvariantCulture));
        recordLine.Add("FirstName", person.FirstName);
        recordLine.Add("LastName", person.LastName);
        recordLine.Add("City", person.City);
        recordLine.Add("Amount", person.Amount.ToString(CultureInfo.InvariantCulture));
        recordLine.Add("Notes", person.Notes);
        return recordLine;
    }

    public static PersonLine ToPersonLine(Person person)
    {
        return new PersonLine
        {
            Id = person.Id,
            FirstName = person.FirstName,
            LastName = person.LastName,
            City = person.City,
            Amount = person.Amount,
            Notes = person.Notes
        };
    }

    private static string? Quote(string? value)
    {
        if (value is null || value.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
