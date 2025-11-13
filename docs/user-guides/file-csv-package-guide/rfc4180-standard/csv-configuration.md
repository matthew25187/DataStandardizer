# Configuring a reader or writer

Various options can be specified to configure the behaviour of a reader or writer.  If you don't specify any custom configuration options, default options will be used that are compatible with the requirements of RFC 4180.

## Bad value handler

**Default:** `null`

A handler can be specified that will be called when a bad value is detected in a CSV field.  The handler is an instance of the `CsvFieldBadValue` delegate.
```
void HandleBadValues(CsvFieldContext<CsvFileRecordLine> ctx)
{
    // do something with the bad value
}

var options = new CsvFileOptions { BadValueHandler = new CsvFieldBadValue<CsvFileRecordLine>(HandleBadValues) };
```

If a handler is not specified, the reader or writer will throw a `CsvFileException` when a bad value is encountered.

## Culture

**Default:** `null`

A custom culture can be used to serialize and deserialize CSV field values as they are read or written.
```
var options = new CsvFileOptions { Culture = new CultureInfo("en-AU") };
```

## Embedded line break

**Default:** `null`

Line breaks embedded in string field values can be normalised.
```
var options = new CsvFileOptions { EmbeddedLineBreak = "\n" };
```

If this option is used, string field values will have whatever line breaks were embedded in the value replaced by the value of this option.

## Encoding

**Default:** `null`

The encoding of the CSV file can be specified.
```
var options = new CsvFileOptions { Encoding = Encoding.UTF8 };
```

## Field delimiter character

**Default:** `,`

The character used to separate fields on a line can be changed.
```
var options = new CsvFileOptions { FieldDelimiterCharacter = ';' };
```

## Header handler

**Default:** `null`

You can intercept the processing of a header line to specify your own list of field names.
```
IReadOnlyList<string> HandleHeaderLine(CsvFileHeaderLine? line)
{
    // return replacement field names
}

var options = new CsvFileOptions { HeaderHandler = new CsvFileHeader(HandleHeaderLine) };
```

If this handler is specified and the file has a header line, this handler will be called one time and passed the header line so a custom list of field names can be returned.  The list returned from this handler will override the field names obtained from the header line.

## Header handling

**Default:** `CsvFileHeaderHandling.None`

How the header line is handled can be configured.
```
var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
```

The available options include having no header, having a header, or ignore the header if it does exist.  The ignore option means you can skip reading the header line in the file and instead use a header handler to specify an override list of field names.

## Inconsistent field count handler

**Default:** `null`

A handler can be specified that will be called when there is an inconsistent number of fields across multiple lines.
```
void HandleInconsistentFieldCount(CsvFieldContext<CsvFileRecordLine> ctx)
{
    // handle bad field count
}

var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<CsvFileRecordLine>(HandleInconsistentFieldCount) };
```

## Quote handling

**Default:** `CsvFieldQuoteHandling.Required`

The method for handling the quoting of field values can be specified.
```
var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Always };
```

Options include the ability to quote all field values, quote only string values, or quote only values required to be quoted by RFC 4180.

## Suppress trailing blank fields

**Default:** `false`

You can suppress the output of trailing empty fields on lines written to a CSV file.
```
var options = new CsvFileOptions { SuppressTrailingBlankFields = true };
```

## Terminator line break

**Default:** `"\r\n"`

The line break used to delimit lines in the CSV file can be changed.
```
var options = new CsvFileOptions { TerminatorLineBreak = "\n" };
```

This setting applies both to how the reader detects the ends of lines and how lines are terminated when written to a CSV file by a writer.