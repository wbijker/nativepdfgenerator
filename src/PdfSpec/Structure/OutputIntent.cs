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

    public string Subtype { get; }
    public string OutputConditionIdentifier { get; }

    public OutputIntent(string subtype, string outputConditionIdentifier)
    {
        Subtype = subtype;
        OutputConditionIdentifier = outputConditionIdentifier;
        Dictionary.Add("Type", new PdfName("OutputIntent"));
        Dictionary.Add("S", new PdfName(subtype));
        Dictionary.Add("OutputConditionIdentifier", new PdfString(outputConditionIdentifier));
    }

    public string? Info
    {
        set => Dictionary.Set("Info", value is null ? null : new PdfString(value));
    }

    public PdfReference? DestOutputProfile
    {
        set => Dictionary.Set("DestOutputProfile", value);
    }
}
