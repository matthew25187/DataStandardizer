---
title: File.vCard
parent: Packages
nav_order: 4.5
has_children: true
---

# DataStandardizer.File.vCard

Reading of vCard data at the level of its content lines. The package is a preview
under development: the card object model, writing, the older vCard versions, jCard and
xCard are still to come.

## Standards

| Standard | What it provides |
| --- | --- |
| **RFC 6350** | *vCard Format Specification*: the content-line syntax, including groups, line folding and TEXT escaping. |
| **RFC 6868** | *Parameter Value Encoding in iCalendar and vCard*: caret encoding of double quotes and line breaks in parameter values. |

## Standards compliance

By default the reader accepts only what RFC 6350 allows. Each accommodation for common
non-compliant input is a separate option on [VcardOptions](reference/VcardOptions.md),
off by default. So a file that other software reads without complaint may need one, for
example `AcceptByteOrderMark` for a file saved with a byte order mark.

## Reference

See the [API reference](reference/index.md).
