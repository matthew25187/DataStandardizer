using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// A media type and its parameters, as in a Content-Type header field (RFC 2045 §5.1), such as <c>text/plain;charset=us-ascii</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type, subtype and parameter names are compared case-insensitively. Parameter values are compared case-sensitively, except for the
    /// value of <c>charset</c>, and the order of the parameters doesn't matter.
    /// </para>
    /// <para>
    /// A media type is written with no whitespace, and with each parameter value as a token if it is one, or otherwise as a quoted-string.
    /// A parameter value that is not US-ASCII, or that has a language, is written as an RFC 2231 extended value.
    /// </para>
    /// <para>
    /// When parsed, a parameter value split into RFC 2231 sections is joined, and an RFC 2231 extended value is decoded, so that each
    /// parameter appears once with its whole value. When a parameter is given both as <c>name</c> and in an RFC 2231 form, such as
    /// <c>name*</c>, the RFC 2231 form is used.
    /// </para>
    /// </remarks>
    public readonly struct MimeMediaType : IEquatable<MimeMediaType>
#if NET7_0_OR_GREATER
        , ISpanFormattable, ISpanParsable<MimeMediaType>
#elif NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string InvalidFormatMessage = "The media type is not in a valid format.";
        private const string UninitializedMessage = nameof(MimeMediaType) + " is uninitialized.";
        private const MimeMediaTypeParseOptions AllParseOptions =
            MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters | MimeMediaTypeParseOptions.AllowRfc2045Tokens |
            MimeMediaTypeParseOptions.AllowNonstandardSections | MimeMediaTypeParseOptions.AllowUnknownCharsets |
            MimeMediaTypeParseOptions.AllowNonstandardExtendedValues;

        private static readonly ReadOnlyCollection<MimeParameter> NoParameters = new ReadOnlyCollection<MimeParameter>(new MimeParameter[0]);

        /// <summary>
        /// <c>text/plain</c> (RFC 2046 §4.1.3).
        /// </summary>
        public static readonly MimeMediaType TextPlain = new MimeMediaType(MimeTopLevelTypes.Text, "plain", NoParameters);

        /// <summary>
        /// <c>text/calendar</c> (RFC 5545 §8.1).
        /// </summary>
        public static readonly MimeMediaType TextCalendar = new MimeMediaType(MimeTopLevelTypes.Text, "calendar", NoParameters);

        /// <summary>
        /// <c>text/vcard</c> (RFC 6350 §10.1).
        /// </summary>
        public static readonly MimeMediaType TextVcard = new MimeMediaType(MimeTopLevelTypes.Text, "vcard", NoParameters);

        /// <summary>
        /// <c>text/directory</c> (RFC 2425 §3).
        /// </summary>
        public static readonly MimeMediaType TextDirectory = new MimeMediaType(MimeTopLevelTypes.Text, "directory", NoParameters);

        /// <summary>
        /// <c>message/rfc822</c> (RFC 2046 §5.2.1).
        /// </summary>
        public static readonly MimeMediaType MessageRfc822 = new MimeMediaType(MimeTopLevelTypes.Message, "rfc822", NoParameters);

        /// <summary>
        /// <c>multipart/mixed</c> (RFC 2046 §5.1.3).
        /// </summary>
        public static readonly MimeMediaType MultipartMixed = new MimeMediaType(MimeTopLevelTypes.Multipart, "mixed", NoParameters);

        /// <summary>
        /// <c>multipart/alternative</c> (RFC 2046 §5.1.4).
        /// </summary>
        public static readonly MimeMediaType MultipartAlternative = new MimeMediaType(MimeTopLevelTypes.Multipart, "alternative", NoParameters);

        /// <summary>
        /// <c>multipart/related</c> (RFC 2387).
        /// </summary>
        public static readonly MimeMediaType MultipartRelated = new MimeMediaType(MimeTopLevelTypes.Multipart, "related", NoParameters);

        /// <summary>
        /// <c>application/octet-stream</c> (RFC 2046 §4.5.1).
        /// </summary>
        public static readonly MimeMediaType ApplicationOctetStream = new MimeMediaType(MimeTopLevelTypes.Application, "octet-stream", NoParameters);

        private readonly string _type;
        private readonly string _subtype;
        private readonly ReadOnlyCollection<MimeParameter> _parameters;

        /// <summary>
        /// Creates a media type with no parameters.
        /// </summary>
        /// <param name="type">The top-level type, such as <c>text</c>, which must follow the RFC 6838 §4.2 naming rules.</param>
        /// <param name="subtype">The subtype, such as <c>plain</c>, which must follow the RFC 6838 §4.2 naming rules.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="subtype"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="type"/> or <paramref name="subtype"/> is not a valid name.</exception>
        public MimeMediaType(string type, string subtype)
            : this(type, subtype, NoParameters)
        {
            CheckNames(type, subtype);
        }

        /// <summary>
        /// Creates a media type.
        /// </summary>
        /// <param name="type">The top-level type, such as <c>text</c>, which must follow the RFC 6838 §4.2 naming rules.</param>
        /// <param name="subtype">The subtype, such as <c>plain</c>, which must follow the RFC 6838 §4.2 naming rules.</param>
        /// <param name="parameters">The parameters, in the order they are written.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="subtype"/> or <paramref name="parameters"/> is
        /// <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="type"/> or <paramref name="subtype"/> is not a valid name, or
        /// <paramref name="parameters"/> contains an uninitialised parameter or more than one parameter with the same name.</exception>
        public MimeMediaType(string type, string subtype, IEnumerable<MimeParameter> parameters)
            : this(type, subtype, CreateParameters(parameters))
        {
            CheckNames(type, subtype);
        }

        private MimeMediaType(string type, string subtype, ReadOnlyCollection<MimeParameter> parameters)
        {
            _type = type;
            _subtype = subtype;
            _parameters = parameters;
        }

        /// <summary>
        /// Gets the top-level type, such as <c>text</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public string Type => _type ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Gets the subtype, such as <c>plain</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public string Subtype => _subtype ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Gets the structured syntax suffix of the subtype (RFC 6839), such as <c>json</c> in <c>application/ld+json</c>.
        /// </summary>
        /// <returns>The text after the last <c>+</c> in the subtype; an empty string if the subtype has no suffix.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public string Suffix
        {
            get
            {
                var subtype = Subtype;
                var index = subtype.LastIndexOf('+');
                return index > 0 && index < subtype.Length - 1 ? subtype.Substring(index + 1) : string.Empty;
            }
        }

        /// <summary>
        /// Gets the registration tree, shown by the prefix of the subtype (RFC 6838 §3).
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public MimeRegistrationTree Tree
        {
            get
            {
                var subtype = Subtype;
                if (MimeGrammar.StartsWithIgnoreCase(subtype, "vnd.")) return MimeRegistrationTree.Vendor;
                if (MimeGrammar.StartsWithIgnoreCase(subtype, "prs.")) return MimeRegistrationTree.Personal;
                if (MimeGrammar.StartsWithIgnoreCase(subtype, "x.") || MimeGrammar.StartsWithIgnoreCase(subtype, "x-")) return MimeRegistrationTree.Unregistered;

                return MimeRegistrationTree.Standards;
            }
        }

        /// <summary>
        /// Gets the parameters, in the order they are written.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public IReadOnlyList<MimeParameter> Parameters => _parameters ?? throw new InvalidOperationException(UninitializedMessage);

        internal bool IsInitialized => _type != null;

        /// <summary>
        /// Gets a parameter by its name, compared case-insensitively.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="parameter">The parameter, if there is one with the name.</param>
        /// <returns><c>true</c> if there is a parameter with the name; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public bool TryGetParameter(string name, out MimeParameter parameter)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

            var index = IndexOfParameter(Parameters, name);
            parameter = index < 0 ? default(MimeParameter) : _parameters[index];
            return index >= 0;
        }

        /// <summary>
        /// Gets a copy of the media type with a parameter added, or with the value of the parameter of the same name replaced.
        /// </summary>
        /// <param name="parameter">The parameter. A parameter of the same name, compared case-insensitively, keeps its position.</param>
        /// <returns>The modified copy.</returns>
        /// <exception cref="ArgumentException"><paramref name="parameter"/> is not initialised.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public MimeMediaType WithParameter(MimeParameter parameter)
        {
            if (!parameter.IsInitialized) throw new ArgumentException("The parameter is not initialised.", nameof(parameter));

            var parameters = Parameters;
            var index = IndexOfParameter(parameters, parameter.Name);
            var copy = new MimeParameter[index < 0 ? parameters.Count + 1 : parameters.Count];
            _parameters.CopyTo(copy, 0);
            copy[index < 0 ? parameters.Count : index] = parameter;
            return new MimeMediaType(_type, _subtype, new ReadOnlyCollection<MimeParameter>(copy));
        }

        /// <summary>
        /// Gets a copy of the media type with a parameter added, or with the value of the parameter of the same name replaced.
        /// </summary>
        /// <param name="name">The parameter name, which must follow the RFC 6838 §4.2 naming rules. A parameter of the same name, compared
        /// case-insensitively, keeps its position.</param>
        /// <param name="value">The parameter value, which may be empty. It must not contain an unpaired surrogate.</param>
        /// <returns>The modified copy.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid parameter name, or <paramref name="value"/> contains an
        /// unpaired surrogate.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public MimeMediaType WithParameter(string name, string value)
        {
            return WithParameter(new MimeParameter(name, value));
        }

        /// <summary>
        /// Gets a copy of the media type without a parameter.
        /// </summary>
        /// <param name="name">The parameter name, compared case-insensitively.</param>
        /// <returns>The modified copy; this media type if it has no parameter with the name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public MimeMediaType WithoutParameter(string name)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));

            var parameters = Parameters;
            var index = IndexOfParameter(parameters, name);
            if (index < 0) return this;

            var copy = new MimeParameter[parameters.Count - 1];
            for (int sourceIndex = 0, copyIndex = 0; sourceIndex < parameters.Count; sourceIndex++)
            {
                if (sourceIndex != index) copy[copyIndex++] = parameters[sourceIndex];
            }

            return new MimeMediaType(_type, _subtype, copy.Length == 0 ? NoParameters : new ReadOnlyCollection<MimeParameter>(copy));
        }

        /// <summary>
        /// Gets a copy of the media type without any parameters, such as to compare only the type and subtype.
        /// </summary>
        /// <returns>The modified copy.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeMediaType"/> is not initialised.</exception>
        public MimeMediaType WithoutParameters()
        {
            if (_type is null) throw new InvalidOperationException(UninitializedMessage);

            return new MimeMediaType(_type, _subtype, NoParameters);
        }

        /// <summary>
        /// Parses a media type strictly.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments or whitespace.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid media type; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, out MimeMediaType result)
        {
            return TryParse(value, MimeMediaTypeParseOptions.None, out result);
        }

        /// <summary>
        /// Parses a media type.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments.</param>
        /// <param name="options">The accommodations to make for media types that don't follow the strict grammar.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid media type; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a combination of
        /// <see cref="MimeMediaTypeParseOptions"/> members.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, MimeMediaTypeParseOptions options, out MimeMediaType result)
        {
            return TryParse(value, options, MimeCharsetResolver.Default, out result);
        }

        /// <summary>
        /// Parses a media type, decoding RFC 2231 extended parameter values with a given character set resolver.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments.</param>
        /// <param name="options">The accommodations to make for media types that don't follow the strict grammar.</param>
        /// <param name="charsetResolver">The resolver for the character sets of extended parameter values.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid media type; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a combination of
        /// <see cref="MimeMediaTypeParseOptions"/> members.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, MimeMediaTypeParseOptions options, IMimeCharsetResolver charsetResolver,
            out MimeMediaType result)
        {
            CheckOptions(options);
            if (charsetResolver is null) throw new ArgumentNullException(nameof(charsetResolver));

            return TryParseCore(value, options, charsetResolver, out result);
        }

        /// <summary>
        /// Parses a media type strictly.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments or whitespace.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid media type; otherwise <c>false</c>, including for <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? value, out MimeMediaType result)
