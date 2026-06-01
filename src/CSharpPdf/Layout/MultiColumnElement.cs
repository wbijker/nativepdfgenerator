using CSharpPdf.Content;
namespace CSharpPdf.Layout;

/// <summary>
/// Flows a single child across N equal-width columns side by side: column 1 is
/// rendered to the element's height, its overflow goes into column 2, that overflow
/// into column 3, and so on. Whatever is left after the last column becomes the
/// element's own overflow (so the next page continues the flow). The element is
/// itself a fixed-height block — set <see cref="Height"/> to the column height.
/// </summary>
public sealed class MultiColumnElement : UIElement
{
    /// <summary>The element whose content flows across columns.</summary>
    public UIElement? Content { get; set; }

    /// <summary>Number of columns (≥ 1).</summary>
    public int Columns { get; set; } = 2;

    /// <summary>Gap between columns in points.</summary>
    public double Gap { get; set; } = 12;

    /// <summary>Column height in points (the block is this tall).</summary>
    public double Height { get; set; }

    public MultiColumnElement() { }
    public MultiColumnElement(UIElement content, int columns, double height, double gap = 12)
    {
        Content = content;
        Columns = columns;
        Height = height;
        Gap = gap;
    }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var inner = InnerAvailable(available);
        var size = new SizeRect(inner.Width, Height);
        return WithOwnInset(new SpaceDimension(size, size, verticalBreakable: true));
    }

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        Point start = context.Cursor;
        if (Content is null || Columns < 1)
        {
            return new RenderResult(null, new Point(start.X, start.Y - Height));
        }

        double colWidth = (available.Width - (Columns - 1) * Gap) / Columns;
        UIElement? current = Content;
        for (int i = 0; i < Columns && current is not null; i++)
        {
            double x = start.X + i * (colWidth + Gap);
            context.Cursor = new Point(x, start.Y);
            var result = current.Render(context, new Size(colWidth, Height));
            current = result.Overflow;
        }

        var next = new Point(start.X, start.Y - Height);
        if (current is not null)
        {
            var overflow = new MultiColumnElement(current, Columns, Height, Gap);
            return new RenderResult(overflow, next);
        }
        return new RenderResult(null, next);
    }
}
