---
title: API reference
parent: File.vCard
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.File.vCard API reference

The public types of **DataStandardizer.File.vCard**. All types are in the
`DataStandardizer.File.vCard` namespace.

## Classes

| Type | Description |
| --- | --- |
| [VcardContentLine](VcardContentLine.md) | A content line (RFC 6350 §3.3): group, property name, parameters and value. |
| [VcardContentLineParameter](VcardContentLineParameter.md) | A parameter of a content line, with its values. |
| [VcardContentLineReader](VcardContentLineReader.md) | Reads the unfolded content lines of vCard data, with zero-copy and async options. |
| [VcardFileException](VcardFileException.md) | The exception for a problem in vCard data, with where it was found. |
| [VcardOptions](VcardOptions.md) | Options for a reader; RFC 6350 defaults. |

## Structs

| Type | Description |
| --- | --- |
| [VcardContentLineView](VcardContentLineView.md) | A zero-copy view of the line a reader has just read. |
