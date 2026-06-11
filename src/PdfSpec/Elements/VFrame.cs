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
/// Like the stacks, inherits <see cref="BoxElement"/> chrome — padding,
/// background, borders, and explicit
/// <see cref="BoxElement.Width"/> / <see cref="BoxElement.Height"/>.
/// </summary>
public partial class VFrame : BoxElement
{
    private readonly List<VFrameItem> _items = new();
    public IReadOnlyList<VFrameItem> Items => _items;

    /// <summary>Fallback horizontal alignment for any item whose <see cref="VFrameItem.HorizontalAlignment"/> is <c>null</c>.</summary>
    public Alignment DefaultHorizontalAlignment { get; set; } = Alignment.Start;

    public VFrame Add(VFrameItem item) { _items.Add(item); return this; }

    public VFrame AddFixed(double height, Element content, Alignment? horizontalAlignment = null)
    {
        _items.Add(VFrameItem.Fixed(height, content, horizontalAlignment));
        return this;
    }

    public VFrame AddAuto(Element content, Alignment? horizontalAlignment = null)
    {
        _items.Add(VFrameItem.Auto(content, horizontalAlignment));
        return this;
    }

    public VFrame AddRelative(double units, Element content, Alignment? horizontalAlignment = null)
    {
        _items.Add(VFrameItem.Relative(units, content, horizontalAlignment));
        return this;
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        var explicitW = ResolveWidth(available.Width);
        var explicitH = ResolveHeight(available.Height);

        double chromeW = HorizontalChrome;
        double chromeH = VerticalChrome;

        // VFrame always fills the primary axis (height), so MaxHeight
        // reports the available height (or the explicit value). Width
        // is content-driven on the cross axis, like VStack.
        double advertisedH = explicitH ?? available.Height;

        if (_items.Count == 0)
        {
            return new PdfSizeHint(
                explicitW ?? chromeW,
                advertisedH,
                explicitW,
                advertisedH);
        }

        var inner = new PdfSize(
            Math.Max(0, (explicitW ?? available.Width) - chromeW),
            Math.Max(0, advertisedH - chromeH));

        double minWidth = 0;
        double? maxWidth = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var hint = item.Content.SizeHint(new PdfSize(inner.Width, inner.Height));
            minWidth = Math.Max(minWidth, hint.MinWidth);
            maxWidth = maxWidth is null || hint.MaxWidth is null
                ? null
                : Math.Max(maxWidth.Value, hint.MaxWidth.Value);
        }

        return new PdfSizeHint(
            explicitW ?? minWidth + chromeW,
            advertisedH,
            explicitW ?? (maxWidth is null ? null : maxWidth.Value + chromeW),
            advertisedH);
    }

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        // VFrame's outer height is always available.Height — even when
        // empty, Draw returns Done(available.Height) so the chrome
        // paints to the full extent.
        if (_items.Count == 0) return RenderResult.Done(available.Height);

        var (heights, _, _, _) = AllocateHeights(available);

        double y = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            double slotHeight = heights[i];

            // Horizontal slack within the column width — same as VStack.
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
            item.Content.Render(sub, new PdfSize(naturalW, slotHeight));
            sub.Build();

            y += slotHeight;
        }

        // Always return the full available height so the chrome paints
        // to it, even if items didn't sum to that much (no relatives,
        // autos shorter than expected, etc.).
        return RenderResult.Done(available.Height);
    }

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
