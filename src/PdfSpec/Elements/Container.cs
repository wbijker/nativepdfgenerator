using PdfSpec.Content;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Decorates a child <see cref="Element"/> with optional padding, a
/// background fill, and per-side borders. Layout is box-model: the
/// container reports the child's size plus padding and border widths.
/// At render time the child is rendered into a deferred sub-stream
/// first to discover its actual height; the background and borders are
/// then sized to that height and emitted onto the parent before the
/// sub-stream is flushed so they paint underneath the content.
/// </summary>
public class Container : Element
{
    public Element? Content { get; private set; }

    /// <summary>Set the wrapped child. Replaces any previous content.</summary>
    public Container Add(Element content)
    {
        Content = content;
        return this;
    }

    public double PaddingTop { get; set; }
    public double PaddingRight { get; set; }
    public double PaddingBottom { get; set; }
    public double PaddingLeft { get; set; }

    public PdfColor? Background { get; set; }

    public double BorderTopWidth { get; set; }
    public double BorderRightWidth { get; set; }
    public double BorderBottomWidth { get; set; }
    public double BorderLeftWidth { get; set; }
    public PdfColor? BorderTopColor { get; set; }
    public PdfColor? BorderRightColor { get; set; }
    public PdfColor? BorderBottomColor { get; set; }
    public PdfColor? BorderLeftColor { get; set; }

    public double HorizontalChrome => PaddingLeft + PaddingRight + BorderLeftWidth + BorderRightWidth;
    public double VerticalChrome => PaddingTop + PaddingBottom + BorderTopWidth + BorderBottomWidth;

    /// <summary>Set uniform padding on all four sides.</summary>
    public Container SetPadding(double all)
    {
        PaddingTop = PaddingRight = PaddingBottom = PaddingLeft = all;
        return this;
    }

    /// <summary>Set a uniform border (same width and colour on every side).</summary>
    public Container SetBorder(double width, PdfColor color)
    {
        BorderTopWidth = BorderRightWidth = BorderBottomWidth = BorderLeftWidth = width;
        BorderTopColor = BorderRightColor = BorderBottomColor = BorderLeftColor = color;
        return this;
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        double chromeW = HorizontalChrome;
        double chromeH = VerticalChrome;
        if (Content is null) return new PdfSizeHint(chromeW, chromeH, null, null);

        var inner = new PdfSize(
            Math.Max(0, available.Width - chromeW),
            Math.Max(0, available.Height - chromeH));

        var hint = Content.SizeHint(inner);

        return new PdfSizeHint(
            hint.MinWidth + chromeW,
            hint.MinHeight + chromeH,
            hint.MaxWidth is null ? null : hint.MaxWidth.Value + chromeW,
            hint.MaxHeight is null ? null : hint.MaxHeight.Value + chromeH);
    }

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        double w = available.Width;

        if (Content is null)
        {
            double chromeHeight = VerticalChrome;
            PaintBackgroundAndBorders(cs, w, chromeHeight);
            return RenderResult.Done(chromeHeight);
        }

        double innerX = PaddingLeft + BorderLeftWidth;
        double innerY = PaddingTop + BorderTopWidth;
        double innerW = Math.Max(0, w - HorizontalChrome);
        double innerH = Math.Max(0, available.Height - VerticalChrome);

        // Render the child into a sub-stream first — its buffer stays held
        // (no Build yet) so we can size the background/borders to the actual
        // content height before flushing the content on top of them.
        var sub = cs.CreateSubStream(innerX, innerY, innerW, innerH);
        var result = Content.Render(sub, new PdfSize(innerW, innerH));

        double boxHeight = result.NextY + VerticalChrome;
        PaintBackgroundAndBorders(cs, w, boxHeight);

        sub.Build();

        return RenderResult.Done(boxHeight);
    }

    private void PaintBackgroundAndBorders(ContentStream cs, double width, double height)
    {
        if (Background is { } bg)
            cs.DrawRectangle(0, 0, width, height, fill: bg);

        if (BorderTopColor is { } tc && BorderTopWidth > 0)
            cs.DrawRectangle(0, 0, width, BorderTopWidth, fill: tc);
        if (BorderRightColor is { } rc && BorderRightWidth > 0)
            cs.DrawRectangle(width - BorderRightWidth, 0, BorderRightWidth, height, fill: rc);
        if (BorderBottomColor is { } bc && BorderBottomWidth > 0)
            cs.DrawRectangle(0, height - BorderBottomWidth, width, BorderBottomWidth, fill: bc);
        if (BorderLeftColor is { } lc && BorderLeftWidth > 0)
            cs.DrawRectangle(0, 0, BorderLeftWidth, height, fill: lc);
    }
}
