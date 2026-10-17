# DataStandardizer.File.iCal benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) benchmarks for the `DataStandardizer.File.iCal` reader, writer and occurrence expansion, with [Ical.Net](https://github.com/ical-org/ical.net) as the comparison.

This project is not packed and is not part of the CI test run.

## Running

Benchmarks must be run in Release, from this folder:

```
dotnet run -c Release -- --filter *
```

Run a subset by filtering on class or method name, for example:

```
dotnet run -c Release -- --filter *ReadBenchmarks*
dotnet run -c Release -- --filter *Read_View*
dotnet run -c Release -- --filter *.WriteBenchmarks.* *.ExpandBenchmarks.*
```

Filters match the full name, including the namespace, so `*iCal*` selects every benchmark. `*Read_View*` also selects `Read_View_Async`.

To check that every benchmark runs without waiting for a full measurement, use the dry job:

```
dotnet run -c Release -- --filter * --job Dry
```

Results are written to `BenchmarkDotNet.Artifacts/results/` (ignored by git).

## Data

Each benchmark class generates its input in memory in `[GlobalSetup]` from a fixed seed, so every run uses identical data. There are two corpora, both a single `VCALENDAR` with `VTIMEZONE` components for `America/New_York` and `Pacific/Auckland` (each a `STANDARD` and a `DAYLIGHT` observance with an `RRULE`, as calendar clients write them):

- The main corpus, used by `ReadBenchmarks`, `WriteBenchmarks`, `FileReadBenchmarks` and `FileWriteBenchmarks`: 5,000 `VEVENT` components, about 4.2 MB of UTF-8 and about 80,000 content lines.
  - Every event has `UID`, `DTSTAMP`, `DTSTART`, `DTEND`, `SUMMARY`, `DESCRIPTION`, `LOCATION`, `ORGANIZER`, `SEQUENCE`, `STATUS` and `TRANSP`; most have `CATEGORIES` and up to four `ATTENDEE` lines with `CN` (quoted when it contains a comma), `ROLE`, `PARTSTAT` and `RSVP` parameters.
  - About 60% of the events start in one of the two time zones (`TZID`), 25% in UTC and 15% on a `DATE`. 1 in 6 has an `RRULE`, some with `EXDATE` and `RDATE`.
  - Text values contain escaped commas, semicolons, backslashes and line breaks, and some non-ASCII characters. Most descriptions are long enough to be folded, at 75 octets.
- The expansion corpus, used by `ExpandBenchmarks`: 140 recurring `VEVENT` components, ten of each of 14 kinds of rule: daily; every other day with `COUNT`; weekly with `BYDAY`, `EXDATE` and `RDATE`; fortnightly with `BYDAY` and `UNTIL`; monthly with `BYMONTHDAY` (15 and -1, the latter in UTC with `COUNT`); the last weekday of the month (`BYDAY` with `BYSETPOS=-1`); the second Tuesday; quarterly on the first Monday; yearly with `BYMONTH` and `BYDAY`, with `BYMONTH` and `BYMONTHDAY`, and on a `DATE`; weekly in UTC with `COUNT`; and weekdays with `UNTIL` and `EXDATE`. Most start at a local time in one of the two time zones. Expanded over the ten years from 2026-01-01T00:00:00Z to 2036-01-01T00:00:00Z, they give 75,767 occurrences.

`ReadBenchmarks` and `WriteBenchmarks` read from a `MemoryStream` and write to a pre-sized `MemoryStream`, so disk I/O is excluded. `FileReadBenchmarks` and `FileWriteBenchmarks` read and write a real temporary file, in each of the three formats: the main corpus as `.ics` (about 4.2 MB), xCal (about 13.9 MB) and jCal (about 5.5 MB). They open the `FileStream` themselves, as the file-path constructors do (the same buffer size, `FileShare.Read` and, for reading, `FileOptions.SequentialScan`), with `FileOptions.Asynchronous` or without it, so the cost of that option can be measured on its own. The writing and expansion benchmarks work on models built in `[GlobalSetup]`, so only the writing or the expansion is measured.

## Scenarios

