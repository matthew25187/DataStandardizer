using System.IO;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Supplies the content of a part that a partial EMLX file stores in another file.
    /// </summary>
    /// <param name="part">The part.</param>
    /// <returns>A stream of the bytes of the body of the part as the message would hold them, still in its transfer encoding, which the reader reads to its end and disposes; or <c>null</c> if the
    /// content isn't available.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    public delegate Stream? EmlxPartResolver(EmlxMissingPart part);
#else
    public delegate Stream EmlxPartResolver(EmlxMissingPart part);
#endif
}
