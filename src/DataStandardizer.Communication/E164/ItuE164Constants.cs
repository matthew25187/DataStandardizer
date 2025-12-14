namespace DataStandardizer.Communication.E164
{
    internal class ItuE164Constants
    {
        internal static class FormatSpecifier
        {
            internal const string InternationalPrefix = "+";
            internal const char CountryCodePlaceholder = 'c';
            internal const char IdentificationCodePlaceholder = 'i';
            internal const char SubscriberNumberPlaceholder = 's';
        }

        internal const char InternationalPrefixSymbol = '+';
        internal const int MaximumDigitCount = 15;
    }
}