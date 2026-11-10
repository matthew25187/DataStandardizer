using System;
using System.Globalization;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// Sets the current culture of the thread for as long as the scope is open, and restores the culture it replaced when it is disposed.
    /// </summary>
    internal sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture;

        internal CultureScope(string cultureName)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
        }
    }
}
