namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The phonetic system of a value: the value of the <c>PHONETIC</c> parameter (RFC 9554 §4.6).
    /// </summary>
    public enum VcardPhoneticSystem
    {
        /// <summary>
        /// <c>ipa</c>: the International Phonetic Alphabet.
        /// </summary>
        Ipa = 0,

        /// <summary>
        /// <c>jyut</c>: Jyutping, a romanization of Cantonese.
        /// </summary>
        Jyutping = 1,

        /// <summary>
        /// <c>piny</c>: Hanyu Pinyin, a romanization of Standard Mandarin.
        /// </summary>
        Pinyin = 2,

        /// <summary>
        /// <c>script</c>: a system that isn't known, for which the <c>SCRIPT</c> parameter has to give the script.
        /// </summary>
        Script = 3
    }
}
