#if !NETCOREAPP3_0_OR_GREATER
using System.Runtime.InteropServices;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Converts a <see cref="float"/> to and from its bits without allocating, for the targets whose <see cref="System.BitConverter"/> has no
    /// <c>SingleToInt32Bits</c>.
    /// </summary>
    internal static class CsvFloatingPointBits
    {
        internal static int SingleToInt32Bits(float value) => new SingleInt32(value).Int32;

        internal static float Int32BitsToSingle(int bits) => new SingleInt32(bits).Single;

        [StructLayout(LayoutKind.Explicit)]
        private struct SingleInt32
        {
            [FieldOffset(0)]
            internal readonly float Single;

            [FieldOffset(0)]
            internal readonly int Int32;

            internal SingleInt32(float value)
            {
                Int32 = 0;
                Single = value;
            }

            internal SingleInt32(int bits)
            {
                Single = 0;
                Int32 = bits;
            }
        }
    }
}
#endif
