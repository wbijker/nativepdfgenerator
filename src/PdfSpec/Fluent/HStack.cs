using PdfSpec.Elements;
using ImperativeElement = PdfSpec.Layout.Element;
using ImperativeHStack = PdfSpec.Elements.HStack;

namespace PdfSpec.Fluent;

/// <summary>
/// Fluent wrapper around <see cref="ImperativeHStack"/>. Items lay out
/// left-to-right with three sizing modes — <see cref="Fixed"/> locks
/// a width, <see cref="Auto"/> shrinks to content, <see cref="Relative"/>
/// shares the leftover width proportionally to its units. Cells admit
/// optional <see cref="HorizontalAlignment"/> (within the cell's own
/// width when the child is narrower) and
/// <see cref="VerticalAlignment"/> (within the row's band).
/// </summary>
public sealed class HStack : Element
{
    private readonly ImperativeHStack _impl = new();

    public HStack Fixed(double width, Element child,
        HorizontalAlignment? horizontalAlignment = null,
        VerticalAlignment? verticalAlignment = null)
    {
        _impl.Add(AxisSize.Fixed((float)width), child.Build(), horizontalAlignment, verticalAlignment);
        return this;
    }

    public HStack Auto(Element child,
        HorizontalAlignment? horizontalAlignment = null,
        VerticalAlignment? verticalAlignment = null)
    {
        _impl.Add(AxisSize.Auto(), child.Build(), horizontalAlignment, verticalAlignment);
        return this;
    }

    public HStack Relative(double units, Element child,
        HorizontalAlignment? horizontalAlignment = null,
        VerticalAlignment? verticalAlignment = null)
    {
        _impl.Add(AxisSize.Relative((float)units), child.Build(), horizontalAlignment, verticalAlignment);
        return this;
    }

    internal override ImperativeElement Build() => _impl;
}
