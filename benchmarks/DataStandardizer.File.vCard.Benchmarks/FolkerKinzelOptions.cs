using FolkerKinzel.VCards.Enums;

namespace DataStandardizer.File.vCard.Benchmarks;

/// <summary>
/// The FolkerKinzel.VCards options the write benchmarks use.
/// </summary>
public static class FolkerKinzelOptions
{
    /// <summary>
    /// The default options, without <see cref="VcfOpts.UpdateTimeStamp"/> so the cards are written as they were read, and with the
    /// non-standard properties and parameters, which <c>VcardWriter</c> always writes.
    /// </summary>
    public const VcfOpts Write =
        (VcfOpts.Default & ~VcfOpts.UpdateTimeStamp) | VcfOpts.WriteNonStandardProperties | VcfOpts.WriteNonStandardParameters;
}
