using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Communication.E164
{
    internal sealed class ItuE164InternationalNumberStructureForTrials : ItuE164InternationalNumberStructureBase, IItuE164InternationalNumberForTrials
    {
        private static readonly ILookup<ushort, ushort> IdentificationCodeLookup;
        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;

        private Dictionary<string, string> _numberParts = new Dictionary<string, string>();

        static ItuE164InternationalNumberStructureForTrials()
        {
            // Compose patterns for numbers with and without spacing.
            IdentificationCodeLookup = (
                    from countryCode in Enum.GetValues(typeof(ItuE164AssignedCountryCodesForTrials)).Cast<ushort>()
                    from trialIdentificationCode in Enum.GetValues(typeof(ItuE164AssignedTrialIdentificationCodesForTrials)).Cast<ItuE164AssignedTrialIdentificationCodesForTrials>()
                    let sharedCodeAttribute = typeof(ItuE164AssignedTrialIdentificationCodesForTrials).GetTypeInfo().DeclaredFields
                        .Single(field => field.GetValue(null)?.Equals(trialIdentificationCode) ?? false)
                        .GetCustomAttribute<ItuE164SharedCodeAttribute>()
                    where sharedCodeAttribute.CountryCode == countryCode
                    select new { CountryCode = countryCode, IdentificationCode = trialIdentificationCode })
                .ToLookup(kvp => kvp.CountryCode, kvp => (ushort)kvp.IdentificationCode);
        }

        internal ItuE164InternationalNumberStructureForTrials(ulong number) : base(number)
        {
        }

#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string s, ItuE164InternationalNumberStyles numberStyles, out ItuE164InternationalNumberStructureForTrials? result)
#else
        internal static bool TryParse(string s, ItuE164InternationalNumberStyles numberStyles, [CanBeNull] out ItuE164InternationalNumberStructureForTrials result)
#endif
        {
            var isParsed = false;

            var parseExpression = GetParseExpression(typeof(ItuE164InternationalNumberStructureForTrials), numberStyles, GetParsePattern);
            var parseMatch = parseExpression.Match(s);
            if (parseMatch.Success)
            {
                var countryCodePart = parseMatch.Groups[NumberPart.CountryCode].Value;
                var trialIdentificationCodePart = parseMatch.Groups[NumberPart.IdentificationCode].Value;
                var subscriberNumberPart = parseMatch.Groups[NumberPart.SubscriberNumber].Value;
                var concatenatedParts = Regex.Replace(countryCodePart + trialIdentificationCodePart + subscriberNumberPart, @"\D", string.Empty);
                var number = ulong.Parse(concatenatedParts, NumberStyles, CultureInfo.InvariantCulture);
                result = new ItuE164InternationalNumberStructureForTrials(number)
                {
                    _numberParts = new Dictionary<string, string>
                    {
                        { NumberPart.CountryCode, countryCodePart },
                        { NumberPart.IdentificationCode, trialIdentificationCodePart },
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
                return ComposePatternForParse(IdentificationCodeLookup, styles, 0);
            }
        }

        public override ushort CountryCode => DoGetCountryCode();

        public ItuE164AssignedTrialIdentificationCodesForTrials TrialIdentificationCode => DoGetTrialIdentificationCode();

        public ItuE164SubscriberNumber? SubscriberNumber => DoGetSubscriberNumber();

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

        private ItuE164SubscriberNumber? DoGetSubscriberNumber()
        {
            // First, try to get delineated Subscriber Number.
            if (_numberParts.TryGetValue(NumberPart.SubscriberNumber, out var subscriberNumberPart))
            {
                return new ItuE164SubscriberNumber(subscriberNumberPart);
            }

            // Second, extract Subscriber Number from whole number.
            subscriberNumberPart = Number > 9999 ? Number.ToString().Substring(4) : null;
            return subscriberNumberPart != null ? new ItuE164SubscriberNumber(subscriberNumberPart) : default(ItuE164SubscriberNumber?);
        }

        private ItuE164AssignedTrialIdentificationCodesForTrials DoGetTrialIdentificationCode()
        {
            // First, try to get delineated Trial Identification Code.
            if (_numberParts.TryGetValue(NumberPart.IdentificationCode, out var trialIdentificationCodePart))
            {
                return (ItuE164AssignedTrialIdentificationCodesForTrials)Enum.Parse(typeof(ItuE164AssignedTrialIdentificationCodesForTrials), trialIdentificationCodePart);
            }

            // Second, extract Trial Identification Code from whole number.
            trialIdentificationCodePart = Number.ToString().Substring(3, 1);
            return Enum.TryParse(trialIdentificationCodePart, out ItuE164AssignedTrialIdentificationCodesForTrials trialIdentificationCode) ? trialIdentificationCode : default;
        }
    }
}