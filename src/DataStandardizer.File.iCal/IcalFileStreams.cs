#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.IO;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Opens the files of the file-path constructors of the readers and writers.
    /// </summary>
    /// <remarks>
    /// Every file is opened with <see cref="FileOptions.Asynchronous"/>, so the asynchronous members do no synchronous I/O.  The
    /// synchronous members still work, at a small cost on Windows.  A caller who only reads or writes synchronously can pass a
    /// <see cref="FileStream"/> of their own to a <see cref="Stream"/> constructor instead.
    /// </remarks>
    internal static class IcalFileStreams
    {
        /// <summary>
        /// Called with each file stream as it is opened, on the thread that opened it, so tests can check how it was opened.
        /// </summary>
        [ThreadStatic]
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static Action<FileStream>? Opened;
#else
        internal static Action<FileStream> Opened;
#endif

        /// <summary>
        /// Opens a file for asynchronous, sequential reading, sharing it for reading.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="bufferSize">The buffer size of the file stream, in bytes.</param>
        internal static FileStream OpenRead(string path, int bufferSize)
        {
            return Notify(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan));
        }

        /// <summary>
        /// Creates or overwrites a file for asynchronous writing, sharing it for reading.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="bufferSize">The buffer size of the file stream, in bytes.</param>
        internal static FileStream OpenWrite(string path, int bufferSize)
        {
            return Notify(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize, FileOptions.Asynchronous));
        }

        private static FileStream Notify(FileStream fileStream)
        {
            Opened?.Invoke(fileStream);
            return fileStream;
        }
    }
}
#endif
