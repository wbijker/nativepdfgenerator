namespace CSharpPdf.Layout;

/// <summary>
/// A grid with column widths shared across all rows (auto-sized from cell content
/// via min + preferred, then distributed to fill the available width). Supports an
/// optional header row that repeats on every page, per-cell borders, a header
/// background, and uniform cell padding. The table paginates between rows: when the
/// next row won't fit, the remaining rows continue on the next page under a fresh
/// header.
/// </summary>
public sealed class TableElement : UIElement
{
    /// <summary>Optional header row, repeated on every page.</summary>
    public UIElement[]? Header { get; set; }

    /// <summary>The data rows.</summary>
    public List<UIElement[]> Rows { get; } = new();

    public Color? CellBorderColor { get; set; }
    public double CellBorderThickness { get; set; }
    public Color? HeaderBackground { get; set; }
    public double CellPadding { get; set; } = 4;

    private double CellInset => CellPadding + CellBorderThickness;

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
        double height = Header is not null ? RowHeight(Header, columns) : 0;
        if (Rows.Count > 0)
        {
            height += RowHeight(Rows[0], columns);
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
        double height = Header is not null ? RowHeight(Header, columns) : 0;
        foreach (var row in Rows)
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

        if (Header is not null)
        {
            y -= DrawRow(context, Header, columns, start.X, y, isHeader: true);
        }

        int i = 0;
        for (; i < Rows.Count; i++)
        {
            double rowHeight = RowHeight(Rows[i], columns);
            if (y - rowHeight < bottom - 0.01)
            {
                break;
            }
            y -= DrawRow(context, Rows[i], columns, start.X, y, isHeader: false);
        }

        if (i < Rows.Count)
        {
            var overflow = new TableElement
            {
                Header = Header,
                CellBorderColor = CellBorderColor,
                CellBorderThickness = CellBorderThickness,
                HeaderBackground = HeaderBackground,
                CellPadding = CellPadding,
            };
            for (int j = i; j < Rows.Count; j++)
            {
                overflow.Rows.Add(Rows[j]);
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
            if (isHeader && HeaderBackground is { } hb)
            {
                context.FillRectangle(x, top, cellWidth, rowHeight, hb);
            }
            if (CellBorderColor is { } bc && CellBorderThickness > 0)
            {
                context.StrokeRectangle(x, top, cellWidth, rowHeight, bc, CellBorderThickness);
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

        if (Header is not null)
        {
            Accumulate(Header);
        }
        foreach (var row in Rows)
        {
            Accumulate(row);
        }
        return (min, pref);
    }

    private int ColumnCount()
    {
        int count = Header?.Length ?? 0;
        foreach (var row in Rows)
        {
            count = System.Math.Max(count, row.Length);
        }
        return count;
    }
}
