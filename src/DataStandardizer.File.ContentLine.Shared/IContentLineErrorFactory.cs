using System;

namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// Builds the exception for a problem the content line engine found.  Each package that compiles the engine supplies one, so that the problem
    /// reaches its callers as that package's exception, with its messages.
    /// </summary>
    internal interface IContentLineErrorFactory
    {
        /// <summary>
        /// Builds the exception for a problem.  The engine throws it.
        /// </summary>
        /// <param name="error">The problem.</param>
        /// <returns>The exception.</returns>
        Exception CreateException(ContentLineError error);
    }
}
