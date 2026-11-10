# Fuzz harnesses

[SharpFuzz](https://github.com/Metalnem/sharpfuzz) harnesses for the parsers that read untrusted input. Unit tests check documented behaviour. These search for inputs that make a parser hang, overflow the stack, allocate without bound or throw an exception it doesn't document, any of which is a denial of service for an application that parses files or headers from outside.

| Project | Harness | Entry point | Seeds |
|---|---|---|---|
| `DataStandardizer.File.CSV.Fuzz` | (only one) | `CsvFileReader`, with each kind of header handling | `Corpus` |
| `DataStandardizer.File.iCal.Fuzz` | `ical` | `IcalReader` | `Corpus/ical` |
| | `content-line` | `IcalContentLineReader` | `Corpus/content-line` |
| | `jcal` | `JcalReader` | `Corpus/jcal` |
| | `xcal` | `XcalReader` | `Corpus/xcal` |
| `DataStandardizer.File.InternetMessage.Fuzz` | `message` | `ImfReader`, then the message's typed fields, `Validate` and the decoded content of each entity | `Corpus/message` |
| | `entity` | `ImfEntityReader`, through multipart and `message/rfc822` nesting | `Corpus/entity` |
| | `mbox` | `MboxReader`, with each variant, as messages and as entity events | `Corpus/mbox` |
| | `emlx` | `EmlxReader`, with a part resolver | `Corpus/emlx` |
| | `mhtml` | `MhtmlReader`, with and without a base URI | `Corpus/mhtml` |
| | `fields` | The typed field parsers, `ImfHeaderField.FromRaw`, `MimeMediaType.TryParse` with RFC 2231 parameters, and the RFC 2047 encoded-word decoder | `Corpus/fields` |
| `DataStandardizer.File.vCard.Fuzz` | `vcard` | `VcardReader`, for vCard 4.0, 3.0 and 2.1, then `Validate`, the lookups by name and every typed member of each card it reads | `Corpus/vcard` |
| | `content-line` | `VcardContentLineReader`, and each part of every line it reads, from the view and from its copy | `Corpus/content-line` |
| | `jcard` | `JcardReader`, then `Validate`, the lookups by name and every typed member of each card it reads | `Corpus/jcard` |
| | `xcard` | `XcardReader`, then `Validate`, the lookups by name and every typed member of each card it reads | `Corpus/xcard` |
| `DataStandardizer.LanguageTag.Fuzz` | (only one) | `Bcp47LanguageTag.TryParse`, string and span overloads | `Corpus` |

Each harness treats only the exceptions that the API documents as expected. Anything else, a hang or a crash, is a finding. The seeds come from the test data of the same package.

The `DataStandardizer.File.InternetMessage.Fuzz` harnesses read each input under the default options and again with every accommodation in `ImfOptions` turned on, so that they also reach the code that runs past a problem the defaults reject. The `DataStandardizer.File.vCard.Fuzz` harnesses do the same with every accommodation in `VcardOptions` that a reader honours, and a third time with only the conformance accommodations, since the lexical ones change what reaches them.

These projects aren't packed, and `fuzz/` is outside `src/` and `tests/`, so a change here never versions or publishes a package. CI builds them when it builds their package, through its `.slnf`, but never runs them. It doesn't build a package for a change that is only in `fuzz/`, so build the project locally before opening a PR for one.

## Findings

**Run the harnesses locally only, never in CI.** The repository is public, and so are CI logs and artifacts. A crashing input is a vulnerability until it is fixed.

- Report each finding in a [private security advisory](https://github.com/matthew25187/DataStandardizer/security/advisories), never in a public issue, PR, commit message or comment.
- Commit a crashing input only in the PR that fixes it, as a regression test. `fuzz/.gitignore` ignores the `work` folder and libFuzzer's artifact names so that one isn't committed by accident.

## Setting up

You need the .NET SDK that `global.json` pins and the `libfuzzer-dotnet` driver for your platform, from [its releases](https://github.com/Metalnem/libfuzzer-dotnet/releases/tag/v2025.05.02.0904). The release publishes no checksums. These are the SHA-256 hashes of the files these instructions were written against:

| File | SHA-256 |
|---|---|
| `libfuzzer-dotnet-windows.exe` | `17af5b3f6ff4d2c57b44b9a35c13051b570eb66f0557d00015df3832709050bf` |
| `libfuzzer-dotnet-ubuntu` | `c2c2a90d94c409a4af339a0d4f244e0442c5a5d249be0f1252ba07871f285958` |

Keep the driver outside the repository. On Linux, make it executable with `chmod +x`.

From this folder, restore the `sharpfuzz` tool, which is pinned in `.config/dotnet-tools.json`:

```
dotnet tool restore
```

## Building and instrumenting

Publish the project to its own folder, then instrument the library assemblies in that folder, so that libFuzzer gets coverage from them. Publish again each time, because an assembly can't be instrumented twice, and because the libraries in `src/` must stay uninstrumented.

On Windows, in PowerShell:

```powershell
$project = 'DataStandardizer.File.iCal.Fuzz'

Remove-Item -Recurse -Force "$project/bin/fuzz" -ErrorAction Ignore
dotnet publish "$project/$project.csproj" -c Release -o "$project/bin/fuzz"
Get-ChildItem "$project/bin/fuzz/DataStandardizer.*.dll" -Exclude "$project.dll" | ForEach-Object { dotnet sharpfuzz $_.FullName }
```

On Linux:

```bash
project=DataStandardizer.File.iCal.Fuzz

rm -rf "$project/bin/fuzz"
dotnet publish "$project/$project.csproj" -c Release -o "$project/bin/fuzz"
for assembly in "$project"/bin/fuzz/DataStandardizer.*.dll; do
    [ "$(basename "$assembly")" = "$project.dll" ] || dotnet sharpfuzz "$assembly"
done
```

## Running

New corpus entries and findings go in `work/`, so the committed seeds stay as they are. The first folder libFuzzer is given is the one it writes to.

libFuzzer's `-rss_limit_mb` measures the driver, not the .NET process that runs the harness. `DOTNET_GCHeapHardLimit` limits the harness instead, so that unbounded allocation ends in an `OutOfMemoryException`, which counts as a crash. The value below is 512 MB, in hexadecimal. `-timeout` is in seconds, and an input that takes longer counts as a hang. The driver accepts inputs of up to 1 MiB.

For a project with several harnesses, pass the harness's name with `--target_arg`. For a project with one harness, leave that out, and use `Corpus` as the seed folder. Here is the `xcal` harness on Windows, in PowerShell:

```powershell
$project = 'DataStandardizer.File.iCal.Fuzz'
$harness = 'xcal'

New-Item -ItemType Directory -Force "work/$project/$harness/corpus", "work/$project/$harness/findings"
$env:DOTNET_GCHeapHardLimit = '0x20000000'
path/to/libfuzzer-dotnet-windows.exe --target_path="$project/bin/fuzz/$project.exe" --target_arg=$harness -timeout=10 -max_len=65536 -artifact_prefix="work/$project/$harness/findings/" "work/$project/$harness/corpus" "$project/Corpus/$harness"
```

The Windows driver joins the path and the argument into one command line, so the path to the harness can't contain a space.

On Linux:

```bash
project=DataStandardizer.File.iCal.Fuzz
harness=xcal

mkdir -p "work/$project/$harness/corpus" "work/$project/$harness/findings"
DOTNET_GCHeapHardLimit=0x20000000 path/to/libfuzzer-dotnet-ubuntu --target_path="$project/bin/fuzz/$project" --target_arg=$harness -timeout=10 -max_len=65536 -artifact_prefix="work/$project/$harness/findings/" "work/$project/$harness/corpus" "$project/Corpus/$harness"
```

libFuzzer stops at the first finding. To limit a run, add `-max_total_time=<seconds>`.

## Reproducing a finding

Run the project on the input outside libFuzzer to get the stack trace. It doesn't need to be instrumented. Give the path to the input, then the harness's name if the project has several:

```
dotnet run -c Release --project DataStandardizer.File.iCal.Fuzz -- work/DataStandardizer.File.iCal.Fuzz/xcal/findings/crash-<hash> xcal
```

## Adding a harness

Add a project for each new parser package, with the layout above, and list it in `DataStandardizer.sln` and in the package's `.slnf`. Read the entry point's XML docs to find which exceptions to expect, and catch only those. Take the seeds from the package's test data.
