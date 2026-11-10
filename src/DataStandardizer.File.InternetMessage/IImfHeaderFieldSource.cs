namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A reader that an <see cref="ImfHeaderFieldView"/> can take its field from.
    /// </summary>
    internal interface IImfHeaderFieldSource
    {
        /// <summary>
        /// Gets the scanner holding the current field, for a view of that field.
        /// </summary>
        /// <param name="fieldVersion">The version of the reader's state that the view was taken at.</param>
        /// <exception cref="System.ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="System.InvalidOperationException">The reader has moved on from the field the view was taken for.</exception>
        ImfHeaderScanner GetScanner(int fieldVersion);
    }
}
