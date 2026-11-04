# DataStandardizer.File.InternetMessage benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) benchmarks for the `DataStandardizer.File.InternetMessage` readers and writers, with [MimeKit](https://github.com/jstedfast/MimeKit) as the comparison.

This project is not packed and is not part of the CI test run.

## Running

Benchmarks must be run in Release, from this folder:

```
dotnet run -c Release -- --filter *
```

Run a subset by filtering on class or method name, for example:

```
dotnet run -c Release -- --filter *MboxBenchmarks*
dotnet run -c Release -- --filter *Stream_Events*
```

Filters match the full name, including the namespace, so `*InternetMessage*` selects every benchmark. `*Stream_Events*` also selects `Stream_Events_Decoded`.

To check that every benchmark runs without waiting for a full measurement, use the dry job:

```
dotnet run -c Release -- --filter * --job Dry
```

Results are written to `BenchmarkDotNet.Artifacts/results/` (ignored by git).

## Data

Each benchmark class generates its input in memory in `[GlobalSetup]` from a fixed seed, so every run uses identical data. The bytes are written directly, rather than with either library, and they follow the standards, so the strict defaults of this package read them with no options.

- `HeaderBenchmarks`: a message as a mail server delivers it, with 41 fields in about 4 KB, many of them folded: a `Return-Path`, eight `Received`, a `DKIM-Signature`, the usual origination, destination and identification fields, some with encoded words, and 18 extension fields.
- `ReadBenchmarks` and `WriteBenchmarks`: a message of about 2.3 MB. A `multipart/mixed` body holds a `multipart/alternative` entity, with text and HTML, and three base64 attachments of random bytes, of 100 KiB, 500 KiB and 1 MiB.
- `StreamBenchmarks`: a message of the same shape, of about 69 MB, with three attachments of 16 MiB each.
- `MboxBenchmarks`: an mbox file of about 74 MB in the mboxrd variant, with 10,000 messages, LF line breaks and only seven-bit characters, as RFC 4155 Appendix A requires. Most are plain text of a few KB, some with a line that has an mboxrd quote, and 1 in 10 has a base64 attachment of 32 KiB.

Every benchmark reads from a `MemoryStream` or writes to a pre-sized one, so disk I/O is excluded.

## Scenarios

| Class | Benchmark | What it measures |
|-------|-----------|------------------|
| `HeaderBenchmarks` | `Header_View` | `ImfHeaderReader.ReadField()`, looking at the name and value spans of each field. Nothing is allocated per field (baseline). |
| | `Header_Strings` | As `Header_View`, getting the name and the unfolded body of each field as strings. |
| | `Header_MimeKit` | `MimeParser.ParseHeaders()`, which makes a `Header` for each field, and leaves its value undecoded. |
| | `Header_MimeKit_Strings` | As `Header_MimeKit`, getting the name and the `Value` of each field, which MimeKit also decodes. |
| `ReadBenchmarks` | `Read_Parts` | `ImfReader.ReadMessage()` and `Attachments`, which finds the parts without decoding them (baseline). |
| | `Read_MimeKit_Parts` | `MimeMessage.Load(Stream)` and `Attachments`. |
| | `Read_Decoded` | As `Read_Parts`, with `GetContent()` for each attachment, decoding it to memory. |
| | `Read_MimeKit_Decoded` | As `Read_MimeKit_Parts`, with `DecodeTo(...)` for each attachment, to a `MeasuringStream` that holds nothing. |
| `StreamBenchmarks` | `Stream_Events` | `ImfEntityReader.ReadEvent()` to the end of the message, with no decoding (baseline). |
| | `Stream_MimeKit_Persistent` | `MimeMessage.Load(Stream, persistent: true)` and `BodyParts`, which keeps each body as a range of the stream, and needs one that can seek. |
| | `Stream_Events_Decoded` | As `Stream_Events`, with `DecodeTransferEncoding` set, decoding each attachment as it goes past. |
| | `Stream_MimeKit_Persistent_Decoded` | As `Stream_MimeKit_Persistent`, with `DecodeTo(...)` for each attachment, to a `MeasuringStream`. |
| | `Stream_MimeKit_InMemory_Decoded` | As `Stream_MimeKit_Persistent_Decoded`, with the default parse, which copies each body. This holds the whole message. |
| `WriteBenchmarks` | `Write_New` | `ImfWriter.WriteMessage(...)` with a message from `ImfMessageBuilder`, which encodes the attachments (baseline). |
| | `Write_New_Entities` | `ImfEntityWriter.WriteMessage(...)` with the same message as a tree of `ImfEntitySource` objects. |
| | `Write_New_MimeKit` | `MimeMessage.WriteTo(Stream)` with a message from `BodyBuilder`. |
| | `Write_Read` | `ImfWriter.WriteMessage(...)` with a message that was read and hasn't changed, which is copied. |
| | `Write_Read_MimeKit` | `MimeMessage.WriteTo(Stream)` with a message that was read and hasn't changed. |
| `MboxBenchmarks` | `Mbox_Messages` | `MboxReader.ReadMessage()`, with the `Subject` of each (baseline). |
| | `Mbox_Events` | `MboxReader.ReadEntityEvents()`, going through every event of each message. |
| | `Mbox_MimeKit` | `MimeParser` with `MimeFormat.Mbox` and `ParseMessage()`, with the `Subject` of each. |
| | `Mbox_MimeKit_Persistent` | As `Mbox_MimeKit`, with `persistent: true`. |

This package checks what it reads against the standards by default, including the bytes of every body as it goes past, and that isn't work that MimeKit does. MimeKit's parser accepts what it can of malformed input. So these numbers compare what each library does in its default configuration, and not two implementations of the same work.

There are no `*_Async` benchmarks: MimeKit's asynchronous API isn't the subject of this comparison.

`[GlobalSetup]` in every class except `WriteBenchmarks` runs each benchmark once and checks its result, and throws if one doesn't give what the corpus holds, so that a library that reads the corpus differently isn't reported as being faster: the header benchmarks give 41 fields, the read benchmarks 3 attachments or 1,662,976 decoded bytes, the stream benchmarks 5 parts or 50,331,648 decoded bytes, and the mbox benchmarks 10,000 messages. The write benchmarks return the number of bytes written, which differ between the libraries, as each chooses its own boundaries and encodings, and nothing checks what they write.

Every class uses `[MemoryDiagnoser]`. The allocation of `Stream_Events` and `Stream_Events_Decoded` should not grow with the size of the message: it is the fixed cost of the reader and its buffers.

## Reporting results

When a performance change is proposed, paste the before and after results into the issue or pull request:

1. The BenchmarkDotNet markdown table, from `BenchmarkDotNet.Artifacts/results/*-report-github.md`.
2. The commit hash that was measured (`git rev-parse --short HEAD`).
3. The machine summary BenchmarkDotNet prints above the table (OS, CPU, .NET SDK and runtime).

Run "before" and "after" on the same machine, one straight after the other, so the numbers can be compared.
