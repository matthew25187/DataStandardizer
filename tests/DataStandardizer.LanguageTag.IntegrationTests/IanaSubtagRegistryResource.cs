using System;
using System.IO;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    /// <summary>
    /// The copy of the IANA Language Subtag Registry embedded in the test assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The fixture has the line feeds that IANA publishes the registry with, on every checkout, as <c>.gitattributes</c> keeps them, so it is
    /// loaded with <see cref="SubtagRegistryLoadOptions.AcceptBareLineFeed"/> (RFC 5646 §3.1.1 asks for CRLF).
    /// </para>
    /// <para>
    /// A missing resource fails the test that reads it: <see cref="SubtagRegistry.CreateFromStream(Stream, SubtagRegistryLoadOptions)"/> and
    /// <see cref="StreamReader"/> both throw <see cref="ArgumentNullException"/> for the <c>null</c> stream it is opened as.
    /// </para>
    /// </remarks>
    internal static class IanaSubtagRegistryResource
    {
        private const string ResourceName = "DataStandardizer.LanguageTag.IntegrationTests.Resources.IanaSubtagRegistry.txt";

        private static readonly Lazy<SubtagRegistry> SharedSubtagRegistry = new Lazy<SubtagRegistry>(CreateSubtagRegistry);

        /// <summary>
        /// One copy of the registry that every test may read. A <see cref="SubtagRegistry"/> can't be changed once it is
        /// loaded, so no test can change this one.
        /// </summary>
        internal static SubtagRegistry Shared => SharedSubtagRegistry.Value;

        /// <summary>
        /// Load a new copy of the registry, for a test that must be the first to use it.
        /// </summary>
        internal static SubtagRegistry CreateSubtagRegistry()
        {
            using (var resourceStream = typeof(IanaSubtagRegistryResource).Assembly.GetManifestResourceStream(ResourceName))
            {
                return SubtagRegistry.CreateFromStream(resourceStream, SubtagRegistryLoadOptions.AcceptBareLineFeed);
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
