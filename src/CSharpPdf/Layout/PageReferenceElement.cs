using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;

/// <summary>
/// Renders the page number an earlier <see cref="NamedAnchorElement"/> ended up on,
/// formatted by <see cref="Format"/> (e.g. <c>"Page {0}"</c>). The page number is
/// looked up from <see cref="PdfContext.Captured"/>, which the measure phase has
/// already populated by the time the render phase runs — so a TOC drawn at the
/// front of a document can show real page numbers for sections that appear later.
/// Width is measured against a pessimistic sample so the two phases reserve the
/// same room.
/// </summary>
public sealed class PageReferenceElement : UIElement
{
    /// <summary>The anchor name registered with <see cref="NamedAnchorElement.Name"/>.</summary>
    public string Anchor { get; set; } = "";

    /// <summary>Format string; <c>{0}</c> is the looked-up page number.</summary>
    public string Format { get; set; } = "{0}";

    public Font Font { get; set; } = CSharpPdf.Text.Standard14Font.Helvetica;
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

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        var metrics = Font.GetVerticalMetrics(FontSize);
        int page = context.Lookup<int>(NamedAnchorElement.PageKey(Anchor));
        string text = string.Format(Format, page);
        double baseline = context.Cursor.Y - metrics.Ascent;
        context.DrawText(Font, FontSize, context.Cursor.X, baseline, text, FontColor);
        return new RenderResult(null, new Point(context.Cursor.X, context.Cursor.Y - metrics.LineHeight));
    }
}
