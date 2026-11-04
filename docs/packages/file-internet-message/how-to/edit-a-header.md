---
title: Edit one header without breaking a signature
parent: File.InternetMessage
grand_parent: Packages
nav_order: 4
---

# Edit one header without breaking a signature

A DKIM or S/MIME signature covers particular bytes, so a tool that reformats a message to
add a field breaks it. Here, a field that was read is written back byte for byte, so you
can change one field and leave the rest as they arrived. See
[Byte-exact round trips](../concepts/round-trips.md).

## Add a field

Read the message, add the field, and write it:

```csharp
using DataStandardizer.File.InternetMessage;

ImfMessage message;
using (var reader = new ImfReader("signed.eml"))
{
    message = reader.ReadMessage();
}

message.Fields.Insert(0, ImfHeaderField.FromText("X-Scanned-By", "Example Scanner 1.2"));

using var writer = new ImfWriter("scanned.eml");
writer.WriteMessage(message);
```

Given this message, whose odd spacing and old-style date are all covered by the signature:

```text
DKIM-Signature: v=1; a=rsa-sha256; d=example.com; s=mail;
	h=from:to:subject:date; bh=abc=; b=def=
From:   Ana   <ana@example.com>
To: ben@example.org
Subject:  Keep   my   spacing
Date: 4 Nov 26 09:30 +1300

Hello
```

the output is the same bytes, with one line added at the top:

```text
X-Scanned-By: Example Scanner 1.2
DKIM-Signature: v=1; a=rsa-sha256; d=example.com; s=mail;
	h=from:to:subject:date; bh=abc=; b=def=
From:   Ana   <ana@example.com>
To: ben@example.org
Subject:  Keep   my   spacing
Date: 4 Nov 26 09:30 +1300

Hello
```

## Change or remove a field

A field doesn't change, so replace it in `Fields`. `Fields` is a list you can search by
name, ignoring case:

| Call | Does |
| --- | --- |
| `Add(field)` | Adds a field after the others. |
| `Insert(index, field)` | Adds a field at an index. RFC 5322 §3.6.6 and §3.6.7 have resent and trace fields put before those already there. |
| `Replace(index, field)` | Replaces the field at an index. |
| `RemoveAt(index)`, `RemoveAll(name)` | Remove fields. |
| `Find(name)`, `FindAll(name)`, `Contains(name)` | Look fields up. |

An accessor works too. Setting `message.Subject` replaces that field where it is, and
removes any others of that name. The other fields aren't touched.

There are three ways to make a field:

| Call | Makes |
| --- | --- |
| `ImfHeaderField.FromText(name, text)` | An unstructured field from text, encoded as the writer's `HeaderEncoding` says. |
| `new ImfHeaderField(name, value, kind)` | A field from its body, written as it is given, and folded as its kind allows. |
| `ImfHeaderField.FromRaw(bytes)` | A field from its raw bytes, written as they are. |

## What still breaks a signature

The package keeps the bytes. It doesn't know what a signature covers, and it doesn't check
one. So:

- **Don't change what is signed.** A DKIM signature's `h=` tag lists the fields it covers.
  Changing one of them, or the body, makes it fail, however exactly the rest is copied.
- **Adding a field may matter.** DKIM signs the last instance of each field that it lists, so
  adding another of a signed field, such as a second From, can break the signature. A new
  field of another name doesn't. For an S/MIME signature, a header outside the signed part
  isn't covered.
- **Don't move the message between file formats** if you need the bytes it was signed in. See
  [Line breaks in other file formats](../concepts/round-trips.md#line-breaks-in-other-file-formats).

## Edit a large message

`ImfReader` holds the whole message. To add a field to one too large for that, copy the
header with `ImfHeaderReader` and `ImfHeaderWriter`, and then the rest of the stream:

```csharp
using var input = File.OpenRead("signed.eml");
using var output = File.Create("scanned.eml");

long bodyStart;
using (var reader = new ImfHeaderReader(input))
using (var writer = new ImfHeaderWriter(output))
{
    writer.WriteTextField("X-Scanned-By", "Example Scanner 1.2");
    writer.WriteFields(reader);
    writer.WriteEndOfHeader();
    writer.Flush();
    bodyStart = reader.BytePosition;
}

input.Position = bodyStart;
input.CopyTo(output);
```

The result is the same as above, byte for byte. A reader reads ahead, so the position of the
stream afterwards isn't the start of the body. `BytePosition` is the offset of the body from
where the reader started, so the stream has to support seeking to use it in this way.

To change one field as you go, read them one at a time:

```csharp
while (reader.ReadField() is { } field)
{
    if (field.GetName().Equals("X-Scanned-By", StringComparison.OrdinalIgnoreCase))
    {
        writer.WriteTextField("X-Scanned-By", "Example Scanner 2.0");
    }
    else
    {
        writer.WriteField(field);
    }
}
```

These two work on one header section only, so the checks of a whole message don't apply to
them. If you need them, use `ImfReader` or `ImfEntityReader`.

## Related

- [Byte-exact round trips](../concepts/round-trips.md)
- [Stream a large message](stream-a-large-message.md)
- [ImfHeaderField](../reference/ImfHeaderField.md),
  [ImfHeaderFieldCollection](../reference/ImfHeaderFieldCollection.md),
  [ImfHeaderReader](../reference/ImfHeaderReader.md) and
  [ImfHeaderWriter](../reference/ImfHeaderWriter.md)
