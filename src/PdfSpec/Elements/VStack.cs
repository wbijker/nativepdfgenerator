using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Vertical axis container — items stack top-to-bottom, each item gets
/// the full column width and either an explicit height
/// (<see cref="VStackItem.Fixed"/>) or its natural rendered height
/// (<see cref="VStackItem.Auto"/>). Relative heights are not in scope —
/// <see cref="VStackItem"/> only exposes Fixed and Auto factories, so
/// the API can't express a Relative slot at all.
///
/// <para>
/// Breakable by default. When an item won't fit in the remaining
/// available height, <see cref="Draw"/> stops and returns a
/// <see cref="RenderResult.Partial(Element)"/> whose continuation is a
/// fresh <see cref="VStack"/> carrying the unrendered tail (plus any
/// remainder from an item that broke mid-render). The caller — a page
/// renderer or other paginating container — keeps invoking that
/// continuation on each next page until everything has rendered.
/// </para>
///
/// <para>
/// Like <see cref="Rows"/>, the column inherits <see cref="BoxElement"/>
/// chrome: padding, background, per-side borders, explicit
/// <see cref="BoxElement.Width"/> / <see cref="BoxElement.Height"/>,
/// horizontal / vertical alignment of the stack inside the chrome.
/// Per-item horizontal alignment lives on <see cref="VStackItem"/> (with
/// <see cref="DefaultHorizontalAlignment"/> as fallback) and positions
/// the item within its slot's width. Per-item vertical alignment is
/// not applicable — each item has its own vertical slot, and any
/// content-vs-slot positioning belongs on the wrapping element (e.g.
/// <see cref="BorderElement"/> with an explicit
/// <see cref="BoxElement.Height"/>).
/// </para>
/// </summary>
public class VStack : BoxElement
{
    private readonly List<VStackItem> _items = new();
    public IReadOnlyList<VStackItem> Items => _items;

    /// <summary>
    /// Fallback horizontal alignment for any item whose
    /// <see cref="VStackItem.HorizontalAlignment"/> is <c>null</c> — applies
    /// when the item's natural width is narrower than the column width.
    /// </summary>
    public Alignment DefaultHorizontalAlignment { get; set; } = Alignment.Start;

    /// <summary>Append a pre-built <see cref="VStackItem"/>.</summary>
    public VStack Add(VStackItem item)
    {
        _items.Add(item);
        return this;
    }

    /// <summary>Append a <see cref="AxisSize.Fixed"/> item of <paramref name="height"/> points.</summary>
    public VStack AddFixed(double height, Element content, Alignment? horizontalAlignment = null)
    {
        _items.Add(VStackItem.Fixed(height, content, horizontalAlignment));
        return this;
    }

    /// <summary>Append an <see cref="AxisSize.Auto"/> item — the slot takes whatever height the content renders into.</summary>
    public VStack AddAuto(Element content, Alignment? horizontalAlignment = null)
    {
        _items.Add(VStackItem.Auto(content, horizontalAlignment));
        return this;
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        // Same explicit Width/Height short-circuit as Rows: a parent
        // sees the requested extent immediately, no item walk needed.
        var explicitW = ResolveWidth(available.Width);
        var explicitH = ResolveHeight(available.Height);

        double chromeW = HorizontalChrome;
        double chromeH = VerticalChrome;

        if (_items.Count == 0)
        {
            return new PdfSizeHint(
                explicitW ?? chromeW,
                explicitH ?? chromeH,
                explicitW,
                explicitH);
        }

        var inner = new PdfSize(
            Math.Max(0, (explicitW ?? available.Width) - chromeW),
            Math.Max(0, (explicitH ?? available.Height) - chromeH));

        double minWidth = 0;
        double? maxWidth = 0;
        double sumHeight = 0;
        bool anyAutoUnknown = false;

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var hint = item.Content.SizeHint(new PdfSize(inner.Width, inner.Height));

            minWidth = Math.Max(minWidth, hint.MinWidth);
            maxWidth = maxWidth is null || hint.MaxWidth is null
                ? null
                : Math.Max(maxWidth.Value, hint.MaxWidth.Value);

            if (item.Size.Type == AxisType.Fixed)
            {
                sumHeight += item.Size.Value;
            }
            else // Auto
            {
                if (hint.MaxHeight is double mh) sumHeight += mh;
                else { sumHeight += hint.MinHeight; anyAutoUnknown = true; }
            }
        }

