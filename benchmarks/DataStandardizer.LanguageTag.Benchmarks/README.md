# DataStandardizer.LanguageTag benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) benchmarks for `DataStandardizer.LanguageTag`: creating and validating BCP 47 language tags, reading their subtags, the builder, and loading the IANA Language Subtag Registry.

This project is not packed and is not part of the CI test run.

## Running

Benchmarks must be run in Release, from this folder. The project targets `net8.0` and `net10.0`, because the package's builds for those targets can differ, so `dotnet run` needs a framework. Run from the `net10.0` build and ask BenchmarkDotNet for both runtimes, so each benchmark is measured on each:

```
dotnet run -c Release -f net10.0 -- --filter * --runtimes net8.0 net10.0
```

Leave out `--runtimes` to measure only the runtime of the framework given with `-f`.

Run a subset by filtering on class or method name, for example:

```
dotnet run -c Release -f net10.0 -- --filter *CreateBenchmarks* --runtimes net8.0 net10.0
dotnet run -c Release -f net10.0 -- --filter *Create_Registry* --runtimes net8.0 net10.0
dotnet run -c Release -f net10.0 -- --filter *.CheckBenchmarks.* *.PropertyBenchmarks.* --runtimes net8.0 net10.0
```

Filters match the full name, including the namespace, so `*LanguageTag*` selects every benchmark. `*Create*` also selects `TryCreate_Invalid`, the `TryCreate_Span` benchmarks and `Registry_CreateFromStream`.

To check that every benchmark runs without waiting for a full measurement, use the dry job:

```
dotnet run -c Release -f net10.0 -- --filter * --job Dry --runtimes net8.0 net10.0
```

Results are written to `BenchmarkDotNet.Artifacts/results/` (ignored by git). BenchmarkDotNet exits with code 0 even when a benchmark fails, so check the "Benchmarks with issues" list at the end of the output, and look for `NA` in the tables.

## Data

The inputs are fixed, so every run uses identical data. They are in `BenchmarkData`:

- 12 valid tags that between them use every kind of subtag: `en`, `es-419`, `zh-Hant-TW`, `zh-yue-HK`, `sr-Latn-RS`, `de-CH-1901`, `sl-rozaj-biske-1994`, `hy-Latn-IT-arevela`, `gsw-u-sd-chzh`, `he-IL-u-ca-hebrew-tz-jeruslm`, `en-US-x-twain` and `x-private`. Each is also valid against the registry.
- 12 strings that are not well-formed, failing in different places: the empty string, a one-letter or nine-letter primary language, leading, trailing and doubled hyphens, two regions, two scripts, a variant that is too long, an extension singleton with no subtag, an empty private-use subtag and a private-use subtag that is too long.
- The IANA Language Subtag Registry, the 777 KB fixture shared with the tests (`tests/DataStandardizer.LanguageTag.Tests/Resources/IanaSubtagRegistry.txt`), embedded in this assembly. It is read from a `MemoryStream`, so disk I/O is excluded.

`CreateBenchmarks`, `CheckBenchmarks` and `BuilderBenchmarks` check their inputs in `[GlobalSetup]`, and fail if a change to the package makes a valid input invalid or an invalid one valid, so that the numbers are not compared across a change in what is measured.

## Scenarios

