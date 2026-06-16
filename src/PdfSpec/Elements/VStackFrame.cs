using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Breakable vertical frame — the same Fixed/Auto/Relative slot allocation
/// as <see cref="VFrame"/>, but when any item's render returns a
/// continuation the frame propagates the overflow up the tree. The next
/// page rebuilds a fresh <see cref="VStackFrame"/> with the original
/// slot list, with each overflowed item swapped for its continuation —
/// so non-overflowing decorative bands (footer-style coloured strips,
/// page chrome that should appear on every page) redraw cleanly on each
/// page while the overflowing item picks up where it left off.
///
/// <para>Always fills available height (like VFrame). Items that fit on
/// the first page still re-render on each continuation — by design, this
/// is the "frame redraws on every page" contract.</para>
/// </summary>
public class VStackFrame : BoxElement
{
    private readonly List<VFrameItem> _items = new();
    public IReadOnlyList<VFrameItem> Items => _items;

    public HorizontalAlignment DefaultHorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    public VStackFrame Add(VFrameItem item) { _items.Add(item); return this; }

    public VStackFrame Fixed(double height, Element content, HorizontalAlignment? horizontalAlignment = null)
    {
        _items.Add(VFrameItem.Fixed(height, content, horizontalAlignment));
        return this;
    }

    public VStackFrame Auto(Element content, HorizontalAlignment? horizontalAlignment = null)
    {
        _items.Add(VFrameItem.Auto(content, horizontalAlignment));
        return this;
    }

    public VStackFrame Relative(double units, Element content, HorizontalAlignment? horizontalAlignment = null)
    {
        _items.Add(VFrameItem.Relative(units, content, horizontalAlignment));
        return this;
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        // VStackFrame fills its slot on both axes — it always takes the full
        // available height, and on the cross axis it advertises "I'll take
        // whatever you give me" (MaxWidth=null). Walking items here would
        // call SizeHint on each child once per page; for a MultiColumn top
        // slot whose own SizeHint traverses every paragraph, that's a hot
        // path for parent BoxElement.DrawNaturalWidth which fires every
        // render. The frame's own Draw is what truly measures items.
        var explicitW = ResolveWidth(available.Width);
        var explicitH = ResolveHeight(available.Height);

        double chromeW = HorizontalChrome;
        double advertisedH = explicitH ?? available.Height;

        return new PdfSizeHint(
            explicitW ?? chromeW,
            advertisedH,
            explicitW,
            advertisedH);
    }

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        if (_items.Count == 0) return RenderResult.Done(available.Height);

        var heights = AllocateHeights(available);

        // Continuation array — null until the first overflow. Once any slot
        // returns a continuation, we seed it with the originals so every
        // non-overflowing slot also re-renders fresh on the next page.
        VFrameItem[]? continuationItems = null;

        double y = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            double slotHeight = heights[i];

            // Only measure natural width when there's potentially slack to
            // distribute. Left alignment leaves the item at xOffset=0 across
            // the full available width, so SizeHint isn't needed — and that
            // call is expensive when the item is a MultiColumn (walks every
            // verse to compute MinHeight) and would fire once per item per
            // page across every overflow continuation.
            var hAlign = item.HorizontalAlignment ?? DefaultHorizontalAlignment;
            double naturalW;
            double xOffset;
            if (hAlign == HorizontalAlignment.Left)
            {
                naturalW = available.Width;
                xOffset = 0;
            }
            else
            {
                var widthHint = item.Content.SizeHint(new PdfSize(available.Width, slotHeight));
                naturalW = Math.Min(available.Width, widthHint.MaxWidth ?? available.Width);
                double hSlack = Math.Max(0, available.Width - naturalW);
                xOffset = hAlign == HorizontalAlignment.Center ? hSlack / 2 : hSlack;
            }

            var sub = cs.CreateSubStream(xOffset, y, naturalW, slotHeight);
            var result = item.Content.Render(sub, new PdfSize(naturalW, slotHeight));
            sub.Build();

