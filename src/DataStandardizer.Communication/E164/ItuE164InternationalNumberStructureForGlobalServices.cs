using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif
using static DataStandardizer.Communication.E164.ItuE164Constants;

namespace DataStandardizer.Communication.E164
{
    internal sealed class ItuE164InternationalNumberStructureForGlobalServices : ItuE164InternationalNumberStructureBase, IItuE164InternationalNumberForGlobalServices
    {
        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;

        private Dictionary<string, string> _numberParts = new Dictionary<string, string>();

        internal ItuE164InternationalNumberStructureForGlobalServices(ulong number) : base(number)
        {
        }

        private static string GetParsePattern(ItuE164InternationalNumberStyles numberStyles)
        {
            // Compose sub-pattern for Country Code.
            var validCountryCodes = Enum.GetValues(typeof(ItuE164AssignedCountryCodesForGlobalServices))
                .Cast<ushort>();
            var countryCodePattern = string.Join("|", validCountryCodes.Select(code => $"{code:000}"));

            // Compose expressions for parsing a number.
            return string.Concat(@"^(?=(?:\D*\d){4,", MaximumDigitCount, @"}\D*$)", numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowLeadingWhite) ? $"[{PatternWhiteSpaceCharacterClass}]*": string.Empty,
                numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowInternationalPrefixSymbol) ? $@"\{InternationalPrefixSymbol}" : string.Empty, "?[", PatternWhiteSpaceCharacterClass, "]*(?<", NumberPart.CountryCode, ">",
                countryCodePattern, ")[", PatternSeparatorCharacterClass, "]*(?<", NumberPart.SubscriberNumber, @">\d(?:[", PatternSeparatorCharacterClass, @"]*\d){1,12})",
                numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowTrailingWhite) ? $"[{PatternWhiteSpaceCharacterClass}]*" : string.Empty, "$");
        }
#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string s, ItuE164InternationalNumberStyles numberStyles, out ItuE164InternationalNumberStructureForGlobalServices? result)
#else
        internal static bool TryParse(string s, ItuE164InternationalNumberStyles numberStyles, [CanBeNull] out ItuE164InternationalNumberStructureForGlobalServices result)
#endif
        {
            var isParsed = false;

            var parseExpression = GetParseExpression(typeof(ItuE164InternationalNumberStructureForGlobalServices), numberStyles, GetParsePattern);
            var parseMatch = parseExpression.Match(s);
            if (parseMatch.Success)
            {
                var countryCodePart = parseMatch.Groups[NumberPart.CountryCode].Value;
                var globalSubscriberNumberPart = parseMatch.Groups[NumberPart.SubscriberNumber].Value;
                var concatenatedParts = Regex.Replace(countryCodePart + globalSubscriberNumberPart, @"\D", string.Empty);
                var number = ulong.Parse(concatenatedParts, NumberStyles, CultureInfo.InvariantCulture);
                result = new ItuE164InternationalNumberStructureForGlobalServices(number)
                {
                    _numberParts = new Dictionary<string, string>
                    {
                        { NumberPart.CountryCode, countryCodePart },
                        { NumberPart.SubscriberNumber, globalSubscriberNumberPart }
                    }
                };
                isParsed = true;
            }
            else
            {
                result = null;
            }

            return isParsed;
        }

        public override ushort CountryCode => DoGetCountryCode();

        public ItuE164GlobalSubscriberNumber GlobalSubscriberNumber => DoGetGlobalSubscriberNumber();

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

        private ItuE164GlobalSubscriberNumber DoGetGlobalSubscriberNumber()
        {
            // First, try to get delineated Global Subscriber Number.
            if (_numberParts.TryGetValue(NumberPart.SubscriberNumber, out var globalSubscriberNumberPart))
            {
                return new ItuE164GlobalSubscriberNumber(globalSubscriberNumberPart);
            }

            // Second, extract Global Subscriber Number from whole number.
            globalSubscriberNumberPart = Number.ToString().Substring(3);
            return new ItuE164GlobalSubscriberNumber(globalSubscriberNumberPart);
        }
    }
}