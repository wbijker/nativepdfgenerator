using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// A <see cref="BoxElement"/> that wraps a single child <see cref="Element"/>.
/// The chrome (padding, background, per-side borders, optional explicit
/// <see cref="BoxElement.Width"/> / <see cref="BoxElement.Height"/>,
/// horizontal / vertical alignment) lives on the base; this subclass only
/// owns the wrapped <see cref="Content"/> and implements the abstract
/// <see cref="BoxElement.Draw"/> hook to render it into the inner area.
/// </summary>
public class BorderElement : BoxElement
{
    public Element? Content { get; set; }

    /// <summary>Imperative-style content setter. Equivalent to assigning <see cref="Content"/>; returns <c>this</c> for chaining.</summary>
    public BorderElement SetContent(Element content)
    {
        Content = content;
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

    /// <summary>
    /// The child's natural drawing width — surfaced so
    /// <see cref="BoxElement.HorizontalAlignment"/> can distribute slack
    /// when the child is narrower than the inner area.
    /// </summary>
    protected override double? DrawNaturalWidth(PdfSize innerAvailable) =>
        Content?.SizeHint(innerAvailable).MaxWidth;

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        if (Content is null) return RenderResult.Done(0);
        return Content.Render(cs, available);
    }
}