| Class | Benchmark | What it measures |
|-------|-----------|------------------|
| `ReadBenchmarks` | `Read_View` | `IcalContentLineReader.ReadContentLine()` and `CurrentLine`, comparing the name and value spans of each line. Nothing is allocated per line. |
| | `Read_ContentLines` | `IcalContentLineReader.ReadLine()`, materializing an `IcalContentLine` for each line. |
| | `Read_Model` | `IcalReader.ReadCalendars()`, building the `IcalCalendar` model (baseline). |
| | `Read_View_Async` | As `Read_View`, with `ReadContentLineAsync()`. |
| | `Read_ContentLines_Async` | As `Read_ContentLines`, with `ReadLineAsync()`. |
| | `Read_Model_Async` | As `Read_Model`, with `ReadCalendarsAsync()`. |
| | `Read_IcalNet` | Ical.Net `Calendar.Load(Stream)`. |
| `WriteBenchmarks` | `Write_ContentLines` | `IcalContentLineWriter.WriteLines(...)` with the corpus's pre-read content lines. |
| | `Write_Model` | `IcalWriter.WriteCalendar(...)` with the pre-read `IcalCalendar` (baseline). |
| | `Write_ContentLines_Async` | As `Write_ContentLines`, with `WriteLinesAsync(...)`. |
| | `Write_Model_Async` | As `Write_Model`, with `WriteCalendarAsync(...)`. |
| | `Write_IcalNet` | Ical.Net `CalendarSerializer.Serialize(...)` with the pre-loaded `Calendar`. |
| `FileReadBenchmarks` | `Read` | `ReadCalendars()` of `IcalReader`, `XcalReader` or `JcalReader` (`Format`), over a `FileStream` opened with or without `FileOptions.Asynchronous` (`Asynchronous`) (baseline). |
| | `Read_Async` | As `Read`, with `ReadCalendarsAsync()`. |
| `FileWriteBenchmarks` | `Write` | `WriteCalendar(...)` of `IcalWriter`, `XcalWriter` or `JcalWriter` (`Format`) with the pre-read `IcalCalendar`, over a `FileStream` opened with or without `FileOptions.Asynchronous` (`Asynchronous`) (baseline). |
| | `Write_Async` | As `Write`, with `WriteCalendarAsync(...)` and `FlushAsync()`. |
| `ExpandBenchmarks` | `Expand_CalendarTimeZones` | `IcalEvent.GetOccurrences(calendar, from, to)` for each event, resolving time zones from the calendar's `VTIMEZONE` components (`IcalTimeZoneResolver.Default`) (baseline). |
| | `Expand_TzData` | As `Expand_CalendarTimeZones`, resolving time zones from the TZ Database (`IcalTimeZoneResolver.TzData`). |
| | `Expand_IcalNet` | Ical.Net `CalendarEvent.GetOccurrences(from).TakeWhileBefore(to)` for each event. Ical.Net resolves `TZID` values from the TZ Database (through NodaTime), not from the calendar's `VTIMEZONE` components, so `Expand_TzData` is the closer comparison. |

Ical.Net has no asynchronous API, so the `*_Async` benchmarks have no Ical.Net counterpart.

Each read benchmark returns the number of `VEVENT` components read, each write benchmark the number of bytes written and each expansion benchmark the number of occurrences, so the results of each class can be checked against one another. `ExpandBenchmarks` also counts every event's occurrences with each method in `[GlobalSetup]`, and writes the totals, and any event whose counts differ, to the benchmark log.

Every class uses `[MemoryDiagnoser]`. Allocation is the main thing to watch. The allocation of `Read_View` should not grow with the size of the data: it is the fixed cost of the reader's `StreamReader` and buffers.

## Reporting results

When a performance change is proposed, paste the before and after results into the issue or pull request:

1. The BenchmarkDotNet markdown table, from `BenchmarkDotNet.Artifacts/results/*-report-github.md`.
2. The commit hash that was measured (`git rev-parse --short HEAD`).
3. The machine summary BenchmarkDotNet prints above the table (OS, CPU, .NET SDK and runtime).

Run "before" and "after" on the same machine, one straight after the other, so the numbers can be compared.
