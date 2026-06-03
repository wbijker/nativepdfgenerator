using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Annotations;

/// <summary>
/// A Pop-up annotation (ISO 32000-1 §12.5.6.14) — the floating window that
/// displays text for a parent markup annotation (e.g. a sticky note). The
/// /Parent link is filled in by <see cref="PdfPage.AddTextNote"/> when paired.
/// </summary>
public sealed class PopupAnnotation : Annotation
{
    public bool Open { get; }
    public PdfReference? Parent { get; set; }

    public PopupAnnotation(PdfRectangle rect, bool open = true) : base(rect) => Open = open;

    public override PdfDictionary Build()
    {
        var d = Base("Popup");
        d["Open"] = new PdfBoolean(Open);
        if (Parent is { } p) d["Parent"] = p;
        return d;
    }
}
