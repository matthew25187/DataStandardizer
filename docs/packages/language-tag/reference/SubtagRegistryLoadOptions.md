---
title: SubtagRegistryLoadOptions Enum
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryLoadOptions Enum

## Definition

Namespace: `DataStandardizer.LanguageTag`

Line breaks that the [SubtagRegistry](SubtagRegistry.md) `CreateFrom…` methods
accept besides the CRLF that the record-jar format of RFC 5646 §3.1.1 requires.

```csharp
[Flags]
public enum SubtagRegistryLoadOptions
```

## Remarks

RFC 5646 §3.1.1 ends every line of the registry, the last one included, with
CRLF. By default, the `CreateFrom…` methods follow it: a line that ends with a
line feed alone, or a last line with no line break, throws `FormatException`,
naming the line.

IANA publishes the registry, at
<https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry>,
with a line feed alone at the end of each line. To load that file, pass
`AcceptBareLineFeed`:

```csharp
var registry = SubtagRegistry.CreateFromFile("language-subtag-registry.txt", SubtagRegistryLoadOptions.AcceptBareLineFeed);
```

Each option accepts only what it names, so combine them with `|` to accept
both. A carriage return that no line feed follows is never a line break: it is
a control character, which a line may not hold, whatever the options.

## Fields

| Field | Value | Description |
| --- | --- | --- |
| `None` | `0` | Read strictly, with CRLF at the end of every line. |
| `AcceptBareLineFeed` | `1 << 0` | Accept a line feed alone at the end of a line, as well as CRLF. Loads the registry as IANA publishes it. |
| `AcceptMissingFinalLineBreak` | `1 << 1` | Accept a last line with no line break after it. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`.

## See also

- [SubtagRegistry](SubtagRegistry.md)
- [LanguageTag API reference](index.md)
