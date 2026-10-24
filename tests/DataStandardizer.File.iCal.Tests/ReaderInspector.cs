using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
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

        public static IcalContentLineScanner GetScanner(IcalContentLineReader reader)
        {
            return (IcalContentLineScanner)GetFieldValue(reader, "_scanner");
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
#if NETCOREAPP
            return ((byte[])GetFieldValue(reader, "_byteBuffer")).Length;
#else
            return ((byte[])GetFieldValue(reader, "byteBuffer")).Length;
#endif
        }

        /// <summary>
        /// Gets the buffer size a <see cref="FileStream"/> was created with, from its buffering strategy on .NET 6 and later.
        /// </summary>
        public static int GetBufferSize(FileStream fileStream)
        {
#if NET6_0_OR_GREATER
            return (int)GetFieldValue(GetFieldValue(fileStream, "_strategy"), "_bufferSize");
#else
            return (int)GetFieldValue(fileStream, "_bufferSize");
#endif
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
}
