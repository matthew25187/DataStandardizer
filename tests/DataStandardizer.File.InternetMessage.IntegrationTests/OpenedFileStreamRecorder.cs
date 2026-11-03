using System;
using System.IO;

namespace DataStandardizer.File.InternetMessage.IntegrationTests
{
    /// <summary>
    /// Records the file stream that the library opens on this thread, until it is disposed.
    /// </summary>
    internal sealed class OpenedFileStreamRecorder : IDisposable
    {
        public OpenedFileStreamRecorder()
        {
            ImfFileStreams.Opened = fileStream => Opened = fileStream;
        }

        /// <summary>
        /// Gets the file stream last opened, or <c>null</c> if none has been.
        /// </summary>
        public FileStream Opened { get; private set; }

        public void Dispose()
        {
            ImfFileStreams.Opened = null;
        }
    }
}
