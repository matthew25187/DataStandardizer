using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static DataStandardizer.Communication.E164.ItuE164Constants;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Provides functionality to format ITU E.164 international numbers into various string representations.
    /// </summary>
    /// <remarks>
    /// This class implements the <see cref="ICustomFormatter"/> interface to allow custom formatting of 
    /// ITU E.164 international numbers. It supports multiple formats, including abbreviated, full, short, 
    /// and long international formats, as well as custom formats defined by the user.
    /// </remarks>
    public class ItuE164InternationalNumberFormatter : ICustomFormatter
    {
        private static class NumberPart
        {
            internal const string CountryCode = "CountryCode";
            internal const string IdentificationCode = "IdentificationCode";
            internal const string SubscriberNumber = "SubscriberNumber";
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            // Get the parts of the number to format.
            string[] numberPartNames;
            IDictionary<string, string> numberParts;
            switch (arg)
            {
                case ItuE164InternationalNumber internationalNumber:
                {
                    if (internationalNumber.IsNumberForGeographicArea())
                    {
                        IItuE164InternationalNumberForGeographicAreas internationalNumberForGeographicArea = internationalNumber;
                        numberParts = new Dictionary<string, string>()
                        {
                            { NumberPart.CountryCode, internationalNumberForGeographicArea.CountryCode.ToString() },
                            { NumberPart.SubscriberNumber, internationalNumberForGeographicArea.NationalSignificantNumber.ToString() }
                        };
                        numberPartNames = new[] { NumberPart.CountryCode, NumberPart.SubscriberNumber };
                    }
                    else if (internationalNumber.IsNumberForGlobalService())
                    {
                        IItuE164InternationalNumberForGlobalServices internationalNumberForGlobalService = internationalNumber;
                        numberParts = new Dictionary<string, string>()
                        {
                            { NumberPart.CountryCode, internationalNumberForGlobalService.CountryCode.ToString() },
                            { NumberPart.SubscriberNumber, internationalNumberForGlobalService.GlobalSubscriberNumber.ToString() }
                        };
                        numberPartNames = new[] { NumberPart.CountryCode, NumberPart.SubscriberNumber };
                    }
                    else if (internationalNumber.IsNumberForNetwork())
                    {
                        IItuE164InternationalNumberForNetworks internationalNumberForNetwork = internationalNumber;
                        numberParts = new Dictionary<string, string>()
                        {
                            { NumberPart.CountryCode, internationalNumberForNetwork.CountryCode.ToString() },
                            { NumberPart.IdentificationCode, ((ushort)internationalNumberForNetwork.IdentificationCode).ToString() },
                            { NumberPart.SubscriberNumber, internationalNumberForNetwork.SubscriberNumber.ToString() }
                        };
                        numberPartNames = new[] { NumberPart.CountryCode, NumberPart.IdentificationCode, NumberPart.SubscriberNumber };
                    }
                    else if (internationalNumber.IsNumberForGroupOfCountries())
                    {
                        IItuE164InternationalNumberForGroupsOfCountries internationalNumberForGroupOfCountries = internationalNumber;
                        numberParts = new Dictionary<string, string>()
                        {
                            { NumberPart.CountryCode, internationalNumberForGroupOfCountries.CountryCode.ToString() },
                            { NumberPart.IdentificationCode, ((byte)internationalNumberForGroupOfCountries.GroupIdentificationCode).ToString() },
                            { NumberPart.SubscriberNumber, internationalNumberForGroupOfCountries.SubscriberNumber.ToString() }
                        };
                        numberPartNames = new[] { NumberPart.CountryCode, NumberPart.IdentificationCode, NumberPart.SubscriberNumber };
                    }
                    else if (internationalNumber.IsNumberForTrial())
                    {
                        IItuE164InternationalNumberForTrials internationalNumberForTrial = internationalNumber;
                        numberParts = new Dictionary<string, string>()
                            { { NumberPart.CountryCode, internationalNumberForTrial.CountryCode.ToString() }, { NumberPart.IdentificationCode, ((byte)internationalNumberForTrial.TrialIdentificationCode).ToString() } };
                        numberPartNames = new[] { NumberPart.CountryCode, NumberPart.IdentificationCode };
                        if (internationalNumberForTrial.SubscriberNumber.HasValue)
                        {
                            numberParts.Add(NumberPart.SubscriberNumber, internationalNumberForTrial.SubscriberNumber.Value.ToString());
                            numberPartNames = new[] { NumberPart.CountryCode, NumberPart.IdentificationCode, NumberPart.SubscriberNumber };
                        }
                    }
                    else
                    {
                        return HandleOtherFormats(format, arg);
                    }
                }
                    break;

                default:
                {
                    return HandleOtherFormats(format, arg);
                }
            }

            // Format argument according to format string.
            string formattedString;
            var internationalNumberFormat = formatProvider?.GetFormat(typeof(ItuE164InternationalNumberFormatInfo)) as ItuE164InternationalNumberFormatInfo
#if NETSTANDARD2_0_OR_GREATER || NET
                                            ?? TelephonyInfo.CurrentTelephony.ItuE164InternationalNumberFormat;
#else
                                            ?? TelephonyInfo.InvariantTelephony.ItuE164InternationalNumberFormat;
#endif
            var useStandardFormat = !string.IsNullOrWhiteSpace(format) ? format[0].ToString() : string.Empty;
            switch (useStandardFormat)
            {
                case "g": // abbreviated general format
                {
                    formattedString = string.Concat(InternationalPrefixSymbol, string.Concat(numberPartNames.Select(name => numberParts[name])));
                }
                    break;
                case "G": // full general format
                {
                    formattedString = string.Concat(InternationalPrefixSymbol, string.Join(" ", numberPartNames.Select(name => numberParts[name])));
                }
                    break;
                case "i": // short international format
                {
                    formattedString = CustomFormatString(internationalNumberFormat.ShortInternationalNumberPattern, InternationalPrefixSymbol, numberParts);
                }
                    break;
                case "I": // long international format
                {
                    formattedString = CustomFormatString(internationalNumberFormat.LongInternationalNumberPattern, InternationalPrefixSymbol, numberParts);
                }
                    break;

                default:
                {
                    // custom format
                    formattedString = CustomFormatString(format, InternationalPrefixSymbol, numberParts);
                }
                    break;
            }

            return formattedString;
        }

        private string CustomFormatString(string customFormat, char internationalPrefixSymbol, IDictionary<string, string> numberParts)
        {
            var countryCodePart = numberParts.TryGetValue(NumberPart.CountryCode, out var countryCodeValue) ? countryCodeValue : string.Empty;
            var identificationCodePart = numberParts.TryGetValue(NumberPart.IdentificationCode, out var identificationCodeValue) ? identificationCodeValue : string.Empty;
            var subscriberNumberPart = numberParts.TryGetValue(NumberPart.SubscriberNumber, out var subscriberNumberValue) ? subscriberNumberValue : string.Empty;

            int countryCodeIndex = 0, identificationCodeIndex = 0, subscriberNumberIndex = 0;
            var formattedStringBuilder = new StringBuilder();
            for (int customFormatIndex = 0; customFormatIndex < customFormat.Length;)
            {
                if (customFormatIndex + FormatSpecifier.InternationalPrefix.Length <= customFormat.Length &&
                    customFormat.Substring(customFormatIndex, FormatSpecifier.InternationalPrefix.Length) == FormatSpecifier.InternationalPrefix)
                {
                    formattedStringBuilder.Append(internationalPrefixSymbol);
                    customFormatIndex += FormatSpecifier.InternationalPrefix.Length;
                }
                else if (customFormat[customFormatIndex] == FormatSpecifier.CountryCodePlaceholder)
                {
                    if (countryCodeIndex < countryCodePart.Length)
                    {
                        formattedStringBuilder.Append(countryCodePart[countryCodeIndex++]);
                    }

                    customFormatIndex++;
                }
                else if (customFormat[customFormatIndex] == FormatSpecifier.IdentificationCodePlaceholder)
                {
                    if (identificationCodeIndex < identificationCodePart.Length)
                    {
                        formattedStringBuilder.Append(identificationCodePart[identificationCodeIndex++]);
                    }

                    customFormatIndex++;
                }
                else if (customFormat[customFormatIndex] == FormatSpecifier.SubscriberNumberPlaceholder)
                {
                    if (subscriberNumberIndex < subscriberNumberPart.Length)
                    {
                        formattedStringBuilder.Append(subscriberNumberPart[subscriberNumberIndex++]);
                    }

                    customFormatIndex++;
                }
                else
                {
                    formattedStringBuilder.Append(customFormat[customFormatIndex]);
                    customFormatIndex++;
                }
            }

            return formattedStringBuilder.ToString();
        }

        private string HandleOtherFormats(string format, object arg)
        {
            if (arg is IFormattable formattableArg)
            {
                return formattableArg.ToString(format, CultureInfo.InvariantCulture);
            }

            if (arg != null)
            {
                return arg.ToString();
            }

            return string.Empty;
        }
    }
}