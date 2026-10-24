namespace DataStandardizer.Geography
{
    /// <summary>
    /// Level of the UN M49 hierarchy occupied by an area.
    /// </summary>
    public enum UnM49AreaLevel
    {
        /// <summary>
        /// The level of the area could not be determined.
        /// </summary>
        Unknown,

        /// <summary>
        /// The world.
        /// </summary>
        Global,

        /// <summary>
        /// A region.
        /// </summary>
        Region,

        /// <summary>
        /// A sub-region.
        /// </summary>
        SubRegion,

        /// <summary>
        /// An intermediate region.
        /// </summary>
        IntermediateRegion,

        /// <summary>
        /// A country or area.
        /// </summary>
        CountryOrArea
    }
}
