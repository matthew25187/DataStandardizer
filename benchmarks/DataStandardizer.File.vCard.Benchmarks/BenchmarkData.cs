using System.Globalization;
using System.Text;

namespace DataStandardizer.File.vCard.Benchmarks;

/// <summary>
/// Generates the seeded, in-memory vCard input shared by all benchmarks.
/// </summary>
public static class BenchmarkData
{
    public const int CardCount = 5_000;
    public const int Seed = 20261106;

    private const int FoldOctets = 75;

    // vCard 2.1 §2.1.3: a quoted-printable line is broken with a soft line break before it reaches 76 characters.
    private const int QuotedPrintableLineLength = 75;

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private static readonly (string Given, string Family)[] People =
    [
        ("Alice", "Smith"),
        ("Bob", "Jones"),
        ("Carol", "Brown"),
        ("Dave", "Taylor"),
        ("Erin", "Wilson"),
        ("Frank", "Walker"),
        ("Grace", "Te Rangi"),
        ("Heidi", "White"),
        ("Hēmi", "Pōtae"),
        ("Zoë", "Müller"),
        ("José", "Núñez"),
        ("François", "Lefèvre"),
        ("Søren", "Kierkegård"),
        ("Aroha", "Ngāta"),
        ("Łukasz", "Wójcik"),
        ("Mei", "Lín")
    ];

    private static readonly string[] Organizations =
    [
        "Example Ltd",
        "Acme Corporation",
        "Te Rūnanga o Ngāi Tahu",
        "Café Māori",
        "Northwind Traders",
        "Contoso Pharmaceuticals"
    ];

    private static readonly string[] Units = ["Sales", "Engineering", "Finance", "Research & Development", "Customer Success"];

    private static readonly string[] Titles =
    [
        "Account Manager",
        "Senior Engineer",
        "Chief Financial Officer",
        "Kaiwhakahaere",
        "Research Scientist, Level 2",
        "Head of Customer Success"
    ];

    private static readonly (string Street, string Locality, string Region, string Code, string Country)[] Addresses =
    [
        ("12 Queen Street", "Auckland", "Auckland", "1010", "New Zealand"),
        ("Level 4, 1 Willis Street", "Wellington", "Wellington", "6011", "New Zealand"),
        ("221B Baker Street", "London", "", "NW1 6XE", "United Kingdom"),
        ("Hauptstraße 5", "München", "Bayern", "80331", "Deutschland"),
        ("1600 Amphitheatre Parkway", "Mountain View", "CA", "94043", "USA"),
        ("Calle de Alcalá 42", "Madrid", "", "28014", "España")
    ];

    private static readonly string[] Sentences =
    [
        "Met at the conference in Ōtautahi; follow up about the pilot.",
        "Prefers email to phone calls, and replies within a day.",
        "Assistant: Tama, +64 9 555 0199.",
        "Birthday lunch every April, usually at the café on the corner.",
        "Path to the shared folder: \\\\fileserver\\contacts\\2026",
        "Speaks English, te reo Māori and some French.",
        "Notes from the last call:\n1. Pricing\n2. Delivery dates\n3. Next steps",
        "Do not call before 9am, or after 5pm on a Friday.",
        "Introduced by \"Heidi\" from the Wellington office.",
        "Owes us a reply about the contract, the invoice and the schedule."
    ];

    private static readonly string[] LanguageTags = ["en", "en-NZ", "mi", "en-GB", "de-DE", "fr-FR", "es-419", "zh-Hant-TW", "sr-Latn-RS", "ja"];

    private static readonly string[] Categories = ["friends", "work", "family", "customers", "suppliers", "board", "golf club"];

    /// <summary>
    /// The language tags the language-tag benchmarks parse: the tags of the corpus, and others of each shape that RFC 5646 §2.1 allows.
    /// </summary>
    public static readonly string[] LanguageTagValues =
    [
        "en",
        "en-NZ",
        "mi",
        "en-GB",
        "de-DE",
        "fr-FR",
        "es-419",
        "zh-Hant-TW",
        "sr-Latn-RS",
        "ja",
        "de-CH-1996",
        "sl-rozaj-biske",
        "zh-yue-HK",
        "hy-Latn-IT-arevela",
        "en-US-u-ca-gregory",
        "de-DE-x-phonebk",
        "x-whatever",
        "i-klingon",
        "zh-min-nan",
        "qaa-Qaaa-QM-x-southern"
    ];

