using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// URIs shared by the tests of <see cref="IcalValue.TryFormatUri"/> and of the typed URI setters.
    /// </summary>
    public static class UriTestData
    {
        /// <summary>
        /// An absolute URI that is written as it was given, though <see cref="System.Uri"/> normalises its scheme and host and adds a path.
        /// </summary>
        public const string UpperCaseUri = "HTTP://EXAMPLE.COM";

        /// <summary>
        /// Gets texts that <see cref="System.Uri"/> takes as absolute URIs, but which are not URIs that RFC 5545 §3.3.13 allows: each has
        /// a character that RFC 3986 §2 excludes, or is a drive path, whose text would be read as a URI with the scheme <c>C</c>.
        /// </summary>
        public static TheoryData<string> UriTextsNotAllowed => new TheoryData<string>
        {
            "http://example.com/a b",
            "http://b\x00FCcher.example/",
            "http://example.com/caf\x00E9",
            "http://example.com/{x}",
            "http://example.com/a|b",
            " http://example.com/",
            "C:/temp/file.txt",
            "C:\\temp\\file.txt",
        };
    }
}
