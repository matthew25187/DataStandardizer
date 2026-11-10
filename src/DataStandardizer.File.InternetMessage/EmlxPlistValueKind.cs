namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The kinds of value in an XML property list.
    /// </summary>
    public enum EmlxPlistValueKind
    {
        /// <summary>
        /// A <c>dict</c>: keys and values, in order.
        /// </summary>
        Dictionary = 0,

        /// <summary>
        /// An <c>array</c>: values, in order.
        /// </summary>
        Array = 1,

        /// <summary>
        /// A <c>string</c>.
        /// </summary>
        String = 2,

        /// <summary>
        /// An <c>integer</c>.
        /// </summary>
        Integer = 3,

        /// <summary>
        /// A <c>real</c>.
        /// </summary>
        Real = 4,

        /// <summary>
        /// A <c>date</c>.
        /// </summary>
        Date = 5,

        /// <summary>
        /// A <c>data</c>: bytes in base64.
        /// </summary>
        Data = 6,

        /// <summary>
        /// A <c>true</c>.
        /// </summary>
        True = 7,

        /// <summary>
        /// A <c>false</c>.
        /// </summary>
        False = 8,
    }
}
