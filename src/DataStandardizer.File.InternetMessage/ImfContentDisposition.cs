using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The value of a Content-Disposition header field (RFC 2183), such as <c>attachment; filename=genome.jpeg</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type and the parameter names are compared case-insensitively, parameter values case-sensitively, and the order of the parameters doesn't matter.  Parameters
    /// in the forms of RFC 2231 are joined and decoded when read, and each is written as <see cref="ImfMimeFields.FormatContentType"/> writes one.
    /// </para>
    /// <para>
    /// A file name comes from whoever sent the message, so treat it with care.  RFC 2183 §2.3 says not to respect any directory path in it, and §5 warns against a name
    /// that overwrites a file or gets a program run.  It isn't checked here.
    /// </para>
    /// </remarks>
    public sealed class ImfContentDisposition : IEquatable<ImfContentDisposition>
    {
        private const string InlineType = "inline";
        private const string AttachmentType = "attachment";
        private const string FileNameParameterName = "filename";
        private const string CreationDateParameterName = "creation-date";
        private const string ModificationDateParameterName = "modification-date";
        private const string ReadDateParameterName = "read-date";
        private const string SizeParameterName = "size";
        private const string InvalidFormatMessage = "The Content-Disposition field body is not in a valid format.";

        // The media type parser and type take a type and a subtype.  A disposition type is held as the subtype, under this type, which is never seen.
        private const string HolderType = "x";
        private const string HolderPrefix = HolderType + "/";

        /// <summary>
        /// The <c>inline</c> disposition (RFC 2183 §2.1), with no parameters.
        /// </summary>
        public static readonly ImfContentDisposition Inline = new ImfContentDisposition(InlineType);

        /// <summary>
        /// The <c>attachment</c> disposition (RFC 2183 §2.2), with no parameters.
        /// </summary>
        public static readonly ImfContentDisposition Attachment = new ImfContentDisposition(AttachmentType);

        private readonly MimeMediaType _value;
        private readonly TypedParameters _typed;

        /// <summary>
        /// Initializes a new disposition with no parameters.
        /// </summary>
        /// <param name="type">The disposition type, such as <c>attachment</c>, which must follow the RFC 6838 §4.2 naming rules.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="type"/> is not a valid name.</exception>
        public ImfContentDisposition(string type)
            : this(type, new MimeParameter[0])
        {
        }

        /// <summary>
        /// Initializes a new disposition.
        /// </summary>
        /// <param name="type">The disposition type, such as <c>attachment</c>, which must follow the RFC 6838 §4.2 naming rules.</param>
        /// <param name="parameters">The parameters, in the order they are written.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="type"/> is not a valid name, or <paramref name="parameters"/> holds a parameter that is not
        /// initialised, more than one parameter with the same name, or a date or size that isn't valid (RFC 2183 §2).</exception>
        public ImfContentDisposition(string type, IEnumerable<MimeParameter> parameters)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            if (parameters is null) throw new ArgumentNullException(nameof(parameters));

            // This takes the type alone, so a type that holds parameters of its own isn't valid.
            if (!MimeMediaType.TryParse(HolderPrefix + type, out var holder) || holder.Parameters.Count != 0)
                throw new ArgumentException("The disposition type is not a valid name.", nameof(type));

            _value = new MimeMediaType(HolderType, type, parameters);
            if (!TypedParameters.TryRead(_value, false, false, out _typed))
                throw new ArgumentException("A date or size parameter is not valid (RFC 2183 §2).", nameof(parameters));
        }

        private ImfContentDisposition(MimeMediaType value, TypedParameters typed)
        {
            _value = value;
            _typed = typed;
        }

        /// <summary>
        /// Gets the disposition type, as written, such as <c>attachment</c>.
        /// </summary>
        public string Type => _value.Subtype;

        /// <summary>
        /// Gets whether the type is <c>inline</c>, compared case-insensitively.
        /// </summary>
        public bool IsInline => string.Equals(Type, InlineType, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets whether to treat the entity as an attachment: its type is anything other than <c>inline</c>, as RFC 2183 §2.8 says of a type that isn't recognised.
        /// </summary>
        public bool IsAttachment => !IsInline;

        /// <summary>
        /// Gets all the parameters, in the order they are written, including those that have properties here.
        /// </summary>
        public IReadOnlyList<MimeParameter> Parameters => _value.Parameters;

        /// <summary>
        /// Gets the suggested file name (RFC 2183 §2.3), or <c>null</c> if there is none.  See the remarks on this type before using it.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FileName => _typed.FileName;
#else
        public string FileName => _typed.FileName;
#endif

        /// <summary>
        /// Gets the date the file was created (RFC 2183 §2.4), or <c>null</c> if there is none, or it isn't valid and <see cref="ImfOptions.AllowMalformedParameters"/> kept it.
        /// </summary>
        public ImfDateTime? CreationDate => _typed.CreationDate;

        /// <summary>
        /// Gets the date the file was last modified (RFC 2183 §2.5), or <c>null</c> if there is none, or it isn't valid and <see cref="ImfOptions.AllowMalformedParameters"/> kept it.
        /// </summary>
        public ImfDateTime? ModificationDate => _typed.ModificationDate;

        /// <summary>
        /// Gets the date the file was last read (RFC 2183 §2.6), or <c>null</c> if there is none, or it isn't valid and <see cref="ImfOptions.AllowMalformedParameters"/> kept it.
        /// </summary>
        public ImfDateTime? ReadDate => _typed.ReadDate;

        /// <summary>
        /// Gets the approximate size of the file in octets (RFC 2183 §2.7), or <c>null</c> if there is none, or it isn't valid and
        /// <see cref="ImfOptions.AllowMalformedParameters"/> kept it.
        /// </summary>
        public long? Size => _typed.Size;

        /// <summary>
        /// Parses the body of a Content-Disposition field.
        /// </summary>
        /// <param name="value">The field body as UTF-8, which may be folded and have comments, such as <c>attachment; filename=genome.jpeg</c>.</param>
        /// <returns>The disposition.</returns>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParse(ReadOnlySpan{byte}, ImfOptions, out ImfContentDisposition)"/> describes.</exception>
        public static ImfContentDisposition Parse(ReadOnlySpan<byte> value)
        {
            return Parse(value, ImfHeaderText.DefaultOptions);
        }

        /// <summary>
        /// Parses the body of a Content-Disposition field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>attachment; filename=genome.jpeg</c>.  Bytes that aren't valid UTF-8 are
        /// decoded with <see cref="ImfOptions.HeaderFallbackCharset"/>, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowInvalidValues"/>,
        /// <see cref="ImfOptions.AllowMalformedParameters"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <returns>The disposition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParse(ReadOnlySpan{byte}, ImfOptions, out ImfContentDisposition)"/> describes.</exception>
        public static ImfContentDisposition Parse(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a Content-Disposition field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>attachment; filename=genome.jpeg</c>.</param>
        /// <returns>The disposition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParse(ReadOnlySpan{byte}, ImfOptions, out ImfContentDisposition)"/> describes.</exception>
        public static ImfContentDisposition Parse(string value)
        {
            return Parse(value, ImfHeaderText.DefaultOptions);
        }

        /// <summary>
        /// Parses the body of a Content-Disposition field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>attachment; filename=genome.jpeg</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowInvalidValues"/>, <see cref="ImfOptions.AllowMalformedParameters"/> and
        /// <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <returns>The disposition.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParse(ReadOnlySpan{byte}, ImfOptions, out ImfContentDisposition)"/> describes.</exception>
        public static ImfContentDisposition Parse(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, options, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a Content-Disposition field.
        /// </summary>
        /// <param name="value">The field body as UTF-8, which may be folded and have comments, such as <c>attachment; filename=genome.jpeg</c>.</param>
        /// <param name="result">The disposition, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid, as <see cref="TryParse(ReadOnlySpan{byte}, ImfOptions, out ImfContentDisposition)"/> describes; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfContentDisposition result)
        {
            return TryParse(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Disposition field.
        /// </summary>
        /// <remarks>
        /// <para>
        /// One semicolon may end the value.  The grammar of RFC 2183 §2 has no place for it, but the example in its §3 has one.  A date has to be a date-time with a numeric
        /// zone (RFC 2183 §2), in the modern or obsolete syntax, and <see cref="ImfOptions.AllowInvalidValues"/> applies to its day of the week as it does in
        /// <see cref="ImfDateTime"/>.  A size has to be digits, and one too large for a <see cref="long"/> is not valid.  A parameter value with raw non-ASCII text is rejected,
        /// as RFC 2183 §2 requires the form of RFC 2231 for one.
        /// </para>
        /// <para>
        /// A value breaks RFC 2231 if its sections or extended value aren't valid, or a character set can't be resolved, and it breaks RFC 2047 §5 if a parameter holds an
        /// encoded word, which is never allowed there.  Those, and a date or size that isn't valid, are rejected unless <see cref="ImfOptions.AllowMalformedParameters"/> is
        /// <c>true</c>, which describes what is then accepted.
        /// </para>
        /// </remarks>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>attachment; filename=genome.jpeg</c>.  Bytes that aren't valid UTF-8 are
        /// decoded with <see cref="ImfOptions.HeaderFallbackCharset"/>, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowInvalidValues"/>,
        /// <see cref="ImfOptions.AllowMalformedParameters"/>, <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <param name="result">The disposition, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out ImfContentDisposition result)
        {
            ImfHeaderText.CheckOptions(options);

            // ref. RFC 2183 §2: disposition := "Content-Disposition" ":" disposition-type *(";" disposition-parm)
            result = null;
            if (!ImfMimeFields.TryParseParameterized(value, HolderPrefix, FileNameParameterName, true, options, out var parsed)) return false;
            if (!TypedParameters.TryRead(parsed, options.AllowInvalidValues, options.AllowMalformedParameters, out var typed)) return false;

            result = new ImfContentDisposition(parsed, typed);
            return true;
        }

        /// <summary>
        /// Parses the body of a Content-Disposition field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>attachment; filename=genome.jpeg</c>.</param>
        /// <param name="result">The disposition, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid, as <see cref="TryParse(ReadOnlySpan{byte}, ImfOptions, out ImfContentDisposition)"/> describes; otherwise <c>false</c>,
        /// including for <c>null</c>.</returns>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out ImfContentDisposition result)
        {
            return TryParse(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Disposition field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>attachment; filename=genome.jpeg</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowInvalidValues"/>, <see cref="ImfOptions.AllowMalformedParameters"/> and
        /// <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <param name="result">The disposition, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid, as <see cref="TryParse(ReadOnlySpan{byte}, ImfOptions, out ImfContentDisposition)"/> describes; otherwise <c>false</c>,
        /// including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, ImfOptions options, [MaybeNullWhen(false)] out ImfContentDisposition result)
        {
            ImfHeaderText.CheckOptions(options);

            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, options, out result);
        }

        /// <summary>
        /// Gets a copy of this disposition with a parameter added, or with the value of the parameter of the same name replaced, which keeps its position.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The modified copy.</returns>
        /// <exception cref="ArgumentException"><paramref name="parameter"/> is not initialised, or is a date or size that isn't valid (RFC 2183 §2).</exception>
        public ImfContentDisposition WithParameter(MimeParameter parameter)
        {
            var added = _value.WithParameter(parameter);
            if (!TypedParameters.TryRead(new MimeMediaType(HolderType, Type, new[] { parameter }), false, false, out _))
                throw new ArgumentException("A date or size parameter is not valid (RFC 2183 §2).", nameof(parameter));

            // A value that was kept with AllowMalformedParameters stays, as this doesn't change it.
            TypedParameters.TryRead(added, true, true, out var typed);
            return new ImfContentDisposition(added, typed);
        }

        /// <summary>
        /// Gets a copy of this disposition with the given file name.
        /// </summary>
        /// <param name="fileName">The file name, without any directory path (RFC 2183 §2.3).  It must not contain an unpaired surrogate.</param>
        /// <returns>The modified copy.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> contains an unpaired surrogate.</exception>
        public ImfContentDisposition WithFileName(string fileName)
        {
            return WithParameter(new MimeParameter(FileNameParameterName, fileName));
        }

        /// <summary>
        /// Gets a copy of this disposition with the given creation date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The modified copy.</returns>
        /// <exception cref="ArgumentException"><paramref name="date"/> was made with <c>default</c>.</exception>
        public ImfContentDisposition WithCreationDate(ImfDateTime date)
        {
            return WithDate(CreationDateParameterName, date, nameof(date));
        }

        /// <summary>
        /// Gets a copy of this disposition with the given modification date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The modified copy.</returns>
        /// <exception cref="ArgumentException"><paramref name="date"/> was made with <c>default</c>.</exception>
        public ImfContentDisposition WithModificationDate(ImfDateTime date)
        {
            return WithDate(ModificationDateParameterName, date, nameof(date));
        }

        /// <summary>
        /// Gets a copy of this disposition with the given read date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>The modified copy.</returns>
        /// <exception cref="ArgumentException"><paramref name="date"/> was made with <c>default</c>.</exception>
        public ImfContentDisposition WithReadDate(ImfDateTime date)
        {
            return WithDate(ReadDateParameterName, date, nameof(date));
        }

        /// <summary>
        /// Gets a copy of this disposition with the given size.
        /// </summary>
        /// <param name="size">The approximate size of the file in octets.</param>
        /// <returns>The modified copy.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
        public ImfContentDisposition WithSize(long size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));

            return WithParameter(new MimeParameter(SizeParameterName, size.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Indicates whether this disposition equals another: the types and parameter names are the same, compared case-insensitively, and so are the parameter values,
        /// compared case-sensitively, in any order.
        /// </summary>
        public bool Equals([AllowNull] ImfContentDisposition other)
        {
            return !(other is null) && _value == other._value;
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is ImfContentDisposition other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Indicates whether two dispositions are equal.
        /// </summary>
        public static bool operator ==([AllowNull] ImfContentDisposition left, [AllowNull] ImfContentDisposition right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two dispositions differ.
        /// </summary>
        public static bool operator !=([AllowNull] ImfContentDisposition left, [AllowNull] ImfContentDisposition right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Writes the disposition as the body of a Content-Disposition field, such as <c>attachment; filename=genome.jpeg</c>.
        /// </summary>
        /// <remarks>
        /// A parameter is written as a token or a quoted-string.  One that isn't US-ASCII, or that has a language, is written as an extended value, and one too long for a
        /// line of 78 characters is split into sections (RFC 2231), as RFC 2183 §2 requires.
        /// </remarks>
        /// <returns>The field body, unfolded, with a space at each place it can be folded, for <see cref="ImfHeaderFieldKind.Structured"/>.</returns>
        public override string ToString()
        {
            var output = new StringBuilder();
            output.Append(Type);
            ImfMimeFields.AppendParameters(Parameters, output);
            return output.ToString();
        }

        private ImfContentDisposition WithDate(string name, ImfDateTime date, string parameterName)
        {
            if (date == default) throw new ArgumentException("The date-time is not initialised.", parameterName);

            return WithParameter(new MimeParameter(name, date.ToString()));
        }

        /// <summary>
        /// The parameters of RFC 2183 §2 as what they stand for.
        /// </summary>
        private struct TypedParameters
        {
#if NETCOREAPP3_0_OR_GREATER
            internal string? FileName;
#else
            internal string FileName;
#endif
            internal ImfDateTime? CreationDate;
            internal ImfDateTime? ModificationDate;
            internal ImfDateTime? ReadDate;
            internal long? Size;

            /// <summary>
            /// Reads the parameters of a value.
            /// </summary>
            /// <param name="value">The value.</param>
            /// <param name="allowInvalidValues">Whether a date may have a day of the week that doesn't match it.</param>
            /// <param name="keepMalformed">Whether a date or size that isn't valid stays only as a parameter, and doesn't make the value invalid.</param>
            /// <param name="result">The parameters.</param>
            /// <returns><c>false</c> if a date or size isn't valid.</returns>
            internal static bool TryRead(MimeMediaType value, bool allowInvalidValues, bool keepMalformed, out TypedParameters result)
            {
                result = default;
                if (value.TryGetParameter(FileNameParameterName, out var fileName)) result.FileName = fileName.Value;

                return TryReadDate(value, CreationDateParameterName, allowInvalidValues, keepMalformed, out result.CreationDate)
                    && TryReadDate(value, ModificationDateParameterName, allowInvalidValues, keepMalformed, out result.ModificationDate)
                    && TryReadDate(value, ReadDateParameterName, allowInvalidValues, keepMalformed, out result.ReadDate)
                    && TryReadSize(value, keepMalformed, out result.Size);
            }

            private static bool TryReadDate(MimeMediaType value, string name, bool allowInvalidValues, bool keepMalformed, out ImfDateTime? date)
            {
                date = null;
                if (!value.TryGetParameter(name, out var parameter)) return true;

                // ref. RFC 2183 §2: quoted-date-time := quoted-string; contents MUST be an RFC 822 date-time; numeric timezones (+HHMM or -HHMM) MUST be used
                if (!ImfGrammar.TryGetBytes(parameter.Value, out var bytes) || !ImfDateTimeParser.TryParse(bytes, allowInvalidValues, true, out var parsed)) return keepMalformed;

                date = parsed;
                return true;
            }

            private static bool TryReadSize(MimeMediaType value, bool keepMalformed, out long? size)
            {
                size = null;
                if (!value.TryGetParameter(SizeParameterName, out var parameter)) return true;

                // ref. RFC 2183 §2: size-parm := "size" "=" 1*DIGIT
                var digits = parameter.Value;
                if (digits.Length == 0) return keepMalformed;

                long total = 0;
                foreach (var digit in digits)
                {
                    if (digit < '0' || digit > '9' || total > (long.MaxValue - (digit - '0')) / 10) return keepMalformed;

                    total = total * 10 + (digit - '0');
                }

                size = total;
                return true;
            }
        }
    }
}