| Class | Benchmark | What it measures |
|-------|-----------|------------------|
| `CreateBenchmarks` | `Create_Rules` | `Bcp47LanguageTag.Create(tag)` over the 12 valid tags (baseline). |
| | `Create_Registry` | `Bcp47LanguageTag.Create(tag, registry)` over the same tags, with the registry loaded in `[GlobalSetup]`. |
| | `TryCreate_Invalid` | `Bcp47LanguageTag.TryCreate(tag, out _)` over the 12 invalid strings. |
| | `IsWellFormed` | `Bcp47LanguageTag.IsWellFormedLanguageTagString(tag)` over the 12 valid tags. |
| | `TryCreate_Span` | `Bcp47LanguageTag.TryCreate(tag.AsSpan(), out _)` over the 12 valid tags. |
| | `TryCreate_Span_Invalid` | `Bcp47LanguageTag.TryCreate(tag.AsSpan(), out _)` over the 12 invalid strings. Should allocate nothing. |
| | `IsWellFormed_Span` | `Bcp47LanguageTag.IsWellFormedLanguageTagString(tag.AsSpan())` over the 12 valid tags. Should allocate nothing. |
| `CheckBenchmarks` | `Check_PrimaryLanguage` | `CheckPrimaryLanguageSubtag("zh")`. |
| | `Check_ExtendedLanguage` | `CheckExtendedLanguageSubtag("yue")`. |
| | `Check_Script` | `CheckScriptSubtag("Hant")`. |
| | `Check_Region` | `CheckRegionSubtag("TW")`. |
| | `Check_Variant` | `CheckVariantSubtag("1994")`. |
| | `Check_Extension` | `CheckExtensionSubtag("u-ca-hebrew-tz-jeruslm")`. |
| | `Check_PrivateUse` | `CheckPrivateUseSubtag("x-private")`. |
| `PropertyBenchmarks` | `Properties_All` | Reads all seven subtag properties of `zh-yue-Hant-HK-1994-u-nu-hanidec-x-private`, created in `[GlobalSetup]`. |
| `BuilderBenchmarks` | `Builder_Build_Enums` | `Bcp47LanguageTagBuilder` building `zh-Hant-TW` from `Iso639Part1Language`, `Iso15924Script` and `Iso3166Part1Alpha2Country` values (baseline). |
| | `Builder_Build_Strings` | The same tag built from strings. |
| `KeyedSubtagBenchmarks` | `KeyedSubtag_Singleton` | `Bcp47KeyedSubtag.Singleton` of `u-sd-chzh`, created in `[GlobalSetup]`. |
| | `KeyedSubtag_Subtags` | `Bcp47KeyedSubtag.Subtags` of the same subtag. |
| `RegistryBenchmarks` | `Registry_CreateFromStream` | `SubtagRegistry.CreateFromStream` over the registry fixture. |
| | `Registry_RecordProperties` | Reads `Type`, `Subtag` or `Tag`, `Added` and `Description` of every record of a registry loaded in `[GlobalSetup]`. |

Every class uses `[MemoryDiagnoser]`. Allocation is the main thing to watch.

## Cold start

BenchmarkDotNet warms up before it measures, so it never sees the one-off cost of the first call in a process, such as building the code lists behind validation or the index of a Subtag Registry. To measure that, run the project with `--coldstart`:

```
dotnet run -c Release -f net8.0 -- --coldstart
dotnet run -c Release -f net10.0 -- --coldstart
```

This runs no benchmarks. It times, with `Stopwatch`, in a fresh process:

1. The first `Bcp47LanguageTag.Create("en")`.
2. Loading the registry fixture, for context only (`RegistryBenchmarks` measures it properly).
3. The first `Bcp47LanguageTag.Create("en", registry)`, which builds the registry's index, and how much of that was JIT compilation (`JitInfo.GetCompilationTime`, on every thread).
4. The second `Bcp47LanguageTag.Create("en", registry)`, to show how much of the first was one-off.
5. The first `Bcp47LanguageTag.Create("en", registry)` against a second copy of the registry, after a forced garbage collection. The code is already compiled, so this is the cost of building an index alone, and the difference from step 3 is the one-off cost of the first registry in a process.

Each figure is a single sample, including JIT compilation, and varies from run to run. Build once, then run the process several times and report every run, for example in PowerShell:

```
dotnet build -c Release
1..5 | ForEach-Object { dotnet run -c Release -f net10.0 --no-build -- --coldstart }
```

## Reporting results

When a performance change is proposed, paste the before and after results into the issue or pull request:

1. The BenchmarkDotNet markdown table, from `BenchmarkDotNet.Artifacts/results/*-report-github.md`.
2. The cold-start output, from several runs on each target, if the change could affect start-up.
3. The commit hash that was measured (`git rev-parse --short HEAD`).
4. The machine summary BenchmarkDotNet prints above the table (OS, CPU, .NET SDK and runtimes).

Run "before" and "after" on the same machine, one straight after the other, so the numbers can be compared.
