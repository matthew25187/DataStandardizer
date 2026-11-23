using System;

namespace DataStandardizer.Money
{
    /// <summary>
    /// Represents an exception that was thrown when a required culture-specific resource was
    /// missing; the package no longer throws it.
    /// </summary>
    /// <remarks>
    /// The package no longer throws this exception: culture-specific currency formatting
    /// information is generated from Unicode CLDR data and is complete for every culture. The type
    /// remains so that code which refers to it still compiles, and will be removed in a future major
    /// version.
    /// </remarks>
    [Obsolete("The package no longer throws MissingCultureResourceException, which will be removed in a future major version. Remove code that catches it.")]
    public sealed class MissingCultureResourceException : Exception
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="MissingCultureResourceException"/> class for a resource missing for a culture.
        /// </summary>
        /// <param name="cultureName">The name of the culture for which the resource is missing.</param>
        /// <param name="resourceName">The name of the missing resource.</param>
        public MissingCultureResourceException(string cultureName, string resourceName)
            : base($"Resource {resourceName} not found for culture {cultureName}.")
        {
            CultureName = cultureName;
            ResourceName = resourceName;
        }

        /// <summary>
        /// Gets the name of the culture for which the required resource is missing.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> representing the name of the culture.
        /// </value>
        /// <remarks>
        /// This property provides the culture identifier that was expected to have the resource.
        /// </remarks>
        public string CultureName { get; }

        /// <summary>
        /// Gets the name of the resource that is missing or could not be found.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> representing the name of the missing resource.
        /// </value>
        /// <remarks>
        /// This property provides the name of the culture-specific resource that was expected
        /// but could not be located, which triggered this exception.
        /// </remarks>
        public string ResourceName { get; }
    }
}