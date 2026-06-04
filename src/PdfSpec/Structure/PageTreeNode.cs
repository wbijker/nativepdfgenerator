using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// A <c>/Pages</c> node in the page tree (ISO 32000-1 §7.7.3). Holds the
/// list of child page references and any inheritable defaults; emits the
/// dictionary fresh at write time.
/// </summary>
public sealed class PageTreeNode : PdfObject
{
    private readonly List<PdfReference> _kids = new();

    /// <summary>Default media box inherited by descendants without their own MediaBox.</summary>
    public PdfRectangle? MediaBox { get; set; }

    public int Count => _kids.Count;

    /// <summary>Append a leaf page (or intermediate node) reference to this node.</summary>
    public void AddKid(PdfReference kid) => _kids.Add(kid);

    public override void Write(Stream stream) => Build().Write(stream);

    private PdfDictionary Build()
    {
        var kids = new PdfArray();
        foreach (var kid in _kids) kids.Add(kid);

        var d = new PdfDictionary
        {
            { "Type", new PdfName("Pages") },
        };
        if (MediaBox is { } mb) d.Add("MediaBox", mb.ToArray());
        d.Add("Kids", kids);
        d.Add("Count", new PdfNumber((long)_kids.Count));
        return d;
    }
}
