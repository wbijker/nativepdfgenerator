using PdfSpec.Objects;

namespace PdfSpec.Layers;

/// <summary>
/// An Optional Content Group (ISO 32000-1 §8.11.2): a named layer that can be
/// toggled on/off in the viewer. Registered with the document via the catalog's
/// OCProperties; referenced by content via the page's Properties resource +
/// <c>/OC /name BDC</c>, or via XObject/annotation OC keys.
/// </summary>
public sealed class OptionalContentGroup
{
    public string Name { get; }
    public string? Intent { get; set; }

    public OptionalContentGroup(string name, string? intent = null)
    {
        Name = name;
        Intent = intent;
    }

    public PdfDictionary Build()
    {
        var d = new PdfDictionary
        {
            ["Type"] = new PdfName("OCG"),
            ["Name"] = new PdfString(Name),
        };
        if (Intent is not null) d["Intent"] = new PdfName(Intent);
        return d;
    }
}
