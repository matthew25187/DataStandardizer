using System;
using System.Globalization;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents the formatting information for international telephone numbers 
    /// adhering to the ITU E.164 standard. This class provides properties and methods 
    /// to define and manage the formatting of international and national numbers, 
    /// including patterns for long and short formats.
    /// </summary>
    /// <remarks>
    /// A new instance has the invariant patterns, those of <see cref="TelephonyInfo.InvariantTelephony"/>, until they are set.
    /// </remarks>
    public sealed class ItuE164InternationalNumberFormatInfo : IFormatProvider
    {
        // The neutral resources hold the invariant patterns. They are looked up once, with the invariant culture rather than the
        // current thread's UI culture, so that a satellite assembly can't stand in for them.
        private static readonly string InvariantLongInternationalNumberPattern = GetInvariantPattern(nameof(LongInternationalNumberPattern));
        private static readonly string InvariantShortInternationalNumberPattern = GetInvariantPattern(nameof(ShortInternationalNumberPattern));

        private string _longInternationalNumberPattern = InvariantLongInternationalNumberPattern;
        private string _shortInternationalNumberPattern = InvariantShortInternationalNumberPattern;

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
        /// code, each <c>s</c> a digit of the subscriber number, and any other character is copied as it stands. The letters
        /// <c>c</c>, <c>i</c> and <c>s</c> are always placeholders. A placeholder with no digit left to write writes nothing, and a
        /// number with a digit left over after the pattern can't be formatted with it. The default is the invariant pattern.
        /// </value>
        /// <exception cref="ArgumentNullException">The property is being set to <c>null</c> and this format information isn't read-only.</exception>
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

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
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
        /// code, each <c>s</c> a digit of the subscriber number, and any other character is copied as it stands. The letters
        /// <c>c</c>, <c>i</c> and <c>s</c> are always placeholders. A placeholder with no digit left to write writes nothing, and a
        /// number with a digit left over after the pattern can't be formatted with it. The default is the invariant pattern.
        /// </value>
        /// <exception cref="ArgumentNullException">The property is being set to <c>null</c> and this format information isn't read-only.</exception>
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

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _shortInternationalNumberPattern = value;
            }
        }

        private static string GetInvariantPattern(string name) =>
            Resources.ResourceManager.GetString(name, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
