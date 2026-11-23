using System.Text;

namespace DataStandardizer.Communication.IntegrationTests.Mime
{
    /// <summary>
    /// Makes the runtime's code pages available to the tests that need them.
    /// </summary>
    internal static class CodePageProvider
    {
        /// <summary>
        /// Makes the runtime's code pages available: on .NET, registers <c>CodePagesEncodingProvider</c>, which the runtime registers only
        /// once however often it is called. .NET Framework has them already.
        /// </summary>
        public static void Register()
        {
#if !NETFRAMEWORK
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }
    }
}
