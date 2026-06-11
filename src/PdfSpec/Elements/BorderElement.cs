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
public partial class BorderElement : BoxElement
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
        // Explicit Width/Height short-circuit the content measurement:
        // the box claims exactly that extent (Min and Max collapse to it),
        // so a parent layout sizes the column / band to the requested
        // value rather than to whatever the content wants.
        var explicitW = ResolveWidth(available.Width);
        var explicitH = ResolveHeight(available.Height);

        double chromeW = HorizontalChrome;
        double chromeH = VerticalChrome;

        var inner = new PdfSize(
            Math.Max(0, (explicitW ?? available.Width) - chromeW),
            Math.Max(0, (explicitH ?? available.Height) - chromeH));

        var hint = Content?.SizeHint(inner) ?? new PdfSizeHint(0, 0, null, null);

        double minW = explicitW ?? (Content is null ? chromeW : hint.MinWidth + chromeW);
        double minH = explicitH ?? (Content is null ? chromeH : hint.MinHeight + chromeH);
        double? maxW = explicitW ?? (hint.MaxWidth is null ? null : hint.MaxWidth.Value + chromeW);
        double? maxH = explicitH ?? (hint.MaxHeight is null ? null : hint.MaxHeight.Value + chromeH);

        return new PdfSizeHint(minW, minH, maxW, maxH);
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
