using System;
using System.IO;
using DataStandardizer.LanguageTag.SubtagRegistry;

namespace DataStandardizer.LanguageTag.Tests
{
    public class Bcp47LanguageTagFixture
    {
        public Bcp47LanguageTagFixture()
        {
            IanaSubtagRegistry = CreateIanaSubtagRegistry();
        }

        public SubtagRegistry.SubtagRegistry IanaSubtagRegistry { get; private set; }

        /// <summary>
        /// Load a new copy of the IANA Subtag Registry fixture, for a test that must not share one with other tests.
        /// </summary>
        public static SubtagRegistry.SubtagRegistry CreateIanaSubtagRegistry()
        {
            using (var resourceStream = typeof(Bcp47LanguageTagFixture).Assembly.GetManifestResourceStream("DataStandardizer.LanguageTag.Tests.Resources.IanaSubtagRegistry.txt"))
            {
                if (resourceStream != null)
                {
                    return SubtagRegistry.SubtagRegistry.CreateFromStream(resourceStream);
                }

                return SubtagRegistry.SubtagRegistry.CreateFromContent($"{SubtagRegistryConstants.FieldName.FileDate}: {DateTime.Today:yyyy-MM-dd}"); // this should never happen
            }
        }
    }
}
