using System.Reflection;

namespace DataStandardizer.File.CSV.Tests;

/// <summary>
/// Reads the buffer sizes of the readers, writers and file streams created by <see cref="CsvFileReader{TRecordLine}"/>
/// and <see cref="CsvFileWriter{TRecordLine}"/>.  These are private runtime details, so reflection is used.
/// </summary>
internal static class StreamBufferInspector
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic;

    public static TextReader GetUnderlyingReader<TRecordLine>(CsvFileReader<TRecordLine> csvReader)
        where TRecordLine : CsvFileRecordLine, new()
    {
        return (TextReader)GetFieldValue(csvReader, "_reader");
    }

    public static TextWriter GetUnderlyingWriter<TRecordLine>(CsvFileWriter<TRecordLine> csvWriter)
        where TRecordLine : CsvFileRecordLine
    {
        return (TextWriter)GetFieldValue(csvWriter, "_writer");
    }

    /// <summary>
    /// Gets the size of the byte buffer of a <see cref="StreamReader"/>, which is the buffer size it was created with.
    /// </summary>
    public static int GetBufferSize(StreamReader reader)
    {
        return ((byte[])GetFieldValue(reader, "_byteBuffer")).Length;
    }

    /// <summary>
    /// Gets the size of the character buffer of a <see cref="StreamWriter"/>, which is the buffer size it was created
    /// with.
    /// </summary>
    public static int GetBufferSize(StreamWriter writer)
    {
        return ((char[])GetFieldValue(writer, "_charBuffer")).Length;
    }

    /// <summary>
    /// Gets the buffer size a <see cref="FileStream"/> was created with, from its buffering strategy.
    /// </summary>
    public static int GetBufferSize(FileStream fileStream)
    {
        return (int)GetFieldValue(GetFieldValue(fileStream, "_strategy"), "_bufferSize");
    }

    private static object GetFieldValue(object instance, string fieldName)
    {
        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(fieldName, InstanceFields);
            if (field != null)
            {
                return field.GetValue(instance)
                       ?? throw new InvalidOperationException($"Field {fieldName} of {type} is null.");
            }
        }

        throw new MissingFieldException(instance.GetType().FullName, fieldName);
    }
}
