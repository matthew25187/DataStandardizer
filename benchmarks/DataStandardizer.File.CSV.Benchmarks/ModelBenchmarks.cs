using System.Collections.Specialized;
using BenchmarkDotNet.Attributes;

namespace DataStandardizer.File.CSV.Benchmarks;

/// <summary>
/// Measures typed property access on <see cref="PersonLine"/>, independent of any reading or writing.
/// </summary>
[MemoryDiagnoser]
public class ModelBenchmarks
{
    private List<Person> _people = [];
    private List<PersonLine> _personLines = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _people = BenchmarkData.CreatePeople();
        _personLines = _people.Select(BenchmarkData.ToPersonLine).ToList();
    }

    [Benchmark]
    public decimal Model_PropertyGet()
    {
        var total = 0m;
        var length = 0;
        foreach (var personLine in _personLines)
        {
            total += personLine.Id + personLine.Amount;
            length += (personLine.FirstName?.Length ?? 0)
                + (personLine.LastName?.Length ?? 0)
                + (personLine.City?.Length ?? 0)
                + (personLine.Notes?.Length ?? 0);
        }

        return total + length;
    }

    [Benchmark]
    public int Model_PropertySet()
    {
        for (var i = 0; i < _personLines.Count; i++)
        {
            var person = _people[i];
            var personLine = _personLines[i];
            personLine.Id = person.Id;
            personLine.FirstName = person.FirstName;
            personLine.LastName = person.LastName;
            personLine.City = person.City;
            personLine.Amount = person.Amount;
            personLine.Notes = person.Notes;
        }

        return _personLines.Count;
    }

    [Benchmark]
    public decimal Model_IndexerGet()
    {
        var total = 0m;
        var length = 0;
        foreach (IOrderedDictionary personLine in _personLines)
        {
            total += (int)personLine["Id"]! + (decimal)personLine["Amount"]!;
            length += (((string?)personLine["FirstName"])?.Length ?? 0)
                + (((string?)personLine["LastName"])?.Length ?? 0)
                + (((string?)personLine["City"])?.Length ?? 0)
                + (((string?)personLine["Notes"])?.Length ?? 0);
        }

        return total + length;
    }

    [Benchmark]
    public int Model_IndexerSet()
    {
        for (var i = 0; i < _personLines.Count; i++)
        {
            var person = _people[i];
            IOrderedDictionary personLine = _personLines[i];
            personLine["Id"] = person.Id;
            personLine["FirstName"] = person.FirstName;
            personLine["LastName"] = person.LastName;
            personLine["City"] = person.City;
            personLine["Amount"] = person.Amount;
            personLine["Notes"] = person.Notes;
        }

        return _personLines.Count;
    }
}
