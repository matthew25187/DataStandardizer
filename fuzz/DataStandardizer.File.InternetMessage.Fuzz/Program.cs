using SharpFuzz;

namespace DataStandardizer.File.InternetMessage.Fuzz;

internal static class Program
{
    private static readonly Dictionary<string, Action<byte[]>> Harnesses = new()
    {
        ["message"] = ReaderHarnesses.ReadMessage,
        ["entity"] = ReaderHarnesses.ReadEntityEvents,
        ["mbox"] = ReaderHarnesses.ReadMbox,
        ["emlx"] = ReaderHarnesses.ReadEmlx,
        ["mhtml"] = ReaderHarnesses.ReadMhtml,
        ["fields"] = FieldHarness.ParseFields
    };

    /// <summary>
    /// Runs the harness named by the last argument.  Under libFuzzer that is the only argument.  Otherwise, SharpFuzz runs the harness once on the
    /// file named by the first argument, so a run outside libFuzzer gives the path and then the name.
    /// </summary>
    private static int Main(string[] args)
    {
        if (args.Length is not (1 or 2) || !Harnesses.TryGetValue(args[^1], out var harness))
        {
            Console.Error.WriteLine($"Usage: [input-file] <{string.Join('|', Harnesses.Keys)}>");
            return 1;
        }

        Fuzzer.LibFuzzer.Run(input => harness(input.ToArray()));
        return 0;
    }
}
