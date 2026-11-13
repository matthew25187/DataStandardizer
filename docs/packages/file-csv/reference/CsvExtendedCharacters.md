---
title: CsvExtendedCharacters Enum
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvExtendedCharacters Enum

## Definition

Namespace: `DataStandardizer.File.CSV`

The kinds of character outside RFC 4180 `TEXTDATA` that a field value may hold. Set on
[CsvFileOptions.ExtendedCharacters](CsvFileOptions.md); applies to reading and writing.

**Syntax**

```csharp
[Flags]
public enum CsvExtendedCharacters
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `None` | 0 | Field values hold only the characters RFC 4180 section 2 allows. The default. |
| `Control` | 1 | Field values may hold control characters, such as TAB and NUL: `%x00-1F` and `%x7F`, and, with `NonAscii`, `%x80-9F`. Relaxes RFC 4180 section 2 (`TEXTDATA`). |
| `NonAscii` | 2 | Field values may hold characters outside US-ASCII, such as `é`, other than the control characters `%x80-9F`, which also need `Control`. Relaxes RFC 4180 section 2 (`TEXTDATA`). |

## Remarks

RFC 4180 section 2 builds both escaped and non-escaped fields from
`TEXTDATA = %x20-21 / %x23-2B / %x2D-7E`, the printable characters of US-ASCII, and lets
an escaped field also hold COMMA, CR, LF and a pair of double quotes. By default, a reader
reports any other character in a field as a bad value, through
[BadValueHandler](CsvFileOptions.md), or throws `CsvFileException` when no handler is set.
A writer throws `CsvFileException` for a value that holds one, and writes nothing for the
line.

The field delimiter takes the place of COMMA, and the characters of the terminator line
break take the place of CR and LF, so an escaped field may hold them whatever this option
is. With a delimiter other than COMMA, COMMA is ordinary text.

Section 3 of RFC 4180 lets a `text/csv` file declare another character set with its
`charset` parameter. `NonAscii` reads that as allowing the printable characters of that
character set, so a UTF-8 or ISO 8859-1 file with accented letters needs it.

`Control` doesn't allow CR or LF in a non-escaped field: RFC 4180 section 2 paragraph 6
still requires a field that holds a line break to be enclosed in double quotes.

```csharp
var options = new CsvFileOptions { ExtendedCharacters = CsvExtendedCharacters.NonAscii };
```

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Configure a reader or writer](../how-to/configure-csv.md)
- [CsvFileOptions](CsvFileOptions.md)
- [File.CSV API reference](index.md)
