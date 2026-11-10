using System;
using System.Globalization;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The progress of a writer.  <see cref="ImfHeaderWriter"/> reports it after each field of a bulk write, <see cref="ImfEntityWriter"/> after each
    /// block of output, and <see cref="ImfWriter"/> after each field and each block of a body, and <see cref="MboxWriter"/> after each message of a bulk write.
    /// </summary>
    /// <remarks>
    /// Counts are cumulative for the writer, not for the call that reports them.
    /// </remarks>
    public readonly struct ImfWriteProgress : IEquatable<ImfWriteProgress>
    {
        /// <summary>
        /// Creates a progress value.
        /// </summary>
        /// <param name="fieldsWritten">The number of header fields written.</param>
        /// <param name="entitiesWritten">The number of entities written.</param>
        /// <param name="bytesWritten">The number of bytes written.</param>
        public ImfWriteProgress(long fieldsWritten, long entitiesWritten, long bytesWritten)
        {
            FieldsWritten = fieldsWritten;
            EntitiesWritten = entitiesWritten;
            BytesWritten = bytesWritten;
        }

        /// <summary>
        /// Gets the number of bytes the writer has handed to the underlying stream so far.
        /// </summary>
        public long BytesWritten { get; }

        /// <summary>
        /// Gets the number of entities the writer has written so far, at any depth, including each message; always 0 for
        /// <see cref="ImfHeaderWriter"/>.  <see cref="MboxWriter"/> counts each message as one.  An entity written from its raw bytes counts as one, whatever it holds.
        /// </summary>
        public long EntitiesWritten { get; }

        /// <summary>
        /// Gets the number of header fields the writer has written so far.  The fields inside an entity written from its raw bytes are not counted.
        /// </summary>
        public long FieldsWritten { get; }

        /// <summary>
        /// Indicates whether this progress value equals another.
        /// </summary>
        /// <param name="other">The other progress value.</param>
        /// <returns><c>true</c> if all the counts are equal; otherwise <c>false</c>.</returns>
        public bool Equals(ImfWriteProgress other)
        {
            return FieldsWritten == other.FieldsWritten && EntitiesWritten == other.EntitiesWritten && BytesWritten == other.BytesWritten;
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is ImfWriteProgress other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (FieldsWritten.GetHashCode() * 397 ^ EntitiesWritten.GetHashCode()) * 397 ^ BytesWritten.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "FieldsWritten = {0}, EntitiesWritten = {1}, BytesWritten = {2}", FieldsWritten,
                EntitiesWritten, BytesWritten);
        }

        /// <summary>
        /// Indicates whether two progress values are equal.
        /// </summary>
        public static bool operator ==(ImfWriteProgress left, ImfWriteProgress right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two progress values differ.
        /// </summary>
        public static bool operator !=(ImfWriteProgress left, ImfWriteProgress right)
        {
            return !left.Equals(right);
        }
    }
}
