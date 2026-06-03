using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>
/// Stack items top-to-bottom. Each item is a <see cref="Container"/>;
/// pick its sizing through <see cref="Item"/> (auto — sized by content),
/// <see cref="ConstantItem(double)"/> (fixed height in points), or
/// <see cref="RelativeItem(double)"/> (share of leftover height by weight).
/// </summary>
public sealed class Column
{
    private readonly RowsElement _rows;
    internal Column(RowsElement rows) { _rows = rows; }

    /// <summary>Add a content-sized item (height grows to fit).</summary>
    public Container Item() => Add(Sizing.Auto, 1);

    /// <summary>Add an item with a fixed height in points.</summary>
    public Container ConstantItem(double height) => Add(Sizing.Fixed, height);

    /// <summary>Add an item with a fixed height in <paramref name="unit"/>.</summary>
    public Container ConstantItem(double height, Unit unit) => Add(Sizing.Fixed, Units.ToPoints(height, unit));

    /// <summary>Add an item that shares the leftover height by weight.</summary>
    public Container RelativeItem(double weight = 1) => Add(Sizing.Relative, weight);

    /// <summary>Alias for <see cref="Item"/> — kept for symmetry with <see cref="Row.AutoItem"/>.</summary>
    public Container AutoItem() => Add(Sizing.Auto, 1);

    /// <summary>Subscribe to the underlying RowsElement's <see cref="Element.OnRendered"/> hook (fires once per page the column lands on).</summary>
    public Column OnRendered(System.Action<RenderedInfo> handler) { _rows.OnRendered = handler; return this; }

    /// <summary>Shortcut: add a content-sized item that wraps a raw <see cref="Element"/>. Equivalent to <c>Item().Element(element)</c>.</summary>
    public Container Element(Element element) => Item().Element(element);

    /// <summary>Shortcut: add a content-sized item composed from an <see cref="IComponent"/>. Equivalent to <c>Item().Component(component)</c>.</summary>
    public Container Component(IComponent component) => Item().Component(component);

    private Container Add(Sizing sizing, double length)
    {
        // Column items are atomic by default — a single item never splits
        // across a page boundary. If it doesn't fit, the whole item moves to
        // the next page so its background/border stay attached to its
        // content. (The Row equivalent doesn't need this — Cols are atomic
        // as a row anyway.)
        var slot = new SlotElement { Sizing = sizing, Length = length, Atomic = true };
        _rows.Slots.Add(slot);
        return new Container(slot);
    }
}

/// <summary>
/// Lay items left-to-right. Same sizing choices as <see cref="Column"/>:
/// auto (content width), constant (fixed), or relative (weighted share).
/// </summary>
public sealed class Row
{
    private readonly ColsElement _cols;
    internal Row(ColsElement cols) { _cols = cols; }

    /// <summary>Add an item whose width fits its content.</summary>
    public Container AutoItem() => Add(Sizing.Auto, 1);

    /// <summary>Add an item with a fixed width in points.</summary>
    public Container ConstantItem(double width) => Add(Sizing.Fixed, width);

    /// <summary>Add an item with a fixed width in <paramref name="unit"/>.</summary>
    public Container ConstantItem(double width, Unit unit) => Add(Sizing.Fixed, Units.ToPoints(width, unit));

    /// <summary>Add an item that shares the leftover width by weight.</summary>
    public Container RelativeItem(double weight = 1) => Add(Sizing.Relative, weight);

    /// <summary>Subscribe to the underlying ColsElement's <see cref="Element.OnRendered"/> hook.</summary>
    public Row OnRendered(System.Action<RenderedInfo> handler) { _cols.OnRendered = handler; return this; }

    /// <summary>Shortcut: add an auto-sized item wrapping a raw <see cref="Element"/>. Equivalent to <c>AutoItem().Element(element)</c>.</summary>
    public Container Element(Element element) => AutoItem().Element(element);

    /// <summary>Shortcut: add an auto-sized item composed from an <see cref="IComponent"/>. Equivalent to <c>AutoItem().Component(component)</c>.</summary>
    public Container Component(IComponent component) => AutoItem().Component(component);

    private Container Add(Sizing sizing, double length)
    {
        var slot = new SlotElement { Sizing = sizing, Length = length };
        _cols.Slots.Add(slot);
        return new Container(slot);
    }
}

