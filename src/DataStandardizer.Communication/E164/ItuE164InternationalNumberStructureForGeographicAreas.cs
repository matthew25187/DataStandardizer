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
    internal sealed class ItuE164InternationalNumberStructureForGeographicAreas : ItuE164InternationalNumberStructureBase, IItuE164InternationalNumberForGeographicAreas
    {
        private static class ErrorMessage
        {
            internal const string InvalidValueTemplate = "{0} is invalid.";
        }

        private static class NumberPart
        {
            internal const string CountryCode = "CountryCode";
            internal const string NationalSignificantNumber = "NationalSignificantNumber";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;
        private static readonly Regex InternationalNumberExpression;

        private Dictionary<string, string> _numberParts = new Dictionary<string, string>();

        static ItuE164InternationalNumberStructureForGeographicAreas()
        {
            // Compose sub-pattern for Country Code.
            var validCountryCodes = Enum.GetValues(typeof(ItuE164AssignedCountryCodesForGeographicAreas))
                .Cast<ushort>()
                .Distinct()
                .ToArray();
            var countryCodePattern = string.Join("|", validCountryCodes);

            // Compose expressions for parsing a number.
            var expressionOptions = RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            expressionOptions |= RegexOptions.Compiled;
#endif
            InternationalNumberExpression = new Regex(string.Concat(@"^(?:\+\s*)?(?<", NumberPart.CountryCode, ">", countryCodePattern, @")\s*(?<", NumberPart.NationalSignificantNumber, @">\d+(?:\s+\d+)*)$"), expressionOptions);
        }

        internal ItuE164InternationalNumberStructureForGeographicAreas(ulong number) : base(number)
        {
        }

#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string s, out ItuE164InternationalNumberStructureForGeographicAreas? result)
#else
        internal static bool TryParse(string s, [CanBeNull] out ItuE164InternationalNumberStructureForGeographicAreas result)
#endif
        {
            var isParsed = false;

            var parseMatch = InternationalNumberExpression.Match(s);
            if (parseMatch.Success)
            {
                var countryCodePart = parseMatch.Groups[NumberPart.CountryCode].Value;
                var nationalSignificantNumberPart = parseMatch.Groups[NumberPart.NationalSignificantNumber].Value;
                var number = ulong.Parse(Regex.Replace(countryCodePart + nationalSignificantNumberPart, @"\s", string.Empty), NumberStyles, CultureInfo.InvariantCulture);
                result = new ItuE164InternationalNumberStructureForGeographicAreas(number)
                {
                    _numberParts = new Dictionary<string, string>
                    {
                        { NumberPart.CountryCode, countryCodePart },
                        { NumberPart.NationalSignificantNumber, nationalSignificantNumberPart }
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

        public override ushort CountryCode => DoGetCountryCode() ?? throw new InvalidOperationException(string.Format(ErrorMessage.InvalidValueTemplate, "Country Code"));

        public ItuE164NationalSignificantNumber NationalSignificantNumber => DoGetNationalSignificantNumber();

        private ushort? DoGetCountryCode()
        {
            // First, get delineated Country Code (if available).
            if (_numberParts.TryGetValue(NumberPart.CountryCode, out var countryCodePart))
            {
                return ushort.Parse(countryCodePart, NumberStyles, CultureInfo.InvariantCulture);
            }

            // Second, attempt to extract Country Code from full number.
            var number = Number.ToString();
            var result = Enum.GetValues(typeof(ItuE164AssignedCountryCodesForGeographicAreas))
                .Cast<ushort>()
                .FirstOrDefault(countryCode => number.StartsWith(countryCode.ToString()));
            return result != 0 ? result : default(ushort?);
        }

        private ItuE164NationalSignificantNumber DoGetNationalSignificantNumber()
        {
            // First, get delineated National Significant Number (if available).
            if (_numberParts.TryGetValue(NumberPart.NationalSignificantNumber, out var nationalSignificantNumberPart))
            {
                return new ItuE164NationalSignificantNumber(nationalSignificantNumberPart);
            }

            // Second, extract National Significant Number from full number.
            var countryCodePart = DoGetCountryCode()?.ToString() ?? string.Empty;
            nationalSignificantNumberPart = Number.ToString().Substring(countryCodePart.Length);
            return new ItuE164NationalSignificantNumber(nationalSignificantNumberPart);
        }
    }
}