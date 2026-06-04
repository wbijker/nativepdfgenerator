using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// A <c>/Pages</c> node in the page tree (ISO 32000-1 §7.7.3). Wraps a single
/// <see cref="PdfDictionary"/> mutated in place; <see cref="Write"/>
/// delegates to it. The Kids array and Count entry are populated as pages
/// are added.
/// </summary>
public sealed class PageTreeNode : PdfObject
{
    private readonly PdfDictionary _dictionary = new();
    private readonly PdfArray _kids = new();

    public PageTreeNode()
    {
        _dictionary.SetName("Type", "Pages");
        _dictionary.Add("Kids", _kids);
        _dictionary.SetInteger("Count", 0);
    }

    /// <summary>Default media box inherited by descendants without their own MediaBox.</summary>
    public PdfRectangle? MediaBox
    {
        set => _dictionary.Set("MediaBox", value?.ToArray());
    }

    public int Count => _kids.Items.Count;

    /// <summary>Append a leaf page (or intermediate node) reference to this node.</summary>
    public void AddKid(PdfReference kid)
    {
        _kids.Add(kid);
        // SetInteger replaces the existing /Count entry in place.
        _dictionary.SetInteger("Count", _kids.Items.Count);
    }

    public override void Write(Stream stream) => _dictionary.Write(stream);
}
