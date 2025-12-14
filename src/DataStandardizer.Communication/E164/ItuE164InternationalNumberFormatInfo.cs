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
        private string _longInternationalNumberPattern;
        private string _shortInternationalNumberPattern;

#if NETCOREAPP3_0_OR_GREATER
        public object? GetFormat(Type? formatType)
        {
            if (formatType == typeof(ItuE164InternationalNumberFormatInfo))
            {
                return this;
            }

            return null;
        }
#else
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ItuE164InternationalNumberFormatInfo))
            {
                return this;
            }

            return null;
        }
#endif


        public bool IsReadOnly { get; internal set; }

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