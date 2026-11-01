using System;
using System.IO;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// The copy of the IANA Language Subtag Registry embedded in the test assembly.
    /// </summary>
    /// <remarks>
    /// A missing resource fails the test that reads it: <see cref="SubtagRegistry.SubtagRegistry.CreateFromStream"/> and
    /// <see cref="StreamReader"/> both throw <see cref="ArgumentNullException"/> for the <c>null</c> stream it is opened as.
    /// </remarks>
    internal static class IanaSubtagRegistryResource
    {
        private const string ResourceName = "DataStandardizer.LanguageTag.Tests.Resources.IanaSubtagRegistry.txt";

        private static readonly Lazy<SubtagRegistry.SubtagRegistry> SharedSubtagRegistry = new Lazy<SubtagRegistry.SubtagRegistry>(CreateSubtagRegistry);

        private static readonly Lazy<string> SharedContent = new Lazy<string>(ReadContent);

        /// <summary>
        /// One copy of the registry that every test may read. A <see cref="SubtagRegistry.SubtagRegistry"/> can't be changed once it is
        /// loaded, so no test can change this one.
        /// </summary>
        internal static SubtagRegistry.SubtagRegistry Shared => SharedSubtagRegistry.Value;

        /// <summary>
        /// The text of the registry, as <see cref="SubtagRegistry.SubtagRegistry.ToString"/> of the shared copy writes it.
        /// </summary>
        internal static string Content => SharedContent.Value;

        /// <summary>
        /// Load a new copy of the registry, for a test that must not share one with other tests.
        /// </summary>
        internal static SubtagRegistry.SubtagRegistry CreateSubtagRegistry()
        {
            using (var resourceStream = typeof(IanaSubtagRegistryResource).Assembly.GetManifestResourceStream(ResourceName))
            {
                return SubtagRegistry.SubtagRegistry.CreateFromStream(resourceStream);
            }
        }

        /// <summary>
        /// Read the text of the resource as it is, without loading it as a registry, for a test case factory that takes expected values from the registry file itself.
        /// </summary>
        internal static string ReadResourceText()
        {
            using (var reader = new StreamReader(typeof(IanaSubtagRegistryResource).Assembly.GetManifestResourceStream(ResourceName)))
            {
                return reader.ReadToEnd();
            }
        }

        private static string ReadContent()
        {
            return Shared.ToString();
        }
    }
}
