using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Multi-column flow container. Carries a <see cref="ColumnCount"/> (how
/// many columns to lay out side by side) and a list of items that flow
/// top-to-bottom within a column and left-to-right across columns —
/// the newspaper-style layout. The container always claims the full
/// available height; items that don't fit the last column become a
/// <see cref="RenderResult.Partial(Element)"/> continuation so the
/// caller can re-render the remainder on the next page.
///
/// <para>
/// Each item is rendered at the column's allocated width and its own
/// natural height. <see cref="DefaultHorizontalAlignment"/> distributes
/// horizontal slack when an item is narrower than its column. Per-item
/// alignment lives on a wrapping element (e.g. <see cref="BorderElement"/>)
/// — Column's items are plain <see cref="Element"/>s, the simplest
/// shape that matches the "list of items" requirement.
/// </para>
///
/// <para>
/// Inherits <see cref="BoxElement"/> chrome — padding, background,
/// per-side borders, explicit <see cref="BoxElement.Width"/> /
/// <see cref="BoxElement.Height"/>. Chrome paints to the section's full
/// extent regardless of how much of the columns the items actually fill.
/// </para>
/// </summary>
public class MultiColumn : BoxElement
{
    private readonly List<Element> _items = new();
    public IReadOnlyList<Element> Items => _items;

    /// <summary>How many columns to flow items through. Defaults to 2.</summary>
    public int ColumnCount { get; set; } = 2;

    /// <summary>Horizontal gap between adjacent columns, in points.</summary>
    public double ColumnGap { get; set; } = 12;

    /// <summary>
    /// Horizontal alignment of an item within its column when the item's
    /// natural width is narrower than the column width. The slack is
    /// distributed as 0 / slack/2 / slack for Start / Center / End.
    /// </summary>
    public Alignment DefaultHorizontalAlignment { get; set; } = Alignment.Start;

    public MultiColumn Add(Element item)
    {
        _items.Add(item);
        return this;
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        // Column always claims the full available height (the section's
        // primary axis) and width (chrome + the column band). The width
        // is content-independent — N columns × column-width + (N-1) gaps
        // — so SizeHint just relays the available extent.
        var explicitW = ResolveWidth(available.Width);
        var explicitH = ResolveHeight(available.Height);

        double w = explicitW ?? available.Width;
        double h = explicitH ?? available.Height;

        return new PdfSizeHint(w, h, w, h);
    }

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        int cols = Math.Max(1, ColumnCount);
        double colWidth = (available.Width - (cols - 1) * ColumnGap) / cols;
        if (colWidth <= 0) return RenderResult.Done(available.Height);

        int currentCol = 0;
        double y = 0;
        int firstUnrendered = -1;

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];

            // Measure the item's natural height at the column width. Auto
            // items with no MaxHeight fall back to MinHeight — same trade-off
            // as VStack: a wrapped paragraph without explicit line counting
            // can under-estimate, which may let it start in a column it
            // can't fully clear.
            var hint = item.SizeHint(new PdfSize(colWidth, available.Height));
            double itemHeight = hint.MaxHeight ?? hint.MinHeight;

            // Doesn't fit in the current column? Move to the next. We never
            // wrap if the current column is still empty (y == 0) — even an
            // oversize item gets drawn there, otherwise the loop would push
            // it through every column without progress.
            if (y > 0 && y + itemHeight > available.Height)
            {
                currentCol++;
                if (currentCol >= cols)
                {
                    firstUnrendered = i;
                    break;
                }
                y = 0;
            }

            // Horizontal alignment within the column.
            var widthHint = item.SizeHint(new PdfSize(colWidth, itemHeight));
            double naturalW = Math.Min(colWidth, widthHint.MaxWidth ?? colWidth);
            double hSlack = Math.Max(0, colWidth - naturalW);
            double xOffset = DefaultHorizontalAlignment switch
            {
                Alignment.Center => hSlack / 2,
                Alignment.End => hSlack,
                _ => 0,
            };

            double colX = currentCol * (colWidth + ColumnGap);
            var sub = cs.CreateSubStream(colX + xOffset, y, naturalW, itemHeight);
            var result = item.Render(sub, new PdfSize(naturalW, itemHeight));
            sub.Build();

            y += result.NextY > 0 ? result.NextY : itemHeight;
        }

        // Always advertise the section's full height — Column is a
        // fixed-height section in the layout, not a shrink-to-content
        // wrapper, so chrome paints to the full extent.
        if (firstUnrendered < 0) return RenderResult.Done(available.Height);

        var remainder = new MultiColumn
        {
            ColumnCount = ColumnCount,
            ColumnGap = ColumnGap,
            DefaultHorizontalAlignment = DefaultHorizontalAlignment,
        };
        CopyChromeTo(remainder);
        for (int j = firstUnrendered; j < _items.Count; j++)
            remainder._items.Add(_items[j]);

        return new RenderResult(available.Height, remainder);
    }

    private void CopyChromeTo(BoxElement other)
    {
        other.Width = Width;
        other.Height = Height;
        other.PaddingTop = PaddingTop;
        other.PaddingRight = PaddingRight;
        other.PaddingBottom = PaddingBottom;
        other.PaddingLeft = PaddingLeft;
        other.Background = Background;
        other.BorderTopWidth = BorderTopWidth;
        other.BorderRightWidth = BorderRightWidth;
        other.BorderBottomWidth = BorderBottomWidth;
        other.BorderLeftWidth = BorderLeftWidth;
        other.BorderTopColor = BorderTopColor;
        other.BorderRightColor = BorderRightColor;
        other.BorderBottomColor = BorderBottomColor;
        other.BorderLeftColor = BorderLeftColor;
        other.HorizontalAlignment = HorizontalAlignment;
        other.VerticalAlignment = VerticalAlignment;
    }
}
