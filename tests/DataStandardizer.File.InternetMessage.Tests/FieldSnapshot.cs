namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// A copy of what an <see cref="ImfHeaderFieldView"/> shows, which stays valid after the reader moves on.  The raw bytes, name and value are
    /// held as strings with one byte in each character; see <see cref="Octets"/>.
    /// </summary>
    public sealed class FieldSnapshot
    {
        public FieldSnapshot(string rawBytes, string name, string value, string decodedValue, long bytePosition, long lineNumber, bool needsHeaderFallbackCharset = false)
        {
            RawBytes = rawBytes;
            Name = name;
            Value = value;
            DecodedName = name;
            DecodedValue = decodedValue;
            BytePosition = bytePosition;
            LineNumber = lineNumber;
            NeedsHeaderFallbackCharset = needsHeaderFallbackCharset;
        }

        private FieldSnapshot(ImfHeaderFieldView view)
        {
            RawBytes = Octets.ToText(view.RawBytes);
            Name = Octets.ToText(view.Name);
            Value = Octets.ToText(view.Value);
            DecodedName = view.GetName();
            DecodedValue = view.GetValue();
            BytePosition = view.BytePosition;
            LineNumber = view.LineNumber;
            NeedsHeaderFallbackCharset = view.NeedsHeaderFallbackCharset;
        }

        public string RawBytes { get; }

        public string Name { get; }

        public string Value { get; }

        public string DecodedName { get; }

        public string DecodedValue { get; }

        public long BytePosition { get; }

        public long LineNumber { get; }

        public bool NeedsHeaderFallbackCharset { get; }

        internal static FieldSnapshot Of(ImfHeaderFieldView view)
        {
            return new FieldSnapshot(view);
        }

        public override string ToString()
        {
            return $"{Name}@{BytePosition}/{LineNumber}";
        }
    }
}
