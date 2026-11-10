using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Communication.E164
{
    internal sealed class ItuE164InternationalNumberStructureForNetworks : ItuE164InternationalNumberStructureBase, IItuE164InternationalNumberForNetworks
    {
        private static class ErrorMessage
        {
            internal const string InvalidValueTemplate = "{0} is invalid.";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;
        private static readonly ILookup<ushort, ushort> IdentificationCodeLookup;

        private Dictionary<string, string> _numberParts = new Dictionary<string, string>();

        static ItuE164InternationalNumberStructureForNetworks()
        {
            IdentificationCodeLookup = ComposeIdentificationCodeLookup(typeof(ItuE164AssignedCountryCodesForNetworks), typeof(ItuE164AssignedIdentificationCodesForNetworks));
        }

        internal ItuE164InternationalNumberStructureForNetworks(ulong number) : base(number)
        {
        }

#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string s, ItuE164InternationalNumberStyles numberStyles, out ItuE164InternationalNumberStructureForNetworks? result)
#else
        internal static bool TryParse(string s, ItuE164InternationalNumberStyles numberStyles, [MaybeNullWhen(false)] out ItuE164InternationalNumberStructureForNetworks result)
#endif
        {
            var isParsed = false;

            var parseExpression = GetParseExpression(typeof(ItuE164InternationalNumberStructureForNetworks), numberStyles, GetParsePattern);
            var parseMatch = parseExpression.Match(s);
            if (parseMatch.Success)
            {
                var countryCodePart = parseMatch.Groups[NumberPart.CountryCode].Value;
                var identificationCodePart = parseMatch.Groups[NumberPart.IdentificationCode].Value;
                var subscriberNumberPart = parseMatch.Groups[NumberPart.SubscriberNumber].Value;
                var concatenatedParts = Regex.Replace(countryCodePart + identificationCodePart + subscriberNumberPart, @"\D", string.Empty);
                var number = ulong.Parse(concatenatedParts, NumberStyles, CultureInfo.InvariantCulture);
                result = new ItuE164InternationalNumberStructureForNetworks(number)
                {
                    _numberParts = new Dictionary<string, string>
                    {
                        { NumberPart.CountryCode, countryCodePart },
                        { NumberPart.IdentificationCode, identificationCodePart },
                        { NumberPart.SubscriberNumber, subscriberNumberPart }
                    }
                };
                isParsed = true;
            }
            else
            {
                result = null;
            }

            return isParsed;

            string GetParsePattern(ItuE164InternationalNumberStyles styles)
            {
                return ComposePatternForParse(IdentificationCodeLookup, styles);
            }
        }

        public override ushort CountryCode => DoGetCountryCode();

        public ItuE164AssignedIdentificationCodesForNetworks IdentificationCode => DoGetIdentificationCode() ?? throw new InvalidOperationException(string.Format(ErrorMessage.InvalidValueTemplate, "Identification Code"));

        public ItuE164SubscriberNumber SubscriberNumber => DoGetSubscriberNumber() ?? throw new InvalidOperationException(string.Format(ErrorMessage.InvalidValueTemplate, "Subscriber Number"));

        private ushort DoGetCountryCode()
        {
            // First, try to get delineated Country Code.
            if (_numberParts.TryGetValue(NumberPart.CountryCode, out var countryCodePart))
            {
                return ushort.Parse(countryCodePart, NumberStyles, CultureInfo.InvariantCulture);
            }

            // Second, extract Country Code from whole number.
            countryCodePart = Number.ToString().Substring(0, 3);
            return ushort.Parse(countryCodePart, NumberStyles, CultureInfo.InvariantCulture);
        }

        private ItuE164AssignedIdentificationCodesForNetworks? DoGetIdentificationCode()
        {
            // First, try to get delineated Identification Code.
            if (_numberParts.TryGetValue(NumberPart.IdentificationCode, out var identificationCodePart))
            {
                return Enum.TryParse(identificationCodePart, out ItuE164AssignedIdentificationCodesForNetworks identificationCode) ? identificationCode : default;
            }

            // Second, extract Identification Code from whole number.
            // Skip a code with more digits than follow the Country Code, and try longer codes first, so that a
            // code which is the start of another can't hide it.
            const int countryCodeDigitCount = 3;
            var number = Number.ToString(CultureInfo.InvariantCulture);
            var identificationCodeItem = IdentificationCodeLookup[CountryCode]
                .Select(code => new { Code = code, Part = code.ToString(CultureInfo.InvariantCulture) })
                .Where(item => number.Length >= countryCodeDigitCount + item.Part.Length)
                .OrderByDescending(item => item.Part.Length)
                .FirstOrDefault(item => string.CompareOrdinal(number, countryCodeDigitCount, item.Part, 0, item.Part.Length) == 0);
            return (ItuE164AssignedIdentificationCodesForNetworks?)identificationCodeItem?.Code;
        }

        private ItuE164SubscriberNumber? DoGetSubscriberNumber()
        {
            // First, try to get delineated Subscriber Number.
            if (_numberParts.TryGetValue(NumberPart.SubscriberNumber, out var subscriberNumberPart))
            {
                return new ItuE164SubscriberNumber(subscriberNumberPart);
            }

            // Second, extract Subscriber Number from whole number.
            var identificationCode = DoGetIdentificationCode();
            if (identificationCode is null)
            {
                return null;
            }

            var subscriberNumberPartIndex = $"{(ushort)identificationCode.Value}".Length + 3;
            subscriberNumberPart = Number.ToString().Substring(subscriberNumberPartIndex);
            return new ItuE164SubscriberNumber(subscriberNumberPart);
        }
    }
}