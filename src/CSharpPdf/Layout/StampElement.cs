using CSharpPdf.Geometry;
using CSharpPdf.Objects;

namespace CSharpPdf.Layout;

/// <summary>
/// Drops a rubber-stamp annotation by built-in <see cref="Name"/> (Approved,
/// Confidential, Draft, Experimental, Final, ForComment, NotApproved,
/// NotForPublicRelease, Sold, TopSecret, …). PDF readers render these
/// stamps with their own appearance; the rendered look therefore varies by
/// reader. <see cref="Contents"/> is the popup text shown when the stamp is
/// hovered or opened.
/// </summary>
public sealed class StampElement : UIElement
{
    public string Name { get; set; } = "Approved";
    public string? Contents { get; set; }
    public double Width { get; set; } = 140;
    public double Height { get; set; } = 50;

    public StampElement() { }
    public StampElement(string name, double width = 140, double height = 50)
    {
        Name = name;
        Width = width;
        Height = height;
    }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var size = new SizeRect(Width, Height);
        return new SpaceDimension(size, size, verticalBreakable: false);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        Point start = context.Cursor;
        var rect = new PdfRectangle(start.X, start.Y - Height, start.X + Width, start.Y);
        var stamp = new PdfDictionary
        {
            ["Type"] = new PdfName("Annot"),
            ["Subtype"] = new PdfName("Stamp"),
            ["Rect"] = rect.ToArray(),
            ["Name"] = new PdfName(Name),
        };
        if (Contents is not null)
        {
            stamp["Contents"] = new PdfString(Contents);
        }
        context.Page.AddAnnotation(stamp);
        return new RenderResult(null, new Point(start.X, start.Y - Height));
    }
}
