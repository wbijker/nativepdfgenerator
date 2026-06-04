using CSharpPdf.Content;
using Font = PdfSpec.Fonts.Font;

using PdfSpec.Geometry;
namespace CSharpPdf.Layout;

/// <summary>
/// Renders the page number an earlier <see cref="NamedAnchorElement"/> ended up on,
/// formatted by <see cref="Format"/> (e.g. <c>"Page {0}"</c>). The page number is
/// looked up from <see cref="PdfCanvas.Captured"/>, which the measure phase has
/// already populated by the time the render phase runs — so a TOC drawn at the
/// front of a document can show real page numbers for sections that appear later.
/// Width is measured against a pessimistic sample so the two phases reserve the
/// same room.
/// </summary>
public sealed class PageReferenceElement : Element
{
    /// <summary>The anchor name registered with <see cref="NamedAnchorElement.Name"/>.</summary>
    public string Anchor { get; set; } = "";

    /// <summary>Format string; <c>{0}</c> is the looked-up page number.</summary>
    public string Format { get; set; } = "{0}";

    public Font Font { get; set; } = PdfSpec.Fonts.Standard14Font.Helvetica;
    public double FontSize { get; set; } = 12;
    public Color FontColor { get; set; } = Colors.Black;

    public PageReferenceElement() { }
    public PageReferenceElement(string anchor, string format = "{0}")
    {
        Anchor = anchor;
        Format = format;
    }

    private string Sample => string.Format(Format, 999);

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var metrics = Font.GetVerticalMetrics(FontSize);
        double w = Font.MeasureText(Sample, FontSize);
        double h = metrics.Ascent + metrics.Descent;
        var size = new SizeRect(w, h);
        return WithOwnInset(new SpaceDimension(size, size, verticalBreakable: false));
    }

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        // Defer the actual draw — the anchor may be later in the document, so
        // its page hasn't been captured yet. Reserve the pessimistic width
        // (measured against the Sample) so the layout never reflows when the
        // closure paints the real number after every anchor has registered.
        var metrics = Font.GetVerticalMetrics(FontSize);
        double w = Font.MeasureText(Sample, FontSize);
        double h = metrics.Ascent + metrics.Descent;
        var font = Font;
        var fontSize = FontSize;
        var format = Format;
        var color = FontColor;
        var anchor = Anchor;
        context.Defer(w, h, sub =>
        {
            int page = sub.Lookup<int>(NamedAnchorElement.PageKey(anchor));
            string text = string.Format(format, page);
            double baseline = h - metrics.Ascent;
            sub.DrawText(font, fontSize, 0, baseline, text, color);
        });
        return new RenderResult(null, new Point(context.Cursor.X, context.Cursor.Y - metrics.LineHeight));
    }
}
