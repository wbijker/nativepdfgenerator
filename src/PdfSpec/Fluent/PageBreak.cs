using ImperativeElement = PdfSpec.Layout.Element;
using ImperativePageBreak = PdfSpec.Elements.PageBreak;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent wrapper around <see cref="ImperativePageBreak"/> — the layout
/// sentinel that forces the next item in its parent container onto a
/// new page.
/// </summary>
public sealed class PageBreak : Element
{
    private readonly ImperativePageBreak _impl = new();
    internal override ImperativeElement Build() => _impl;
}
