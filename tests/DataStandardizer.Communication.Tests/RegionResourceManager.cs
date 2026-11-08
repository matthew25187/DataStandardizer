using System.Globalization;
using System.Resources;

namespace DataStandardizer.Communication.Tests
{
    /// <summary>
    /// Resources that hold one value for a single region's culture, and another for every other culture.
    /// </summary>
    /// <remarks>
    /// <see cref="GetString(string, CultureInfo)"/> returns the region's value when the culture's name is the region's culture name,
    /// and the neutral value otherwise, including for a <c>null</c> culture. It returns the same value whatever the resource name.
    /// </remarks>
    internal sealed class RegionResourceManager : ResourceManager
    {
        private readonly string _regionCultureName;
        private readonly string _regionValue;
        private readonly string _neutralValue;

        public RegionResourceManager(string regionCultureName, string regionValue, string neutralValue)
        {
            _regionCultureName = regionCultureName;
            _regionValue = regionValue;
            _neutralValue = neutralValue;
        }

        public override string GetString(string name) => GetString(name, null);

        public override string GetString(string name, CultureInfo culture) => culture?.Name == _regionCultureName ? _regionValue : _neutralValue;
    }
}
