using System;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents the formatting information for international telephone numbers 
    /// adhering to the ITU E.164 standard. This class provides properties and methods 
    /// to define and manage the formatting of international and national numbers, 
    /// including patterns for long and short formats.
    /// </summary>
    public sealed class ItuE164InternationalNumberFormatInfo : IFormatProvider
    {
        private string _longInternationalNumberPattern = string.Empty;
        private string _shortInternationalNumberPattern = string.Empty;

#if NETCOREAPP3_0_OR_GREATER
        /// <inheritdoc/>
        /// <remarks>
        /// Returns this instance when <paramref name="formatType"/> is <see cref="ItuE164InternationalNumberFormatInfo"/>;
        /// otherwise, <c>null</c>.
        /// </remarks>
        public object? GetFormat(Type? formatType)
        {
            if (formatType == typeof(ItuE164InternationalNumberFormatInfo))
            {
                return this;
            }

            return null;
        }
#else
        /// <inheritdoc/>
        /// <remarks>
        /// Returns this instance when <paramref name="formatType"/> is <see cref="ItuE164InternationalNumberFormatInfo"/>;
        /// otherwise, <c>null</c>.
        /// </remarks>
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ItuE164InternationalNumberFormatInfo))
            {
                return this;
            }

            return null;
        }
#endif


        /// <summary>
        /// Gets a value indicating whether this format information is read-only.
        /// </summary>
        /// <value>
        /// <c>true</c> if the patterns can't be changed, as for the format information of a read-only
        /// <see cref="TelephonyInfo"/>; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get; internal set; }

        /// <summary>
        /// Gets or sets the pattern of the long international format, <c>I</c>.
        /// </summary>
        /// <value>
        /// A custom pattern, in which <c>+</c> is the
        /// international prefix symbol, each <c>c</c> a digit of the country code, each <c>i</c> a digit of the identification
        /// code, each <c>s</c> a digit of the subscriber number, and any other character is copied as it stands. A placeholder
        /// with no digit left to write writes nothing.
        /// </value>
        /// <exception cref="InvalidOperationException">The property is being set and this format information is read-only.</exception>
        public string LongInternationalNumberPattern
        {
            get => _longInternationalNumberPattern;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(ItuE164InternationalNumberFormatInfo)} is read only.");
                }

                _longInternationalNumberPattern = value;
            }
        }

        /// <summary>
        /// Gets or sets the pattern of the short international format, <c>i</c>.
        /// </summary>
        /// <value>
        /// A custom pattern, in which <c>+</c> is the
        /// international prefix symbol, each <c>c</c> a digit of the country code, each <c>i</c> a digit of the identification
        /// code, each <c>s</c> a digit of the subscriber number, and any other character is copied as it stands. A placeholder
        /// with no digit left to write writes nothing.
        /// </value>
        /// <exception cref="InvalidOperationException">The property is being set and this format information is read-only.</exception>
        public string ShortInternationalNumberPattern
        {
            get => _shortInternationalNumberPattern;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(ItuE164InternationalNumberFormatInfo)} is read only.");
                }

                _shortInternationalNumberPattern = value;
            }
        }
    }
}