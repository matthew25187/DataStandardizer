---
title: Limits for hostile input
parent: File.InternetMessage
grand_parent: Packages
nav_order: 13
---

# Limits for hostile input

Email comes from anyone. The standards set almost no limits on the size or shape of a
message, so a crafted one could make a reader use all the memory it can get. The package
has limits of its own, which are on by default and sized so that ordinary mail doesn't
reach them. They aren't relaxations of a standard, or additions to one: a message within
them is treated as the standard says, and one beyond them is refused.

## The limits

| Option | Default | What it limits |
| --- | --- | --- |
| `MaxHeaderFieldCount` | 1,000 | The fields in one header section. |
| `MaxHeaderSectionLength` | 1 MiB | The length of one header section, counting its line breaks and the empty line that ends it. A reader holds a whole field in memory. |
| `MaxEntitySize` | 256 MiB | The body that an `ImfReader` holds in memory, and the content given to `ImfEntity.SetContent`. |
| `MaxNestingDepth` | 100 | How deep the entities of a message go, with the message itself at 0. It also limits how deeply the values of an EMLX property list nest. |
| `MaxPartCount` | 10,000 | The entities in a message at any depth, not counting the message itself. |
| `MaxMessageCount` | 1,000,000 | The messages an `MboxReader` reads from a file. The other limits apply to each message. |
| `MaxEmlxMetadataLength` | 1 MiB | The length of the property list of an EMLX file. |

The defaults are also public constants, such as `ImfOptions.DefaultMaxPartCount`.

Lower them to suit what you accept. A value below 1 makes the reader's constructor throw
`ArgumentException`:

```csharp
var options = new ImfOptions
{
    MaxEntitySize = 25 * 1024 * 1024,
    MaxPartCount = 500,
    MaxNestingDepth = 10
};

using var reader = new ImfReader(stream, options);
```

Passing a limit throws `ImfFileException`:

```text
The header section has more than the maximum of 1000 fields.
```

A field's line break, line length or limit being at fault ends reading, as where the
field ends is then unknown. Later reads throw `InvalidOperationException`.

## What memory each reader uses

The limits work with the layer you choose. See [The layers](layers.md).

- `ImfHeaderReader` holds one field at a time.
- `ImfEntityReader` holds one block of a body at a time, and `MaxEntitySize` doesn't
  apply to it, so a message of any size can go through. Whatever you do with the bytes
  is up to you: copying them to a file has its own limit to set. The fields of the
  header of a message are held until the header ends, up to `MaxHeaderSectionLength`.
- `ImfReader` holds the whole body in memory, up to `MaxEntitySize`, and parts share
  those bytes. Decoding a part can only make it shorter.
- `MboxReader.ReadMessage` holds one message at a time, under the limits of `ImfReader`.
  `ReadEntityEvents` holds what `ImfEntityReader` holds.
- `EmlxReader` holds the whole file while it reads it, up to the sum of `MaxEntitySize`,
  `MaxHeaderSectionLength` and `MaxEmlxMetadataLength`, and the message then takes as
  much again.

`AllowLongLines` removes the 998-octet limit on a line of a header. A line is then
limited only by `MaxHeaderSectionLength`.

## What the limits don't cover

Some of what a message says can't be checked, and it comes from whoever sent the message:

- **File names.** `ImfEntity.FileName` and `ImfContentDisposition.FileName` give the
  name as the message has it, which may be `../../evil.exe`. RFC 2183 §2.3 says not to
  respect any directory path in it, and §5 warns against a name that overwrites a file
  or gets a program run. Use `Path.GetFileName` at the least, and choose where it goes.
- **URLs.** A Content-Location field labels a part. It doesn't say where the part came
  from, which a sender can misrepresent (RFC 2557 §11.1). Don't treat the content of a
  saved web page as if it had come from that URL.
- **Addresses.** A From field says what the sender wrote. The package doesn't verify it.
- **Active content.** HTML from a message may hold scripts and remote references. The
  package gives it to you as it is, and doesn't sanitize it.
- **Size of what you make.** `GetText` and `GetContent` allocate. If you decode every
  part of every message, that memory is yours to limit.

## Related

- [The layers](layers.md)
- [Compliant defaults and the opt-ins](compliant-defaults.md)
- [Stream a large message](../how-to/stream-a-large-message.md)
- [ImfOptions](../reference/ImfOptions.md)
