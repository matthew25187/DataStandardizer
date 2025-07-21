using System;
using System.Collections.Generic;

namespace DataStandardizer.BCP47.SubtagRegistry
{
    public interface ISubtagRegistryRecord : IList<Tuple<string, object>>
    {
    }
}