using DataStandardizer.BCP47.SubtagRegistry;

namespace DataStandardizer.BCP47.Tests;

public class Bcp47LanguageTagFixture
{
    public Bcp47LanguageTagFixture()
    {
        using var resourceStream = this.GetType().Assembly.GetManifestResourceStream("DataStandardizer.BCP47.Tests.Resources.IanaSubtagRegistry.txt");
        if (resourceStream is not null)
        {
            IanaSubtagRegistry = SubtagRegistry.SubtagRegistry.CreateFromStream(resourceStream);
        }
        else
        {
            IanaSubtagRegistry = SubtagRegistry.SubtagRegistry.CreateFromContent($"{SubtagRegistryConstants.FieldName.FileDate}: {DateTime.Today:yyyy-MM-dd}"); // this should never happen
        }
    }

    public SubtagRegistry.SubtagRegistry IanaSubtagRegistry { get; private set; }
}