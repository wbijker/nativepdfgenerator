using CSharpPdf.Content;
using PdfSpec.Geometry;
namespace CSharpPdf.Layout;

/// <summary>
/// A grid with column widths shared across all rows (auto-sized from cell content
/// via min + preferred, then distributed to fill the available width). Supports an
/// optional header row that repeats on every page, per-cell borders, a header
/// background, and uniform cell padding. The table paginates between rows: when the
/// next row won't fit, the remaining rows continue on the next page under a fresh
/// header.
/// </summary>
public sealed class TableElement : Element
{
    /// <summary>Optional header row, repeated on every page.</summary>
    public Element[]? Header { get; set; }

    /// <summary>The data rows.</summary>
    public List<Element[]> Rows { get; } = new();

    public Color? CellBorderColor { get; set; }
    public double CellBorderThickness { get; set; }
    public Color? HeaderBackground { get; set; }
    public double CellPadding { get; set; } = 4;

    private double CellInset => CellPadding + CellBorderThickness;

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var inner = InnerAvailable(available);
        double[] columns = ComputeColumnWidths(inner.Width);
        double width = 0;
        foreach (double c in columns) width += c;

        double headerH = Header is not null ? RowHeight(Header, columns) : 0;
        // Minimum to start: header + first row (orphan-control — we don't want
        // a header alone on a page without at least one row).
        double minH = headerH;
        if (Rows.Count > 0) minH += RowHeight(Rows[0], columns);

        // Recommended (everything): header + every row.
        double recH = headerH;
        foreach (var row in Rows) recH += RowHeight(row, columns);

        return WithOwnInset(new SpaceDimension(
            new SizeRect(width, minH),
            new SizeRect(width, recH),
            verticalBreakable: true));
    }

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
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

    private double DrawRow(PdfCanvas context, Element[] cells, double[] columns, double startX, double top, bool isHeader)
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

    private double RowHeight(Element[] cells, double[] columns)
    {
        double inset = CellInset;
        double height = 0;
        for (int c = 0; c < cells.Length && c < columns.Length; c++)
        {
            double contentWidth = columns[c] - 2 * inset;
            double cellHeight = cells[c].SpaceHint(new SizeRect(contentWidth, null)).Recommended.Height ?? 0;
            height = System.Math.Max(height, cellHeight);
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

        void Accumulate(Element[] cells)
        {
            for (int c = 0; c < cells.Length; c++)
            {
                // Ask each cell at unconstrained width — its intrinsic Min/Recommended
                // are what drive Distribution.Across across the columns.
                var s = cells[c].SpaceHint(new SizeRect(double.MaxValue, null));
                min[c] = System.Math.Max(min[c], s.Minimal.Width + extra);
                pref[c] = System.Math.Max(pref[c], s.Recommended.Width + extra);
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
