using CSharpPdf.Geometry;

namespace CSharpPdf.Layout;

/// <summary>
/// Drops a sticky-note (Text) annotation at the current cursor with an attached
/// closed Popup that holds <see cref="Note"/>. Visually contributes a small
/// square in the document flow whose size is <see cref="Side"/> points.
/// </summary>
public sealed class TextNoteElement : UIElement
{
    public string Note { get; set; } = "";

    /// <summary>Builtin annotation icon name (Note, Comment, Help, Insert, Key, NewParagraph, Paragraph).</summary>
    public string Icon { get; set; } = "Note";

    /// <summary>Side length of the icon rectangle in points (default 18 pt).</summary>
    public double Side { get; set; } = 18;

    public TextNoteElement() { }
    public TextNoteElement(string note) { Note = note; }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var size = new SizeRect(Side, Side);
        return new SpaceDimension(size, size, verticalBreakable: false);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        Point start = context.Cursor;
        var iconRect = new PdfRectangle(start.X, start.Y - Side, start.X + Side, start.Y);
        var popupRect = new PdfRectangle(start.X + Side + 6, start.Y - 80, start.X + Side + 220, start.Y);
        context.Page.AddTextNote(iconRect, Note, Icon, popupRect, open: false);
        return new RenderResult(null, new Point(start.X, start.Y - Side));
    }
}
