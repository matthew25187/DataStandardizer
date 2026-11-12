---
title: Configure a reader or writer
parent: File.CSV
grand_parent: Packages
nav_order: 2
---

# Configure a reader or writer

Pass a `CsvFileOptions` instance to a reader or writer to adjust how it parses
and produces CSV, while staying RFC 4180–compliant by default.

Various options can be specified to configure the behaviour of a reader or
writer. If you don't specify any custom configuration options, default options
will be used that are compatible with the requirements of RFC 4180.

## Bad value handler

**Default:** `null`

A handler can be specified that will be called when a bad value is detected in a
CSV field. The handler is an instance of the `CsvFieldBadValue` delegate.

```csharp
void HandleBadValues(CsvFieldContext<CsvFileRecordLine> ctx)
{
    // do something with the bad value
}

var options = new CsvFileOptions { BadValueHandler = new CsvFieldBadValue<CsvFileRecordLine>(HandleBadValues) };
```

If a handler is not specified, the reader or writer will throw a
`CsvFileException` when a bad value is encountered.

When a reader finds a field that breaks the quoting rules of RFC 4180 section 2,
such as an unquoted field that holds a quote, or text after a closing quote, it
calls the handler once for each violation it finds, so one field can lead to
several calls. Each call's `RawFieldValue` holds the field's text read up to that
violation, with quotes removed, not the whole field: for `"a"bc` the handler is
called with `ab` and then `abc`. Once the handler returns, the reader uses an
empty value for that field, wherever it is in the record. A header field gets an
empty name, and a mapped property bound to the field gets what it would get from
an empty field. If the property's converter rejects an empty value, that is a
second bad value, and the handler is called again for the field, with an empty
`RawFieldValue`.

## Culture

**Default:** `null`

A custom culture can be used to serialize and deserialize CSV field values as
they are read or written.

```csharp
var options = new CsvFileOptions { Culture = new CultureInfo("en-AU") };
```

## Embedded line break

**Default:** `null`

Line breaks embedded in string field values can be normalised.

```csharp
var options = new CsvFileOptions { EmbeddedLineBreak = "\n" };
```

If this option is used, string field values will have whatever line breaks were
embedded in the value replaced by the value of this option.

## Encoding

**Default:** `null`

The encoding of the CSV file can be specified. It applies when the reader or writer is
constructed from a `Stream` or a file path, since the library then creates the
underlying `StreamReader` or `StreamWriter` itself. Passing an `Encoding` together with
an existing `TextReader` or `TextWriter` throws `ArgumentException`; set the encoding
when creating that reader or writer instead.

```csharp
var options = new CsvFileOptions { Encoding = Encoding.UTF8 };
```

## Field delimiter character

**Default:** `,`

The character used to separate fields on a line can be changed.

```csharp
var options = new CsvFileOptions { FieldDelimiterCharacter = ';' };
```

## Header handler

**Default:** `null`

You can intercept the processing of a header line to specify your own list of
field names.

```csharp
IReadOnlyList<string> HandleHeaderLine(CsvFileHeaderLine? line)
{
    // return replacement field names
}

var options = new CsvFileOptions { HeaderHandler = new CsvFileHeader(HandleHeaderLine) };
```

If this handler is specified and the file has a header line, this handler will
be called one time and passed the header line so a custom list of field names
can be returned. The list returned from this handler will override the field
names obtained from the header line.

## Header handling

**Default:** `CsvFileHeaderHandling.None`

How the header line is handled can be configured.

```csharp
var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
```

The available options include having no header, having a header, or ignoring the
header if it does exist. The ignore option means you can skip reading the header
line in the file and instead use a header handler to specify an override list of
field names.

## Inconsistent field count handler

**Default:** `null`

A handler can be specified that will be called when there is an inconsistent
number of fields across multiple lines. The line it is called for is skipped: a
reader doesn't return it, and a writer writes nothing for it. Without a handler, an
inconsistent field count throws `CsvFileException`.

```csharp
void HandleInconsistentFieldCount(CsvFieldContext<CsvFileRecordLine> ctx)
{
    // handle bad field count
}

var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<CsvFileRecordLine>(HandleInconsistentFieldCount) };
```

## Quote handling

**Default:** `CsvFieldQuoteHandling.Required`

The method for handling the quoting of field values can be specified.

```csharp
var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Always };
```

Options include the ability to quote all field values (`Always`), quote only
string (non-numeric) values (`Auto`), or quote only values required to be quoted
by RFC 4180 (`Required`).

## Suppress trailing blank fields

**Default:** `false`

You can suppress the output of trailing empty fields on lines written to a CSV
file. A line written this way can have fewer fields than the lines around it,
which RFC 4180 section 2, rule 4, says a file should not have, so the option is
off by default.

A field is blank when the text written for it is empty; whitespace is not blank.
Trailing fields are the last ones in the order the line is written: header line
order, else mapper order, else the order of the first record line. A record line
whose fields are all blank is still written with all of them.

```csharp
var options = new CsvFileOptions { SuppressTrailingBlankFields = true };
```

## Terminator line break

**Default:** `"\r\n"`

The line break used to delimit lines in the CSV file can be changed.

```csharp
var options = new CsvFileOptions { TerminatorLineBreak = "\n" };
```

This setting applies both to how the reader detects the ends of lines and how
lines are terminated when written to a CSV file by a writer.
