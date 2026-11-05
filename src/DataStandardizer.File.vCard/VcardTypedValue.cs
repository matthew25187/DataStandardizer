using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The value of a property that can have more than one value type: <c>TEL</c>, <c>UID</c>, <c>KEY</c>, <c>RELATED</c>, <c>BIRTHPLACE</c> and <c>DEATHPLACE</c> (text or URI),
    /// <c>BDAY</c>, <c>ANNIVERSARY</c> and <c>DEATHDATE</c> (date-and-or-time or text), and <c>TZ</c> (text, URI or UTC offset).
    /// </summary>
    /// <remarks>
    /// <see cref="ValueType"/> says which it holds, and asking for another throws <see cref="InvalidOperationException"/>.  A default instance is not initialised: every member
    /// but the equality members and <see cref="ToString"/> throws <see cref="InvalidOperationException"/>, and <see cref="ToString"/> gives the type name.
    /// </remarks>
    public readonly struct VcardTypedValue : IEquatable<VcardTypedValue>
    {
        private const string UninitializedMessage = "The VcardTypedValue instance is not initialised.";

        private readonly VcardDateAndOrTime _dateAndOrTime;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly string? _text;
        private readonly Uri? _uri;
#else
        private readonly string _text;
        private readonly Uri _uri;
#endif
        private readonly VcardUtcOffset _utcOffset;
        private readonly VcardValueType _valueType;

        /// <summary>
        /// Creates a text value.
        /// </summary>
        /// <param name="text">The text, without any escaping.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        public VcardTypedValue(string text)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            VcardText.CheckCharacters(text.AsSpan(), nameof(text));
            this = default(VcardTypedValue);
            _text = text;
            _valueType = VcardValueType.Text;
        }

        /// <summary>
        /// Creates a URI value.
        /// </summary>
        /// <param name="uri">The URI, which must be absolute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="uri"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is not absolute, or was given with characters a URI value can't hold, such as a space.</exception>
        public VcardTypedValue(Uri uri)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            VcardValue.CheckUri(uri, nameof(uri));
            this = default(VcardTypedValue);
            _uri = uri;
            _valueType = VcardValueType.Uri;
        }

        /// <summary>
        /// Creates a date-and-or-time value.
        /// </summary>
        /// <param name="dateAndOrTime">The date and/or time.</param>
        public VcardTypedValue(VcardDateAndOrTime dateAndOrTime)
        {
            this = default(VcardTypedValue);
            _dateAndOrTime = dateAndOrTime;
            _valueType = VcardValueType.DateAndOrTime;
        }

        /// <summary>
        /// Creates a UTC offset value.
        /// </summary>
        /// <param name="utcOffset">The offset.</param>
        public VcardTypedValue(VcardUtcOffset utcOffset)
        {
            this = default(VcardTypedValue);
            _utcOffset = utcOffset;
            _valueType = VcardValueType.UtcOffset;
        }

        /// <summary>
        /// Gets the date and/or time.
        /// </summary>
        /// <exception cref="InvalidOperationException">The value is of another type, or the instance is not initialised.</exception>
        public VcardDateAndOrTime DateAndOrTime
        {
            get
            {
                CheckValueType(VcardValueType.DateAndOrTime);
                return _dateAndOrTime;
            }
        }

        /// <summary>
        /// Gets the text, without any escaping.
        /// </summary>
        /// <exception cref="InvalidOperationException">The value is of another type, or the instance is not initialised.</exception>
        public string Text
        {
            get
            {
                CheckValueType(VcardValueType.Text);
                return _text ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <exception cref="InvalidOperationException">The value is of another type, or the instance is not initialised.</exception>
        public Uri Uri
        {
            get
            {
                CheckValueType(VcardValueType.Uri);
                return _uri ?? throw new InvalidOperationException(UninitializedMessage);
            }
        }

        /// <summary>
        /// Gets the UTC offset.
        /// </summary>
        /// <exception cref="InvalidOperationException">The value is of another type, or the instance is not initialised.</exception>
        public VcardUtcOffset UtcOffset
        {
            get
            {
                CheckValueType(VcardValueType.UtcOffset);
                return _utcOffset;
            }
        }

        /// <summary>
        /// Gets the type of the value: <see cref="VcardValueType.Text"/>, <see cref="VcardValueType.Uri"/>, <see cref="VcardValueType.DateAndOrTime"/> or
        /// <see cref="VcardValueType.UtcOffset"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance is not initialised.</exception>
        public VcardValueType ValueType
        {
            get
            {
                if (_valueType == VcardValueType.Unknown)
                    throw new InvalidOperationException(UninitializedMessage);

                return _valueType;
            }
        }

        /// <summary>
        /// Indicates whether this value equals another: they have the same type, and the same value.  A URI is compared as it was given.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns><c>true</c> if they are equal; otherwise <c>false</c>.</returns>
        public bool Equals(VcardTypedValue other)
        {
            if (_valueType != other._valueType) return false;

            switch (_valueType)
            {
                case VcardValueType.Text:
                    return string.Equals(_text, other._text, StringComparison.Ordinal);
                case VcardValueType.Uri:
                    return string.Equals(_uri?.OriginalString, other._uri?.OriginalString, StringComparison.Ordinal);
                case VcardValueType.DateAndOrTime:
                    return _dateAndOrTime.Equals(other._dateAndOrTime);
                case VcardValueType.UtcOffset:
                    return _utcOffset.Equals(other._utcOffset);
                default:
                    return true;
            }
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is VcardTypedValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            switch (_valueType)
            {
                case VcardValueType.Text:
                    return StringComparer.Ordinal.GetHashCode(_text ?? string.Empty);
                case VcardValueType.Uri:
                    return StringComparer.Ordinal.GetHashCode(_uri?.OriginalString ?? string.Empty);
                case VcardValueType.DateAndOrTime:
                    return _dateAndOrTime.GetHashCode();
                case VcardValueType.UtcOffset:
                    return _utcOffset.GetHashCode();
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets the value as it appears in a content line of vCard 4.0, with any escaping; or the type name for an instance that is not initialised.
        /// </summary>
        public override string ToString()
        {
            switch (_valueType)
            {
                case VcardValueType.Text:
                    return VcardValue.FormatText(_text ?? string.Empty, VcardValueFormat.Text, VcardVersion.V4_0);
                case VcardValueType.Uri:
                    return VcardValue.EscapeUri(_uri?.OriginalString ?? string.Empty);
                case VcardValueType.DateAndOrTime:
                    return _dateAndOrTime.ToString(VcardValueFormat.Text);
                case VcardValueType.UtcOffset:
                    return _utcOffset.ToString(VcardValueFormat.Text);
                default:
                    return base.ToString() ?? nameof(VcardTypedValue);
            }
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(VcardTypedValue left, VcardTypedValue right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(VcardTypedValue left, VcardTypedValue right)
        {
            return !left.Equals(right);
        }

        private void CheckValueType(VcardValueType valueType)
        {
            if (_valueType == VcardValueType.Unknown)
                throw new InvalidOperationException(UninitializedMessage);
            if (_valueType != valueType)
                throw new InvalidOperationException("The value is of another type; see ValueType.");
        }
    }
}
