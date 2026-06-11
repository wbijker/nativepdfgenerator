using PdfSpec.Elements;
using ImperativeElement = PdfSpec.Layout.Element;
using ImperativeVFrame = PdfSpec.Elements.VFrame;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent wrapper around <see cref="ImperativeVFrame"/>. The vertical
/// mirror of <see cref="HStack"/> — items stack top-to-bottom with
/// three sizing modes (<see cref="Fixed"/>, <see cref="Auto"/>,
/// <see cref="Relative"/>) and the frame always consumes its full
/// available height. Not breakable across pages — use
/// <see cref="VStack"/> for that.
/// </summary>
public sealed class VFrame : Element
{
    private readonly ImperativeVFrame _impl = new();

    public VFrame Fixed(double height, Element child, Alignment? horizontalAlignment = null)
    {
        _impl.AddFixed(height, child.Build(), horizontalAlignment);
        return this;
    }

    public VFrame Auto(Element child, Alignment? horizontalAlignment = null)
    {
        _impl.AddAuto(child.Build(), horizontalAlignment);
        return this;
    }

    public VFrame Relative(double units, Element child, Alignment? horizontalAlignment = null)
    {
        _impl.AddRelative(units, child.Build(), horizontalAlignment);
        return this;
    }

    internal override ImperativeElement Build() => _impl;
}
