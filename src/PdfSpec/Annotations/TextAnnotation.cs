using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace PdfSpec.Annotations;

/// <summary>
/// A Text ("sticky note") annotation (ISO 32000-1 §12.5.6.4). A small icon at
/// <see cref="Annotation.Rect"/>; clicking it opens a popup carrying
/// <see cref="Contents"/>.
/// </summary>
public sealed class TextAnnotation : Annotation
{
    public string Contents { get; }
    public string Icon { get; }

    public TextAnnotation(PdfRectangle iconRect, string contents, string icon = "Note")
        : base(iconRect)
    {
        Contents = contents;
        Icon = icon;
    }

    public override PdfDictionary Build()
    {
        var d = Base("Text");
        d.Add("Contents", new PdfString(Contents));
        d.Add("Name", new PdfName(Icon));
        return d;
    }
}
