using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Communication.E164
{
    internal sealed class ItuE164InternationalNumberStructureForGroupsOfCountries : ItuE164InternationalNumberStructureBase, IItuE164InternationalNumberForGroupsOfCountries
    {
        private static readonly ILookup<ushort, ushort> IdentificationCodeLookup;
        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;

        private Dictionary<string, string> _numberParts = new Dictionary<string, string>();

        static ItuE164InternationalNumberStructureForGroupsOfCountries()
        {
            // Compose patterns for numbers with and without spacing.
            IdentificationCodeLookup = (
                    from countryCode in Enum.GetValues(typeof(ItuE164AssignedCountryCodesForGroupsOfCountries)).Cast<ushort>()
                    from groupIdentificationCode in Enum.GetValues(typeof(ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)).Cast<ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries>()
                    let sharedCodeAttribute = typeof(ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries).GetTypeInfo().DeclaredFields
                        .Single(field => field.GetValue(null)?.Equals(groupIdentificationCode) ?? false)
                        .GetCustomAttribute<ItuE164SharedCodeAttribute>()
                    where sharedCodeAttribute.CountryCode == countryCode
                    select new { CountryCode = countryCode, IdentificationCode = groupIdentificationCode })
                .ToLookup(kvp => kvp.CountryCode, kvp => (ushort)kvp.IdentificationCode);
        }

        internal ItuE164InternationalNumberStructureForGroupsOfCountries(ulong number) : base(number)
        {
        }

#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string s, ItuE164InternationalNumberStyles numberStyles, out ItuE164InternationalNumberStructureForGroupsOfCountries? result)
#else
        internal static bool TryParse(string s, ItuE164InternationalNumberStyles numberStyles, [CanBeNull] out ItuE164InternationalNumberStructureForGroupsOfCountries result)
#endif
        {
            var isParsed = false;

            var parseExpression = GetParseExpression(typeof(ItuE164InternationalNumberStructureForGroupsOfCountries), numberStyles, GetParsePattern);
            var parseMatch = parseExpression.Match(s);
            if (parseMatch.Success)
            {
                var countryCodePart = parseMatch.Groups[NumberPart.CountryCode].Value;
                var groupIdentificationCodePart = parseMatch.Groups[NumberPart.IdentificationCode].Value;
                var subscriberNumberPart = parseMatch.Groups[NumberPart.SubscriberNumber].Value;
                var numberWhole = new string(string.Concat(countryCodePart, groupIdentificationCodePart, subscriberNumberPart).ToCharArray().Where(character => !char.IsWhiteSpace(character)).ToArray());
                var number = ulong.Parse(numberWhole, NumberStyles, CultureInfo.InvariantCulture);
                result = new ItuE164InternationalNumberStructureForGroupsOfCountries(number)
                {
                    _numberParts = new Dictionary<string, string>
                    {
                        { NumberPart.CountryCode, countryCodePart },
                        { NumberPart.IdentificationCode, groupIdentificationCodePart },
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

        public ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries GroupIdentificationCode => DoGetGroupIdentificationCode();

        public ItuE164SubscriberNumber SubscriberNumber => DoGetSubscriberNumber();

        private ushort DoGetCountryCode()
        {
            // First, try to get the delineated Country Code.
            if (_numberParts.TryGetValue(NumberPart.CountryCode, out var countryCodePart))
            {
                return ushort.Parse(countryCodePart, NumberStyles, CultureInfo.InvariantCulture);
            }

            // Second, extract the Country Code from the whole number.
            countryCodePart = Number.ToString().Substring(0, 3);
            return ushort.Parse(countryCodePart, NumberStyles, CultureInfo.InvariantCulture);
        }

        private ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries DoGetGroupIdentificationCode()
        {
            if (!_numberParts.TryGetValue(NumberPart.IdentificationCode, out var groupIdentificationCodePart))
            {
                groupIdentificationCodePart = Number.ToString().Substring(3, 1);
            }

            return Enum.TryParse(groupIdentificationCodePart, out ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries groupIdentificationCode) ? groupIdentificationCode : default;
        }

        private ItuE164SubscriberNumber DoGetSubscriberNumber()
        {
            // First, try to get the delineated Subscriber Number.
            if (_numberParts.TryGetValue(NumberPart.SubscriberNumber, out var subscriberNumberPart))
            {
                return new ItuE164SubscriberNumber(subscriberNumberPart);
            }

            // Second, extract the Subscriber Number from the whole number.
            subscriberNumberPart = Number.ToString().Substring(4);
            return new ItuE164SubscriberNumber(subscriberNumberPart);
        }
    }
}