---
title: EmlxMetadata Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# EmlxMetadata Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The metadata of an EMLX file, from the XML property list that ends it.

**Syntax**

```csharp
public sealed class EmlxMetadata
```

## Remarks

Every key and value is kept, in order, whether or not this package knows it. Metadata that was read and hasn't changed is written back byte for byte. A change writes the whole property list again, as Apple's tools lay it out, with the keys that weren't changed still in order.

EMLX is proprietary and undocumented, so the well-known keys come from community documentation, and a key whose value isn't of the kind expected gives `null` from its property. The value itself is still in `Entries`. If a key is repeated, a lookup gives the last, as Apple's parser does, and all of them are kept.

## Constructors

| Constructor | Notes |
| --- | --- |
| `EmlxMetadata()` | Creates metadata with no entries. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `AttachmentCount` | `int? AttachmentCount { get; }` | Gets the number of attachments that the `flags` key holds, or `null` if it has none. This comes from community documentation. |
| `DateLastViewed` | `DateTimeOffset? DateLastViewed { get; set; }` | Gets or sets the date and time in the `date-last-viewed` key, in UTC. A value is read from a `date`, or from a number of seconds since 1970, and set as a whole number of seconds since 1970. Setting `null` removes the key. |
| `DateReceived` | `DateTimeOffset? DateReceived { get; set; }` | Gets or sets the date and time in the `date-received` key, in UTC. A value is read from a `date`, or from a number of seconds since 1970, and set as a whole number of seconds since 1970. Setting `null` removes the key. |
| `Entries` | `IReadOnlyList<KeyValuePair<string, EmlxPlistValue>> Entries { get; }` | Gets the keys and values, in order. The list is read-only. |
| `Flags` | `long? Flags { get; set; }` | Gets or sets the whole integer of the `flags` key. Setting `null` removes the key. |
| `IsWellFormed` | `bool IsWellFormed { get; }` | Gets a flag that is `false` if this came from a file whose property list was missing or not well formed, under [`ImfOptions.AllowInvalidEmlxMetadata`](ImfOptions.md). Any change makes it `true`, and replaces those bytes. |
| `MessageFlags` | `EmlxMessageFlags? MessageFlags { get; }` | Gets the single-bit flags of the `flags` key that are known, or `null` if it has none. These come from community documentation, as [`EmlxMessageFlags`](EmlxMessageFlags.md) describes. To change them, set `Flags`. |
| `PriorityLevel` | `int? PriorityLevel { get; }` | Gets the priority that the `flags` key holds, or `null` if it has none. This comes from community documentation. |
| `RawBytes` | `ReadOnlyMemory<byte> RawBytes { get; }` | Gets the bytes that were read, as they were, until a change is made. It is empty if nothing was read, or the file had no property list. |
| `RemoteId` | `string? RemoteId { get; set; }` | Gets or sets the text of the `remote-id` key, which is the identifier of the message on its server, from a `string` or an `integer`. Setting `null` removes the key. **Throws** `ArgumentException`: On set: the value holds a character that XML doesn't allow. |
| `Subject` | `string? Subject { get; set; }` | Gets or sets the text of the `subject` key. Setting `null` removes the key. **Throws** `ArgumentException`: On set: the value holds a character that XML doesn't allow. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Remove(string key)` | `bool` | Removes a key, and any repeats of it. **Returns:** `true` if the key was there. **Throws** `ArgumentNullException`: `key` is `null`. |
| `Set(string key, EmlxPlistValue value)` | `void` | Sets the value of a key. A key that is there keeps its place, and any repeats of it go. A new key goes at the end. **Throws** `ArgumentNullException`: `key` or `value` is `null`. **Throws** `ArgumentException`: `key` holds a character that XML doesn't allow. |
| `TryGetValue(string key, out EmlxPlistValue? value)` | `bool` | Gets the value of a key. **Returns:** `true` if the key is there. **Throws** `ArgumentNullException`: `key` is `null`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read Apple Mail EMLX files](../how-to/read-emlx.md)
- [EmlxMessageFlags](EmlxMessageFlags.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
