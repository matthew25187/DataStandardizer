namespace DataStandardizer.File.CSV.IntegrationTests;

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
    /// Deletes the file.  Deleting a file that a test has already deleted does nothing.
    /// </summary>
    public void Dispose()
    {
        System.IO.File.Delete(Path);
    }
}
