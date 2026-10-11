# DataStandardizer.File.CSV benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) benchmarks for the `DataStandardizer.File.CSV` reader and writer, with [CsvHelper](https://joshclose.github.io/CsvHelper/) as the comparison baseline.

This project is not packed and is not part of the CI test run.

## Running

Benchmarks must be run in Release, from this folder:

```
dotnet run -c Release -- --filter *
```

Run a subset by filtering on class or method name, for example:

```
dotnet run -c Release -- --filter *ReadBenchmarks*
dotnet run -c Release -- --filter *Read_Mapped*
```

To check that every benchmark runs without waiting for a full measurement, use the dry job:

```
dotnet run -c Release -- --filter * --job Dry
```

Results are written to `BenchmarkDotNet.Artifacts/results/` (ignored by git).

## Data

Each benchmark class generates its input in memory in `[GlobalSetup]` from a fixed seed, so every run uses identical data:

- 100,000 record lines plus a header line.
- 6 fields: `Id` (`int`), `FirstName`, `LastName` and `City` (short strings), `Amount` (`decimal`) and `Notes`.
- Roughly 1 in 7 rows has a quoted `Notes` value containing the delimiter, and 1 in 11 has one containing an escaped quote.

Reads come from a `MemoryStream` and writes go to a pre-sized `MemoryStream`, so disk I/O is excluded.

## Scenarios

| Class | Benchmark | What it measures |
|-------|-----------|------------------|
| `ReadBenchmarks` | `Read_Raw` | `CsvFileReader<CsvFileRecordLine>` with `HeaderHandling = Use` (baseline). |
| | `Read_Mapped` | `CsvFileReader<PersonLine>` with the imperative `PersonLineMapper` (`Int32Converter`, `DecimalConverter`). |
| | `Read_CsvHelper_Records` | CsvHelper `GetRecords<Person>()`. |
| | `Read_CsvHelper_Raw` | CsvHelper `Read()` and `GetField(i)` for each field. |
| `WriteBenchmarks` | `Write_Raw` | `CsvFileWriter<CsvFileRecordLine>` with pre-built string fields (baseline). |
| | `Write_Mapped` | `CsvFileWriter<PersonLine>` with `PersonLineMapper`. |
| | `Write_CsvHelper` | CsvHelper `WriteRecords(...)`. |
| `ModelBenchmarks` | `Model_PropertyGet` | 6 typed property reads per row on a pre-built `List<PersonLine>`. |
| | `Model_PropertySet` | 6 typed property writes per row on a pre-built `List<PersonLine>`. |

Every class uses `[MemoryDiagnoser]`. Allocation is the main thing to watch.

## Reporting results

When a performance change is proposed, paste the before and after results into the issue or pull request:

1. The BenchmarkDotNet markdown table, from `BenchmarkDotNet.Artifacts/results/*-report-github.md`.
2. The commit hash that was measured (`git rev-parse --short HEAD`).
3. The machine summary BenchmarkDotNet prints above the table (OS, CPU, .NET SDK and runtime).

Run "before" and "after" on the same machine, one straight after the other, so the numbers can be compared.