        return new PdfSizeHint(
            explicitW ?? minWidth + chromeW,
            explicitH ?? sumHeight + chromeH,
            explicitW ?? (maxWidth is null ? null : maxWidth.Value + chromeW),
            explicitH ?? (anyAutoUnknown ? null : sumHeight + chromeH));
    }

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        if (_items.Count == 0) return RenderResult.Done(0);

        double y = 0;
        int firstUnrendered = -1;
        Element? itemRemainder = null;

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            double remainingH = Math.Max(0, available.Height - y);

            // Slot height — Fixed locks in its value; Auto uses the item's
            // SizeHint MaxHeight (fall back to MinHeight when the child can't
            // report a max, e.g. paragraphs without explicit wrap counts).
            double slotHeight;
            if (item.Size.Type == AxisType.Fixed)
            {
                slotHeight = item.Size.Value;
            }
            else
            {
                var hint = item.Content.SizeHint(new PdfSize(available.Width, remainingH));
                slotHeight = hint.MaxHeight ?? hint.MinHeight;
            }

            // Pre-check fit. We never defer the first item — even if it's
            // taller than available, render it and accept the overflow,
            // otherwise the column would loop forever on the next page.
            //
            // Compare slot against remainingH directly (rather than
            // re-summing y + slot vs available.Height) to avoid the
            // floating-point slop that arises when an Auto item
            // advertises exactly the remaining space — e.g. a child
            // that fills its allocation can return MaxHeight = remainingH,
            // and y + remainingH then overshoots available.Height by an
            // FP ulp, which would defer the item incorrectly.
            if (i > 0 && slotHeight > remainingH)
            {
                firstUnrendered = i;
                break;
            }

            // Horizontal alignment within the column width: query the item's
            // natural max width and shift the sub-stream by the slack.
            var hAlign = item.HorizontalAlignment ?? DefaultHorizontalAlignment;
            var widthHint = item.Content.SizeHint(new PdfSize(available.Width, slotHeight));
            double naturalW = Math.Min(available.Width, widthHint.MaxWidth ?? available.Width);
            double hSlack = Math.Max(0, available.Width - naturalW);
            double xOffset = hAlign switch
            {
                Alignment.Center => hSlack / 2,
                Alignment.End => hSlack,
                _ => 0,
            };

            var sub = cs.CreateSubStream(xOffset, y, naturalW, slotHeight);
            var result = item.Content.Render(sub, new PdfSize(naturalW, slotHeight));
            sub.Build();

            // Slot advance: Fixed honours the explicit height (so the next
            // item starts after the full slot, leaving any unused space at
            // the bottom of the slot); Auto follows the actual rendered
            // height.
            y += item.Size.Type == AxisType.Fixed ? slotHeight : result.NextY;

            // Item itself returned a Partial — its remainder leads the
            // continuation on the next page, followed by the items that
            // haven't started.
            if (result.NextElement is not null)
            {
                itemRemainder = result.NextElement;
                firstUnrendered = i + 1;
                break;
            }
        }

        if (firstUnrendered < 0 && itemRemainder is null)
            return RenderResult.Done(y);

        // Build the continuation VStack for the next page. Chrome and
        // alignment defaults are copied so the next page mirrors the
        // styling of this one.
        var remainder = new VStack
        {
            DefaultHorizontalAlignment = DefaultHorizontalAlignment,
        };
        CopyChromeTo(remainder);

        if (itemRemainder is not null)
            remainder._items.Add(VStackItem.Auto(itemRemainder));
        if (firstUnrendered >= 0)
        {
            for (int j = firstUnrendered; j < _items.Count; j++)
                remainder._items.Add(_items[j]);
        }

        // Report the height actually rendered on this page (so chrome
        // sizes to it) plus the continuation for the next page. The
        // RenderResult.Partial factory hardcodes NextY = 0, which would
        // make the wrapping BoxElement shrink to chrome-only; we use the
        // constructor directly to keep the y we actually consumed.
        return new RenderResult(y, remainder);
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
