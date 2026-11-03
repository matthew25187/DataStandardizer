using System;
using System.IO;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// One field of the header of an Internet message: either what a reader read, which is written back byte for byte, or a name and body that a writer
    /// formats.  A field doesn't change.  To change a field of a message, replace it in its <see cref="ImfHeaderFieldCollection"/>.
    /// </summary>
    public sealed class ImfHeaderField
    {
        private static readonly byte[] NoBytes = new byte[0];

        private readonly Func<ImfHeaderEncoding, byte[]> _getBody;
        private readonly ImfHeaderFieldKind _kind;
        private readonly byte[] _nameBytes;
        private readonly byte[] _rawBytes;
        private readonly byte[] _value;

        private string _valueText;

        /// <summary>
        /// Creates a field from its name and its body, which is written as it is given, folded as its kind allows.
        /// </summary>
        /// <remarks>
        /// The name and body are checked when the field is written, in the way that <see cref="ImfHeaderWriter.WriteField(string, string, ImfHeaderFieldKind)"/> describes.
        /// </remarks>
        /// <param name="name">The field name.</param>
        /// <param name="value">The unfolded field body.  One space is written before it if it doesn't start with a space or tab, and isn't empty.</param>
        /// <param name="kind">How the field may be folded.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not an <see cref="ImfHeaderFieldKind"/> member.</exception>
        /// <exception cref="ImfFileException"><paramref name="name"/> or <paramref name="value"/> is not valid Unicode.</exception>
        public ImfHeaderField(string name, string value, ImfHeaderFieldKind kind)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (kind != ImfHeaderFieldKind.Unstructured && kind != ImfHeaderFieldKind.Structured)
                throw new ArgumentOutOfRangeException(nameof(kind));

            var body = ImfHeaderFormatter.GetBytes(value);

            Name = name;
            BytePosition = -1;
            _nameBytes = ImfHeaderFormatter.GetBytes(name);
            _kind = kind;
            _rawBytes = NoBytes;
            _value = body;
            _valueText = value;
            _getBody = encoding => body;
        }

        /// <summary>
        /// Creates a field by copying what a reader has just read, to be written byte for byte.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <exception cref="ObjectDisposedException">The reader of the field has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on from the field, or the view was not obtained from a reader.</exception>
        public ImfHeaderField(ImfHeaderFieldView field)
            : this(field.GetName(), field.RawBytes.ToArray(), field.Value.ToArray(), field.NeedsHeaderFallbackCharset ? field.GetValue() : string.Empty, field.BytePosition, field.LineNumber)
        {
        }

        /// <summary>
        /// Creates a field from what a reader read.
        /// </summary>
        /// <param name="valueText">The body as a string, if it had to be decoded with the fallback character set; otherwise empty, for it to be decoded as UTF-8 when asked for.</param>
        internal ImfHeaderField(string name, byte[] rawBytes, byte[] value, string valueText, long bytePosition, long lineNumber)
        {
            Name = name;
            BytePosition = bytePosition;
            LineNumber = lineNumber;
            _nameBytes = NoBytes;
            _kind = ImfHeaderFieldKind.Unstructured;
            _rawBytes = rawBytes;
            _value = value;
            _valueText = valueText;
            _getBody = encoding => NoBytes;
        }

        private ImfHeaderField(string name, ImfHeaderFieldKind kind, byte[] value, Func<ImfHeaderEncoding, byte[]> getBody)
        {
            Name = name;
            BytePosition = -1;
            _nameBytes = ImfHeaderFormatter.GetBytes(name);
            _kind = kind;
            _rawBytes = NoBytes;
            _value = value;
            _valueText = string.Empty;
            _getBody = getBody;
        }

        /// <summary>
        /// Gets the field name, without any white space before the colon.  Field names are case-insensitive.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the field body as a string, unfolded (RFC 5322 §2.2.3): as UTF-8 (RFC 6532), or with <see cref="ImfOptions.HeaderFallbackCharset"/> if it was read
        /// under <see cref="ImfOptions.AllowEightBitHeaders"/> and is not valid UTF-8.  RFC 2047 encoded words are left as they are.
        /// </summary>
        /// <remarks>
        /// The body of a field that was read keeps its white space, including any after the colon.  The body of a field made from text or a typed value is the form
        /// it has with <see cref="ImfHeaderEncoding.EncodedWords"/>.
        /// </remarks>
        public string Value
        {
            get
            {
                if (_valueText.Length == 0 && _value.Length > 0)
                {
                    _valueText = Encoding.UTF8.GetString(_value, 0, _value.Length);
                }

                return _valueText;
            }
        }

        /// <summary>
        /// Gets the bytes of <see cref="Value"/>, unfolded, as they were read.
        /// </summary>
        public ReadOnlyMemory<byte> ValueBytes => _value;

        /// <summary>
        /// Gets the whole field exactly as it was read: its name, any white space before the colon, its folds and its final line break.  It is empty for a field
        /// that was not read, which a writer formats.
        /// </summary>
        public ReadOnlyMemory<byte> RawBytes => _rawBytes;

        /// <summary>
        /// Gets the 0-based offset, in bytes from the start of the data, of the first byte of the field, or -1 for a field that was not read.
        /// </summary>
        internal long BytePosition { get; }

        /// <summary>
        /// Gets the 1-based number of the physical line on which the field starts, or 0 for a field that was not read.
        /// </summary>
        internal long LineNumber { get; }

        /// <summary>
        /// Creates a field that holds text, as an unstructured field such as Subject, to be written as <see cref="ImfHeaderWriter.WriteTextField"/> writes it, with
        /// the <see cref="ImfOptions.HeaderEncoding"/> of the writer.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="text">The text.</param>
        /// <returns>The field.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException"><paramref name="name"/> or <paramref name="text"/> is not valid Unicode, or <paramref name="text"/> holds a control
        /// character other than tab.</exception>
        public static ImfHeaderField FromText(string name, string text)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            var nameBytes = ImfHeaderFormatter.GetBytes(name);
            return new ImfHeaderField(name, ImfHeaderFieldKind.Unstructured, ImfHeaderFormatter.GetTextBody(nameBytes, text, ImfHeaderEncoding.EncodedWords),
                encoding => ImfHeaderFormatter.GetTextBody(nameBytes, text, encoding));
        }

        /// <summary>
        /// Creates a field from its raw bytes, to be written byte for byte, including its folding and its final line break.
        /// </summary>
        /// <param name="rawField">The bytes of one whole field.</param>
        /// <returns>The field.</returns>
        /// <exception cref="ImfFileException">The bytes are not one whole field, as an <see cref="ImfHeaderReader"/> would read one.</exception>
        public static ImfHeaderField FromRaw(ReadOnlyMemory<byte> rawField)
        {
            return FromRaw(rawField, ImfHeaderText.DefaultOptions);
        }

        /// <summary>
        /// Creates a field from its raw bytes, to be written byte for byte, including its folding and its final line break.
        /// </summary>
        /// <param name="rawField">The bytes of one whole field.</param>
        /// <param name="options">The options to read the bytes under.</param>
        /// <returns>The field.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has a limit that is less than 1, no <see cref="ImfOptions.CharsetResolver"/>, or an
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="ImfFileException">The bytes are not one whole field, as an <see cref="ImfHeaderReader"/> would read one under the options.</exception>
        public static ImfHeaderField FromRaw(ReadOnlyMemory<byte> rawField, ImfOptions options)
        {
            using (var stream = new MemoryStream(rawField.ToArray()))
            using (var reader = new ImfHeaderReader(stream, options))
            {
                var view = reader.ReadField();
                if (view is null)
                    throw new ImfFileException("A raw header field must start with a field name and a colon (RFC 5322 §3.6.8).");

                var field = new ImfHeaderField(view.Value);
                if (reader.BytePosition != rawField.Length)
                    throw new ImfFileException("A raw header field must be one field: each line after the first must start with a space or tab (RFC 5322 §2.2.3).");

                return field;
            }
        }

        /// <summary>
        /// Decodes <see cref="Value"/> as the body of an unstructured field, such as Subject or Comments, or a field of unknown meaning, as
        /// <see cref="ImfHeaderText.DecodeUnstructured(string)"/> does.
        /// </summary>
        /// <returns>The text.</returns>
        public string GetDecodedValue()
        {
            return GetDecodedValue(ImfHeaderText.DefaultOptions);
        }

        /// <summary>
        /// Decodes <see cref="Value"/> as the body of an unstructured field, such as Subject or Comments, or a field of unknown meaning, as
        /// <see cref="ImfHeaderText.DecodeUnstructured(string, ImfOptions)"/> does.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>The text.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public string GetDecodedValue(ImfOptions options)
        {
            return ImfHeaderText.DecodeUnstructured(Value, options);
        }

        /// <summary>
        /// Determines if the field has a name, which is case-insensitive.
        /// </summary>
        internal bool HasName(string name)
        {
            return string.Equals(Name, name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a field whose body comes from a typed value, as the <see cref="ImfOptions.HeaderEncoding"/> of the writer asks.
        /// </summary>
        internal static ImfHeaderField FromValue(string name, Func<ImfHeaderEncoding, string> format)
        {
            return new ImfHeaderField(name, ImfHeaderFieldKind.Structured, ImfHeaderFormatter.GetBytes(format(ImfHeaderEncoding.EncodedWords)),
                encoding => ImfHeaderFormatter.GetBytes(format(encoding)));
        }

        /// <summary>
        /// Gets the bytes a writer writes for the field: those it was read with, or a new formatting of it.
        /// </summary>
        /// <exception cref="ImfFileException">The field can't be written.</exception>
        internal ReadOnlyMemory<byte> GetOutput(ImfHeaderEncoding encoding, byte[] lineBreak, ref byte[] buffer)
        {
            if (_rawBytes.Length > 0) return _rawBytes;

            var length = ImfHeaderFormatter.Format(_nameBytes, _getBody(encoding), _kind, encoding, lineBreak, ref buffer);
            var output = new byte[length];
            Array.Copy(buffer, output, length);
            return output;
        }
    }
}
