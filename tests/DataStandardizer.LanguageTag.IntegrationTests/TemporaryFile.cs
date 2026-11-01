using System;
using System.IO;
using System.Text;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    /// <summary>
    /// A file in the temporary folder with given content, which is deleted when the scope is disposed.
    /// </summary>
    internal sealed class TemporaryFile : IDisposable
    {
        internal TemporaryFile(string content)
        {
            Path = System.IO.Path.GetTempFileName();
            File.WriteAllText(Path, content, new UTF8Encoding(false));
        }

        internal string Path { get; }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
