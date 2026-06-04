using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// An OutputIntent dictionary (ISO 32000-1 §14.11.5) describing the target
/// colour space — a requirement of standards such as PDF/X and PDF/A.
/// State is held directly in the dictionary.
/// </summary>
public sealed class OutputIntent
{
    internal PdfDictionary Dictionary { get; } = new();

    public OutputIntentSubtype Subtype { get; }
    public string OutputConditionIdentifier { get; }

    public OutputIntent(OutputIntentSubtype subtype, string outputConditionIdentifier)
    {
        Subtype = subtype;
        OutputConditionIdentifier = outputConditionIdentifier;
        Dictionary.SetName("Type", "OutputIntent");
        Dictionary.SetName("S", SubtypeName(subtype));
        Dictionary.SetString("OutputConditionIdentifier", outputConditionIdentifier);
    }

    public string? Info { set => Dictionary.SetString("Info", value); }

    public PdfReference? DestOutputProfile { set => Dictionary.Set("DestOutputProfile", value); }

    internal static string SubtypeName(OutputIntentSubtype subtype) => subtype switch
    {
        OutputIntentSubtype.PdfX => "GTS_PDFX",
        OutputIntentSubtype.PdfA => "GTS_PDFA1",
        OutputIntentSubtype.PdfE => "ISO_PDFE1",
        _ => throw new ArgumentOutOfRangeException(nameof(subtype), subtype, null),
    };
}

/// <summary>
/// OutputIntent <c>/S</c> entry (ISO 32000-1 §14.11.5 Table 365). Identifies
/// the PDF subset standard the output intent is for. The actual PDF name
/// emitted is <c>GTS_PDFX</c> / <c>GTS_PDFA1</c> / <c>ISO_PDFE1</c>;
/// <c>GTS_PDFA1</c> is reused by PDF/A-2 and PDF/A-3 as well as PDF/A-1.
/// </summary>
public enum OutputIntentSubtype
{
    /// <summary>PDF/X family — emitted as <c>/GTS_PDFX</c>.</summary>
    PdfX,

    /// <summary>PDF/A family (1, 2, 3) — emitted as <c>/GTS_PDFA1</c>.</summary>
    PdfA,

    /// <summary>PDF/E-1 — emitted as <c>/ISO_PDFE1</c>.</summary>
    PdfE,
}
