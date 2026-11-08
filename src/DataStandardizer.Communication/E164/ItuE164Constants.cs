namespace DataStandardizer.Communication.E164
{
    internal static class ItuE164Constants
    {
        internal static class FormatSpecifier
        {
            internal const string InternationalPrefix = "+";
            internal const char CountryCodePlaceholder = 'c';
            internal const char IdentificationCodePlaceholder = 'i';
            internal const char SubscriberNumberPlaceholder = 's';
        }

        // The format used when none is given: the full general format.
        internal const string DefaultFormat = "G";

        internal const char InternationalPrefixSymbol = '+';
        internal const int MaximumDigitCount = 15;
    }
}