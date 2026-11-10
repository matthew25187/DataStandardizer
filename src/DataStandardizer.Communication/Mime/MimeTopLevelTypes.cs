namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The registered top-level media types.
    /// </summary>
    /// <remarks>
    /// This is not a copy of the IANA media type registry. Any top-level type that follows the naming rules can be used.
    /// </remarks>
    public static class MimeTopLevelTypes
    {
        /// <summary>
        /// <c>text</c> (RFC 2046 §4.1).
        /// </summary>
        public const string Text = "text";

        /// <summary>
        /// <c>image</c> (RFC 2046 §4.2).
        /// </summary>
        public const string Image = "image";

        /// <summary>
        /// <c>audio</c> (RFC 2046 §4.3).
        /// </summary>
        public const string Audio = "audio";

        /// <summary>
        /// <c>video</c> (RFC 2046 §4.4).
        /// </summary>
        public const string Video = "video";

        /// <summary>
        /// <c>application</c> (RFC 2046 §4.5).
        /// </summary>
        public const string Application = "application";

        /// <summary>
        /// <c>multipart</c> (RFC 2046 §5.1).
        /// </summary>
        public const string Multipart = "multipart";

        /// <summary>
        /// <c>message</c> (RFC 2046 §5.2).
        /// </summary>
        public const string Message = "message";

        /// <summary>
        /// <c>font</c> (RFC 8081).
        /// </summary>
        public const string Font = "font";

        /// <summary>
        /// <c>model</c> (RFC 2077).
        /// </summary>
        public const string Model = "model";

        /// <summary>
        /// <c>haptics</c> (RFC 9695).
        /// </summary>
        public const string Haptics = "haptics";
    }
}
