using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>Populates a <see cref="TableElement"/>.</summary>
public sealed class TableBuilder
{
    private readonly TableElement _table;
    internal TableBuilder(TableElement t) { _table = t; }

    public TableBuilder CellBorder(Color color, double width = 0.5)
    {
        _table.CellBorderColor = color;
        _table.CellBorderThickness = width;
        return this;
    }
    public TableBuilder HeaderBackground(Color color) { _table.HeaderBackground = color; return this; }
    public TableBuilder CellPadding(double padding) { _table.CellPadding = padding; return this; }

    public TableBuilder Header(System.Action<CellsBuilder> build)
    {
        var cells = new System.Collections.Generic.List<UIElement>();
        build(new CellsBuilder(cells));
        _table.Header = cells.ToArray();
        return this;
    }

    public TableBuilder Row(System.Action<CellsBuilder> build)
    {
        var cells = new System.Collections.Generic.List<UIElement>();
        build(new CellsBuilder(cells));
        _table.Rows.Add(cells.ToArray());
        return this;
    }
}

/// <summary>Each <c>Cell()</c> call appends one cell to the current row.</summary>
public sealed class CellsBuilder
{
    private readonly System.Collections.Generic.List<UIElement> _cells;
    internal CellsBuilder(System.Collections.Generic.List<UIElement> cells) { _cells = cells; }

    public FluentContainer Cell()
    {
        var slot = new SlotElement();
        _cells.Add(slot);
        return new FluentContainer(slot);
    }
}
