using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Communication.E164
{
    internal sealed class ItuE164InternationalNumberStructureForGlobalServices : ItuE164InternationalNumberStructureBase, IItuE164InternationalNumberForGlobalServices
    {
        private static class NumberPart
        {
            internal const string CountryCode = "CountryCode";
            internal const string GlobalSubscriberNumber = "GlobalSubscriberNumber";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;
        private static readonly Regex InternationalNumberExpression;

        private Dictionary<string, string> _numberParts = new Dictionary<string, string>();

        static ItuE164InternationalNumberStructureForGlobalServices()
        {
            // Compose sub-pattern for Country Code.
            var validCountryCodes = Enum.GetValues(typeof(ItuE164AssignedCountryCodesForGlobalServices))
                .Cast<ushort>();
            var countryCodePattern = string.Join("|", validCountryCodes.Select(code => $"{code:000}"));

            // Compose expressions for parsing a number.
            var expressionOptions = RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            expressionOptions |= RegexOptions.Compiled;
#endif
            InternationalNumberExpression = new Regex(string.Concat(@"^(?:\+\s*)?(?<", NumberPart.CountryCode, ">", countryCodePattern, @")\s*(?<", NumberPart.GlobalSubscriberNumber, @">\d{1,12})$"), expressionOptions);
        }

        internal ItuE164InternationalNumberStructureForGlobalServices(ulong number) : base(number)
        {
        }

#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string s, out ItuE164InternationalNumberStructureForGlobalServices? result)
#else
        internal static bool TryParse(string s, [CanBeNull] out ItuE164InternationalNumberStructureForGlobalServices result)
#endif
        {
            var isParsed = false;

            var parseMatch = InternationalNumberExpression.Match(s);
            if (parseMatch.Success)
            {
                var countryCodePart = parseMatch.Groups[NumberPart.CountryCode].Value;
                var globalSubscriberNumberPart = parseMatch.Groups[NumberPart.GlobalSubscriberNumber].Value;
                var number = ulong.Parse(Regex.Replace(countryCodePart + globalSubscriberNumberPart, @"\s", string.Empty), NumberStyles, CultureInfo.InvariantCulture);
                result = new ItuE164InternationalNumberStructureForGlobalServices(number)
                {
                    _numberParts = new Dictionary<string, string>
                    {
                        { NumberPart.CountryCode, countryCodePart },
                        { NumberPart.GlobalSubscriberNumber, globalSubscriberNumberPart }
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
            if (_numberParts.TryGetValue(NumberPart.GlobalSubscriberNumber, out var globalSubscriberNumberPart))
            {
                return new ItuE164GlobalSubscriberNumber(globalSubscriberNumberPart);
            }

            // Second, extract Global Subscriber Number from whole number.
            globalSubscriberNumberPart = Number.ToString().Substring(3);
            return new ItuE164GlobalSubscriberNumber(globalSubscriberNumberPart);
        }
    }
}