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
    public TextAnnotationIcon Icon { get; }

    public TextAnnotation(PdfRectangle iconRect, string contents, TextAnnotationIcon icon = TextAnnotationIcon.Note)
        : base(iconRect)
    {
        Contents = contents;
        Icon = icon;
    }

    public override PdfDictionary Build()
    {
        var d = Base("Text");
        d.SetString("Contents", Contents);
        d.SetName("Name", Icon.ToString());
        return d;
    }
}

/// <summary>
/// Standard icon names for a Text annotation (<c>/Name</c> entry,
/// ISO 32000-1 §12.5.6.4 Table 172). Enum names match the PDF name objects
/// emitted to the file.
/// </summary>
public enum TextAnnotationIcon
{
    Comment,
    Key,
    Note,
    Help,
    NewParagraph,
    Paragraph,
    Insert,
}
