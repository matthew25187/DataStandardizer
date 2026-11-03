namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Sends a field body through a writer, which folds it, and back through a reader.
    /// </summary>
    internal static class MimeFieldRoundTrips
    {
        /// <summary>
        /// Writes a structured field, and gets the body of the field that a reader finds in what was written, with its folds.
        /// </summary>
        public static byte[] WriteAndRead(string name, string body)
        {
            var section = MessageWrites.WriteField(name, body, ImfHeaderFieldKind.Structured) + "\r\n";
            using (var reader = HeaderSections.CreateReader(section))
            {
                return reader.ReadField().Value.ValueMemory.ToArray();
            }
        }
    }
}
