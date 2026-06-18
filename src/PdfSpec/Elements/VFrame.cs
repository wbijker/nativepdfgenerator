using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Vertical axis frame — the vertical mirror of <see cref="HStack"/>.
/// Items stack top-to-bottom and the frame always consumes the full
/// available height (the primary axis); <see cref="VFrameItem"/>
/// admits all three sizing modes — Fixed, Auto, Relative — and the
/// height-allocation algorithm matches <see cref="HStack"/>'s width
/// allocation.
///
/// <para>
/// Differences from <see cref="VStack"/>:
/// </para>
/// <list type="bullet">
/// <item><description><b>Not breakable.</b> Everything renders in one pass;
/// content that exceeds the frame overflows. Use <see cref="VStack"/>
/// when content needs to flow across pages.</description></item>
/// <item><description><b>Supports Relative slots.</b> Leftover height
/// after Fixed and Auto items is distributed proportionally by unit
/// across relative slots — the same shrink/expand modes
/// <see cref="HStack"/> applies on the width axis.</description></item>
/// <item><description><b>Always fills available height.</b> Even when
/// the items don't sum to <c>available.Height</c>, the frame's outer
/// height reaches <c>available.Height</c> — chrome paints to that
/// extent.</description></item>
/// </list>
///
/// Like the stacks, inherits <see cref="Element"/> chrome — padding,
/// background, borders, and explicit
/// <see cref="Element.Width"/> / <see cref="Element.Height"/>.
/// </summary>
public class VFrame : Element
{
    private readonly List<VFrameItem> _items = new();
    public IReadOnlyList<VFrameItem> Items => _items;

    /// <summary>Fallback horizontal alignment for any item whose <see cref="VFrameItem.HorizontalAlignment"/> is <c>null</c>.</summary>
    public HorizontalAlignment DefaultHorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    public VFrame Add(VFrameItem item) { _items.Add(item); return this; }

    public VFrame Fixed(double height, Element content, HorizontalAlignment? horizontalAlignment = null)
    {
        _items.Add(VFrameItem.Fixed(height, content, horizontalAlignment));
        return this;
    }

    public VFrame Auto(Element content, HorizontalAlignment? horizontalAlignment = null)
    {
        _items.Add(VFrameItem.Auto(content, horizontalAlignment));
        return this;
    }

    public VFrame Relative(double units, Element content, HorizontalAlignment? horizontalAlignment = null)
    {
        _items.Add(VFrameItem.Relative(units, content, horizontalAlignment));
        return this;
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        // VFrame fills its slot on both axes — it always claims the full
        // available height, and on the cross axis it advertises "I'll
        // take whatever you give me" (MaxWidth=null). Walking items here
        // would call SizeHint on each child once per page; for a top
        // slot wrapping a MultiColumn whose own SizeHint traverses every
        // paragraph, that's a hot path because parent
        // Element.DrawNaturalWidth fires every render. The frame's
        // own Draw is what truly measures items.
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
        // VFrame's outer height is always available.Height — even when
        // empty, Draw returns Done(available.Height) so the chrome
        // paints to the full extent.
        if (_items.Count == 0) return RenderResult.Done(available.Height);

        var (heights, _, _, _) = AllocateHeights(available);

        // Continuation array — null until the first overflow. Once any slot
        // returns a continuation, we seed it with the originals so every
        // non-overflowing slot also re-renders fresh on the next page (e.g.
        // a decorative band that should appear on every page produced by
        // overflow propagation).
        VFrameItem[]? continuationItems = null;

        double y = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            double slotHeight = heights[i];

            // Only measure natural width when there's potentially slack to
            // distribute. Left alignment leaves xOffset=0 across the full
            // available width, so we can skip the SizeHint call — and that
            // call is expensive when the item wraps a content-heavy element
            // (e.g. a MultiColumn whose own SizeHint walks every child) and
            // would fire once per item per page through every continuation.
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
            // VFrame slots are reserved space. A Element item with no
            // explicit Height would otherwise shrink to content (0 for a
            // chrome-only band), losing the slot fill the parent reserved.
            // Toggle the box's fill-slot flag transiently so its chrome
            // paints to the slot's full height.
            var box = item.Content as Element;
            bool prevFill = false;
            if (box is not null)
            {
                prevFill = box._fillSlotHeight;
                box._fillSlotHeight = true;
            }
            var result = item.Content.Render(sub, new PdfSize(naturalW, slotHeight));
            if (box is not null) box._fillSlotHeight = prevFill;
            sub.Build();

            // Overflow propagation. The frame is breakable: if any item
            // hands back a continuation, we capture it and roll a fresh
            // VFrame for the next page that keeps every original slot
            // (so decorative bands redraw) but swaps overflowed items for
            // their continuations.
            if (result.NextElement is not null)
            {
                continuationItems ??= _items.ToArray();
                continuationItems[i] = RebuildItem(item, result.NextElement);
            }

            y += slotHeight;
        }

        if (continuationItems is null) return RenderResult.Done(available.Height);

        var remainder = new VFrame
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
    /// Distribute <paramref name="available"/>.Height across slots. The
    /// algorithm mirrors <see cref="HStack"/>'s AllocateWidths: Fixed
    /// slots lock in their value, Auto slots desire their content's
    /// MaxHeight (or MinHeight if unknown), Relative slots split any
    /// remaining height proportionally. Shrink mode kicks in if the
    /// desired sum exceeds the available height.
    /// </summary>
    private (double[] Heights, double FixedSum, double AutoDesiredSum, double RelativeUnits) AllocateHeights(PdfSize available)
    {
        var heights = new double[_items.Count];
        double fixedSum = 0, relUnits = 0;
        double autoDesiredSum = 0;
        double largestRelPerUnit = 0;
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
                {
                    relUnits += item.Size.Value;
                    var hint = item.Content.SizeHint(new PdfSize(available.Width, available.Height));
                    if (item.Size.Value > 0)
                    {
                        double perUnit = hint.MinHeight / item.Size.Value;
                        if (perUnit > largestRelPerUnit) largestRelPerUnit = perUnit;
                    }
                    break;
                }
            }
        }

        double relativeBaseSum = largestRelPerUnit * relUnits;
        double totalDesired = fixedSum + autoDesiredSum + relativeBaseSum;

        if (totalDesired > available.Height)
        {
            // SHRINK MODE — autos overflowed. Pull height out of every auto
            // slot proportional to its share of total auto desire.
            double toShrink = totalDesired - available.Height;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Size.Type == AxisType.Auto)
                {
                    double share = autoDesiredSum > 0 ? autoDesired[i] / autoDesiredSum : 0;
                    heights[i] = autoDesired[i] - share * toShrink;
                }
                else if (_items[i].Size.Type == AxisType.Relative)
                {
                    heights[i] = largestRelPerUnit * _items[i].Size.Value;
                }
            }
        }
        else
        {
            // EXPAND MODE — autos lock at desired; relatives soak up the
            // leftover. With no relatives, any leftover space stays unused
            // (the frame is still its full height, just with empty space at
            // the bottom).
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Size.Type == AxisType.Auto)
                    heights[i] = autoDesired[i];
            }

            if (relUnits > 0)
            {
                double remainingForRelatives = available.Height - fixedSum - autoDesiredSum;
                double perUnit = remainingForRelatives / relUnits;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].Size.Type == AxisType.Relative)
                        heights[i] = perUnit * _items[i].Size.Value;
                }
            }
        }

        return (heights, fixedSum, autoDesiredSum, relUnits);
    }
}
