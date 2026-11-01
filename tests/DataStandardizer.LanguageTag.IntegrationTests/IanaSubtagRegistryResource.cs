using System;
using System.IO;

namespace DataStandardizer.LanguageTag.IntegrationTests
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
        private const string ResourceName = "DataStandardizer.LanguageTag.IntegrationTests.Resources.IanaSubtagRegistry.txt";

        private static readonly Lazy<SubtagRegistry.SubtagRegistry> SharedSubtagRegistry = new Lazy<SubtagRegistry.SubtagRegistry>(CreateSubtagRegistry);

        /// <summary>
        /// One copy of the registry that every test may read. A <see cref="SubtagRegistry.SubtagRegistry"/> can't be changed once it is
        /// loaded, so no test can change this one.
        /// </summary>
        internal static SubtagRegistry.SubtagRegistry Shared => SharedSubtagRegistry.Value;

        /// <summary>
        /// Load a new copy of the registry, for a test that must be the first to use it.
        /// </summary>
        internal static SubtagRegistry.SubtagRegistry CreateSubtagRegistry()
        {
            using (var resourceStream = typeof(IanaSubtagRegistryResource).Assembly.GetManifestResourceStream(ResourceName))
            {
                return SubtagRegistry.SubtagRegistry.CreateFromStream(resourceStream);
            }
        }

        /// <summary>
        /// Read the text of the resource as it is.
        /// </summary>
        internal static string ReadResourceText()
        {
            using (var reader = new StreamReader(typeof(IanaSubtagRegistryResource).Assembly.GetManifestResourceStream(ResourceName)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
