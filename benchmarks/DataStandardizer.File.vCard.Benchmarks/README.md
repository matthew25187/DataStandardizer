# DataStandardizer.File.vCard benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) benchmarks for the `DataStandardizer.File.vCard` readers and writers, version conversion and LANGUAGE-TAG values, with [FolkerKinzel.VCards](https://github.com/FolkerKinzel/VCards) as the comparison. FolkerKinzel.VCards is maintained, MIT-licensed and reads and writes vCard 2.1, 3.0 and 4.0 with the extensions of RFC 9554, so the comparison is like for like.

This project is not packed and is not part of the CI test run.

## Running

Benchmarks must be run in Release, from this folder:

```
dotnet run -c Release -- --filter "*"
```

Run a subset by filtering on class or method name, for example:

```
dotnet run -c Release -- --filter "*ReadBenchmarks*"
dotnet run -c Release -- --filter "*Read_View*"
dotnet run -c Release -- --filter "*.Version21Benchmarks.*" "*.ConvertVersionBenchmarks.*"
```

Filters match the full name, including the namespace, so `*vCard*` selects every benchmark. `*Read_View*` also selects `Read_View_Async` and `Read_View_Decoded`.

To check that every benchmark runs without waiting for a full measurement, use the dry job:

```
dotnet run -c Release -- --filter "*" --job Dry
```

Results are written to `BenchmarkDotNet.Artifacts/results/` (ignored by git).

## Data

Each benchmark class generates its input in memory in `[GlobalSetup]` from a fixed seed, so every run uses identical data. There are two corpora of 5,000 cards each:

- The vCard 4.0 corpus, used by every class but `Version21Benchmarks`: about 4.9 MB of UTF-8 and about 113,500 content lines.
  - Every card has `FN`, `N`, `KIND`, `ORG`, `TITLE`, a `TEL` with `TYPE` and `PREF`, an `EMAIL`, an `ADR` with a `LABEL` parameter in RFC 6868 caret encoding, `URL`, `NOTE`, `LANG`, `TZ`, `GEO`, `CATEGORIES`, `UID`, `REV` and an `X-` property. Most have a `BDAY`, some a second `TEL`, a second `LANG`, a `NICKNAME`, or an `EMAIL` and an `X-ABLABEL` in a group, and 1 in 10 has a `PHOTO` as a `data:` URI of about 1.6 KB.
  - Text values contain escaped commas, semicolons, backslashes and line breaks, and many non-ASCII characters. Most notes and addresses are long enough to be folded, at 75 octets.
  - Every card is valid by RFC 6350, so it is read and written with the default options.
- The vCard 2.1 corpus, used by `Version21Benchmarks`: about 5.1 MB and about 83,900 content lines, about 26,800 of them in quoted-printable. It has the forms that phones export: parameters with no name (`TEL;WORK;VOICE;PREF:`), `ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8` with soft line breaks on every `NOTE` and `LABEL` and on about half of the `N`, `FN`, `ORG`, `TITLE` and `ADR` values, and, on 1 in 10 cards, a `PHOTO` in `BASE64` on indented lines that end with an empty line. It has no `GEO`, since FolkerKinzel.VCards drops the vCard 2.1 form (`GEO:lat,lon`), and the readers would not then do the same work.

The format benchmarks write the vCard 4.0 corpus in each format in `[GlobalSetup]`: about 4.9 MB of `.vcf`, 6.8 MB of jCard and 13.6 MB of xCard.

Every benchmark reads from a `MemoryStream` and writes to a pre-sized `MemoryStream`, so disk I/O is excluded. The writing and conversion benchmarks work on cards and lines read in `[GlobalSetup]`, so only the writing or the conversion is measured.

## Scenarios

