using System;
using System.IO;

namespace DataStandardizer.File.iCal.IntegrationTests
{
    /// <summary>
    /// A file in the temporary folder that is deleted when it is disposed.
    /// </summary>
    internal sealed class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Gets the full path of the file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates an empty file.
        /// </summary>
        public static TemporaryFile Create()
        {
            return new TemporaryFile(System.IO.Path.GetTempFileName());
        }

        /// <summary>
        /// Creates a file that holds <paramref name="content"/>.
        /// </summary>
        public static TemporaryFile Create(byte[] content)
        {
            var temporaryFile = Create();
            System.IO.File.WriteAllBytes(temporaryFile.Path, content);

            return temporaryFile;
        }

        /// <summary>
        /// Gets the bytes that the file holds.
        /// </summary>
        public byte[] ReadAllBytes()
        {
            return System.IO.File.ReadAllBytes(Path);
        }

        /// <summary>
        /// Opens the file for reading and writing, sharing it with nothing, and closes it again.  Throws <see cref="IOException"/> if anything
        /// still has the file open.
        /// </summary>
        public void OpenExclusively()
        {
            new FileStream(Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Dispose();
        }

        /// <summary>
        /// Deletes the file.  Deleting a file that has already been deleted does nothing.
        /// </summary>
        public void Dispose()
        {
            System.IO.File.Delete(Path);
        }
    }
}
