namespace DataStandardizer.File.vCard.Benchmarks;

/// <summary>
/// The formats the format benchmarks read and write.
/// </summary>
public enum CardFormat
{
    /// <summary>
    /// vCard (<c>.vcf</c>), with <see cref="VcardReader"/> and <see cref="VcardWriter"/>.
    /// </summary>
    Vcf,

    /// <summary>
    /// jCard (RFC 7095), with <see cref="JcardReader"/> and <see cref="JcardWriter"/>.
    /// </summary>
    Jcard,

    /// <summary>
    /// xCard (RFC 6351), with <see cref="XcardReader"/> and <see cref="XcardWriter"/>.
    /// </summary>
    Xcard
}

/// <summary>
/// Reads and writes cards in each <see cref="CardFormat"/>.
/// </summary>
public static class CardFormats
{
    /// <summary>
    /// Reads every card of <paramref name="stream"/> in <paramref name="format"/>, as the sequence is enumerated.
    /// </summary>
    public static IEnumerable<VcardCard> Read(Stream stream, CardFormat format)
    {
        switch (format)
        {
            case CardFormat.Vcf:
                using (var reader = new VcardReader(stream))
                {
                    foreach (var card in reader.ReadCards())
                    {
                        yield return card;
                    }
                }

                break;
            case CardFormat.Jcard:
                using (var reader = new JcardReader(stream))
                {
                    foreach (var card in reader.ReadCards())
                    {
                        yield return card;
                    }
                }

                break;
            case CardFormat.Xcard:
                using (var reader = new XcardReader(stream))
                {
                    foreach (var card in reader.ReadCards())
                    {
                        yield return card;
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    /// <summary>
    /// Writes <paramref name="cards"/> to <paramref name="stream"/> in <paramref name="format"/>.
    /// </summary>
    public static void Write(Stream stream, CardFormat format, IEnumerable<VcardCard> cards)
    {
        switch (format)
        {
            case CardFormat.Vcf:
                using (var writer = new VcardWriter(stream))
                {
                    writer.WriteCards(cards);
                }

                break;
            case CardFormat.Jcard:
                using (var writer = new JcardWriter(stream))
                {
                    writer.WriteCards(cards);
                }

                break;
            case CardFormat.Xcard:
                using (var writer = new XcardWriter(stream))
                {
                    writer.WriteCards(cards);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }
}