            if (result.NextElement is not null)
            {
                continuationItems ??= _items.ToArray();
                continuationItems[i] = RebuildItem(item, result.NextElement);
            }

            y += slotHeight;
        }

        if (continuationItems is null) return RenderResult.Done(available.Height);

        var remainder = new VStackFrame
        {
            DefaultHorizontalAlignment = DefaultHorizontalAlignment,
        };
        CopyChromeTo(remainder);
        foreach (var item in continuationItems) remainder._items.Add(item);

        return new RenderResult(available.Height, remainder);
    }

    /// <summary>
    /// Build a fresh <see cref="VFrameItem"/> with the same sizing mode and
    /// alignment as <paramref name="original"/> but wrapping
    /// <paramref name="newContent"/> — used when an item's render returned
    /// a continuation that should fill the same slot on the next page.
    /// </summary>
    private static VFrameItem RebuildItem(VFrameItem original, Element newContent) =>
        original.Size.Type switch
        {
            AxisType.Fixed => VFrameItem.Fixed(original.Size.Value, newContent, original.HorizontalAlignment),
            AxisType.Auto => VFrameItem.Auto(newContent, original.HorizontalAlignment),
            AxisType.Relative => VFrameItem.Relative(original.Size.Value, newContent, original.HorizontalAlignment),
            _ => VFrameItem.Auto(newContent, original.HorizontalAlignment),
        };

    /// <summary>
    /// Allocate slot heights. Fixed slots lock in their value; Auto slots
    /// take their content's desired max; Relative slots split whatever is
    /// left after Fixed + Auto, proportional to their units. Unlike
    /// <see cref="VFrame"/>, Relative items are NOT measured for a
    /// minimum — Relative is treated as "flexible to whatever's left",
    /// so a child that declares a 100%-of-available height (legitimate
    /// for items that want their chrome to fill their slot) doesn't
    /// inflate the allocation total and collapse the layout. Shrink
    /// mode kicks in only when Fixed + Auto exceeds the available height,
    /// and pulls only from Autos.
    /// </summary>
    private double[] AllocateHeights(PdfSize available)
    {
        var heights = new double[_items.Count];
        double fixedSum = 0, relUnits = 0;
        double autoDesiredSum = 0;
        var autoDesired = new double[_items.Count];

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            switch (item.Size.Type)
            {
                case AxisType.Fixed:
                    heights[i] = item.Size.Value;
                    fixedSum += item.Size.Value;
                    break;

                case AxisType.Auto:
                {
                    var hint = item.Content.SizeHint(new PdfSize(available.Width, available.Height));
                    double desired = hint.MaxHeight ?? hint.MinHeight;
                    autoDesired[i] = desired;
                    autoDesiredSum += desired;
                    break;
                }

                case AxisType.Relative:
                    relUnits += item.Size.Value;
                    break;
            }
        }

        double rigid = fixedSum + autoDesiredSum;

        if (rigid > available.Height)
        {
            // SHRINK — autos overflowed. Pull height out of every auto
            // slot proportional to its share of total auto desire.
            // Relatives get 0 (no room).
            double toShrink = rigid - available.Height;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Size.Type == AxisType.Auto)
                {
                    double share = autoDesiredSum > 0 ? autoDesired[i] / autoDesiredSum : 0;
                    heights[i] = autoDesired[i] - share * toShrink;
                }
            }
        }
        else
        {
            // EXPAND — autos lock at desired; relatives soak up the leftover.
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Size.Type == AxisType.Auto)
                    heights[i] = autoDesired[i];
            }

            if (relUnits > 0)
            {
                double perUnit = (available.Height - rigid) / relUnits;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].Size.Type == AxisType.Relative)
                        heights[i] = perUnit * _items[i].Size.Value;
                }
            }
        }

        return heights;
    }
}
