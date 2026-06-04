using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// An OutputIntent dictionary (ISO 32000-1 §14.11.5) describing the target
/// colour space — a requirement of standards such as PDF/X and PDF/A.
/// </summary>
public sealed class OutputIntent
{
    public string Subtype { get; }
    public string OutputConditionIdentifier { get; }
    public string? Info { get; set; }
    public PdfReference? DestOutputProfile { get; set; }

    public OutputIntent(string subtype, string outputConditionIdentifier)
    {
        Subtype = subtype;
        OutputConditionIdentifier = outputConditionIdentifier;
    }

    public PdfDictionary Build()
    {
        var d = new PdfDictionary
        {
            { "Type", new PdfName("OutputIntent") },
            { "S", new PdfName(Subtype) },
            { "OutputConditionIdentifier", new PdfString(OutputConditionIdentifier) },
        };
        if (Info is not null) d.Add("Info", new PdfString(Info));
        if (DestOutputProfile is { } profile) d.Add("DestOutputProfile", profile);
        return d;
    }
}
