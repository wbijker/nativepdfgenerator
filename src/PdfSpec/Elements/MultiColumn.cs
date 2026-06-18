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
/// Stateful, like <see cref="Paragraph"/>: items are consumed through a
/// <see cref="ContentIterator"/> that is threaded into the continuation,
/// so the next slot resumes exactly where the previous left off. When
/// an item itself returns a Partial, its continuation is pushed back
/// onto the iterator so the next column / page picks it up.
/// </para>
///
/// <para>
/// Each item is rendered at the column's allocated width and its own
/// natural height — or, for flexible items (no <see cref="PdfSizeHint.MaxHeight"/>),
/// at the column's remaining vertical space so wrapped content (e.g. a
/// long <see cref="Paragraph"/>) can claim a full column instead of a
/// single line.
/// <see cref="DefaultHorizontalAlignment"/> distributes horizontal slack
/// when an item is narrower than its column. Per-item alignment lives on
/// a wrapping element (e.g. <see cref="Element"/>) — Column's
/// items are plain <see cref="Element"/>s, the simplest shape that
/// matches the "list of items" requirement.
/// </para>
///
/// <para>
/// Inherits <see cref="Element"/> chrome — padding, background,
/// per-side borders, explicit <see cref="Element.Width"/> /
/// <see cref="Element.Height"/>. Chrome paints to the section's full
/// extent regardless of how much of the columns the items actually fill.
/// </para>
/// </summary>
public class MultiColumn : Element
{
    public readonly List<Element> Items = new();
    private ContentIterator<Element>? _iterator;

    public MultiColumn() { }

    private MultiColumn(ContentIterator<Element> iterator)
    {
        _iterator = iterator;
    }

    /// <summary>How many columns to flow items through. Defaults to 2.</summary>
    public int ColumnCount { get; set; } = 2;

    /// <summary>Horizontal gap between adjacent columns, in points.</summary>
    public double ColumnGap { get; set; } = 12;

    /// <summary>
    /// Horizontal alignment of an item within its column when the item's
    /// natural width is narrower than the column width. The slack is
    /// distributed as 0 / slack/2 / slack for Start / Center / End.
    /// </summary>
    public HorizontalAlignment DefaultHorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    public MultiColumn Add(Element item)
    {
        Items.Add(item);
        return this;
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        // Continuations have an empty Items list (their state lives on the
        // iterator), so fall back to flexible-with-zero in that case.
        if (Items.Count == 0) return PdfSizeHint.Flexible(available.Width, 0);
        var min = Items.Select(i => i.SizeHint(available).MinHeight).Max();
        return PdfSizeHint.Flexible(available.Width, min);
    }

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        int cols = Math.Max(1, ColumnCount);
        double colWidth = (available.Width - (cols - 1) * ColumnGap) / cols;
        if (colWidth <= 0) return RenderResult.Done(0);

        _iterator ??= new ContentIterator<Element>(Items);

        // Balanced column fill — divide the remaining items evenly across
        // columns by count, so a short section that comfortably fits in
        // one column doesn't leave the next column nearly empty (the old
        // greedy fill packed col 0 to the bottom and stranded col 1).
        // Items have heterogeneous heights so this is an approximation,
        // but for verse-style content where items are roughly uniform it
        // produces visually balanced columns. The per-item "won't fit"
        // check below still kicks in for oversized items, so overflow
        // pagination keeps working when content genuinely exceeds the
        // available area.
        int totalItems = _iterator.Remaining;
        int targetPerCol = totalItems <= 0 ? int.MaxValue : (int)Math.Ceiling(totalItems / (double)cols);
        int itemsInCurrentCol = 0;

        int currentCol = 0;
        double y = 0;
        double maxColY = 0;

        while (!_iterator.Done)
        {
            _iterator.TryPeek(out var item);

            // Measure the item's natural height at the column width. Flexible
            // items (no MaxHeight) get the column's remaining vertical space
            // so wrapped content can fill the column rather than being clipped
            // to a single-line MinHeight slot.
            double remaining = available.Height - y;
            var hint = item.SizeHint(new PdfSize(colWidth, remaining));
            double itemHeight = hint.MaxHeight ?? remaining;

            // Advance to the next column when (a) this column has its
            // share of items (balancing) or (b) even one line of the
            // next item wouldn't fit (overflow). We never wrap if the
            // current column is still empty (y == 0) — even an oversize
            // item gets drawn there, otherwise the loop would push it
            // through every column without progress.
            if (y > 0 && (itemsInCurrentCol >= targetPerCol || hint.MinHeight > remaining))
            {
                if (y > maxColY) maxColY = y;
                currentCol++;
                if (currentCol >= cols) break;
                y = 0;
                itemsInCurrentCol = 0;
                continue;
            }

            // Horizontal alignment within the column.
            double naturalW = Math.Min(colWidth, hint.MaxWidth ?? colWidth);
            double hSlack = Math.Max(0, colWidth - naturalW);
            double xOffset = DefaultHorizontalAlignment switch
            {
                HorizontalAlignment.Center => hSlack / 2,
                HorizontalAlignment.Right => hSlack,
                _ => 0,
            };

            double colX = currentCol * (colWidth + ColumnGap);
            var sub = cs.CreateSubStream(colX + xOffset, y, naturalW, itemHeight);
            var result = item.Render(sub, new PdfSize(naturalW, itemHeight));
            sub.Build();

            // Per-item Partial: stash the continuation back on the iterator
            // so the next column / page picks up exactly there. If the item
            // also reported NextY == 0 then it couldn't make any progress in
            // this slot — break to the next column to avoid spinning on the
            // same continuation forever.
            if (result.NextElement is not null)
            {
                _iterator.Putback(result.NextElement);
                if (result.NextY <= 0)
                {
                    if (y > maxColY) maxColY = y;
                    currentCol++;
                    if (currentCol >= cols) break;
                    y = 0;
                    itemsInCurrentCol = 0;
                    continue;
                }
            }
            else
            {
                _iterator.MoveNext();
            }

            // Trust the item's reported height — empty Done(0) items leave
            // y unchanged so the column keeps filling instead of being
            // wasted by a fallback to itemHeight (which equals the whole
            // remaining column for flexible items).
            y += result.NextY;
            itemsInCurrentCol++;
        }
        if (y > maxColY) maxColY = y;

        // When everything fit, the section settles at the tallest column's
        // height — not the full available — so the chrome wraps just the
        // content. On overflow we report the full available height plus
        // a continuation so the parent paginator can lay out the remainder.
        if (_iterator.Done) return RenderResult.Done(maxColY);

        var remainder = new MultiColumn(_iterator)
        {
            ColumnCount = ColumnCount,
            ColumnGap = ColumnGap,
            DefaultHorizontalAlignment = DefaultHorizontalAlignment,
        };
        CopyChromeTo(remainder);

        return new RenderResult(available.Height, remainder);
    }
}