#else
        public static bool TryParse(string value, out MimeMediaType result)
#endif
        {
            return TryParse(value, MimeMediaTypeParseOptions.None, out result);
        }

        /// <summary>
        /// Parses a media type.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments.</param>
        /// <param name="options">The accommodations to make for media types that don't follow the strict grammar.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid media type; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a combination of
        /// <see cref="MimeMediaTypeParseOptions"/> members.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? value, MimeMediaTypeParseOptions options, out MimeMediaType result)
#else
        public static bool TryParse(string value, MimeMediaTypeParseOptions options, out MimeMediaType result)
#endif
        {
            return TryParse(value, options, MimeCharsetResolver.Default, out result);
        }

        /// <summary>
        /// Parses a media type, decoding RFC 2231 extended parameter values with a given character set resolver.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments.</param>
        /// <param name="options">The accommodations to make for media types that don't follow the strict grammar.</param>
        /// <param name="charsetResolver">The resolver for the character sets of extended parameter values.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid media type; otherwise <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a combination of
        /// <see cref="MimeMediaTypeParseOptions"/> members.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? value, MimeMediaTypeParseOptions options, IMimeCharsetResolver charsetResolver,
            out MimeMediaType result)
#else
        public static bool TryParse(string value, MimeMediaTypeParseOptions options, IMimeCharsetResolver charsetResolver,
            out MimeMediaType result)
