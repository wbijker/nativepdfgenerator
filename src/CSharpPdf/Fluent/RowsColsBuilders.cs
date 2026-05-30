using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>
/// Populates a <see cref="RowsElement"/>: <c>Auto()</c> / <c>Fixed(length)</c> /
/// <c>Relative(weight)</c> each add a slot and return a <see cref="FluentContainer"/>
/// so the slot's styling and content can be set inline.
/// </summary>
public sealed class RowsBuilder
{
    private readonly RowsElement _rows;
    internal RowsBuilder(RowsElement r) { _rows = r; }

    public FluentContainer Auto() => Add(Sizing.Auto, 0);
    public FluentContainer Fixed(double length) => Add(Sizing.Fixed, length);
    public FluentContainer Relative(double weight = 1) => Add(Sizing.Relative, weight);

    private FluentContainer Add(Sizing sizing, double length)
    {
        var slot = new SlotElement { Sizing = sizing, Length = sizing == Sizing.Auto ? 1 : length };
        _rows.Slots.Add(slot);
        return new FluentContainer(slot);
    }
}

/// <summary>
/// Populates a <see cref="ColsElement"/>: <c>Auto()</c> / <c>Fixed(length)</c> /
/// <c>Relative(weight)</c> each add a column slot and return a <see cref="FluentContainer"/>.
/// </summary>
public sealed class ColsBuilder
{
    private readonly ColsElement _cols;
    internal ColsBuilder(ColsElement c) { _cols = c; }

    public FluentContainer Auto() => Add(Sizing.Auto, 0);
    public FluentContainer Fixed(double length) => Add(Sizing.Fixed, length);
    public FluentContainer Relative(double weight = 1) => Add(Sizing.Relative, weight);

    private FluentContainer Add(Sizing sizing, double length)
    {
        var slot = new SlotElement { Sizing = sizing, Length = sizing == Sizing.Auto ? 1 : length };
        _cols.Slots.Add(slot);
        return new FluentContainer(slot);
    }
}

/// <summary>Adds children to a <see cref="LayersElement"/> in bottom-to-top z-order.</summary>
public sealed class LayersBuilder
{
    private readonly LayersElement _layers;
    internal LayersBuilder(LayersElement l) { _layers = l; }

    /// <summary>Add a layer; the returned container becomes the layer's element.</summary>
    public FluentContainer Layer()
    {
        var slot = new SlotElement();
        _layers.Children.Add(slot);
        return new FluentContainer(slot);
    }
}
