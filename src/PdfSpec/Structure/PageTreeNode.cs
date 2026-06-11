using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// A <c>/Pages</c> node in the page tree (ISO 32000-1 §7.7.3). Used for the
/// root, intermediate nodes, and leaves uniformly — the same dictionary shape
/// (Type, Kids, Count, optional Parent / MediaBox). <see cref="PdfDoc"/>
/// builds the tree bottom-up at save time using
/// <see cref="PdfDoc.PagesPerLeaf"/> and <see cref="PdfDoc.KidsPerNode"/>.
/// </summary>
public sealed class PageTreeNode : PdfObject
{
    private readonly PdfDictionary _dictionary = new();

    public PageTreeNode()
    {
        _dictionary.SetName("Type", "Pages");
    }

    private PdfRectangle? _mediaBox;

    /// <summary>Default media box inherited by descendants without their own MediaBox.</summary>
    public PdfRectangle? MediaBox
    {
        get => _mediaBox;
        set
        {
            _mediaBox = value;
            _dictionary.Set("MediaBox", value?.ToArray());
        }
    }

    /// <summary>Reference to the parent /Pages node — omitted for the root.</summary>
    internal PdfReference? Parent
    {
        set => _dictionary.Set("Parent", value);
    }

    /// <summary>
    /// Replace the <c>/Kids</c> array and set <c>/Count</c> to the total
    /// leaf-page count under this subtree.
    /// </summary>
    internal void SetKidsAndCount(IReadOnlyList<PdfReference> kids, int totalPages)
    {
        var array = new PdfArray();
        foreach (var k in kids) array.Add(k);
        _dictionary.Add("Kids", array);
        _dictionary.SetInteger("Count", totalPages);
    }

    public override void Write(Stream stream) => _dictionary.Write(stream);
}
