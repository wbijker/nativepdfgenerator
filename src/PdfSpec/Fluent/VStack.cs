using ImperativeElement = PdfSpec.Layout.Element;
using ImperativeVStack = PdfSpec.Elements.VStack;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent wrapper around <see cref="ImperativeVStack"/>. Items stack
/// top-to-bottom; each item is either
/// <see cref="Auto(Element, HorizontalAlignment?)"/> (shrink-to-content)
/// or <see cref="Fixed(double, Element, HorizontalAlignment?)"/>
/// (locked height). The imperative VStack intentionally has no
/// <c>Relative</c> slot, so neither does this wrapper.
/// </summary>
public sealed class VStack : Element
{
    private readonly ImperativeVStack _impl = new();

    public VStack Auto(Element child, HorizontalAlignment? horizontalAlignment = null)
    {
        _impl.AddAuto(child.Build(), horizontalAlignment);
        return this;
    }

    public VStack Fixed(double height, Element child, HorizontalAlignment? horizontalAlignment = null)
    {
        _impl.AddFixed(height, child.Build(), horizontalAlignment);
        return this;
    }

    internal override ImperativeElement Build() => _impl;
}
