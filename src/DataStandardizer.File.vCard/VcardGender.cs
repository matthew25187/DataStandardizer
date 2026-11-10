using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The structured value of the GENDER property (RFC 6350 §6.2.7): a sex and a free-form gender identity, each optional.  Only vCard 4.0 has this property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type holds a copy of the identity.  To read a value without allocating, use
    /// <see cref="TryParseParts(ReadOnlySpan{char}, VcardValueFormat, out VcardSex, out ReadOnlySpan{char})"/>.
    /// </para>
    /// <para>
    /// A default instance is not initialised: <see cref="Sex"/> and <see cref="Identity"/> throw <see cref="InvalidOperationException"/>,
    /// <see cref="TryFormat(Span{char}, out int, VcardValueFormat)"/> writes nothing, and <see cref="ToString()"/> gives the type name.
    /// </para>
    /// <para>
    /// In jCard (RFC 7095 §3.3.1.3) the value is a JSON array with an element for each component, not a single string, so it has no <see cref="VcardValueFormat.Json"/> form of
    /// its own, and parsing or formatting in that form throws <see cref="NotSupportedException"/>.  <c>JcardReader</c> and <c>JcardWriter</c> map the array to and from the text form.
    /// Likewise in xCard (RFC 6351 §5) the value is a tree of XML elements, one for each component, so it has no <see cref="VcardValueFormat.Xml"/> form of its own either, and
    /// <c>XcardReader</c> and <c>XcardWriter</c> map the elements.
    /// </para>
    /// </remarks>
    public readonly struct VcardGender : IEquatable<VcardGender>
    {
        private const string UninitializedMessage = "The VcardGender instance is not initialised.";
        private const int MaxSexLength = 1;

        private readonly byte _sexPlusOne;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly string? _identity;
#else
        private readonly string _identity;
#endif

        /// <summary>
        /// Creates a gender.
        /// </summary>
        /// <param name="sex">The sex.</param>
        /// <param name="identity">The gender identity; or <c>null</c> to leave out the component, written <c>M</c> rather than <c>M;</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sex"/> is not a <see cref="VcardSex"/> member.</exception>
        /// <exception cref="ArgumentException"><paramref name="identity"/> holds a control character other than a tab or a line break, or a lone surrogate.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public VcardGender(VcardSex sex, string? identity)
#else
        public VcardGender(VcardSex sex, string identity)
#endif
        {
            if (sex < VcardSex.Unspecified || sex > VcardSex.Unknown)
                throw new ArgumentOutOfRangeException(nameof(sex), sex, "The sex is not recognised.");
            if (identity != null)
            {
                VcardText.CheckCharacters(identity.AsSpan(), nameof(identity));
            }

            _sexPlusOne = (byte)((int)sex + 1);
            _identity = identity;
        }

        /// <summary>
        /// Gets the sex.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardSex Sex
        {
            get
            {
                CheckInitialized();
                return (VcardSex)(_sexPlusOne - 1);
            }
        }

        /// <summary>
        /// Gets the gender identity; or <c>null</c> if the value leaves out the component.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Identity
#else
        public string Identity
#endif
        {
            get
            {
                CheckInitialized();
                return _identity;
            }
        }

        /// <summary>
        /// Parses a value, such as <c>M</c>, <c>M;Fellow</c> or <c>;it's complicated</c>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The gender, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParse(ReadOnlySpan<char> value, VcardValueFormat format, out VcardGender result)
        {
            result = default(VcardGender);
            VcardSex sex;
            ReadOnlySpan<char> identity;
            if (!TryParseParts(value, format, out sex, out identity)) return false;

            result = new VcardGender(sex, value.IndexOf(';') < 0 ? null : VcardText.Unescape(identity, VcardVersion.V4_0));
            return true;
        }

        /// <summary>
        /// Parses a value without allocating.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="sex">The sex, if the value is valid.</param>
        /// <param name="identity">The gender identity, with its escaping in place, if the value is valid.  It is empty if the value leaves it out.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static bool TryParseParts(ReadOnlySpan<char> value, VcardValueFormat format, out VcardSex sex, out ReadOnlySpan<char> identity)
        {
            VcardValueFormatting.CheckStructuredFormat(format, "A GENDER value");

            // ref. RFC 6350 §6.2.7: GENDER-value = sex [";" text]; sex = "" / "M" / "F" / "O" / "N" / "U"
            sex = VcardSex.Unspecified;
            identity = default(ReadOnlySpan<char>);
            var delimiterIndex = value.IndexOf(';');
            var sexPart = delimiterIndex < 0 ? value : value.Slice(0, delimiterIndex);
            if (sexPart.Length > MaxSexLength || (sexPart.Length == MaxSexLength && !TryParseSex(sexPart[0], out sex))) return false;

            if (delimiterIndex < 0) return true;

            var identityPart = value.Slice(delimiterIndex + 1);
            if (!VcardText.IsValidText(identityPart, VcardVersion.V4_0))
            {
                sex = VcardSex.Unspecified;
                return false;
            }

            identity = identityPart;
            return true;
        }

        /// <summary>
        /// Parses a value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>The gender.</returns>
        /// <exception cref="FormatException">The value is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public static VcardGender Parse(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            VcardGender result;
            if (!TryParse(value, format, out result))
                throw new FormatException("The value is not a valid GENDER value.");

            return result;
        }

        /// <summary>
        /// Formats the value into a span.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short or the instance is not initialised.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            VcardValueFormatting.CheckStructuredFormat(format, "A GENDER value");

            var writer = new VcardSpanWriter(destination);
            if (_sexPlusOne != 0)
            {
                var letter = GetSexLetter((VcardSex)(_sexPlusOne - 1));
                if (letter != '\0')
                {
                    writer.Append(letter);
                }

                if (_identity != null)
                {
                    writer.Append(';');
                    VcardText.AppendEscaped(ref writer, _identity.AsSpan(), VcardVersion.V4_0, false);
                }
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats the value as a string.
        /// </summary>
        /// <param name="format">The lexical form to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is not <see cref="VcardValueFormat.Text"/>.</exception>
        public string ToString(VcardValueFormat format)
        {
            var buffer = new char[MaxSexLength + 1 + (_identity is null ? 0 : VcardText.GetMaxEscapedLength(_identity.Length))];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormat(buffer, out charsWritten, format), charsWritten);
        }

        /// <summary>
        /// Formats the value in <see cref="VcardValueFormat.Text"/> form; or gives the type name if the instance is not initialised.
        /// </summary>
        public override string ToString()
        {
            return _sexPlusOne == 0 ? typeof(VcardGender).FullName ?? nameof(VcardGender) : ToString(VcardValueFormat.Text);
        }

        /// <summary>
        /// Indicates whether this value equals another: the same sex and the same identity, compared ordinally.
        /// </summary>
        public bool Equals(VcardGender other)
        {
            return _sexPlusOne == other._sexPlusOne && string.Equals(_identity, other._identity, StringComparison.Ordinal);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardGender other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _sexPlusOne ^ (_identity is null ? 0 : StringComparer.Ordinal.GetHashCode(_identity));
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(VcardGender left, VcardGender right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(VcardGender left, VcardGender right)
        {
            return !left.Equals(right);
        }

        // ref. RFC 5234 §2.3: ABNF strings are case-insensitive.
        private static bool TryParseSex(char letter, out VcardSex sex)
        {
            switch (letter)
            {
                case 'M':
                case 'm':
                    sex = VcardSex.Male;
                    return true;
                case 'F':
                case 'f':
                    sex = VcardSex.Female;
                    return true;
                case 'O':
                case 'o':
                    sex = VcardSex.Other;
                    return true;
                case 'N':
                case 'n':
                    sex = VcardSex.NotApplicable;
                    return true;
                case 'U':
                case 'u':
                    sex = VcardSex.Unknown;
                    return true;
                default:
                    sex = VcardSex.Unspecified;
                    return false;
            }
        }

        private static char GetSexLetter(VcardSex sex)
        {
            switch (sex)
            {
                case VcardSex.Male:
                    return 'M';
                case VcardSex.Female:
                    return 'F';
                case VcardSex.Other:
                    return 'O';
                case VcardSex.NotApplicable:
                    return 'N';
                case VcardSex.Unknown:
                    return 'U';
                default:
                    return '\0';
            }
        }

        private void CheckInitialized()
        {
            if (_sexPlusOne == 0)
                throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