    /// <summary>
    /// Builds the vCard 4.0 corpus: <see cref="CardCount"/> cards, each with the properties a contacts application writes.
    /// </summary>
    public static byte[] CreateVersion40()
    {
        var random = new Random(Seed);
        var builder = new StringBuilder(CardCount * 1_200);
        for (var i = 0; i < CardCount; i++)
        {
            AppendVersion40Card(builder, random, i);
        }

        return Utf8NoBom.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Builds the vCard 2.1 corpus: <see cref="CardCount"/> cards in the forms that phones export, with most text values in quoted-printable.
    /// </summary>
    public static byte[] CreateVersion21()
    {
        var random = new Random(Seed + 1);
        var builder = new StringBuilder(CardCount * 1_400);
        for (var i = 0; i < CardCount; i++)
        {
            AppendVersion21Card(builder, random, i);
        }

        return Utf8NoBom.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Counts the logical (unfolded) content lines of a corpus.
    /// </summary>
    public static int CountContentLines(byte[] vcf)
    {
        using var stream = new MemoryStream(vcf, writable: false);
        using var reader = new VcardContentLineReader(stream);
        var count = 0;
        while (reader.ReadContentLine())
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Counts the lines of a corpus that have <c>ENCODING=QUOTED-PRINTABLE</c>.
    /// </summary>
    public static int CountQuotedPrintableLines(byte[] vcf)
    {
        using var stream = new MemoryStream(vcf, writable: false);
        using var reader = new VcardContentLineReader(stream);
        var count = 0;
        while (reader.ReadContentLine())
        {
            var line = reader.CurrentLine;
            var index = line.IndexOfParameter("ENCODING");
            if (index >= 0 && line.GetParameterValue(index, 0).Equals("QUOTED-PRINTABLE", StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    private static void AppendVersion40Card(StringBuilder builder, Random random, int index)
    {
        var (given, family) = People[random.Next(People.Length)];
        var organization = Organizations[random.Next(Organizations.Length)];
        var address = Addresses[random.Next(Addresses.Length)];
        var email = $"{Ascii(given)}.{Ascii(family)}{index}@example.com".ToLowerInvariant().Replace(' ', '-');

        AppendLine(builder, "BEGIN:VCARD");
        AppendLine(builder, "VERSION:4.0");
        AppendLine(builder, $"FN:{EscapeText($"{given} {family}")}");
        AppendLine(builder, $"N:{EscapeText(family)};{EscapeText(given)};;;");
        if (random.Next(4) == 0)
        {
            AppendLine(builder, $"NICKNAME:{EscapeText(given[..Math.Min(3, given.Length)])}");
        }

        AppendLine(builder, "KIND:individual");
        AppendLine(builder, $"ORG:{EscapeText(organization)};{EscapeText(Units[random.Next(Units.Length)])}");
        AppendLine(builder, $"TITLE:{EscapeText(Titles[random.Next(Titles.Length)])}");
        AppendLine(builder, $"TEL;VALUE=uri;TYPE=work,voice;PREF=1:tel:+64-9-555-{random.Next(10_000):D4}");
        if (random.Next(2) == 0)
        {
            AppendLine(builder, $"TEL;VALUE=uri;TYPE=cell:tel:+64-21-555-{random.Next(10_000):D4}");
        }

        AppendLine(builder, $"EMAIL;TYPE=work;PREF=1:{email}");
        if (random.Next(3) == 0)
        {
            AppendLine(builder, $"item1.EMAIL;TYPE=home:{Ascii(given).ToLowerInvariant()}{index}@example.net");
            AppendLine(builder, "item1.X-ABLABEL:Personal");
        }

        var label = $"{address.Street}\n{address.Locality} {address.Code}\n{address.Country}";
        AppendLine(builder,
            $"ADR;TYPE=work;LABEL=\"{EncodeCaret(label)}\":;;{EscapeText(address.Street)};{EscapeText(address.Locality)};" +
            $"{EscapeText(address.Region)};{EscapeText(address.Code)};{EscapeText(address.Country)}");
        AppendLine(builder, $"URL:https://example.com/people/{index}");
        AppendLine(builder, $"NOTE:{EscapeText(Note(random))}");
        if (random.Next(3) != 0)
        {
            var birthday = new DateTime(1950, 1, 1).AddDays(random.Next(0, 18_000));
            AppendLine(builder, random.Next(5) == 0
                ? string.Create(CultureInfo.InvariantCulture, $"BDAY:--{birthday:MMdd}")
                : string.Create(CultureInfo.InvariantCulture, $"BDAY:{birthday:yyyyMMdd}"));
        }

        AppendLine(builder, $"LANG;PREF=1:{LanguageTags[random.Next(LanguageTags.Length)]}");
        if (random.Next(2) == 0)
        {
            AppendLine(builder, $"LANG;PREF=2:{LanguageTags[random.Next(LanguageTags.Length)]}");
        }

        AppendLine(builder, "TZ:Pacific/Auckland");
        AppendLine(builder, string.Create(CultureInfo.InvariantCulture,
            $"GEO:geo:{-36.8 - random.NextDouble():F4}\\,{174.7 + random.NextDouble():F4}"));
        AppendLine(builder, $"CATEGORIES:{Categories[random.Next(Categories.Length)]},{Categories[random.Next(Categories.Length)]}");
        if (random.Next(10) == 0)
        {
            AppendLine(builder, $"PHOTO:data:image/jpeg;base64\\,{Convert.ToBase64String(Bytes(random, 1_200))}");
        }

        AppendLine(builder, $"UID:urn:uuid:{new Guid(Bytes(random, 16))}");
        AppendLine(builder, string.Create(CultureInfo.InvariantCulture,
            $"REV:{new DateTime(2026, 1, 1).AddMinutes(random.Next(0, 400_000)):yyyyMMdd'T'HHmmss'Z'}"));
        AppendLine(builder, $"X-PHONETIC-LAST-NAME:{EscapeText(family)}");
        AppendLine(builder, "END:VCARD");
    }

    private static void AppendVersion21Card(StringBuilder builder, Random random, int index)
    {
        var (given, family) = People[random.Next(People.Length)];
        var organization = Organizations[random.Next(Organizations.Length)];
        var address = Addresses[random.Next(Addresses.Length)];

        AppendLine(builder, "BEGIN:VCARD");
        AppendLine(builder, "VERSION:2.1");
        AppendQuotedPrintableLine(builder, "N", $"{family};{given};;;", random.Next(2) == 0);
        AppendQuotedPrintableLine(builder, "FN", $"{given} {family}", random.Next(2) == 0);
        AppendQuotedPrintableLine(builder, "ORG", $"{organization};{Units[random.Next(Units.Length)]}", random.Next(2) == 0);
        AppendQuotedPrintableLine(builder, "TITLE", Titles[random.Next(Titles.Length)], random.Next(2) == 0);
        AppendLine(builder, $"TEL;WORK;VOICE;PREF:+64-9-555-{random.Next(10_000):D4}");
        AppendLine(builder, $"TEL;CELL:+64-21-555-{random.Next(10_000):D4}");
        AppendLine(builder, $"EMAIL;INTERNET:{Ascii(given).ToLowerInvariant()}.{Ascii(family).ToLowerInvariant().Replace(' ', '-')}{index}@example.com");
        AppendQuotedPrintableLine(builder, "ADR;WORK",
            $";;{address.Street};{address.Locality};{address.Region};{address.Code};{address.Country}", random.Next(2) == 0);
        AppendQuotedPrintableLine(builder, "LABEL;WORK",
            $"{address.Street}\r\n{address.Locality} {address.Code}\r\n{address.Country}", alwaysEncode: true);
        AppendQuotedPrintableLine(builder, "NOTE", Note(random).Replace("\n", "\r\n"), alwaysEncode: true);
        if (random.Next(3) != 0)
        {
            AppendLine(builder, string.Create(CultureInfo.InvariantCulture,
                $"BDAY:{new DateTime(1950, 1, 1).AddDays(random.Next(0, 18_000)):yyyy-MM-dd}"));
        }

        // No GEO: FolkerKinzel.VCards 8.2.0 drops the comma form vCard 2.1 gives it (GEO:lat,lon), so the readers wouldn't do the same work.
        AppendLine(builder, "URL:https://example.com/people/" + index.ToString(CultureInfo.InvariantCulture));
        if (random.Next(10) == 0)
        {
            AppendBase64Line(builder, "PHOTO;ENCODING=BASE64;TYPE=JPEG", Bytes(random, 1_200));
        }

        AppendLine(builder, $"UID:{new Guid(Bytes(random, 16))}");
        AppendLine(builder, string.Create(CultureInfo.InvariantCulture,
            $"REV:{new DateTime(2026, 1, 1).AddMinutes(random.Next(0, 400_000)):yyyyMMdd'T'HHmmss'Z'}"));
        AppendLine(builder, "END:VCARD");
    }

    private static string Note(Random random)
    {
        var count = random.Next(1, 4);
        var builder = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(Sentences[random.Next(Sentences.Length)]);
        }

        return builder.ToString();
    }

    private static byte[] Bytes(Random random, int count)
    {
        var bytes = new byte[count];
        random.NextBytes(bytes);
        return bytes;
    }

    /// <summary>
    /// Appends a content line, folded so that no physical line is longer than 75 octets of UTF-8 (RFC 6350 §3.2).
    /// </summary>
    private static void AppendLine(StringBuilder builder, string line)
    {
        var octets = 0;
        var limit = FoldOctets;
        for (var i = 0; i < line.Length; i++)
        {
            var width = char.IsHighSurrogate(line[i]) ? 4 : char.IsLowSurrogate(line[i]) ? 0 : line[i] < 0x80 ? 1 : line[i] < 0x800 ? 2 : 3;
            if (octets + width > limit)
            {
                builder.Append("\r\n ");
                octets = 0;
                limit = FoldOctets - 1;
            }

            builder.Append(line[i]);
            octets += width;
        }

        builder.Append("\r\n");
    }

    /// <summary>
    /// Appends a vCard 2.1 line whose value is in quoted-printable (vCard 2.1 §2.1.3) with <c>CHARSET=UTF-8</c>, if it has to be or
    /// <paramref name="alwaysEncode"/> says so, and otherwise as it is.  A soft line break keeps each physical line under 76 characters.
    /// </summary>
    private static void AppendQuotedPrintableLine(StringBuilder builder, string nameAndParameters, string value, bool alwaysEncode)
    {
        var needsEncoding = alwaysEncode || value.Any(c => c >= 0x80 || c < 0x20);
        if (!needsEncoding)
        {
            builder.Append(nameAndParameters).Append(':').Append(value).Append("\r\n");
            return;
        }

        var prefix = $"{nameAndParameters};ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:";
        builder.Append(prefix);
        var column = prefix.Length;
        var bytes = Encoding.UTF8.GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            var isLast = i == bytes.Length - 1;
            var literal = b is >= 0x21 and <= 0x7E and not (byte)'=' || (b == (byte)' ' && !isLast);
            var token = literal ? ((char)b).ToString() : $"={b:X2}";

            // Leave room for the '=' of a soft line break.
            if (column + token.Length > QuotedPrintableLineLength - 1)
            {
                builder.Append("=\r\n");
                column = 0;
            }

            builder.Append(token);
            column += token.Length;
        }

        builder.Append("\r\n");
    }

    /// <summary>
    /// Appends a vCard 2.1 line whose value is in <c>BASE64</c>, on indented lines after the property's own, ending with the empty
    /// line that vCard 2.1 §2.9 requires.
    /// </summary>
    private static void AppendBase64Line(StringBuilder builder, string nameAndParameters, byte[] data)
    {
        builder.Append(nameAndParameters).Append(":\r\n");
        var base64 = Convert.ToBase64String(data);
        for (var i = 0; i < base64.Length; i += 72)
        {
            builder.Append("    ").Append(base64, i, Math.Min(72, base64.Length - i)).Append("\r\n");
        }

        builder.Append("\r\n");
    }

    /// <summary>
    /// Escapes a TEXT value (RFC 6350 §3.4).
    /// </summary>
    private static string EscapeText(string value)
    {
        return value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
    }

    /// <summary>
    /// Encodes a parameter value with RFC 6868 caret encoding.
    /// </summary>
    private static string EncodeCaret(string value)
    {
        return value.Replace("^", "^^").Replace("\n", "^n").Replace("\"", "^'");
    }

    /// <summary>
    /// Gives the letters of a name without their diacritics, for an email address.
    /// </summary>
    private static string Ascii(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            if (c < 0x80)
            {
                builder.Append(c);
            }
            else if (c == 'Ł')
            {
                builder.Append('L');
            }
            else if (c == 'ø')
            {
                builder.Append('o');
            }
        }

        return builder.ToString();
    }
}
