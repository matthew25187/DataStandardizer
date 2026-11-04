---
title: Byte-exact round trips
parent: File.InternetMessage
grand_parent: Packages
nav_order: 11
---

# Byte-exact round trips

What you read and don't change is written back byte for byte. A DKIM or S/MIME
signature covers particular bytes, so a tool that reformats a message it only meant
to pass along breaks the signature. Here, a change rewrites only what changed.

## What is kept

| What | How it is kept |
| --- | --- |
| A field that was read | `ImfHeaderField.RawBytes` holds its name, any white space before the colon, its folds and its line break. A writer copies them, obsolete syntax included, whatever options it was read under. |
| A body that hasn't changed | `ImfMessage.RawBody` holds the bytes after the empty line that ends the header. A writer copies them. |
| The empty line that ends the header | Its line break is kept, and so is its absence, if the data ended first and there is still no body. |
| A part that hasn't changed, in a message where another has | The bytes of the part are copied, and so are the delimiter lines of a multipart entity whose parts are all still there. |
| An mbox separator line | `MboxEnvelope.RawLine` holds it. |
| EMLX metadata that hasn't changed | `EmlxMetadata.RawBytes` holds it, and the first line of the file is kept if the count is still right. |

## What a change rewrites

Each of these rewrites that thing and leaves the rest alone:

- **Setting an accessor**, such as `ImfMessage.Subject`, replaces that field where it
  is, or adds it after the other fields if there is none. It is written in the modern
  form, with CRLF line breaks.
- **Changing `ImfHeaderFieldCollection`**, with `Add`, `Insert`, `Replace` or
  `RemoveAll`, changes those fields and no others.
- **Calling `ImfEntity.SetContent` or `SetText`** gives that entity new content, for
  which the writer chooses a transfer encoding.
- **Changing an `EmlxMetadata`** writes the whole property list again, as Apple's tools
  lay it out, with the keys that weren't changed still in order.

One thing changes without being asked: a multipart entity that was read with no close
delimiter, under `ImfOptions.AllowMissingBoundaryTerminator`, gets one if it is written
again after a change.

## Line breaks in other file formats

RFC 5322 §2.1 requires CRLF line breaks in a message. RFC 4155 Appendix A requires LF
in an mbox file, and an EMLX file uses LF too. So moving a message between these files
changes every line break:

| From | To | Result |
| --- | --- | --- |
| An mbox file with LF | `MboxWriter` with LF | Byte for byte, apart from what the variant adds. |
| An EMLX file | `EmlxWriter` | Byte for byte. |
| An mbox or EMLX file | `ImfWriter` | Every line break becomes CRLF. |
| An EML file | `MboxWriter` or `EmlxWriter` | Every line break becomes LF. |

That changes the bytes, so check what your verifier expects of a file that has been
moved in this way. A message with a `binary` entity, or
one with an extension transfer encoding, can't change in this way, because a line break
in it may not be one, so the writer throws `ImfFileException`.

The mbox variants differ in what reads back. See
[Read, filter and rewrite an mbox file](../how-to/read-and-rewrite-mbox.md#choose-a-variant).

## Keeping bytes at the lower layers

The field and entity layers copy bytes in the same way:

- `ImfHeaderWriter.WriteField(view)` and `WriteFields(reader)` copy fields that a
  reader has just read. `WriteRawField` copies the bytes you give it, once it has
  checked that they are one whole field.
- `ImfEntitySource.FromRaw` describes an entity by its raw bytes. With an
  `ImfEntityReader`, those are the range of the data that the `EntityEnd` event covers.
  `AddField` and `AddRawField` do the same for a field of an entity that is otherwise
  written from parts.

## Signatures aren't checked

The package keeps the bytes that a signature covers, and doesn't read the signature. It
neither creates nor verifies DKIM or S/MIME signatures. A change to a field or body that
a signature covers still makes the signature fail, however carefully the rest is copied.

## Related

- [Edit one header without breaking a signature](../how-to/edit-a-header.md)
- [One message format, several file formats](file-formats.md)
- [ImfWriter](../reference/ImfWriter.md), [ImfHeaderField](../reference/ImfHeaderField.md)
  and [ImfEntitySource](../reference/ImfEntitySource.md)
