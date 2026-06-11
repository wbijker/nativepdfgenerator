using PdfSpec.Layout;
using ImperativeElement = PdfSpec.Layout.Element;
using ImperativeDeferred = PdfSpec.Elements.DeferredComponent;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent wrapper around <see cref="ImperativeDeferred"/>. Two-phase
/// content: <paramref name="SizeHint"/> reserves a box during the
/// normal layout pass; <paramref name="Render"/> runs once the page
/// count is known and decides what actually paints into that box. Use
/// for page numbers, "Page N of M" footers, or any element whose
/// content depends on the final document structure.
/// </summary>
public sealed class Deferred : Element
{
    private readonly ImperativeDeferred _impl;

    internal Deferred(Element sizeHint, Func<PageData, Element> render) =>
        _impl = new(sizeHint.Build(), data => render(data).Build());

    internal override ImperativeElement Build() => _impl;
}
