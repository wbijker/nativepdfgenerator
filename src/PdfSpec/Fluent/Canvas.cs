using PdfSpec.Content;
using PdfSpec.Layout;
using ImperativeElement = PdfSpec.Layout.Element;
using ImperativeCanvas = PdfSpec.Elements.Canvas;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent wrapper around <see cref="ImperativeCanvas"/> — the escape
/// hatch for imperative drawing inside a fluent composition. Built with
/// a width, height, and a <c>Draw</c> delegate that receives the
/// sub-content-stream rooted at (0, 0) of the canvas.
/// </summary>
public sealed class Canvas : Element
{
    private readonly ImperativeCanvas _impl;

    internal Canvas(double width, double height, Action<ContentStream, PdfSize> draw) =>
        _impl = new() { Width = width, Height = height, Draw = draw };

    internal override ImperativeElement Build() => _impl;
}