/// <summary>Add overlays in bottom-to-top z-order. Each <see cref="Layer"/> call returns a fresh container.</summary>
public sealed class Layers
{
    private readonly LayersElement _layers;
    internal Layers(LayersElement layers) { _layers = layers; }

    public Container Layer()
    {
        var slot = new SlotElement();
        _layers.Children.Add(slot);
        return new Container(slot);
    }

    /// <summary>Subscribe to the underlying LayersElement's <see cref="Element.OnRendered"/> hook.</summary>
    public Layers OnRendered(System.Action<RenderedInfo> handler) { _layers.OnRendered = handler; return this; }
}

/// <summary>
/// Populate a <see cref="TableElement"/>. Header row and body rows are
/// built through <see cref="Header"/> and <see cref="Row(System.Action{Cells})"/>;
/// each cell is a <see cref="Container"/>.
/// </summary>
public sealed class Table
{
    private readonly TableElement _table;
    internal Table(TableElement t) { _table = t; }

    public Table CellBorder(Color color, double width = 0.5)
    {
        _table.CellBorderColor = color;
        _table.CellBorderThickness = width;
        return this;
    }

    public Table CellBorder(Color color, double width, Unit unit)
    {
        _table.CellBorderColor = color;
        _table.CellBorderThickness = Units.ToPoints(width, unit);
        return this;
    }

    public Table HeaderBackground(Color color) { _table.HeaderBackground = color; return this; }
    public Table CellPadding(double padding) { _table.CellPadding = padding; return this; }
    public Table CellPadding(double padding, Unit unit) { _table.CellPadding = Units.ToPoints(padding, unit); return this; }

    /// <summary>Subscribe to the underlying TableElement's <see cref="Element.OnRendered"/> hook (fires once per page the table lands on, with the table's actual rendered box — narrower than the enclosing slot when columns are content-sized).</summary>
    public Table OnRendered(System.Action<RenderedInfo> handler) { _table.OnRendered = handler; return this; }

    /// <summary>Define the header row (repeats on every continuation page).</summary>
    public Table Header(System.Action<Cells> build)
    {
        var cells = new System.Collections.Generic.List<Element>();
        build(new Cells(cells));
        _table.Header = cells.ToArray();
        return this;
    }

    /// <summary>Append a body row.</summary>
    public Table Row(System.Action<Cells> build)
    {
        var cells = new System.Collections.Generic.List<Element>();
        build(new Cells(cells));
        _table.Rows.Add(cells.ToArray());
        return this;
    }
}

/// <summary>Each <c>Cell()</c> call appends one cell to the current row/header.</summary>
public sealed class Cells
{
    private readonly System.Collections.Generic.List<Element> _cells;
    internal Cells(System.Collections.Generic.List<Element> cells) { _cells = cells; }

    public Container Cell()
    {
        var slot = new SlotElement();
        _cells.Add(slot);
        return new Container(slot);
    }
}

/// <summary>Configure a <see cref="TransformElement"/>: rotation, scaling, pivot, plus the wrapped child.</summary>
public sealed class Transform
{
    private readonly TransformElement _t;
    internal Transform(TransformElement t) { _t = t; }

    /// <summary>Rotation in degrees, counter-clockwise around the pivot.</summary>
    public Transform Rotate(double degrees) { _t.Rotate = degrees; return this; }

    /// <summary>Uniform scale factor.</summary>
    public Transform Scale(double s) { _t.ScaleX = s; _t.ScaleY = s; return this; }

    /// <summary>Independent x / y scale.</summary>
    public Transform Scale(double sx, double sy) { _t.ScaleX = sx; _t.ScaleY = sy; return this; }

    /// <summary>Pivot point as fractions of the wrapped child's box (0..1, defaults to centre via the underlying element).</summary>
    public Transform Pivot(double fractionX, double fractionY) { _t.PivotX = fractionX; _t.PivotY = fractionY; return this; }

    /// <summary>Subscribe to the underlying TransformElement's <see cref="Element.OnRendered"/> hook.</summary>
    public Transform OnRendered(System.Action<RenderedInfo> handler) { _t.OnRendered = handler; return this; }

    /// <summary>The child to transform.</summary>
    public void Content(System.Action<Container> build)
    {
        var inner = new Container();
        build(inner);
        _t.Content = inner.Slot.Content;
    }
}
