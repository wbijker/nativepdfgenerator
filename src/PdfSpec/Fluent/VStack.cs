using PdfSpec.Elements;
using ImperativeElement = PdfSpec.Layout.Element;
using ImperativeVStack = PdfSpec.Elements.VStack;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent wrapper around <see cref="ImperativeVStack"/>. Items stack
/// top-to-bottom; each item is either <see cref="Auto(Element, Alignment?)"/>
/// (shrink-to-content) or <see cref="Fixed(double, Element, Alignment?)"/>
/// (locked height). The imperative VStack intentionally has no
/// <c>Relative</c> slot, so neither does this wrapper.
///
/// <para>
/// Sizing / chrome / alignment of the stack itself live on
/// <see cref="ImperativeVStack"/> (inherited from
/// <see cref="BoxElement"/>); shape mirrors what <see cref="Container"/>
/// exposes. If you need those, build a Container around a VStack.
/// </para>
/// </summary>
public sealed class VStack : Element
{
    private readonly ImperativeVStack _impl = new();

    public VStack Auto(Element child, Alignment? horizontalAlignment = null)
    {
        _impl.AddAuto(child.Build(), horizontalAlignment);
        return this;
    }

    public VStack Fixed(double height, Element child, Alignment? horizontalAlignment = null)
    {
        _impl.AddFixed(height, child.Build(), horizontalAlignment);
        return this;
    }

    internal override ImperativeElement Build() => _impl;
}
