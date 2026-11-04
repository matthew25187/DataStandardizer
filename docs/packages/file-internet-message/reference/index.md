---
title: API reference
parent: File.InternetMessage
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.File.InternetMessage API reference

The public types of **DataStandardizer.File.InternetMessage**. All types are in the
`DataStandardizer.File.InternetMessage` namespace.

Every type is available on `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
Each page's **Applies to** section names the members that some targets don't have.

## Classes

| Type | Description |
| --- | --- |
| [EmlxEntry](EmlxEntry.md) | The content of an EMLX file: one message and its metadata. |
| [EmlxMetadata](EmlxMetadata.md) | The metadata of an EMLX file, from the XML property list that ends it. |
| [EmlxMissingPart](EmlxMissingPart.md) | A part that a partial EMLX file stores in another file: an entity with an `X-Apple-Content-Length` field and an empty body. |
| [EmlxPlistValue](EmlxPlistValue.md) | A value in an XML property list. It can't be changed once made. |
| [EmlxReader](EmlxReader.md) | Represents a reader of an Apple Mail EMLX file (`.emlx`, `.partial.emlx`) into an [`EmlxEntry`](EmlxEntry.md): one message, with a byte count before it and a property list of metadata after it. |
| [EmlxWriter](EmlxWriter.md) | Represents a writer of an Apple Mail EMLX file from an [`EmlxEntry`](EmlxEntry.md): the byte count, the message and the property list of metadata. |
| [ImfAddress](ImfAddress.md) | An address (RFC 5322 §3.4): an [`ImfMailbox`](ImfMailbox.md) or an [`ImfGroup`](ImfGroup.md). |
| [ImfAddressList](ImfAddressList.md) | An ordered, read-only list of addresses (RFC 5322 §3.4), with at least one. |
| [ImfContentDisposition](ImfContentDisposition.md) | The value of a Content-Disposition header field (RFC 2183), such as `attachment; filename=genome.jpeg`. |
| [ImfDiagnostic](ImfDiagnostic.md) | A problem in a message that does not stop it being read. |
| [ImfEntity](ImfEntity.md) | A MIME entity (RFC 2045 §2.4): an ordered list of header fields, with typed accessors over them, and a body. An [`ImfMultipart`](ImfMultipart.md) holds other entities, an [`ImfMessagePart`](ImfMessagePart.md) holds a message, and any other entity holds content. |
| [ImfEntityCollection](ImfEntityCollection.md) | The parts of an [`ImfMultipart`](ImfMultipart.md), in order. |
| [ImfEntityNode](ImfEntityNode.md) | An entity that [`ImfMessage.EnumerateEntities`](ImfMessage.md) found, with where it is in the message. |
| [ImfEntityReader](ImfEntityReader.md) | Represents a reader that provides fast, non-cached, forward-only access to a whole Internet message as a stream of events: entity start, header field, header end, body chunk and entity end. It follows RFC 2046 §5 into multipart and embedded message entities, and never holds a whole body in memory. |
| [ImfEntitySource](ImfEntitySource.md) | Describes one entity for an [`ImfEntityWriter`](ImfEntityWriter.md) to write: the fields and the content of a message or a part, or the raw bytes of one that has not changed. |
| [ImfEntityWriter](ImfEntityWriter.md) | Represents a writer that provides fast, non-cached, forward-only output of whole Internet messages, with their MIME entities (RFC 2045, RFC 2046), from a description of each message. |
| [ImfGroup](ImfGroup.md) | A group (RFC 5322 §3.4): a display name and a list of mailboxes, which may be empty, such as `Undisclosed recipients:;`. |
| [ImfHeaderField](ImfHeaderField.md) | One field of the header of an Internet message: either what a reader read, which is written back byte for byte, or a name and body that a writer formats. A field doesn't change. To change a field of a message, replace it in its [`ImfHeaderFieldCollection`](ImfHeaderFieldCollection.md). |
| [ImfHeaderFieldCollection](ImfHeaderFieldCollection.md) | The fields of a header, in order. Lookups by name are case-insensitive. |
| [ImfHeaderReader](ImfHeaderReader.md) | Represents a reader that provides fast, non-cached, forward-only access to the fields of one header section of an Internet message (RFC 5322 §2.2). |
| [ImfHeaderWriter](ImfHeaderWriter.md) | Represents a writer that provides fast, non-cached, forward-only output of the fields of one header section of an Internet message (RFC 5322 §2.2). |
| [ImfMailbox](ImfMailbox.md) | A mailbox (RFC 5322 §3.4): an optional display name, and an address made of a local part and a domain. |
| [ImfMessage](ImfMessage.md) | An Internet message (RFC 5322): an ordered list of header fields, with typed accessors over them, and a body. |
| [ImfMessageBuilder](ImfMessageBuilder.md) | Builds a message from its header, its text, and what goes with the text, in the structure that those call for. |
| [ImfMessageId](ImfMessageId.md) | A message identifier (RFC 5322 §3.6.4), as in a Message-ID, In-Reply-To or References header field, such as `<1234@local.machine.example>`. |
| [ImfMessagePart](ImfMessagePart.md) | An entity that holds a message (RFC 2046 §5.2.1, RFC 6532 §3.7), such as one that is being forwarded. |
| [ImfMultipart](ImfMultipart.md) | A multipart entity (RFC 2046 §5.1): an ordered list of entities, with a preamble and an epilogue. |
| [ImfOptions](ImfOptions.md) | Options to configure the behaviour of an Internet message reader or writer. |
| [ImfPhraseList](ImfPhraseList.md) | An ordered, read-only list of phrases, as in a Keywords header field (RFC 5322 §3.6.5). |
| [ImfReader](ImfReader.md) | Represents a reader of a whole Internet message into an [`ImfMessage`](ImfMessage.md), which holds it in memory. To read a message without holding it, use an [`ImfEntityReader`](ImfEntityReader.md). |
| [ImfResentBlock](ImfResentBlock.md) | The resent fields of one resending of a message (RFC 5322 §3.6.6). |
| [ImfTraceField](ImfTraceField.md) | A Return-Path or Received field (RFC 5322 §3.6.7). These are strictly informational, and any formal interpretation of them is left to RFC 5321. |
| [ImfWriter](ImfWriter.md) | Represents a writer of whole Internet messages from [`ImfMessage`](ImfMessage.md) objects. |
| [MboxEntry](MboxEntry.md) | One message of an mbox file, with its separator line. |
| [MboxEnvelope](MboxEnvelope.md) | The separator line before a message in an mbox file (RFC 4155 Appendix A): `From`, a space, the address of the sender, a space, and the UTC date and time that the message was received, in the form of the `ctime` function with no time zone. |
| [MboxReader](MboxReader.md) | Represents a reader of an mbox file (RFC 4155): many Internet messages in one file, each after a separator line. Messages are read one at a time, so a mailbox of any size can be processed without loading it whole. |
| [MboxWriter](MboxWriter.md) | Represents a writer of mbox files (RFC 4155): many Internet messages in one file, each after a separator line and ended by an empty line. |
| [MhtmlDocument](MhtmlDocument.md) | A view of an [`ImfMessage`](ImfMessage.md) as an aggregate document (RFC 2557): a web page saved with its resources, as in an MHTML file. |
| [MhtmlDocumentBuilder](MhtmlDocumentBuilder.md) | Builds an aggregate document (RFC 2557) from a root document and its resources: a `multipart/related` entity whose `type` and `start` parameters name the root, with each part labelled with its URL. |
| [MhtmlReader](MhtmlReader.md) | Represents a reader of an MHTML file (`.mht`, `.mhtml`), or any other aggregate document (RFC 2557), into an [`MhtmlDocument`](MhtmlDocument.md). |
| [MhtmlResource](MhtmlResource.md) | A part of an [`MhtmlDocument`](MhtmlDocument.md), with the URI that its Content-Location field resolves to. |
| [MhtmlWriter](MhtmlWriter.md) | Represents a writer of MHTML files (`.mht`, `.mhtml`), or any other aggregate documents (RFC 2557), from [`MhtmlDocument`](MhtmlDocument.md) objects. |

## Static classes

| Type | Description |
| --- | --- |
| [ImfHeaderText](ImfHeaderText.md) | Reads and writes the text of an unstructured header field or a comment, with RFC 2047 encoded words where RFC 2047 §5 allows them. |
| [ImfMimeFields](ImfMimeFields.md) | Reads and writes the bodies of the MIME header fields whose values are not types of this package: MIME-Version, Content-Type, Content-Transfer-Encoding and Content-Language. |

## Structs

| Type | Description |
| --- | --- |
| [ImfDateTime](ImfDateTime.md) | A date-time (RFC 5322 §3.3), as in a Date or Resent-Date header field or at the end of a Received header field, such as `Fri, 21 Nov 1997 09:55:06 -0600`. |
| [ImfEntityEvent](ImfEntityEvent.md) | A view of the event an [`ImfEntityReader`](ImfEntityReader.md) has just read, without allocation. |
| [ImfHeaderFieldView](ImfHeaderFieldView.md) | A view of the header field an [`ImfHeaderReader`](ImfHeaderReader.md) or [`ImfEntityReader`](ImfEntityReader.md) has just read, without allocation. |
| [ImfWriteProgress](ImfWriteProgress.md) | The progress of a writer. [`ImfHeaderWriter`](ImfHeaderWriter.md) reports it after each field of a bulk write, [`ImfEntityWriter`](ImfEntityWriter.md) after each block of output, and [`ImfWriter`](ImfWriter.md) after each field and each block of a body, and [`MboxWriter`](MboxWriter.md) after each message of a bulk write. |

## Exceptions

| Type | Description |
| --- | --- |
| [ImfFileException](ImfFileException.md) | Exception to normal processing of an Internet message. |

## Delegates

| Type | Description |
| --- | --- |
| [EmlxPartResolver](EmlxPartResolver.md) | Supplies the content of a part that a partial EMLX file stores in another file. |

## Enumerations

| Type | Description |
| --- | --- |
| [EmlxMessageFlags](EmlxMessageFlags.md) | The single-bit flags in the `flags` key of the property list of an EMLX file. |
| [EmlxPlistValueKind](EmlxPlistValueKind.md) | The kinds of value in an XML property list. |
| [ImfDiagnosticKind](ImfDiagnosticKind.md) | The kinds of problem that an [`ImfDiagnostic`](ImfDiagnostic.md) reports. |
| [ImfEntityEventKind](ImfEntityEventKind.md) | The kinds of event that an [`ImfEntityReader`](ImfEntityReader.md) gives. |
| [ImfHeaderEncoding](ImfHeaderEncoding.md) | How an Internet message writer writes header text that is not US-ASCII. |
| [ImfHeaderFieldKind](ImfHeaderFieldKind.md) | How an Internet message writer may fold a header field (RFC 5322 §2.2). |
| [ImfTraceFieldKind](ImfTraceFieldKind.md) | The kinds of trace field (RFC 5322 §3.6.7). |
| [MboxLineBreak](MboxLineBreak.md) | The line breaks that an [`MboxWriter`](MboxWriter.md) writes. |
| [MboxVariant](MboxVariant.md) | The variants of the mbox format, which differ in how they keep a line of a message that starts with `From ` from being taken for a separator line. |
