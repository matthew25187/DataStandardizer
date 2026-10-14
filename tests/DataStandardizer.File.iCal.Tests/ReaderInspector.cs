using System.Reflection;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Reads the readers, streams and buffer sizes created by <see cref="IcalContentLineReader"/>.  These are private runtime details, so reflection
/// is used.
/// </summary>
internal static class ReaderInspector
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic;

    public static TextReader GetUnderlyingReader(IcalContentLineReader reader)
    {
        return (TextReader)GetFieldValue(reader, "_reader");
    }

    public static Stream GetInnerStream(Utf8FoldRealigningStream stream)
    {
        return (Stream)GetFieldValue(stream, "_inner");
    }

    /// <summary>
    /// Gets the size of the byte buffer of a <see cref="StreamReader"/>, which is the buffer size it was created with.
    /// </summary>
    public static int GetBufferSize(StreamReader reader)
    {
        return ((byte[])GetFieldValue(reader, "_byteBuffer")).Length;
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
