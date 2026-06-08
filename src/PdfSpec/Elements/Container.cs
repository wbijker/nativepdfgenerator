using PdfSpec.Content;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Decorates a child <see cref="Element"/> with optional padding, a
/// background fill, and per-side borders. Layout is box-model: the
/// container reports the child's size plus padding and border widths;
/// at render time it paints background (full box), then each border
/// side (only where both width &gt; 0 and a colour is set), then the
/// child inside a sub-stream offset by padding + border thickness.
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
        double w = available.Width, h = available.Height;

        if (Background is { } bg)
            cs.DrawRectangle(0, 0, w, h, fill: bg);

        if (BorderTopColor is { } tc && BorderTopWidth > 0)
            cs.DrawRectangle(0, 0, w, BorderTopWidth, fill: tc);
        if (BorderRightColor is { } rc && BorderRightWidth > 0)
            cs.DrawRectangle(w - BorderRightWidth, 0, BorderRightWidth, h, fill: rc);
        if (BorderBottomColor is { } bc && BorderBottomWidth > 0)
            cs.DrawRectangle(0, h - BorderBottomWidth, w, BorderBottomWidth, fill: bc);
        if (BorderLeftColor is { } lc && BorderLeftWidth > 0)
            cs.DrawRectangle(0, 0, BorderLeftWidth, h, fill: lc);

        if (Content is null) return RenderResult.Done(VerticalChrome);

        double innerX = PaddingLeft + BorderLeftWidth;
        double innerY = PaddingTop + BorderTopWidth;
        double innerW = Math.Max(0, w - HorizontalChrome);
        double innerH = Math.Max(0, h - VerticalChrome);

        var sub = cs.CreateSubStream(innerX, innerY, innerW, innerH);
        var result = Content.Render(sub, new PdfSize(innerW, innerH));
        sub.Build();

        return RenderResult.Done(result.NextY + VerticalChrome);
    }
}
