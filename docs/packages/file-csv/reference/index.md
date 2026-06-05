---
title: API reference
parent: File.CSV
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.File.CSV API reference

The public types of **DataStandardizer.File.CSV**. All types are in the
`DataStandardizer.File.CSV` namespace.

## Classes

| Type | Description |
| --- | --- |
| [CsvContext](CsvContext.md) | The state of a CSV reader or writer (its mappers and options). |
| [CsvExtensions](CsvExtensions.md) | Conversion extension methods for mappers and line-model objects. |
| [CsvFieldAttribute](CsvFieldAttribute.md) | Maps a property to a named CSV field for direct field access. |
| [CsvFieldContext](CsvFieldContext.md) | The context passed to the field handler and converter delegates. |
| [CsvFieldMapping](CsvFieldMapping.md) | The details of a single property-to-field mapping. |
| [CsvFieldMappingAttribute](CsvFieldMappingAttribute.md) | Declarative mapping metadata, discovered automatically. |
| [CsvFieldMappingBuilder](CsvFieldMappingBuilder.md) | The concrete fluent field-mapping builder and its step interfaces. |
| [CsvFileCustomMapperBase](CsvFileCustomMapperBase.md) | Base class for an imperative mapper of plain (non-line) model objects. |
| [CsvFileHeaderLine](CsvFileHeaderLine.md) | A CSV header line; each field value is a field name. |
| [CsvFileIoBase](CsvFileIoBase.md) | The shared base of the CSV reader and writer. |
| [CsvFileLineBase](CsvFileLineBase.md) | The abstract base of the CSV line types. |
| [CsvFileMapper](CsvFileMapper.md) | The sealed collection of field mappings produced from declarative attributes. |
| [CsvFileMapperBase](CsvFileMapperBase.md) | Base class for an imperative mapper of `CsvFileRecordLine`-derived models. |
| [CsvFileMappingBuilder](CsvFileMappingBuilder.md) | The entry point to the fluent mapping pipeline. |
| [CsvFileOptions](CsvFileOptions.md) | Options to configure a CSV reader or writer. |
| [CsvFileReader](CsvFileReader.md) | A forward-only, non-cached CSV reader. |
| [CsvFileRecordLine](CsvFileRecordLine.md) | A CSV data record; subclass it for strongly-typed fields. |
| [CsvFileWriter](CsvFileWriter.md) | A CSV writer over a stream, text writer, or file. |

## Exceptions

| Type | Description |
| --- | --- |
| [CsvFileException](CsvFileException.md) | Thrown for abnormal CSV processing. |

## Interfaces

| Type | Description |
| --- | --- |
| [ICsvFileLine](ICsvFileLine.md) | A CSV line as an ordered, name-keyed bag of field values. |
| [ICsvFileMapper](ICsvFileMapper.md) | A read-only set of field mappings keyed by property name. |
| [ICsvFileOptions](ICsvFileOptions.md) | The read-only view of a reader or writer's options. |

## Enumerations

| Type | Description |
| --- | --- |
| [CsvFieldQuoteHandling](CsvFieldQuoteHandling.md) | How field values are quoted when writing. |
| [CsvFileHeaderHandling](CsvFileHeaderHandling.md) | How the header line is handled when reading. |

## Delegates

| Type | Description |
| --- | --- |
| [CsvFieldBadValue](CsvFieldBadValue.md) | Called when a bad CSV field value is encountered. |
| [CsvFieldConvertFromString](CsvFieldConvertFromString.md) | Deserializes a CSV field value to a property value. |
| [CsvFieldConvertToString](CsvFieldConvertToString.md) | Serializes a property value to a CSV field value. |
| [CsvFieldCount](CsvFieldCount.md) | Called when an inconsistent field count is detected. |
| [CsvFieldGenerate](CsvFieldGenerate.md) | Generates a value for a CSV field. |
| [CsvFieldValidate](CsvFieldValidate.md) | Validates an incoming CSV field value. |
| [CsvFileHeader](CsvFileHeader.md) | Supplies field names in lieu of a header line from the file. |
</content>
</invoke>
