using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace DataStandardizer.Communication.Tests
{
    /// <summary>
    /// Resources that hold one set of named values for a single region's culture, and another set for every other culture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GetString(string, CultureInfo)"/> looks the name up in the region's values when the culture's name is the region's
    /// culture name, and in the neutral values otherwise, including for a <c>null</c> culture. A name the chosen set doesn't hold
    /// gives <c>null</c>.
    /// </para>
    /// <para>
    /// <see cref="GetString(string)"/>, which a real <see cref="ResourceManager"/> resolves through the current thread's UI culture,
    /// looks the name up in the UI culture's values when they are given, and as <see cref="GetString(string, CultureInfo)"/> does for
    /// a <c>null</c> culture otherwise. That lets a test show which overload the code under test calls, without changing the
    /// thread's UI culture.
    /// </para>
    /// </remarks>
    internal sealed class RegionResourceManager : ResourceManager
    {
        private readonly string _regionCultureName;
        private readonly IReadOnlyDictionary<string, string> _regionValues;
        private readonly IReadOnlyDictionary<string, string> _neutralValues;
        private readonly IReadOnlyDictionary<string, string> _uiCultureValues;

        public RegionResourceManager(string regionCultureName, IReadOnlyDictionary<string, string> regionValues, IReadOnlyDictionary<string, string> neutralValues)
            : this(regionCultureName, regionValues, neutralValues, null)
        {
        }

        public RegionResourceManager(
            string regionCultureName,
            IReadOnlyDictionary<string, string> regionValues,
            IReadOnlyDictionary<string, string> neutralValues,
            IReadOnlyDictionary<string, string> uiCultureValues)
        {
            _regionCultureName = regionCultureName;
            _regionValues = regionValues;
            _neutralValues = neutralValues;
            _uiCultureValues = uiCultureValues;
        }

        public override string GetString(string name) =>
            _uiCultureValues == null
                ? GetString(name, null)
                : _uiCultureValues.TryGetValue(name, out var value) ? value : null;

        public override string GetString(string name, CultureInfo culture)
        {
            var values = culture?.Name == _regionCultureName ? _regionValues : _neutralValues;
            return values.TryGetValue(name, out var value) ? value : null;
        }
    }
}
