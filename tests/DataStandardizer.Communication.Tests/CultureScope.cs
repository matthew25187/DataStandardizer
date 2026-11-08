using System;
using System.Globalization;

namespace DataStandardizer.Communication.Tests
{
    /// <summary>
    /// Sets <see cref="CultureInfo.CurrentCulture"/> for the lifetime of the scope, and restores the culture it replaced when disposed.
    /// </summary>
    internal sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture;

        public CultureScope(string cultureName)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCulture;
        }
    }
}
