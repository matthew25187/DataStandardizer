---
title: The layers
parent: File.InternetMessage
grand_parent: Packages
nav_order: 10
---

# The layers

The package is built in layers. Each can be used on its own, so you can stop at the
lowest one that does the job. The lower layers are fast and hold little in memory.
The higher ones are easier to use and hold more.

All public types here are in the `DataStandardizer.File.InternetMessage` namespace.

```text
                     EML files and streams
                               │
Fields      ImfHeaderReader / ImfHeaderWriter          one header section,
            ImfHeaderFieldView, ImfHeaderField         nothing held but the current field
                               │
Entities    ImfEntityReader / ImfEntityWriter          a whole message as events,
            ImfEntityEvent, ImfEntitySource            no body held in memory
                               │
Messages    ImfReader / ImfWriter                      a whole message in memory
            ImfMessage, ImfEntity, ImfMultipart
            ImfMessageBuilder
                               │
Formats     MhtmlReader, MboxReader, EmlxReader        the same messages in other files
            MhtmlWriter, MboxWriter, EmlxWriter
```

Beside them are the values that field bodies hold. `ImfAddressList`, `ImfMailbox`,
`ImfGroup`, `ImfDateTime`, `ImfMessageId`, `ImfPhraseList` and
`ImfContentDisposition` parse and format their fields, and `ImfHeaderText` and
`ImfMimeFields` do the same for unstructured text and the MIME fields. You can use
them with a field from any layer.

## Which layer to use

| You want to | Use | Memory |
| --- | --- | --- |
| Find a few fields, or copy a header with a change | `ImfHeaderReader`, `ImfHeaderWriter` | One field at a time. |
| Go through a message of any size, or save its attachments to files | `ImfEntityReader` | One block of a body at a time. |
| Write a message whose bodies are in files | `ImfEntityWriter` | One block of a body at a time, and a tree of `ImfEntitySource` objects. |
| Read, change or build a message with typed fields | `ImfReader`, `ImfWriter`, `ImfMessageBuilder` | The whole message, up to `ImfOptions.MaxEntitySize` for its body. |
| Open a saved web page | `MhtmlReader` | The whole document. |
| Go through a mailbox of any size | `MboxReader` | One message at a time, or, with `ReadEntityEvents`, one block of a body at a time. |
| Read an Apple Mail file | `EmlxReader` | The whole file, and the message again. |

## Reading with views

`ImfHeaderReader.ReadField` gives an `ImfHeaderFieldView`, and
`ImfEntityReader.ReadEvent` an `ImfEntityEvent`. Neither allocates, and each is valid
only until the next read. After that, every member throws. A span or memory taken from
one can't be checked in that way, and using it later gives undefined content, so copy
what you need to keep. `new ImfHeaderField(view)` copies a field.

## Asynchronous reads and writes

Every reader and writer has `...Async` forms of its methods, and `IAsyncDisposable`,
on `netstandard2.0`, `netstandard2.1` and `net10.0`. `netstandard1.1` has only the
synchronous methods. A file-path constructor opens the file for asynchronous use.

Only one operation may be in progress at a time on a reader or writer. Synchronous and
asynchronous calls may follow one another.

## Related

- [Stream a large message](../how-to/stream-a-large-message.md)
- [Byte-exact round trips](round-trips.md)
- [One message format, several file formats](file-formats.md)
- [API reference](../reference/index.md)
