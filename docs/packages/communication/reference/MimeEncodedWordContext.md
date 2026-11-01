---
title: MimeEncodedWordContext Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeEncodedWordContext Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

The place in a header field where an RFC 2047 encoded word appears (RFC 2047 §5), which sets the
characters that its "Q"-encoded text may hold as themselves. In every context, `=` starts an escape
and `_` stands for a space. The "B" encoding writes only characters that every context allows. Which
context applies is set by the grammar of the field, which is up to the caller.

**Syntax**

```csharp
public enum MimeEncodedWordContext
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Text` | 0 | A word that replaces `text` in a `*text` header field, such as Subject (RFC 2047 §5(1)): any printable ASCII character other than `=`, `?` and `_` stands for itself. |
| `Comment` | 1 | A word in a comment (RFC 2047 §5(2)): as `Text`, except that `(`, `)` and `"` don't stand for themselves. A word that's written also encodes `\`, as it would start a quoted-pair (RFC 5322 §3.2.2). |
| `Phrase` | 2 | A word that replaces a word in a phrase, such as the display name of an address (RFC 2047 §5(3)): only ASCII letters, digits, `!`, `*`, `+`, `-` and `/` stand for themselves. |
