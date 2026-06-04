using PdfSpec.Actions;
using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Annotations;

/// <summary>
/// A Link annotation (ISO 32000-1 §12.5.6.5): a clickable rectangle bound to
/// an action. The default border is suppressed.
/// </summary>
public sealed class LinkAnnotation : Annotation
{
    public PdfAction Action { get; }

    public LinkAnnotation(PdfRectangle rect, PdfAction action) : base(rect) => Action = action;

    public override PdfDictionary Build()
    {
        var d = Base("Link");
        d.Add("Border", new PdfArray(new PdfNumber(0L), new PdfNumber(0L), new PdfNumber(0L)));
        d.Add("A", Action.Build());
        return d;
    }
}