| Class | Benchmark | What it measures |
|-------|-----------|------------------|
| `ReadBenchmarks` | `Read_View` | `VcardContentLineReader.ReadContentLine()` and `CurrentLine`, comparing the name and value spans of each line. Nothing is allocated per line. |
| | `Read_ContentLines` | `VcardContentLineReader.ReadLine()`, materializing a `VcardContentLine` for each line. |
| | `Read_Model` | `VcardReader.ReadCards()`, building the `VcardCard` model and checking each card (baseline). |
| | `Read_View_Async` | As `Read_View`, with `ReadContentLineAsync()`. |
| | `Read_ContentLines_Async` | As `Read_ContentLines`, with `ReadLineAsync()`. |
| | `Read_Model_Async` | As `Read_Model`, with `ReadCardsAsync()`. |
| | `Read_FolkerKinzel` | FolkerKinzel.VCards `VcfReader.ReadToEnd()`, which reads one card at a time, as `VcardReader` does. |
| | `Read_FolkerKinzel_Async` | FolkerKinzel.VCards `Vcf.DeserializeAsync(...)`, which also resolves the references between the cards. |
| `Version21Benchmarks` | `Read_View` | As `ReadBenchmarks.Read_View`, over the vCard 2.1 corpus. Soft line breaks and `BASE64` lines are joined, and nothing is decoded. |
| | `Read_View_Decoded` | As `Read_View`, also decoding the quoted-printable and `CHARSET` of every `NOTE` with `GetDecodedText()`. |
| | `Read_Model` | `VcardReader.ReadCards()`, which checks each card by the rules of vCard 2.1 and maps it to the canonical form of the model (baseline). |
| | `Read_FolkerKinzel` | FolkerKinzel.VCards `VcfReader.ReadToEnd()`. |
| | `Upgrade_Model` | `VcardReader.ReadCards()` into `VcardWriter.WriteCards(...)`, writing each card as vCard 4.0. |
| | `Upgrade_FolkerKinzel` | FolkerKinzel.VCards `VcfReader.ReadToEnd()` into `Vcf.Serialize(...)` with `VCdVersion.V4_0`. |
| `WriteBenchmarks` | `Write_ContentLines` | `VcardContentLineWriter.WriteLines(...)` with the corpus's pre-read content lines. |
| | `Write_Model` | `VcardWriter.WriteCards(...)` with the pre-read cards, checking each card before it is written (baseline). |
| | `Write_ContentLines_Async` | As `Write_ContentLines`, with `WriteLinesAsync(...)`. |
| | `Write_Model_Async` | As `Write_Model`, with `WriteCardsAsync(...)`. |
| | `Write_FolkerKinzel` | FolkerKinzel.VCards `Vcf.Serialize(...)` with `VCdVersion.V4_0` and the pre-loaded cards. |
| `ConvertVersionBenchmarks` | `Write_Model` | `VcardWriter.WriteCards(...)` with `WriteVersion` set to `Version`, vCard 3.0 or 2.1, and `AllowLossyVersionConversion`, since the corpus has data neither version can hold, such as `KIND` and `LANG` (baseline). |
| | `Convert_Model` | `VcardVersionConverter.Convert(...)` for each card, which makes a copy with only what `Version` can hold, and writes nothing. |
| | `Write_FolkerKinzel` | FolkerKinzel.VCards `Vcf.Serialize(...)` with `VCdVersion.V3_0` or `VCdVersion.V2_1`. |
| `ConvertFormatBenchmarks` | `Convert` | Reads the corpus in `Source` with `VcardReader`, `JcardReader` or `XcardReader`, and writes each card in `Target` with `VcardWriter`, `JcardWriter` or `XcardWriter`. The cases where `Source` and `Target` are the same are a round trip. |
| `LanguageTagBenchmarks` | `Parse_VcardValue` | `VcardValue.TryParseLanguageTag(...)` for each of 1,000 LANGUAGE-TAG values, 20 of them distinct (baseline). |
| | `Parse_Bcp47LanguageTag` | `Bcp47LanguageTag.TryParse(...)` for the same values, so the cost of the vCard layer shows apart from that of the tag. |
| | `Format_VcardValue` | `VcardValue.TryFormatLanguageTag(...)` for each parsed value, into a buffer. |

FolkerKinzel.VCards has no asynchronous writer, and reads and writes neither jCard nor xCard, so `Write_*_Async` and `ConvertFormatBenchmarks` have no FolkerKinzel.VCards counterpart. Its write benchmarks use `VcfOpts.Default` without `UpdateTimeStamp`, so the cards are written as they were read, and with `WriteNonStandardProperties` and `WriteNonStandardParameters`, which `VcardWriter` always writes.

The libraries don't do the same work. `VcardReader` checks every card against RFC 6350 and its extensions, and `VcardWriter` checks every card before it writes it; a card that breaks a rule throws. FolkerKinzel.VCards doesn't make those checks. So the comparison includes the cost of the checks as well as that of the parsing and writing.

Each read benchmark returns the number of cards read, each write and conversion benchmark the number of bytes written, and `Convert_Model` the number of cards converted, so the results of each class can be checked against one another.

Every class uses `[MemoryDiagnoser]`. Allocation is the main thing to watch. The allocation of `Read_View` should not grow with the size of the data: it is the fixed cost of the reader's `StreamReader` and buffers.

## Reporting results

When a performance change is proposed, paste the before and after results into the issue or pull request:

1. The BenchmarkDotNet markdown table, from `BenchmarkDotNet.Artifacts/results/*-report-github.md`.
2. The commit hash that was measured (`git rev-parse --short HEAD`).
3. The .NET SDK and runtime versions. Leave out the rest of the summary BenchmarkDotNet prints above the table (the OS and CPU), as these reports are public and it describes your own machine.

Run "before" and "after" on the same machine, one straight after the other, so the numbers can be compared.
