namespace CSharpPdf.Layout;

/// <summary>
/// A grid with column widths shared across all rows (auto-sized from cell content
/// via min + preferred, then distributed to fill the available width). Supports an
/// optional header row that repeats on every page, per-cell borders, a header
/// background, and uniform cell padding. The table paginates between rows: when the
/// next row won't fit, the remaining rows continue on the next page under a fresh
/// header.
/// </summary>
public sealed class TableElement : UIElement<TableElement>
{
    private readonly List<UIElement[]> _rows = new();
    private UIElement[]? _header;
    private Color? _cellBorderColor;
    private double _cellBorderWidth;
    private Color? _headerBackground;
    private double _cellPadding = 4;

    public TableElement Header(params UIElement[] cells) { _header = cells; return this; }
    public TableElement Row(params UIElement[] cells) { _rows.Add(cells); return this; }
    public TableElement CellBorder(Color color, double width = 0.5) { _cellBorderColor = color; _cellBorderWidth = width; return this; }
    public TableElement HeaderBackground(Color color) { _headerBackground = color; return this; }
    public TableElement CellPadding(double padding) { _cellPadding = padding; return this; }

    private double CellInset => _cellPadding + _cellBorderWidth;

    public override Size PreferredSize => MeasureCore(new Size(double.MaxValue, double.MaxValue));

    public override Size MinimalSpaceRequired
    {
        get
        {
            var (min, _) = ColumnMinMax();
            double width = 0;
            foreach (double m in min)
            {
                width += m;
            }
            return new Size(width, MinRenderHeight(new Size(width, double.MaxValue)));
        }
    }

    internal override double MinRenderHeight(Size available)
    {
        double[] columns = ComputeColumnWidths(available.Width);
        double height = _header is not null ? RowHeight(_header, columns) : 0;
        if (_rows.Count > 0)
        {
            height += RowHeight(_rows[0], columns);
        }
        return height;
    }

    protected override Size MeasureCore(Size available)
    {
        double[] columns = ComputeColumnWidths(available.Width);
        double width = 0;
        foreach (double c in columns)
        {
            width += c;
        }
        double height = _header is not null ? RowHeight(_header, columns) : 0;
        foreach (var row in _rows)
        {
            height += RowHeight(row, columns);
        }
        return new Size(width, height);
    }

    protected override RenderResult RenderCore(PdfContext context, Size available)
    {
        double[] columns = ComputeColumnWidths(available.Width);
        Point start = context.Cursor;
        double y = start.Y;
        double bottom = start.Y - available.Height;

        if (_header is not null)
        {
            y -= DrawRow(context, _header, columns, start.X, y, isHeader: true);
        }

        int i = 0;
        for (; i < _rows.Count; i++)
        {
            double rowHeight = RowHeight(_rows[i], columns);
            if (y - rowHeight < bottom - 0.01)
            {
                break; // this row doesn't fit; continue on the next page
            }
            y -= DrawRow(context, _rows[i], columns, start.X, y, isHeader: false);
        }

        if (i < _rows.Count)
        {
            var overflow = new TableElement
            {
                _header = _header,
                _cellBorderColor = _cellBorderColor,
                _cellBorderWidth = _cellBorderWidth,
                _headerBackground = _headerBackground,
                _cellPadding = _cellPadding,
            };
            for (int j = i; j < _rows.Count; j++)
            {
                overflow._rows.Add(_rows[j]);
            }
            return new RenderResult(overflow, new Point(start.X, y));
        }
        return new RenderResult(null, new Point(start.X, y));
    }

    private double DrawRow(PdfContext context, UIElement[] cells, double[] columns, double startX, double top, bool isHeader)
    {
        double rowHeight = RowHeight(cells, columns);
        double inset = CellInset;
        double x = startX;
        for (int c = 0; c < columns.Length; c++)
        {
            double cellWidth = columns[c];
            if (isHeader && _headerBackground is { } hb)
            {
                context.FillRectangle(x, top, cellWidth, rowHeight, hb);
            }
            if (_cellBorderColor is { } bc && _cellBorderWidth > 0)
            {
                context.StrokeRectangle(x, top, cellWidth, rowHeight, bc, _cellBorderWidth);
            }
            if (c < cells.Length)
            {
                context.Cursor = new Point(x + inset, top - inset);
                cells[c].Render(context, new Size(cellWidth - 2 * inset, rowHeight - 2 * inset));
            }
            x += cellWidth;
        }
        return rowHeight;
    }

    private double RowHeight(UIElement[] cells, double[] columns)
    {
        double inset = CellInset;
        double height = 0;
        for (int c = 0; c < cells.Length && c < columns.Length; c++)
        {
            double contentWidth = columns[c] - 2 * inset;
            height = System.Math.Max(height, cells[c].Measure(new Size(contentWidth, double.MaxValue)).Height);
        }
        return height + 2 * inset;
    }

    private double[] ComputeColumnWidths(double available)
    {
        var (min, pref) = ColumnMinMax();
        return Distribution.Across(min, pref, available);
    }

    private (double[] Min, double[] Preferred) ColumnMinMax()
    {
        int columns = ColumnCount();
        var min = new double[columns];
        var pref = new double[columns];
        double extra = 2 * CellInset;

        void Accumulate(UIElement[] cells)
        {
            for (int c = 0; c < cells.Length; c++)
            {
                min[c] = System.Math.Max(min[c], cells[c].MinimalSpaceRequired.Width + extra);
                pref[c] = System.Math.Max(pref[c], cells[c].PreferredSize.Width + extra);
            }
        }

        if (_header is not null)
        {
            Accumulate(_header);
        }
        foreach (var row in _rows)
        {
            Accumulate(row);
        }
        return (min, pref);
    }

    private int ColumnCount()
    {
        int count = _header?.Length ?? 0;
        foreach (var row in _rows)
        {
            count = System.Math.Max(count, row.Length);
        }
        return count;
    }
}
