using PdfSpec.Content;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Decorates a child <see cref="Element"/> with optional padding, a
/// background fill, and per-side borders. Layout is box-model: the
/// element reports the child's size plus padding and border widths.
/// At render time the child is rendered into a deferred sub-stream
/// first to discover its actual height; the background and borders are
/// then sized to that height and emitted onto the parent before the
/// sub-stream is flushed so they paint underneath the content.
/// </summary>
public class BorderElement : Element
{
    public Element? Content { get; set; }

    /// <summary>Imperative-style content setter. Equivalent to assigning <see cref="Content"/>; returns <c>this</c> for chaining.</summary>
    public BorderElement SetContent(Element content)
    {
        Content = content;
        return this;
    }

    public double PaddingTop { get; set; }
    public double PaddingRight { get; set; }
    public double PaddingBottom { get; set; }
    public double PaddingLeft { get; set; }

    public PdfColor? Background { get; set; }

    /// <summary>
    /// Where <see cref="Content"/> sits within the inner area when its
    /// natural width is narrower than the available inner width. Slack is
    /// distributed as 0 / slack/2 / slack for Start / Center / End.
    /// </summary>
    public Alignment HorizontalAlignment { get; set; } = Alignment.Start;

    /// <summary>
    /// Where <see cref="Content"/> sits within the inner area vertically.
    /// When set to <see cref="Alignment.Start"/> (the default) the box
    /// shrinks to <c>content height + chrome</c> — there is no vertical slack
    /// and the content sits at the top. Setting <see cref="Alignment.Center"/>
    /// or <see cref="Alignment.End"/> makes the box claim the full
    /// <c>available.Height</c>, so the chrome (background + borders) extends
    /// to that height and the content can be positioned within the resulting
    /// vertical slack.
    /// </summary>
    public Alignment VerticalAlignment { get; set; } = Alignment.Start;

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
    public BorderElement SetPadding(double all)
    {
        PaddingTop = PaddingRight = PaddingBottom = PaddingLeft = all;
        return this;
    }

    /// <summary>Set a uniform border (same width and colour on every side).</summary>
    public BorderElement SetBorder(double width, PdfColor color)
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
        bool fillHeight = VerticalAlignment != Alignment.Start;

        if (Content is null)
        {
            double chromeOnlyHeight = fillHeight ? available.Height : VerticalChrome;
            PaintBackgroundAndBorders(cs, w, chromeOnlyHeight);
            return RenderResult.Done(chromeOnlyHeight);
        }

        double innerX = PaddingLeft + BorderLeftWidth;
        double innerY = PaddingTop + BorderTopWidth;
        double innerW = Math.Max(0, w - HorizontalChrome);
        double innerH = Math.Max(0, available.Height - VerticalChrome);

        // Natural width = child's MaxWidth clamped to the inner area. If the
        // child would draw narrower than the available inner width, the
        // difference is the horizontal slack distributed by HorizontalAlignment.
        var hint = Content.SizeHint(new PdfSize(innerW, innerH));
        double naturalW = Math.Min(innerW, hint.MaxWidth ?? innerW);
        double hSlack = Math.Max(0, innerW - naturalW);
        double xOffset = HorizontalAlignment switch
        {
            Alignment.Center => hSlack / 2,
            Alignment.End => hSlack,
            _ => 0,
        };

        // Render the child into a sub-stream first — its buffer stays held
        // (no Build yet) so we can size the chrome and resolve vertical
        // alignment after we know the content's actual height.
        var sub = cs.CreateSubStream(innerX + xOffset, innerY, naturalW, innerH);
        var result = Content.Render(sub, new PdfSize(naturalW, innerH));

        // Vertical slack lives between the content's actual height and the
        // inner box height. There only IS slack when we're filling vertically;
        // the shrink-to-content default has none, so the box always lands at
        // result.NextY + chrome.
        double boxHeight = fillHeight ? available.Height : result.NextY + VerticalChrome;
        double vSlack = fillHeight ? Math.Max(0, innerH - result.NextY) : 0;
        double yOffset = VerticalAlignment switch
        {
            Alignment.Center => vSlack / 2,
            Alignment.End => vSlack,
            _ => 0,
        };

        if (yOffset != 0) sub.SetParentPosition(innerX + xOffset, innerY + yOffset);

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
