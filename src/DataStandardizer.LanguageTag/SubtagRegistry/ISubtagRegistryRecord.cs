using System;
using System.Collections.Generic;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    public interface ISubtagRegistryRecord : IList<Tuple<string, object>>
    {
    }
}