#endif
        {
            CheckOptions(options);
            if (charsetResolver is null) throw new ArgumentNullException(nameof(charsetResolver));

            if (value is null)
            {
                result = default(MimeMediaType);
                return false;
            }

            return TryParseCore(value.AsSpan(), options, charsetResolver, out result);
        }

        /// <summary>
        /// Parses a media type strictly.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments or whitespace.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="FormatException">The value is not a valid media type.</exception>
        public static MimeMediaType Parse(ReadOnlySpan<char> value)
        {
            return Parse(value, MimeMediaTypeParseOptions.None);
        }

        /// <summary>
        /// Parses a media type.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments.</param>
        /// <param name="options">The accommodations to make for media types that don't follow the strict grammar.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="FormatException">The value is not a valid media type.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a combination of
        /// <see cref="MimeMediaTypeParseOptions"/> members.</exception>
        public static MimeMediaType Parse(ReadOnlySpan<char> value, MimeMediaTypeParseOptions options)
        {
            return Parse(value, options, MimeCharsetResolver.Default);
        }

        /// <summary>
        /// Parses a media type, decoding RFC 2231 extended parameter values with a given character set resolver.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments.</param>
        /// <param name="options">The accommodations to make for media types that don't follow the strict grammar.</param>
        /// <param name="charsetResolver">The resolver for the character sets of extended parameter values.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid media type.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a combination of
        /// <see cref="MimeMediaTypeParseOptions"/> members.</exception>
        public static MimeMediaType Parse(ReadOnlySpan<char> value, MimeMediaTypeParseOptions options, IMimeCharsetResolver charsetResolver)
        {
            MimeMediaType result;
            if (!TryParse(value, options, charsetResolver, out result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a media type strictly.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments or whitespace.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid media type.</exception>
        public static MimeMediaType Parse(string value)
        {
            return Parse(value, MimeMediaTypeParseOptions.None);
        }

        /// <summary>
        /// Parses a media type.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments.</param>
        /// <param name="options">The accommodations to make for media types that don't follow the strict grammar.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid media type.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a combination of
        /// <see cref="MimeMediaTypeParseOptions"/> members.</exception>
        public static MimeMediaType Parse(string value, MimeMediaTypeParseOptions options)
        {
            return Parse(value, options, MimeCharsetResolver.Default);
        }

        /// <summary>
        /// Parses a media type, decoding RFC 2231 extended parameter values with a given character set resolver.
        /// </summary>
        /// <param name="value">The media type, such as <c>text/plain;charset=us-ascii</c>, with no comments.</param>
        /// <param name="options">The accommodations to make for media types that don't follow the strict grammar.</param>
        /// <param name="charsetResolver">The resolver for the character sets of extended parameter values.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="charsetResolver"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid media type.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> is not a combination of
        /// <see cref="MimeMediaTypeParseOptions"/> members.</exception>
        public static MimeMediaType Parse(string value, MimeMediaTypeParseOptions options, IMimeCharsetResolver charsetResolver)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            return Parse(value.AsSpan(), options, charsetResolver);
        }

#if NET7_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The media type is parsed strictly. The provider is ignored.</remarks>
        static MimeMediaType IParsable<MimeMediaType>.Parse(string s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The media type is parsed strictly. The provider is ignored.</remarks>
        static bool IParsable<MimeMediaType>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MimeMediaType result)
        {
            return TryParse(s, out result);
        }

        /// <inheritdoc />
        /// <remarks>The media type is parsed strictly. The provider is ignored.</remarks>
        static MimeMediaType ISpanParsable<MimeMediaType>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The media type is parsed strictly. The provider is ignored.</remarks>
        static bool ISpanParsable<MimeMediaType>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MimeMediaType result)
        {
            return TryParse(s, out result);
        }
#endif

        /// <summary>
        /// Formats the media type into a span, such as <c>text/plain;charset=us-ascii</c>.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <returns><c>true</c> if the media type was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <remarks>
        /// Nothing is written if the <see cref="MimeMediaType"/> is not initialised.
        /// </remarks>
        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            var position = 0;
            if (!TryWrite(destination, ref position))
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = position;
            return true;
        }

        /// <summary>
        /// Formats the media type, such as <c>text/plain;charset=us-ascii</c>.
        /// </summary>
        /// <returns>The media type; an empty string if the <see cref="MimeMediaType"/> is not initialised.</returns>
        public override string ToString()
        {
            if (_type is null) return string.Empty;

            var buffer = new char[GetFormattedLength()];
            var position = 0;
            TryWrite(buffer, ref position);
            return new string(buffer, 0, position);
        }

