using System;

namespace DataStandardizer.Money
{
    /// <summary>
    /// Represents an exception that is thrown when a required culture-specific resource
    /// is missing or cannot be found.
    /// </summary>
    /// <remarks>
    /// This exception is typically used in scenarios where culture-specific resources,
    /// such as currency formatting information, are expected but not available.
    /// </remarks>
    public sealed class MissingCultureResourceException : Exception
    {
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