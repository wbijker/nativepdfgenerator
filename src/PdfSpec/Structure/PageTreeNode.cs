using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// A <c>/Pages</c> node in the page tree (ISO 32000-1 §7.7.3). Holds the list
/// of child page references and any inheritable defaults (MediaBox, Resources,
/// CropBox, Rotate) that descendants pick up.
/// </summary>
public sealed class PageTreeNode
{
    internal PdfDictionary Dictionary { get; } = new();
    private readonly PdfArray _kids = new();

    public PageTreeNode()
    {
        Dictionary["Type"] = new PdfName("Pages");
        Dictionary["Kids"] = _kids;
        Dictionary["Count"] = new PdfNumber(0L);
    }

    /// <summary>Total number of leaf pages reachable from this node.</summary>
    public int Count => _kids.Items.Count;

    /// <summary>Append a leaf page (or intermediate node) reference to this node.</summary>
    public void AddKid(PdfReference kid)
    {
        _kids.Add(kid);
        Dictionary["Count"] = new PdfNumber((long)_kids.Items.Count);
    }

    /// <summary>Default media box inherited by descendants without their own MediaBox.</summary>
    public PdfRectangle? MediaBox
    {
        set
        {
            if (value is null) Dictionary.Remove("MediaBox");
            else Dictionary["MediaBox"] = value.Value.ToArray();
        }
    }
}