#if NET6_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as a media type has only one form.</remarks>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten);
        }

        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as a media type has only one form.</remarks>
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }
#endif

        /// <summary>
        /// Indicates whether this media type equals another.
        /// </summary>
        /// <remarks>
        /// The type, subtype and parameter names are compared case-insensitively. Parameter values are compared case-sensitively, except for
        /// the value of <c>charset</c>, and the order of the parameters doesn't matter.
        /// </remarks>
        public bool Equals(MimeMediaType other)
        {
            if (!string.Equals(_type, other._type, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(_subtype, other._subtype, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (_parameters is null || other._parameters is null) return _parameters is null && other._parameters is null;
            if (_parameters.Count != other._parameters.Count) return false;

            // Names are unique, so each parameter has at most one counterpart.
            foreach (var parameter in _parameters)
            {
                var index = IndexOfParameter(other._parameters, parameter.Name);
                if (index < 0 || !parameter.Equals(other._parameters[index])) return false;
            }

            return true;
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is MimeMediaType other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Must agree with Equals, which compares names case-insensitively and ignores the order of the parameters.
            if (_type is null) return 0;

            unchecked
            {
                var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(_type) * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(_subtype);
                var parametersHash = 0;
                foreach (var parameter in _parameters)
                {
                    parametersHash += parameter.GetHashCode();
                }

                return hash * 31 + parametersHash;
            }
        }

        /// <summary>
        /// Indicates whether two media types are equal.
        /// </summary>
        public static bool operator ==(MimeMediaType left, MimeMediaType right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two media types differ.
        /// </summary>
        public static bool operator !=(MimeMediaType left, MimeMediaType right)
        {
            return !left.Equals(right);
        }

        private int GetFormattedLength()
        {
            var length = _type.Length + 1 + _subtype.Length;
            foreach (var parameter in _parameters)
            {
                length += 1 + parameter.GetFormattedLength();
            }

            return length;
        }

        private bool TryWrite(Span<char> destination, ref int position)
        {
            if (_type is null) return true;

            if (!MimeGrammar.TryWrite(_type, destination, ref position) ||
                !MimeGrammar.TryWrite('/', destination, ref position) ||
                !MimeGrammar.TryWrite(_subtype, destination, ref position))
            {
                return false;
            }

            foreach (var parameter in _parameters)
            {
                if (!MimeGrammar.TryWrite(';', destination, ref position) || !parameter.TryWrite(destination, ref position)) return false;
            }

            return true;
        }

        private static bool TryParseCore(ReadOnlySpan<char> value, MimeMediaTypeParseOptions options, IMimeCharsetResolver charsetResolver,
            out MimeMediaType result)
        {
            // ref. RFC 2045 §5.1: content := "Content-Type" ":" type "/" subtype *(";" parameter)
            //                     parameter := attribute "=" value; attribute := token; value := token / quoted-string
            result = default(MimeMediaType);
            var allowWhitespace = (options & MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters) != 0;
            var allowRfc2045Tokens = (options & MimeMediaTypeParseOptions.AllowRfc2045Tokens) != 0;
            var position = 0;

            var type = ReadToken(value, ref position);
            if (!MimeGrammar.IsName(type, allowRfc2045Tokens) || position >= value.Length || value[position] != '/') return false;

            position++;
            var subtype = ReadToken(value, ref position);
            if (!MimeGrammar.IsName(subtype, allowRfc2045Tokens)) return false;

            var rawParameters = default(List<MimeExtendedParameter.RawParameter>);
            while (true)
            {
                if (allowWhitespace) SkipWhitespace(value, ref position);
                if (position >= value.Length) break;
                if (value[position] != ';') return false;

                position++;
                if (allowWhitespace) SkipWhitespace(value, ref position);

                // ref. RFC 2231 §7: parameter := regular-parameter / extended-parameter
                var name = ReadToken(value, ref position);
                var rawParameter = default(MimeExtendedParameter.RawParameter);
                int baseNameLength;
                MimeExtendedParameter.SplitName(name, out baseNameLength, ref rawParameter);

                var baseName = name.Slice(0, baseNameLength);
                if (!MimeGrammar.IsName(baseName, allowRfc2045Tokens)) return false;

                if (allowWhitespace) SkipWhitespace(value, ref position);
                if (position >= value.Length || value[position] != '=') return false;

                position++;
                if (allowWhitespace) SkipWhitespace(value, ref position);

                string parameterValue;
                if (position < value.Length && value[position] == '"')
                {
                    if (!MimeGrammar.TryReadQuotedString(value, ref position, out parameterValue)) return false;

                    rawParameter.IsQuoted = true;
                }
                else
                {
                    var token = ReadToken(value, ref position);
                    if (token.IsEmpty) return false;

                    parameterValue = token.ToString();
                }

                rawParameter.Name = baseName.ToString();
                rawParameter.Value = parameterValue;
                if (rawParameters is null) rawParameters = new List<MimeExtendedParameter.RawParameter>();
                rawParameters.Add(rawParameter);
            }

            ReadOnlyCollection<MimeParameter> parameters;
            if (!TryJoinParameters(rawParameters, options, charsetResolver, out parameters)) return false;

            result = new MimeMediaType(type.ToString(), subtype.ToString(), parameters);
            return true;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static bool TryJoinParameters(List<MimeExtendedParameter.RawParameter>? rawParameters, MimeMediaTypeParseOptions options,
            IMimeCharsetResolver charsetResolver, out ReadOnlyCollection<MimeParameter> parameters)
#else
        private static bool TryJoinParameters(List<MimeExtendedParameter.RawParameter> rawParameters, MimeMediaTypeParseOptions options,
            IMimeCharsetResolver charsetResolver, out ReadOnlyCollection<MimeParameter> parameters)
#endif
        {
            parameters = NoParameters;
            if (rawParameters is null) return true;

            // Each parameter takes the place of the first of its forms.
            var list = new List<MimeParameter>();
            var isJoined = new bool[rawParameters.Count];
            for (var index = 0; index < rawParameters.Count; index++)
            {
                if (isJoined[index]) continue;

                var name = rawParameters[index].Name;
                var plainIndex = -1;
                var forms = default(List<MimeExtendedParameter.RawParameter>);
                for (var formIndex = index; formIndex < rawParameters.Count; formIndex++)
                {
                    var form = rawParameters[formIndex];
                    if (!string.Equals(form.Name, name, StringComparison.OrdinalIgnoreCase)) continue;

                    isJoined[formIndex] = true;
                    if (form.IsRfc2231)
                    {
                        if (forms is null) forms = new List<MimeExtendedParameter.RawParameter>();
                        forms.Add(form);
                    }
                    else
                    {
                        // ref. RFC 6838 §4.3: it is an error for a specific parameter to be specified more than once.
                        if (plainIndex >= 0) return false;

                        plainIndex = formIndex;
                    }
                }

                // ref. RFC 2231 §4: the RFC 2231 form is used in preference to a plain one, which is there for readers that don't know it.
                MimeParameter parameter;
                if (forms is null)
                {
                    parameter = MimeParameter.CreateUnchecked(name, rawParameters[plainIndex].Value, null, null);
                }
                else if (!MimeExtendedParameter.TryJoin(forms, options, charsetResolver, out parameter))
                {
                    return false;
                }

                list.Add(parameter);
            }

            parameters = new ReadOnlyCollection<MimeParameter>(list);
            return true;
        }

        private static ReadOnlySpan<char> ReadToken(ReadOnlySpan<char> value, ref int position)
        {
            var start = position;
            while (position < value.Length && MimeGrammar.IsTokenChar(value[position]))
            {
                position++;
            }

            return value.Slice(start, position - start);
        }

        private static void SkipWhitespace(ReadOnlySpan<char> value, ref int position)
        {
            while (position < value.Length && MimeGrammar.IsWhitespace(value[position]))
            {
                position++;
            }
        }

        private static int IndexOfParameter(IReadOnlyList<MimeParameter> parameters, string name)
        {
            for (var index = 0; index < parameters.Count; index++)
            {
                if (parameters[index].HasName(name)) return index;
            }

            return -1;
        }

        private static void CheckNames(string type, string subtype)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            if (subtype is null) throw new ArgumentNullException(nameof(subtype));
            if (!MimeGrammar.IsRestrictedName(type.AsSpan())) throw new ArgumentException("The type is not a valid name.", nameof(type));
            if (!MimeGrammar.IsRestrictedName(subtype.AsSpan())) throw new ArgumentException("The subtype is not a valid name.", nameof(subtype));
        }

        private static void CheckOptions(MimeMediaTypeParseOptions options)
        {
            if ((options & ~AllParseOptions) != 0) throw new ArgumentOutOfRangeException(nameof(options), options, "The options are not valid.");
        }

        private static ReadOnlyCollection<MimeParameter> CreateParameters(IEnumerable<MimeParameter> parameters)
        {
            if (parameters is null) throw new ArgumentNullException(nameof(parameters));

            var list = new List<MimeParameter>(parameters);
            for (var index = 0; index < list.Count; index++)
            {
                if (!list[index].IsInitialized) throw new ArgumentException("A parameter is not initialised.", nameof(parameters));
                if (IndexOfParameter(list, list[index].Name) < index) throw new ArgumentException("More than one parameter has the same name.", nameof(parameters));
            }

            return list.Count == 0 ? NoParameters : new ReadOnlyCollection<MimeParameter>(list);
        }
    }
}
