using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Communication.E164
{
    internal sealed class ItuE164InternationalNumberStructureForNetworks : ItuE164InternationalNumberStructureBase, IItuE164InternationalNumberForNetworks
    {
        private static class ErrorMessage
        {
            internal const string InvalidValueTemplate = "{0} is invalid.";
        }

        private static class NumberPart
        {
            internal const string CountryCode = "CountryCode";
            internal const string IdentificationCode = "IdentificationCode";
            internal const string SubscriberNumber = "SubscriberNumber";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;
        private static readonly ILookup<ushort, ItuE164AssignedIdentificationCodesForNetworks> IdentificationCodeLookup;
        private static readonly Regex InternationalNumberExpression;

        private Dictionary<string, string> _numberParts = new Dictionary<string, string>();

        static ItuE164InternationalNumberStructureForNetworks()
        {
            // Create lookup for Identification Code by Country Code.
            IdentificationCodeLookup = typeof(ItuE164AssignedIdentificationCodesForNetworks).GetTypeInfo().DeclaredFields
#if NETSTANDARD2_0_OR_GREATER||NETCOREAPP2_0_OR_GREATER
                .Where(field => Attribute.IsDefined(field, typeof(ItuE164SharedCodeAttribute)))
#endif
                .Select(field =>
                {
                    var sharedCodeAttribute = field.GetCustomAttribute<ItuE164SharedCodeAttribute>();
                    return new
                    {
#if NETCOREAPP3_0_OR_GREATER
                        CountryCode = (ushort)sharedCodeAttribute?.CountryCode!,
                        IdentificationCode = (ItuE164AssignedIdentificationCodesForNetworks)field.GetValue(null)!
#else
                        sharedCodeAttribute.CountryCode,
                        IdentificationCode = (ItuE164AssignedIdentificationCodesForNetworks)field.GetValue(null)
#endif
                    };
                })
                .ToLookup(kvp => kvp.CountryCode,kvp=>kvp.IdentificationCode);
            
            // Compose patterns for numbers with and without spacing.
            var internationalNumberPatternBuilder = new StringBuilder();
            var isFirst = true;
            var subPatterns =
                from countryCode in Enum.GetValues(typeof(ItuE164AssignedCountryCodesForNetworks)).Cast<ushort>()
                from identificationCode in Enum.GetValues(typeof(ItuE164AssignedIdentificationCodesForNetworks)).Cast<ItuE164AssignedIdentificationCodesForNetworks>()
                let sharedCodeAttribute = typeof(ItuE164AssignedIdentificationCodesForNetworks).GetTypeInfo().DeclaredFields
                    .Single(field => field.GetValue(identificationCode)?.Equals(identificationCode) ?? false)
                    .GetCustomAttribute<ItuE164SharedCodeAttribute>()
                where sharedCodeAttribute.CountryCode == countryCode
                select string.Concat("(?<", NumberPart.CountryCode, ">", countryCode, @")\s*(?<", NumberPart.IdentificationCode, ">", (ushort)identificationCode, @")\s*(?<", NumberPart.SubscriberNumber, @">\d{1,",
                    12 - $"{(ushort)identificationCode}".Length, "})");
            foreach (var subPattern in subPatterns)
            {
                if (!isFirst) internationalNumberPatternBuilder.Append("|");
                internationalNumberPatternBuilder.Append(subPattern);

                isFirst = false;
            }

            // Compose expressions for parsing a number.
            var expressionOptions = RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            expressionOptions |= RegexOptions.Compiled;
#endif
            InternationalNumberExpression = new Regex(string.Concat(@"^(?:\+\s*)?(?:", internationalNumberPatternBuilder, ")"), expressionOptions);
        }

        internal ItuE164InternationalNumberStructureForNetworks(ulong number) : base(number)
        {
        }

#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string s, out ItuE164InternationalNumberStructureForNetworks? result)
#else
        internal static bool TryParse(string s, [CanBeNull] out ItuE164InternationalNumberStructureForNetworks result)
#endif
        {
            var isParsed = false;

            var parseMatch = InternationalNumberExpression.Match(s);
            if (parseMatch.Success)
            {
                var countryCodePart = parseMatch.Groups[NumberPart.CountryCode].Value;
                var identificationCodePart = parseMatch.Groups[NumberPart.IdentificationCode].Value;
                var subscriberNumberPart = parseMatch.Groups[NumberPart.SubscriberNumber].Value;
                var number = ulong.Parse(Regex.Replace(countryCodePart + identificationCodePart + subscriberNumberPart, @"\s", String.Empty), NumberStyles, CultureInfo.InvariantCulture);
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
        }

        public override ushort CountryCode => DoGetCountryCode();

        public ItuE164AssignedIdentificationCodesForNetworks IdentificationCode => DoGetIdentificationCode() ?? throw new InvalidOperationException(string.Format(ErrorMessage.InvalidValueTemplate, "Identification Code"));

        public ItuE164SubscriberNumber SubscriberNumber => DoGetSubscriberNumber()??throw new InvalidOperationException(string.Format(ErrorMessage.InvalidValueTemplate,"Subscriber Number"));

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
            var number = Number.ToString();
            var identificationCodeItem = IdentificationCodeLookup[CountryCode]
                .Select(code => new { Code = code, Part = $"{(ushort)code}" })
                .FirstOrDefault(item => number.Substring(3, item.Part.Length) == item.Part);
            return identificationCodeItem?.Code;
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