using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Reads whole header sections for the tests.
    /// </summary>
    internal static class HeaderSections
    {
        /// <summary>
        /// The outcome that <see cref="ReadOutcomes"/> gives for a field that failed to parse.
        /// </summary>
        public const string Rejected = "(rejected)";

        private const int BlockSize = 4096;

        /// <summary>
        /// Creates a reader over a header section written with one byte in each character.
        /// </summary>
        public static ImfHeaderReader CreateReader(string section)
        {
            return new ImfHeaderReader(new MemoryStream(Octets.Of(section)));
        }

        /// <summary>
        /// Creates a reader over a header section written with one byte in each character.
        /// </summary>
        public static ImfHeaderReader CreateReader(string section, ImfOptions options)
        {
            return new ImfHeaderReader(new MemoryStream(Octets.Of(section)), options);
        }

        /// <summary>
        /// Creates a reader that treats a line feed as the line break, as the readers of container formats do.
        /// </summary>
        public static ImfHeaderReader CreateContainerReader(string section)
        {
            return new ImfHeaderReader(new MemoryStream(Octets.Of(section)), new ImfOptions(), BlockSize, true);
        }

        /// <summary>
        /// Reads the rest of the section.
        /// </summary>
        public static List<FieldSnapshot> ReadAll(ImfHeaderReader reader)
        {
            var fields = new List<FieldSnapshot>();
            while (reader.ReadField() is ImfHeaderFieldView field)
            {
                fields.Add(FieldSnapshot.Of(field));
            }

            return fields;
        }

        /// <summary>
        /// Reads the rest of the section asynchronously.
        /// </summary>
        public static async Task<List<FieldSnapshot>> ReadAllAsync(ImfHeaderReader reader)
        {
            var fields = new List<FieldSnapshot>();
            while (await reader.ReadFieldAsync() is ImfHeaderFieldView field)
            {
                fields.Add(FieldSnapshot.Of(field));
            }

            return fields;
        }

        /// <summary>
        /// Reads the rest of the section, going on after a field that fails to parse.
        /// </summary>
        /// <returns>The name of each field, or <see cref="Rejected"/> for one that failed to parse.</returns>
        public static List<string> ReadOutcomes(ImfHeaderReader reader)
        {
            var outcomes = new List<string>();
            while (true)
            {
                try
                {
                    if (!(reader.ReadField() is ImfHeaderFieldView field)) return outcomes;

                    outcomes.Add(field.GetName());
                }
                catch (ImfFileException)
                {
                    outcomes.Add(Rejected);
                }
            }
        }

        /// <summary>
        /// Reads until a field fails, and swallows the exception, to leave the reader as that failure left it.
        /// </summary>
        public static void ReadUntilRejected(ImfHeaderReader reader)
        {
            try
            {
                while (reader.ReadField() != null)
                {
                }
            }
            catch (ImfFileException)
            {
            }
        }
    }
}
