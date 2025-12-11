namespace DataStandardizer.Communication.E164
{
    internal abstract class ItuE164InternationalNumberStructureBase : IItuE164InternationalNumber
    {
        protected internal ItuE164InternationalNumberStructureBase(ulong number)
        {
            Number = number;
        }

        public ulong Number { get; }

        public abstract ushort CountryCode { get; }
    }
}