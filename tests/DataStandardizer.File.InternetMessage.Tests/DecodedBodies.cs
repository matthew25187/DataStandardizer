using System.Text;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Reads the bodies of a whole message for the tests.
    /// </summary>
    internal static class DecodedBodies
    {
        /// <summary>
        /// Reads the rest of the message, and gives all of its body chunks joined together, with one byte in each character.
        /// </summary>
        public static string Read(ImfEntityReader reader)
        {
            var bodies = new StringBuilder();
            while (reader.ReadEvent() is ImfEntityEvent entityEvent)
            {
                if (entityEvent.Kind == ImfEntityEventKind.BodyChunk)
                {
                    bodies.Append(Octets.ToText(entityEvent.Data));
                }
            }

            return bodies.ToString();
        }

        /// <summary>
        /// Reads the rest of the message, and gives the length of its longest body chunk.
        /// </summary>
        public static int ReadLongestChunk(ImfEntityReader reader)
        {
            var longest = 0;
            while (reader.ReadEvent() is ImfEntityEvent entityEvent)
            {
                if (entityEvent.Kind == ImfEntityEventKind.BodyChunk && entityEvent.Data.Length > longest)
                {
                    longest = entityEvent.Data.Length;
                }
            }

            return longest;
        }

        /// <summary>
        /// Reads the rest of the message asynchronously, and gives all of its body chunks joined together, with one byte in each character.
        /// </summary>
        public static async Task<string> ReadAsync(ImfEntityReader reader)
        {
            var bodies = new StringBuilder();
            while (await reader.ReadEventAsync() is ImfEntityEvent entityEvent)
            {
                if (entityEvent.Kind == ImfEntityEventKind.BodyChunk)
                {
                    bodies.Append(Octets.ToText(entityEvent.Data));
                }
            }

            return bodies.ToString();
        }
    }
}
