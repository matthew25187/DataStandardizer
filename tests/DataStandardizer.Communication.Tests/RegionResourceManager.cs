using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace DataStandardizer.Communication.Tests
{
    /// <summary>
    /// Resources that hold one set of named values for a single region's culture, and another set for every other culture.
    /// </summary>
    /// <remarks>
    /// <see cref="GetString(string, CultureInfo)"/> looks the name up in the region's values when the culture's name is the region's
    /// culture name, and in the neutral values otherwise, including for a <c>null</c> culture. A name the chosen set doesn't hold
    /// gives <c>null</c>.
    /// </remarks>
    internal sealed class RegionResourceManager : ResourceManager
    {
        private readonly string _regionCultureName;
        private readonly IReadOnlyDictionary<string, string> _regionValues;
        private readonly IReadOnlyDictionary<string, string> _neutralValues;

        public RegionResourceManager(string regionCultureName, IReadOnlyDictionary<string, string> regionValues, IReadOnlyDictionary<string, string> neutralValues)
        {
            _regionCultureName = regionCultureName;
            _regionValues = regionValues;
            _neutralValues = neutralValues;
        }

        public override string GetString(string name) => GetString(name, null);

        public override string GetString(string name, CultureInfo culture)
        {
            var values = culture?.Name == _regionCultureName ? _regionValues : _neutralValues;
            return values.TryGetValue(name, out var value) ? value : null;
        }
    }
}
