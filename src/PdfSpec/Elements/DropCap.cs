using PdfSpec.Content;
using PdfSpec.Fonts;
using PdfSpec.Geometry;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// A dropped capital — one or more glyphs (an initial letter, or a chapter
/// number) drawn large enough to fill the box it is given. Intended to be
/// floated at the start of a <see cref="ReflowParagraph"/> via
/// <see cref="ReflowParagraph.Float(Element, ReflowSide, double, double)"/>
/// so the body text wraps around it.
///
/// <para>
/// The glyph is auto-scaled so its cap height matches the box height,
/// clamped down if that would overflow the box width (so multi-digit
/// numbers shrink to fit rather than spill). It is drawn with its cap-top
/// aligned to the top of the box, lining up with the first wrapped text
/// line.
/// </para>
/// </summary>
public sealed class DropCap : Element
{
    /// <summary>The glyph(s) to draw — typically a single letter or a chapter number. Intentionally hides the <see cref="Element.Text(string)"/> static factory; on a <see cref="DropCap"/> instance <c>Text</c> is this glyph property.</summary>
    public new string Text { get; set; }

    /// <summary>Face used for the cap (e.g. a bold standard font).</summary>
    public Font Font { get; set; }

    /// <summary>Fill colour for the cap. <c>null</c> = device default (black).</summary>
    public PdfColor? Color { get; set; }

    public DropCap(string text, Font font, PdfColor? color = null)
    {
        Text = text;
        Font = font;
        Color = color;
    }

    public override PdfSizeHint SizeHint(PdfSize available) =>
        PdfSizeHint.Fixed(available.Width, available.Height);

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        if (string.IsNullOrEmpty(Text) || available.Height <= 0 || available.Width <= 0)
            return RenderResult.Done(available.Height);

        // Scale the glyph to fill the box. Cap height ≈ the font's ascent, so
        // the size whose ascent equals the box height fills it vertically;
        // clamp down if that size would overflow the box width.
        double ascentPerPt = Font.GetVerticalMetrics(100).Ascent / 100.0;
        double sizeForHeight = ascentPerPt > 0 ? available.Height / ascentPerPt : available.Height;

        double widthPerPt = Font.MeasureText(Text, 100) / 100.0;
        double sizeForWidth = widthPerPt > 0 ? available.Width / widthPerPt : sizeForHeight;

        double size = Math.Min(sizeForHeight, sizeForWidth);

        // Baseline placed an ascent below the top so the cap-top sits at y = 0.
        double baseline = Font.GetVerticalMetrics(size).Ascent;

        var text = cs.AddText(Font, size);
        if (Color is { } c) text.SetFillColor(c);
        text.SetBaseline(0, baseline);
        text.ShowText(Text);
        text.Build();

        return RenderResult.Done(available.Height);
    }
}
