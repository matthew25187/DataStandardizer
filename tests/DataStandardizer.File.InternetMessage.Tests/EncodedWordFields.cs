using System;
using System.IO;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Writes fields of text and reads them back for the tests of encoded words.
    /// </summary>
    internal static class EncodedWordFields
    {
        /// <summary>
        /// Writes text as one field, and gives the output as text with one byte in each character.
        /// </summary>
        public static string WriteText(string name, string text, ImfOptions options)
        {
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream, options))
            {
                writer.WriteTextField(name, text);
            }

            return Octets.ToText(stream.ToArray());
        }

        /// <summary>
        /// Writes text as one field asynchronously, and gives the output as text with one byte in each character.
        /// </summary>
        public static async Task<string> WriteTextAsync(string name, string text, ImfOptions options)
        {
            var stream = new MemoryStream();
            var writer = new ImfHeaderWriter(stream, options);
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteTextFieldAsync(name, text).AsTask());
            return Octets.ToText(stream.ToArray());
        }

        /// <summary>
        /// Reads the first field of a section written with one byte in each character, and gives its decoded value.
        /// </summary>
        public static string ReadDecodedValue(string section, ImfOptions options)
        {
            using (var reader = HeaderSections.CreateReader(section, options))
            {
                var field = reader.ReadField();
                return field.HasValue ? field.Value.GetDecodedValue() : string.Empty;
            }
        }

        /// <summary>
        /// Reads the first field of a section written with one byte in each character, and gives its value as UTF-16 text, as the types of its body parse it.
        /// </summary>
        public static string ReadValue(string section)
        {
            using (var reader = HeaderSections.CreateReader(section))
            {
                var field = reader.ReadField();
                return field.HasValue ? field.Value.GetValue() : string.Empty;
            }
        }

        /// <summary>
        /// Gets the length of the longest line, without its line break, in some output.
        /// </summary>
        public static int LongestLineLength(string output)
        {
            var longest = 0;
            foreach (var line in output.Split(new[] { "\r\n" }, StringSplitOptions.None))
            {
                longest = Math.Max(longest, line.Length);
            }

            return longest;
        }
    }
}
